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
from glob import glob

from ce_bias import CompoundLoss

""" Global parameters """
H = 256
W = 256
SEED = 42
LOSS_TYPE = "DiceLoss"

def checkIfFolderExists(folderPath):
    if not os.path.isdir(folderPath):
        os.makedirs(folderPath)
        print(f"The folder '{folderPath}' has been created.")

def create_dir(path):
    if not os.path.exists(path):
        os.makedirs(path)

def load_dataset(path):
    images = sorted(glob(os.path.join(path, "images", "*.jpg")))

    return (images, None)

def save_results(image, y_pred, save_image_path):

    y_pred = np.expand_dims(y_pred, axis=-1)
    y_pred = np.concatenate([y_pred, y_pred, y_pred], axis=-1)
    y_pred = y_pred * 255

    green_pred = y_pred * np.array([0, 1, 0], dtype=np.uint8)
    green_pred = green_pred.astype(image.dtype)
    
    image_with_green_masks = cv2.addWeighted(image, 1.0, green_pred, 0.8, 0)
    line = np.ones((H, 10, 3)) * 255
    final_image = np.concatenate([image_with_green_masks], axis=1) 
    cv2.imwrite(save_image_path, final_image)

if __name__ == "__main__":
    
    """ Seeding """
    np.random.seed(SEED)
    tf.random.set_seed(SEED)

    """ Directory for storing files """
    create_dir("demo_results")
    model_name = "final_model"
    model_file_path = os.path.join(f"{model_name}.keras")
    print(f"Using the model from {model_file_path}")

    
    if LOSS_TYPE == "CompoundLoss":
        loss_fn = CompoundLoss(CompoundLoss.BINARY_MODE)
        with CustomObjectScope({"CompoundLoss": loss_fn}):
            print("Loading model!")
            model = tf.keras.models.load_model(model_file_path)
    elif LOSS_TYPE == "DiceLoss":
        with CustomObjectScope({"dice_coef": dice_coef, "dice_loss": dice_loss}):
            print("Loading model!")
            model = tf.keras.models.load_model(model_file_path)
    else:
        raise Exception("No loss mode was found!")

    print("Done loading the model!")
    
    """ Dataset """
    dataset_path = "./demo_images"
    (test_x, test_y) = load_dataset(dataset_path)

    """ Prediction and Evaluation """
    SCORE = []

    checkIfFolderExists(os.path.join("results_demo", f"{model_name}", "images"))
    for x in tqdm(zip(test_x), total=len(test_x)):
        """ Extracting the name """
        name = x.split("/")[-1]

        """ Reading the image """
        image = cv2.imread(x, cv2.IMREAD_COLOR) ## [H, w, 3]
        image = cv2.resize(image, (W, H))       ## [H, w, 3]
        x = image/255.0                         ## [H, w, 3]
        x = np.expand_dims(x, axis=0)           ## [1, H, w, 3]

        """ Prediction """
        y_pred = model.predict(x, verbose=0)[0]
        y_pred = np.squeeze(y_pred, axis=-1)
        y_pred = y_pred >= 0.5
        y_pred = y_pred.astype(np.int32)

        """ Saving the prediction """
        save_image_path = os.path.join("results_demo", f"{model_name}", "images", name)
        save_results(image, y_pred, save_image_path)

        """ Flatten the array """
        y_pred = y_pred.flatten()