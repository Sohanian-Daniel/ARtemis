import os
import json
import cv2
import numpy as np
from tqdm import tqdm

# ---------------- PATHS ----------------
PRE_DATASET = "./pre_dataset"
POST_IMAGES = "./post_dataset/images"
POST_MASKS = "./post_dataset/masks"

# ---------------- CREATE FOLDER ----------------
def check_create(folder):
    if not os.path.exists(folder):
        os.makedirs(folder)
        print(f"Created folder: {folder}")

check_create(POST_IMAGES)
check_create(POST_MASKS)

# ---------------- COLOR PALETTE ----------------
# Assign a unique color for each category (BGR)
CATEGORY_COLORS = [
    (255, 0, 0),   # category 0 -> blue
    (0, 255, 0),   # category 1 -> green
    (0, 0, 255),   # category 2 -> red
    (255, 255, 0), # category 3 -> cyan
    (255, 0, 255), # category 4 -> magenta
    (0, 255, 255), # category 5 -> yellow
]

# ---------------- LOAD COCO JSON ----------------
def load_annotations(file_path):
    with open(file_path) as f:
        data = json.load(f)
    return data

# ---------------- CREATE MASK ----------------
def create_colored_mask(image_shape, annotations, categories):
    mask = np.zeros((image_shape[0], image_shape[1], 3), dtype=np.uint8)
    for ann in annotations:
        cat_id = ann["category_id"]
        color = CATEGORY_COLORS[cat_id % len(CATEGORY_COLORS)]
        bbox = ann["bbox"]  # [x_min, y_min, width, height]
        x, y, w, h = map(int, bbox)
        cv2.rectangle(mask, (x, y), (x + w, y + h), color, -1)
    return mask

# ---------------- MAIN ----------------
def process_folder(folder):
    images_folder = os.path.join(folder)
    annotations_file = os.path.join(folder, "_annotations.coco.json")
    
    if not os.path.exists(annotations_file):
        print(f"No annotation file in {folder}, skipping.")
        return
    
    data = load_annotations(annotations_file)
    images_info = {img["file_name"]: img for img in data["images"]}
    
    print(f"Processing {folder} - {len(images_info)} images, {len(data['categories'])} categories")
    
    for img_name, img_info in tqdm(images_info.items()):
        img_path = os.path.join(images_folder, img_name)
        if not os.path.exists(img_path):
            continue

        image = cv2.imread(img_path)
        if image is None:
            continue

        anns = [a for a in data["annotations"] if a["image_id"] == img_info["id"]]
        mask = create_colored_mask(image.shape, anns, data["categories"])
        
        # Save image and mask
        cv2.imwrite(os.path.join(POST_IMAGES, img_name), image)
        cv2.imwrite(os.path.join(POST_MASKS, img_name), mask)

# ---------------- RUN ----------------
for subfolder in ["train", "valid", "test"]:
    folder_path = os.path.join(PRE_DATASET, subfolder)
    process_folder(folder_path)
