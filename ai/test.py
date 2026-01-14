import os
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"

import numpy as np
import cv2
import pandas as pd
from tqdm import tqdm
import tensorflow as tf
from tensorflow.keras.utils import CustomObjectScope
from sklearn.metrics import f1_score, jaccard_score, precision_score, recall_score, confusion_matrix
import yaml
import sys

from train import load_dataset
from metrics import dice_coef_multi, combined_loss, pixel_precision, per_class_precision
from DicePerClass import DicePerClassMetric
""" ---------------- GLOBAL PARAMETERS ---------------- """
H = 256
W = 256
SEED = 42
CONFIG_FILE_PATH = sys.argv[1]

# ---------------- MASK COLORS ----------------
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
def checkIfFolderExists(folderPath):
    if not os.path.isdir(folderPath):
        os.makedirs(folderPath)
        print(f"The folder '{folderPath}' has been created.")

def create_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def write_results_to_file(score, file_path):
    try:
        with open(file_path, 'w') as file:
            file.write(f"F1: {score[0]:0.5f}\n")
            file.write(f"Jaccard: {score[1]:0.5f}\n")
            file.write(f"Recall: {score[2]:0.5f}\n")
            file.write(f"Precision: {score[3]:0.5f}\n")
        print(f"Content successfully written to {file_path}")
    except Exception as e:
        print(f"An error occurred: {e}")

# ---------------- MASK HANDLING ----------------
def read_mask(path):
    """Read a color mask and convert to one-hot and class indices"""
    mask = cv2.imread(path, cv2.IMREAD_COLOR)
    mask = cv2.resize(mask, (W, H), interpolation=cv2.INTER_NEAREST)
    
    # Robust matching using Euclidean distance
    diff = mask[:, :, np.newaxis, :] - np.array(CLASS_COLORS)[np.newaxis, np.newaxis, :, :]
    dist = np.sum(np.square(diff), axis=-1)
    class_mask = np.argmin(dist, axis=-1).astype(np.uint8)

    one_hot = np.eye(NUM_CLASSES)[class_mask]
    return one_hot.astype(np.float32), class_mask, mask

def colorize_mask(mask_class_indices):
    """Colorize the mask indices for visualization"""
    mask_class_indices = mask_class_indices.astype(np.uint8)
    color_mask = np.zeros((H, W, 3), dtype=np.uint8)
    for idx, color in enumerate(CLASS_COLORS):
        color_mask[mask_class_indices == idx] = color
    return color_mask

def save_results(image, mask_image, pred_class, save_image_path):
    """Save a two-panel image: left = GT mask, right = prediction mask"""
    left = cv2.addWeighted(image, 0.5, mask_image, 0.9, 0)
    right = colorize_mask(pred_class)
    separator = np.ones((H, 10, 3), dtype=np.uint8) * 255
    combined = np.concatenate([left, separator, right], axis=1)
    cv2.imwrite(save_image_path, combined)

# ---------------- CONFIG & SEEDS ----------------
config_params = None
with open(CONFIG_FILE_PATH, 'r') as f:
    try:
        config_params = yaml.safe_load(f)
    except yaml.YAMLError as exc:
        print(f"Error reading YAML file: {exc}")
        raise Exception("Could not read the config file!")

np.random.seed(SEED)
tf.random.set_seed(SEED)

# ---------------- LOAD MODEL ----------------
create_dir("results")
model_name = config_params["name"]
model_file_path = os.path.join("results", model_name, f"{model_name}.h5")
print(f"Using model: {model_file_path}")

with CustomObjectScope({
    "combined_loss": combined_loss,
    "dice_coef_multi": dice_coef_multi,
    "pixel_precision": pixel_precision,
    "per_class_precision": per_class_precision,
}):
    model = tf.keras.models.load_model(model_file_path)
    

print("Model loaded!")

# ---------------- LOAD DATASET ----------------
dataset_path = "./post_dataset_taco"
(train_x, train_y), (valid_x, valid_y), (test_x, test_y) = load_dataset(dataset_path)

#test_x = test_x + train_x + valid_x
#test_y = test_y + train_y + valid_y


# ---------------- PREDICTION & METRICS ----------------
SCORE = []
save_dir = os.path.join("results", model_name, "images")
checkIfFolderExists(save_dir)
total_cm = np.zeros((NUM_CLASSES, NUM_CLASSES))

for x_path, y_path in tqdm(zip(test_x, test_y), total=len(test_y)):
    name = os.path.basename(x_path)

    # Read image
    image = cv2.imread(x_path, cv2.IMREAD_COLOR)
    image_resized = cv2.resize(image, (W, H))
    x_input = np.expand_dims(image_resized / 255.0, axis=0)

    # Read mask
    mask_onehot, mask_class, mask_image = read_mask(y_path)

    # Predict
    y_pred = model.predict(x_input, verbose=0)[0]  # [H, W, num_classes]

    pred_class = np.argmax(y_pred, axis=-1)

    unique_gt = np.unique(mask_class)
    unique_pred = np.unique(pred_class)
    tqdm.write(f"Image: {name} | GT Classes: {unique_gt} | Pred Classes: {unique_pred}")

    # Save overlay results
    save_image_path = os.path.join(save_dir, name)
    save_results(image_resized, mask_image, pred_class, save_image_path)

    # Flatten for metrics
    mask_flat = mask_class.flatten()
    pred_flat = pred_class.flatten()
    valid_labels = np.unique(mask_flat)  # compute metrics only for present classes

    f1_value = f1_score(mask_flat, pred_flat, average='macro', labels=valid_labels, zero_division=0)
    jac_value = jaccard_score(mask_flat, pred_flat, average='macro', labels=valid_labels, zero_division=0)
    recall_value = recall_score(mask_flat, pred_flat, average='macro', labels=valid_labels, zero_division=0)
    precision_value = precision_score(mask_flat, pred_flat, average='macro', labels=valid_labels, zero_division=0)

    # Update Confusion Matrix
    total_cm += confusion_matrix(mask_flat, pred_flat, labels=range(NUM_CLASSES))

    SCORE.append([name, f1_value, jac_value, recall_value, precision_value])

# ---------------- SAVE METRICS ----------------
score_mean = np.mean([s[1:] for s in SCORE], axis=0)
write_results_to_file(score_mean, os.path.join("results", model_name, "final_score.txt"))

df = pd.DataFrame(SCORE, columns=["Image", "F1", "Jaccard", "Recall", "Precision"])
df.to_csv(os.path.join("results", model_name, "testing_score.csv"), index=False)

print("\n--- Confusion Matrix (Rows: GT, Cols: Pred) ---")
print(total_cm.astype(int))

# Calculate accuracy per class from CM
class_acc = np.diag(total_cm) / (total_cm.sum(axis=1) + 1e-7)
for idx, acc in enumerate(class_acc):
    print(f"Class {idx} Accuracy: {acc:.4f}")

print("Testing completed!")
