import os
import sys
import shutil
from glob import glob

# ---------------- ENVIRONMENT ----------------
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"  # Reduce TensorFlow logs
os.environ["XLA_FLAGS"] = "--xla_gpu_strict_conv_algorithm_picker=false"

# ---------------- GPU SETUP ----------------
import tensorflow as tf

gpus = tf.config.list_physical_devices('GPU')
if gpus:
    try:
        for gpu in gpus:
            tf.config.experimental.set_memory_growth(gpu, True)
        print(f"Dynamic memory growth enabled for {len(gpus)} GPU(s).")
    except RuntimeError as e:
        print(f"GPU configuration error: {e}")
else:
    print("No GPU detected, using CPU.")

# ---------------- OTHER IMPORTS ----------------
from tensorflow.keras.callbacks import ModelCheckpoint, CSVLogger, ReduceLROnPlateau, EarlyStopping
from tensorflow.keras.optimizers import Adam, SGD
from sklearn.model_selection import train_test_split
import numpy as np
import cv2
import yaml

from ce_bias import CompoundLoss
from image_proccessing import init_transform, apply_transform
from unet import build_unet
from metrics import dice_loss, dice_coef

# ---------------- GLOBAL PARAMETERS ----------------
H = 256
W = 256
SEED = 42
CONFIG_FILE_PATH = sys.argv[1]

# ---------------- UTILITY FUNCTIONS ----------------
def create_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def read_config(file_path):
    with open(file_path, 'r') as file:
        try:
            return yaml.safe_load(file)
        except yaml.YAMLError as exc:
            print(f"Error reading YAML file: {exc}")
            return None

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
    if isinstance(path, tf.Tensor):
        path = path.numpy()  # convert to bytes
    if isinstance(path, bytes):
        path = path.decode()  # convert bytes -> str
    x = cv2.imread(path, cv2.IMREAD_COLOR)
    x = cv2.resize(x, (W, H))
    x = np.float32(x) / 255.0
    return x

def read_mask(path):
    if isinstance(path, tf.Tensor):
        path = path.numpy()
    if isinstance(path, bytes):
        path = path.decode()
    x = cv2.imread(path, cv2.IMREAD_GRAYSCALE)
    x = cv2.resize(x, (W, H))
    x = np.float32(x) / 255.0
    x = np.expand_dims(x, axis=-1)
    return x

def tf_parse(X, Y, use_augmentation=True):
    """ TF Dataset parsing function """
    def _parse(x, y):
        img = read_image(x)
        mask = read_mask(y)
        if use_augmentation:
            transformed = apply_transform(img, mask)
            return transformed['image'], transformed['mask']
        else:
            return img, mask

    x, y = tf.numpy_function(_parse, [X, Y], [tf.float32, tf.float32])
    x.set_shape([H, W, 3])
    y.set_shape([H, W, 1])
    return x, y

def tf_dataset(X, Y, batch=2, use_augmentation=True):
    dataset = tf.data.Dataset.from_tensor_slices((X, Y))
    dataset = dataset.map(lambda x, y: tf_parse(x, y, use_augmentation))
    dataset = dataset.batch(batch)
    dataset = dataset.prefetch(tf.data.AUTOTUNE)
    return dataset

# ---------------- MAIN ----------------
if __name__ == "__main__":
    np.random.seed(SEED)
    tf.random.set_seed(SEED)

    # Load config
    config_params = read_config(CONFIG_FILE_PATH)
    if config_params is None:
        raise Exception("Could not read the config file!")

    init_transform(config_params)

    # Directory for results
    model_name = config_params["name"]
    create_dir(f"results/{model_name}")
    model_path = os.path.join("results", model_name, f"{model_name}.h5")  # H5 format
    csv_path = os.path.join("results", model_name, "training_log.csv")
    print(f"Model will be saved to: {model_path}")

    # Dataset
    dataset_path = "./post_dataset"
    (train_x, train_y), (valid_x, valid_y), (test_x, test_y) = load_dataset(dataset_path)
    print(f"Train: {len(train_x)}, Valid: {len(valid_x)}, Test: {len(test_x)}")

    # TF Datasets
    batch_size = int(config_params.get("batch_size", 2))
    train_dataset = tf_dataset(train_x, train_y, batch=batch_size, use_augmentation=False)
    valid_dataset = tf_dataset(valid_x, valid_y, batch=batch_size, use_augmentation=False)

    # Debug: check a batch
    for x_batch, y_batch in train_dataset.take(1):
        print("x_batch min/max:", x_batch.numpy().min(), x_batch.numpy().max())
        print("y_batch min/max:", y_batch.numpy().min(), y_batch.numpy().max())

    # Model
    model = build_unet((H, W, 3), config_params)
    optimizer = config_params.get("optimizer", "Adam")
    loss_type = config_params.get("loss", "DiceLoss")
    lr = float(config_params.get("lr", 1e-4))

    # Compile model
    if loss_type == "CompoundLoss":
        loss_fn = CompoundLoss(CompoundLoss.BINARY_MODE)
        if optimizer == "Adam":
            model.compile(loss=loss_fn, optimizer=Adam(learning_rate=lr), metrics=['accuracy'])
        elif optimizer == "SGD":
            model.compile(loss=loss_fn, optimizer=SGD(learning_rate=lr), metrics=['accuracy'])
        else:
            raise Exception("Unknown optimizer")
    elif loss_type == "DiceLoss":
        if optimizer == "Adam":
            model.compile(loss=dice_loss, optimizer=Adam(learning_rate=lr), metrics=[dice_coef])
        elif optimizer == "SGD":
            model.compile(loss=dice_loss, optimizer=SGD(learning_rate=lr), metrics=[dice_coef])
        else:
            raise Exception("Unknown optimizer")
    else:
        raise Exception("Unknown loss type")

    print(f"Model uses {loss_type} with {optimizer} optimizer.")

    # Callbacks
    callbacks = [
        ModelCheckpoint(model_path, verbose=1, save_best_only=True),
        ReduceLROnPlateau(monitor='val_loss', factor=0.1, patience=5, min_lr=1e-7, verbose=1),
        CSVLogger(csv_path),
        EarlyStopping(monitor='val_loss', patience=20, restore_best_weights=False),
    ]

    # Training
    model.fit(
        train_dataset,
        epochs=int(config_params.get("num_epochs", 100)),
        validation_data=valid_dataset,
        callbacks=callbacks
    )

    # Save config
    shutil.copyfile(CONFIG_FILE_PATH, os.path.join("results", model_name, "config.yaml"))
