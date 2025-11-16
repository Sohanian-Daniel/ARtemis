
import os
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "0"
os.environ["XLA_FLAGS"] = "--xla_gpu_strict_conv_algorithm_picker=false"
#os.environ['CUDA_VISIBLE_DEVICES'] = '-1'

import tensorflow as tf
gpus = tf.config.experimental.list_physical_devices('GPU')
tf.config.experimental.set_virtual_device_configuration(
          gpus[0], [tf.config.experimental.VirtualDeviceConfiguration(memory_limit=1024)])
print("ok")
from tensorflow.keras.callbacks import ModelCheckpoint, CSVLogger, ReduceLROnPlateau, EarlyStopping
from tensorflow.keras.optimizers import Adam, SGD

from sklearn.model_selection import train_test_split
import numpy as np
import cv2
import yaml
from glob import glob

import shutil
import sys

from ce_bias import CompoundLoss
from image_proccessing import init_transform, apply_transform
from unet import build_unet
from metrics import dice_loss, dice_coef

""" Global parameters """
H = 256
W = 256
SEED = 42
CONFIG_FILE_PATH = sys.argv[1]

def create_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def load_dataset(path, split=0.2):
    images = sorted(glob(os.path.join(path, "images", "*.jpg")))
    masks = sorted(glob(os.path.join(path, "masks", "*.jpg")))
    split_size = int(len(images) * split)

    train_x, valid_x = train_test_split(images, test_size=split_size, random_state=SEED)
    train_y, valid_y = train_test_split(masks, test_size=split_size, random_state=SEED)

    train_x, test_x = train_test_split(train_x, test_size=split_size, random_state=SEED)
    train_y, test_y = train_test_split(train_y, test_size=split_size, random_state=SEED)

    return (train_x, train_y), (valid_x, valid_y), (test_x, test_y)

def read_image(path):
    path = path.decode()
    x = cv2.imread(path, cv2.IMREAD_COLOR)
    x = cv2.resize(x, (W, H))
    x = np.float32(x)
    x = x / 255.0
    return x

def read_mask(path):
    path = path.decode()
    x = cv2.imread(path, cv2.IMREAD_GRAYSCALE)  ## (h, w)
    x = cv2.resize(x, (W, H))     ## (h, w)
    x = np.float32(x)
    x = x / 255.0
    x = np.expand_dims(x, axis=-1)## (h, w, 1)
    return x

def read_config(file_path):
    with open(file_path, 'r') as file:
        try:
            return yaml.safe_load(file)
        except yaml.YAMLError as exc:
            print(f"Error reading YAML file: {exc}")
            return None

def tf_parse(x, y):
    def _parse(x, y):
        x = read_image(x)
        y = read_mask(y)
        transformed = apply_transform(x, y)
        return transformed['image'], transformed['mask']

    x, y = tf.numpy_function(_parse, [x, y], [tf.float32, tf.float32])
    x.set_shape([H, W, 3])
    y.set_shape([H, W, 1])
    return x, y

def tf_dataset(X, Y, batch=2):
    dataset = tf.data.Dataset.from_tensor_slices((X, Y))
    dataset = dataset.map(tf_parse)
    dataset = dataset.batch(batch)
    dataset = dataset.prefetch(10)
    return dataset

if __name__ == "__main__":
    config_params = read_config(CONFIG_FILE_PATH)
    if config_params == None:
        raise Exception("Could not read the config file!")
    print("Num GPUs Available: ", len(tf.config.list_physical_devices('GPU')))
    np.random.seed(SEED)
    tf.random.set_seed(SEED)
    init_transform(config_params)
    """ Directory for storing files """
    gpus = tf.config.experimental.list_physical_devices('GPU')
    if gpus:
        try:
            tf.config.experimental.set_virtual_device_configuration(gpus[0], [tf.config.experimental.VirtualDeviceConfiguration(memory_limit=1024)])
        except RuntimeError as e:
            print(e)
    """ Hyperparameters """
    batch_size = int(config_params["batch_size"])
    lr = float(config_params["lr"])
    num_epochs = int(config_params["num_epochs"])
    model_name = config_params["name"]
    create_dir(f"results/{model_name}")
    model_path = os.path.join("results", model_name, f"{model_name}.keras")
    csv_path = os.path.join("results", model_name, "training_log.csv")

    print(f"BATCH_SIZE: {batch_size}; LR:{lr}; NUM_EPOCHS:{num_epochs}; MODEL_PATH:{model_path}; CSV_PATH:{csv_path}")

    """ Dataset """
    dataset_path = "./post_dataset"
    (train_x, train_y), (valid_x, valid_y), (test_x, test_y) = load_dataset(dataset_path)

    print(f"Train: {len(train_x)} - {len(train_y)}")
    print(f"Valid: {len(valid_x)} - {len(valid_y)}")
    print(f"Test : {len(test_x)} - {len(test_y)}")

    train_dataset = tf_dataset(train_x, train_y, batch=batch_size)
    valid_dataset = tf_dataset(valid_x, valid_y, batch=batch_size)
    """ Model """
    model = build_unet((H, W, 3), config_params)
    optimizer = config_params["optimizer"]
    loss_type = config_params["loss"]
    if loss_type == "CompoundLoss":
        loss_fn = CompoundLoss(CompoundLoss.BINARY_MODE)
        if config_params["optimizer"] == "Adam":
            model.compile(loss=loss_fn, optimizer=Adam(lr), metrics=['accuracy'])
        elif config_params["optimizer"] == "SGD":
            model.compile(loss=loss_fn, optimizer=SGD(lr), metrics=['accuracy'])
        else:
            raise Exception("No optimizer was found!")
    elif loss_type == "DiceLoss":
        if config_params["optimizer"] == "Adam":
            model.compile(loss=dice_loss, optimizer=Adam(lr), metrics=[dice_coef])
        elif config_params["optimizer"] == "SGD":
            model.compile(loss=dice_loss, optimizer=SGD(lr), metrics=[dice_coef])
        else:
            raise Exception("No optimizer was found!")
    else:
        raise Exception("No loss mode was found!")
    print(f"Model uses {loss_type} type!")
    print(f"Model uses {optimizer} optimizer!")

    callbacks = [
        ModelCheckpoint(model_path, verbose=1, save_best_only=True),
        ReduceLROnPlateau(monitor='val_loss', factor=0.1, patience=5, min_lr=1e-7, verbose=1),
        CSVLogger(csv_path),
        EarlyStopping(monitor='val_loss', patience=20, restore_best_weights=False),
    ]

    model.fit(
        train_dataset,
        epochs=num_epochs,
        validation_data=valid_dataset,
        callbacks=callbacks
    )
    
    shutil.copyfile(CONFIG_FILE_PATH, os.path.join("results", config_params["name"], "config.yaml"))
    
