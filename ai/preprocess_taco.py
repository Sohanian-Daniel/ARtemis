import os
import json
import cv2
import numpy as np
from tqdm import tqdm

# ---------------- PATHS ----------------
TACO_DATA = "./data_taco"
ANNOT_PATH = os.path.join(TACO_DATA, "annotations.json")
IMG_ROOT = TACO_DATA

POST_ROOT = "./post_dataset_taco"
IMG_OUT = os.path.join(POST_ROOT, "images")
MASK_OUT = os.path.join(POST_ROOT, "masks")

os.makedirs(IMG_OUT, exist_ok=True)
os.makedirs(MASK_OUT, exist_ok=True)

# ---------------- LOAD COCO ----------------
with open(ANNOT_PATH, "r") as f:
    taco = json.load(f)

images_info = {img["id"]: img for img in taco["images"]}
annotations = taco["annotations"]
categories = taco["categories"]

print(f"Images: {len(images_info)}")
print(f"Annotations: {len(annotations)}")
print(f"Categories: {len(categories)}")

# ---------------- CATEGORY → RECYCLING ----------------
category_to_recycling = {
    "Aluminium foil": "metal",
    "Battery": "other",
    "Aluminium blister pack": "metal",
    "Carded blister pack": "plastic",
    "Other plastic bottle": "plastic",
    "Clear plastic bottle": "plastic",
    "Glass bottle": "other",
    "Plastic bottle cap": "plastic",
    "Metal bottle cap": "metal",
    "Broken glass": "other",
    "Food Can": "metal",
    "Aerosol": "metal",
    "Drink can": "metal",
    "Toilet tube": "paper",
    "Other carton": "paper",
    "Egg carton": "paper",
    "Drink carton": "paper",
    "Corrugated carton": "paper",
    "Meal carton": "paper",
    "Pizza box": "paper",
    "Paper cup": "paper",
    "Disposable plastic cup": "plastic",
    "Foam cup": "plastic",
    "Glass cup": "other",
    "Other plastic cup": "plastic",
    "Food waste": "bio",
    "Glass jar": "other",
    "Plastic lid": "plastic",
    "Metal lid": "metal",
    "Other plastic": "plastic",
    "Magazine paper": "paper",
    "Tissues": "paper",
    "Wrapping paper": "paper",
    "Normal paper": "paper",
    "Paper bag": "paper",
    "Plastified paper bag": "paper",
    "Plastic film": "plastic",
    "Six pack rings": "plastic",
    "Garbage bag": "plastic",
    "Other plastic wrapper": "plastic",
    "Single-use carrier bag": "plastic",
    "Polypropylene bag": "plastic",
    "Crisp packet": "plastic",
    "Spread tub": "plastic",
    "Tupperware": "plastic",
    "Disposable food container": "plastic",
    "Foam food container": "plastic",
    "Other plastic container": "plastic",
    "Plastic glooves": "plastic",
    "Plastic utensils": "plastic",
    "Pop tab": "metal",
    "Rope & strings": "other",
    "Scrap metal": "metal",
    "Shoe": "other",
    "Squeezable tube": "plastic",
    "Plastic straw": "plastic",
    "Paper straw": "paper",
    "Styrofoam piece": "other",
    "Unlabeled litter": "other",
    "Cigarette": "other",
}

# Map recycling types → index
recycling_to_index = {
    "background": 0,
    "plastic": 1,
    "paper": 2,
    "bio": 3,
    "metal": 4,
    "other": 5,
}

# Map recycling types → BGR colors for visualization
RECYCLING_COLORS = {
    "background": (0, 0, 0),
    "plastic": (255, 0, 0),  # blue
    "paper": (0, 255, 0),    # green
    "bio": (42, 42, 165),    # brown
    "metal": (128, 128, 128),# gray
    "other": (0, 0, 255),    # red
}

# Map category id → recycling index
cat_id_to_recycling_idx = {
    cat["id"]: recycling_to_index[category_to_recycling[cat["name"]]]
    for cat in categories
}

# ---------------- GROUP ANNOTATIONS BY IMAGE ----------------
anns_by_image = {}
for ann in annotations:
    anns_by_image.setdefault(ann["image_id"], []).append(ann)

# ---------------- PROCESS IMAGES ----------------
for img_id, img_info in tqdm(images_info.items()):
    file_name = img_info["file_name"]
    img_path = os.path.join(IMG_ROOT, file_name)

    if not os.path.exists(img_path):
        print("Missing:", img_path)
        continue

    # Read & resize image
    image = cv2.imread(img_path)
    if image is None:
        continue
    image = cv2.resize(image, (256, 256))
    h, w = image.shape[:2]

    # Create empty mask (single channel)
    mask = np.zeros((256, 256), dtype=np.uint8)

    # Process annotations for this image
    for ann in anns_by_image.get(img_id, []):
        cat_id = ann["category_id"]
        recycling_idx = cat_id_to_recycling_idx[cat_id]

        for seg in ann.get("segmentation", []):
            poly = np.array(seg).reshape(-1, 2)
            poly[:, 0] = (poly[:, 0] * 256 / img_info["width"]).astype(np.int32)
            poly[:, 1] = (poly[:, 1] * 256 / img_info["height"]).astype(np.int32)
            poly = poly.astype(np.int32)

            cv2.fillPoly(mask, [poly], recycling_idx)

    # Save outputs
    name_noext = os.path.splitext(file_name)[0].replace("/", "_")

    #tqdm.write(f"Name : {name_noext}")
    cv2.imwrite(os.path.join(IMG_OUT, name_noext + ".png"), image)

    # Convert mask to BGR for visualization
    mask_bgr = np.zeros((256, 256, 3), dtype=np.uint8)
    for r_type, color in RECYCLING_COLORS.items():
        mask_bgr[mask == recycling_to_index[r_type]] = color
    
    cv2.imwrite(os.path.join(MASK_OUT, name_noext + ".png"), mask_bgr)

print("Done!")
