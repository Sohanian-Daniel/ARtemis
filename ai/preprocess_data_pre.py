import os
import cv2 
from image_proccessing import get_image_mask
from tqdm import tqdm

IMAGES_PATH = "./pre_dataset/pre_images"
DATA_PATH = "./pre_dataset/pre_data"

def checkIfFolderExists(folderPath):
    if not os.path.isdir(folderPath):
        os.makedirs(folderPath)
        print(f"The folder '{folderPath}' has been created.")

def list_jpg_files(folder_path):
    jpg_files = []
    for file in os.listdir(folder_path):
        if file.endswith(".jpg"):
            jpg_files.append(file)
    return jpg_files

if __name__ == "__main__":
    all_img = list_jpg_files(IMAGES_PATH)
    checkIfFolderExists("./post_dataset")
    checkIfFolderExists("./post_dataset/images")
    checkIfFolderExists("./post_dataset/masks")
    for img_path in tqdm(all_img):
        image, mask = get_image_mask(
            image_file_path=os.path.join(IMAGES_PATH, img_path),
            data_file_path=os.path.join(DATA_PATH, img_path.split(".")[0] + ".xml")
        )

        
        cv2.imwrite("./post_dataset/images/" + img_path.split(".")[0] + ".jpg", image)
        cv2.imwrite("./post_dataset/masks/" + img_path.split(".")[0] + ".jpg", mask)