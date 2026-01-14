import os
import cv2
from glob import glob
from tqdm import tqdm
from concurrent.futures import ThreadPoolExecutor
from threading import Lock

# ---------------- PARAMETERS ----------------
folder_path = "./post_dataset/masks"  # folder with mask images
image_ext = "*.jpg"      # or "*.png"
num_threads = 4          # adjust based on CPU cores

# ---------------- PREDEFINED CLASS COLORS ----------------
CLASS_COLORS = [
    (0, 0, 0),      # background (black)
    (255, 0, 0),    # blue (BGR)
    (0, 255, 0),    # green
    (0, 0, 255),    # red
    (255, 255, 0),  # cyan
    (255, 0, 255),  # magenta
    (0, 255, 255),  # yellow
]
class_colors_set = set(CLASS_COLORS)

# ---------------- LOAD IMAGE FILES ----------------
image_files = glob(os.path.join(folder_path, image_ext))
image_files = image_files[3000:len(image_files) - 1]

# ---------------- THREAD-SAFE SET ----------------
found_colors = set()
lock = Lock()

# ---------------- FUNCTION TO READ IMAGE ----------------
def read_image(file):
    img = cv2.imread(file, cv2.IMREAD_COLOR)
    if img is None:
        return

    # get all unique colors in this image
    pixels = set(tuple(c) for c in img.reshape(-1, 3))
    # keep only colors that are in CLASS_COLORS
    pixels = pixels.intersection(class_colors_set)

    new_colors = []
    with lock:
        for color in pixels:
            if color not in found_colors:
                found_colors.add(color)
                new_colors.append(color)

    for c in new_colors:
        print("Found color:", c)

# ---------------- READ IMAGES IN PARALLEL ----------------
with ThreadPoolExecutor(max_workers=num_threads) as executor:
    list(tqdm(executor.map(read_image, image_files), total=len(image_files), desc="Reading images"))

print("All found colors:", found_colors)
