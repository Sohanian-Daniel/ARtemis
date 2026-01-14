import os
import sys
import shutil
from glob import glob
# docker run --name my_tensorflow_1 -it --gpus all -v D:\Universitate_Politehnica\ARtemis-main:/workspace tensorflow/tensorflow:2.14.0-gpu bash
# ---------------- ENVIRONMENT ----------------
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"
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
from tqdm import tqdm
import yaml

from image_proccessing import init_transform, apply_transform
from unet import build_unet
from metrics import dice_coef_multi, combined_loss, pixel_precision, per_class_precision
from DicePerClass import DicePerClassMetric

# ---------------- GLOBAL PARAMETERS ----------------
H = 256
W = 256
SEED = 42
CONFIG_FILE_PATH = sys.argv[1]

# ---------------- DEFINE MASK COLORS ----------------
# Map each category index to a color (BGR) for normal data set
# CLASS_COLORS = [
#     (0, 0, 0),      # category 0 -> background (black)
#     (0, 255, 0),    # category 1 -> green
#     (0, 0, 255),    # category 3 -> red (BGR)
#     (255, 255, 0),  # category 3 -> cyan
# ]
# Corrected CLASS_COLORS based on preprocess_taco.py
CLASS_COLORS = [
    (0, 0, 0),       # category 0 -> background (black)
    (255, 0, 0),     # category 1 -> plastic (blue)
    (0, 255, 0),     # category 2 -> paper (green)
    (42, 42, 165),   # category 3 -> bio (brown)
    (128, 128, 128), # category 4 -> metal (gray)
    (0, 0, 255),     # category 5 -> other (red)
]

NUM_CLASSES = len(CLASS_COLORS)

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
    """Load images and masks and split into train/valid/test"""
    images = sorted(glob(os.path.join(path, "images", "*.png")))
    masks = sorted(glob(os.path.join(path, "masks", "*.png")))
    # images = images[0:50]
    # masks = masks[0:50]
    if len(images) == 0 or len(masks) == 0:
        raise ValueError(f"No images or masks found in {path}")
    if len(images) != len(masks):
        raise ValueError("Number of images and masks do not match!")
    
    split = min(max(split, 0.0), 0.5)  # safety
    # Train/Valid split
    train_x, valid_x, train_y, valid_y = train_test_split(
        images, masks, test_size=split, random_state=SEED
    )
    # Train/Test split
    train_x, test_x, train_y, test_y = train_test_split(
        train_x, train_y, test_size=split, random_state=SEED
    )

    return (train_x, train_y), (valid_x, valid_y), (test_x, test_y)

def read_image(path):
    """Read image and normalize"""
    if isinstance(path, tf.Tensor):
        path = path.numpy()
    if isinstance(path, bytes):
        path = path.decode()
    x = cv2.imread(path, cv2.IMREAD_COLOR)
    x = cv2.resize(x, (W, H))
    x = np.float32(x) / 255.0
    return x

def read_mask(path):
    """Read color mask and convert to multi-class one-hot"""
    if isinstance(path, tf.Tensor):
        path = path.numpy()
    if isinstance(path, bytes):
        path = path.decode()
    mask = cv2.imread(path, cv2.IMREAD_COLOR)
    mask = cv2.resize(mask, (W, H), interpolation=cv2.INTER_NEAREST)

    # Robust matching using Euclidean distance to find nearest class color
    diff = mask[:, :, np.newaxis, :] - np.array(CLASS_COLORS)[np.newaxis, np.newaxis, :, :]
    dist = np.sum(np.square(diff), axis=-1)
    class_mask = np.argmin(dist, axis=-1).astype(np.uint8)

    return np.expand_dims(class_mask, axis=-1)

def oversample_dataset(images, masks):
    """Oversample images containing rare classes to balance the dataset"""
    print("Scanning dataset for oversampling (this might take a moment)...")
    aug_images = []
    aug_masks = []
    
    for x, y in tqdm(zip(images, masks), total=len(images)):
        aug_images.append(x)
        aug_masks.append(y)
        
        # Read mask to check for rare classes
        # We use the robust read_mask function to ensure we catch them
        mask = read_mask(y) # returns (H, W, 1) with class indices
        present_classes = np.unique(mask)
        
        # Class 3 (Bio) is extremely rare -> Replicate 20x
        if 3 in present_classes:
            for _ in range(20):
                aug_images.append(x)
                aug_masks.append(y)
        # Class 4 (Metal) and 5 (Other) -> Replicate 5x
        elif 4 in present_classes or 5 in present_classes:
            for _ in range(5):
                aug_images.append(x)
                aug_masks.append(y)
                
    print(f"Oversampling complete. Original: {len(images)} -> Augmented: {len(aug_images)}")
    return aug_images, aug_masks

def tf_parse(X, Y, use_augmentation=True):
    """TF Dataset parsing function for multi-class masks"""
    def _parse(x, y):
        img = read_image(x)
        mask = read_mask(y)
        if use_augmentation:
            transformed = apply_transform(img, mask)
            img = transformed['image']
            mask = transformed['mask']
        
        # Convert to one-hot after augmentation to preserve integrity
        mask_ind = mask[:, :, 0]
        one_hot = np.eye(NUM_CLASSES)[mask_ind]
        return img, np.float32(one_hot)

    x, y = tf.numpy_function(_parse, [X, Y], [tf.float32, tf.float32])
    x.set_shape([H, W, 3])
    y.set_shape([H, W, NUM_CLASSES])
    return x, y

def tf_dataset(X, Y, batch=2, use_augmentation=True):
    dataset = tf.data.Dataset.from_tensor_slices((X, Y))
    dataset = dataset.shuffle(buffer_size=5000)
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
    model_path = os.path.join("results", model_name, f"{model_name}.h5")
    csv_path = os.path.join("results", model_name, "training_log.csv")
    print(f"Model will be saved to: {model_path}")

    # Load dataset
    dataset_path = "./post_dataset_taco"
    (train_x, train_y), (valid_x, valid_y), (test_x, test_y) = load_dataset(dataset_path)
    print(f"Train: {len(train_x)}, Valid: {len(valid_x)}, Test: {len(test_x)}")

    # Apply oversampling to training data
    train_x, train_y = oversample_dataset(train_x, train_y)

    # TF datasets
    batch_size = int(config_params.get("batch_size", 2))
    train_dataset = tf_dataset(train_x, train_y, batch=batch_size, use_augmentation=True)
    valid_dataset = tf_dataset(valid_x, valid_y, batch=batch_size, use_augmentation=False)

    # Build model
    config_params["num_classes"] = NUM_CLASSES
    model = build_unet((H, W, 3), config_params)
    optimizer = config_params.get("optimizer", "Adam")
    loss_type = config_params.get("loss", "DiceMultiLoss")
    lr = float(config_params.get("lr", 1e-4))

    if optimizer == "Adam":
        model.compile(
            loss=combined_loss,
            optimizer=Adam(lr),
            metrics=[dice_coef_multi, pixel_precision, per_class_precision]
        )
    elif optimizer == "SGD":
        model.compile(
            loss=combined_loss,
            optimizer=SGD(lr),
            metrics=[dice_coef_multi, pixel_precision, per_class_precision]
        )
    else:
        raise Exception("Unknown loss type")

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
