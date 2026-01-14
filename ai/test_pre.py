import os
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"

import numpy as np
import cv2
import pandas as pd
from tqdm import tqdm
import tensorflow as tf
from tensorflow.keras.utils import CustomObjectScope
from sklearn.metrics import f1_score, jaccard_score, precision_score, recall_score
from metrics import dice_loss, dice_coef
from train import load_dataset
import yaml
import sys

from ce_bias import CompoundLoss

""" Global parameters """
H = 256
W = 256
SEED = 42
CONFIG_FILE_PATH = sys.argv[1]

def checkIfFolderExists(folderPath):
    if not os.path.isdir(folderPath):
        os.makedirs(folderPath)
        print(f"The folder '{folderPath}' has been created.")

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

def read_config(file_path):
    with open(file_path, 'r') as file:
        try:
            return yaml.safe_load(file)
        except yaml.YAMLError as exc:
            print(f"Error reading YAML file: {exc}")
            return None

""" Creating a directory """
def create_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def save_results(image, mask, y_pred, save_image_path):
    mask = np.expand_dims(mask, axis=-1)
    mask = np.concatenate([mask, mask, mask], axis=-1)

    y_pred = np.expand_dims(y_pred, axis=-1)
    y_pred = np.concatenate([y_pred, y_pred, y_pred], axis=-1)
    y_pred = y_pred * 255

    red_mask = mask * np.array([0, 0, 1], dtype=np.uint8)
    green_pred = y_pred * np.array([0, 1, 0], dtype=np.uint8)
    red_mask = red_mask.astype(image.dtype)
    green_pred = green_pred.astype(image.dtype)
    
    image_with_red_mask = cv2.addWeighted(image, 1.0, red_mask, 0.8, 0)
    image_with_green_masks = cv2.addWeighted(image, 1.0, green_pred, 0.8, 0)
    line = np.ones((H, 10, 3)) * 255
    final_image = np.concatenate([image_with_red_mask, line, image_with_green_masks], axis=1) 
    cv2.imwrite(save_image_path, final_image)

if __name__ == "__main__":
    config_params = read_config(CONFIG_FILE_PATH)
    if config_params == None:
        raise Exception("Could not read the config file!")
    
    """ Seeding """
    np.random.seed(SEED)
    tf.random.set_seed(SEED)

    """ Directory for storing files """
    create_dir("results")

    """ Load the model """
    model_name = config_params["name"]
    model_file_path = os.path.join("results", model_name,  f"{model_name}.h5")
    print(f"Using the model from {model_file_path}")

    loss_type = config_params["loss"]
    if loss_type == "CompoundLoss":
        loss_fn = CompoundLoss(CompoundLoss.BINARY_MODE)
        with CustomObjectScope({"CompoundLoss": loss_fn}):
            print("Loading model!")
            model = tf.keras.models.load_model(model_file_path)
    elif loss_type == "DiceLoss":
        with CustomObjectScope({"dice_coef": dice_coef, "dice_loss": dice_loss}):
            print("Loading model!")
            model = tf.keras.models.load_model(model_file_path)
    else:
        raise Exception("No loss mode was found!")

    print("Done loading the model!")
    
    """ Dataset """
    dataset_path = "./post_dataset"
    (train_x, train_y), (valid_x, valid_y), (test_x, test_y) = load_dataset(dataset_path)

    """ Prediction and Evaluation """
    SCORE = []
    checkIfFolderExists(os.path.join("results", f"{model_name}", "images"))
    for x, y in tqdm(zip(test_x, test_y), total=len(test_y)):
        """ Extracting the name """
        name = x.split("/")[-1]

        """ Reading the image """
        image = cv2.imread(x, cv2.IMREAD_COLOR) ## [H, w, 3]
        image = cv2.resize(image, (W, H))       ## [H, w, 3]
        x = image/255.0                         ## [H, w, 3]
        x = np.expand_dims(x, axis=0)           ## [1, H, w, 3]

        """ Reading the mask """
        mask = cv2.imread(y, cv2.IMREAD_GRAYSCALE)
        mask = cv2.resize(mask, (W, H))

        """ Prediction """
        y_pred = model.predict(x, verbose=0)[0]
        y_pred = np.squeeze(y_pred, axis=-1)
        y_pred = y_pred >= 0.5
        y_pred = y_pred.astype(np.int32)

        """ Saving the prediction """
        save_image_path = os.path.join("results", f"{model_name}", "images", name)
        save_results(image, mask, y_pred, save_image_path)

        """ Flatten the array """
        mask = mask/255.0
        mask = (mask > 0.5).astype(np.int32).flatten()
        y_pred = y_pred.flatten()

        """ Calculating the metrics values """
        f1_value = f1_score(mask, y_pred, labels=[0, 1], average="binary")
        jac_value = jaccard_score(mask, y_pred, labels=[0, 1], average="binary")
        recall_value = recall_score(mask, y_pred, labels=[0, 1], average="binary", zero_division=0)
        precision_value = precision_score(mask, y_pred, labels=[0, 1], average="binary", zero_division=0)
        SCORE.append([name, f1_value, jac_value, recall_value, precision_value])

    """ Metrics values """
    score = [s[1:]for s in SCORE]
    score = np.mean(score, axis=0)
    write_results_to_file(score, os.path.join("results", config_params["name"], "final_score.txt"))
    df = pd.DataFrame(SCORE, columns=["Image", "F1", "Jaccard", "Recall", "Precision"])
    df.to_csv(f"results/{model_name}/testing_score.csv")