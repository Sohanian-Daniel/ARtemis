import albumentations as A
from albumentations.core.transforms_interface import DualTransform
import random
import numpy as np
import xml.etree.ElementTree as ET
import cv2

IMAGE = 'image'
TUMOR_MASK = 'tumorMask'

TRANSFORM = None

class CustomCropNonEmptyMaskIfExists(DualTransform):
    def __init__(self, height, width, always_apply=False, p=1.0):
        super(CustomCropNonEmptyMaskIfExists, self).__init__(always_apply=always_apply, p=p)
        self.height = height
        self.width = width

    def apply(self, img, x_min=0, y_min=0, **params):
        return img[y_min:y_min+self.height, x_min:x_min+self.width]

    def apply_to_mask(self, img, x_min=0, y_min=0, **params):
        return img[y_min:y_min+self.height, x_min:x_min+self.width]

    def get_params_dependent_on_data(self, params, data):
        mask = data["mask"]
        mask_height, mask_width = mask.shape[:2]

        if self.height > mask_height or self.width > mask_width:
            return {"x_min": 0, "y_min": 0}

        # Check if mask has any non-zero values (foreground)
        if mask.ndim == 3:
            mask_flat = np.any(mask > 0, axis=-1)
        else:
            mask_flat = mask > 0
            
        non_zero_yx = np.argwhere(mask_flat)

        if len(non_zero_yx) > 0:
            # Select a random pixel from the mask
            idx = random.randint(0, len(non_zero_yx) - 1)
            y, x = non_zero_yx[idx]
            
            # Center crop around it
            x_min = x - self.width // 2
            y_min = y - self.height // 2
            
            # Clip to boundaries
            x_min = max(0, min(x_min, mask_width - self.width))
            y_min = max(0, min(y_min, mask_height - self.height))
        else:
            # Fallback to random crop if mask is empty
            x_min = random.randint(0, mask_width - self.width)
            y_min = random.randint(0, mask_height - self.height)

        return {"x_min": x_min, "y_min": y_min}

    @property
    def targets_as_params(self):
        return ["mask"]

    def get_transform_init_args_names(self):
        return ("height", "width")

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
            elif type == "horizontal and vertical":
                toTransform.append(A.HorizontalFlip(p = 0.5))
                toTransform.append(A.VerticalFlip(p = 0.5))
            else:
                raise Exception("Wrong type of flip found!")
            print(f"Added flip({type}) to the model")
        if "rotation" in config_data["augmentation"]:
            rotation_value = float(config_data["augmentation"]["rotation"])
            toTransform.append(A.Rotate(limit = (-rotation_value, rotation_value)))
            print(f"Added rotation({(-rotation_value, rotation_value)}) to the model")
        if "zoom" in config_data["augmentation"] and config_data["augmentation"]["zoom"]:
            toTransform.append(CustomCropNonEmptyMaskIfExists(height=160, width=160, p=0.75))
            toTransform.append(A.Resize(height=256, width=256))
            print("Added zoom (CustomCropNonEmptyMaskIfExists) to the model")
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
