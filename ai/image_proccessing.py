import albumentations as A
import numpy as np
import xml.etree.ElementTree as ET
import cv2

IMAGE = 'image'
TUMOR_MASK = 'tumorMask'

TRANSFORM = None

def init_transform(config_data):
    global TRANSFORM
    toTransform = []
    if "augmentation" in config_data:
        if "flip" in config_data["augmentation"]:
            type = config_data["augmentation"]["flip"]
            if type == "horizontal":
                toTransform.append(A.HorizontalFlip(p = 0.5))
            elif type == "vertical":
                toTransform.append(A.VerticalFlip(p = 0.5))
            else:
                raise Exception("Wrong type of flip found!")
            print(f"Added flip({type}) to the model")
        if "rotation" in config_data["augmentation"]:
            rotation_value = float(config_data["augmentation"]["rotation"])
            toTransform.append(A.Rotate(limit = (-rotation_value, rotation_value)))
            print(f"Added rotation({(-rotation_value, rotation_value)}) to the model")
        TRANSFORM = A.Compose(toTransform)

def apply_transform(image, mask):
    if TRANSFORM == None:
        return {
            "image": image,
            "mask": mask
        }
    return TRANSFORM(image=image, mask=mask)

def map_to_interval(arr):
    # Flatten the array to 1D
    flattened_arr = arr.flatten()
    
    # Find the min and max values
    min_val = np.min(flattened_arr)
    max_val = np.max(flattened_arr)
    
    # Map values to the interval [0, 255]
    mapped_values = np.interp(flattened_arr, (min_val, max_val), (0, 255))
    
    # Reshape back to 2x2 array
    mapped_arr = mapped_values.reshape(arr.shape)
    
    return mapped_arr

def get_image_mask(image_file_path, data_file_path):
    # Read image
    image = cv2.imread(image_file_path)
    height, width = image.shape[:2]

    # Parse XML
    tree = ET.parse(data_file_path)
    root = tree.getroot()
    bndbox = root.find('.//bndbox')
    xmin = int(float(bndbox.find('xmin').text))
    ymin = int(float(bndbox.find('ymin').text))
    xmax = int(float(bndbox.find('xmax').text))
    ymax = int(float(bndbox.find('ymax').text))

    # Create mask
    mask = np.zeros((height, width), dtype=np.uint8)
    mask[ymin:ymax, xmin:xmax] = 255

    # Resize to 256x256
    image_resized = cv2.resize(image, (256, 256), interpolation=cv2.INTER_AREA)
    mask_resized = cv2.resize(mask, (256, 256), interpolation=cv2.INTER_NEAREST)

    return image_resized, mask_resized


