import tensorflow as tf
from metrics import combined_loss, dice_coef_multi, pixel_precision, per_class_precision

# Load model
model = tf.keras.models.load_model(
    "./results/Unet_v3_CompoundLoss/Unet_v3_CompoundLoss.h5",
    custom_objects={
        "combined_loss": combined_loss,
        "dice_coef_multi": dice_coef_multi,
        "pixel_precision": pixel_precision,
        "per_class_precision": per_class_precision
    }
)

# Convert to TFLite
converter = tf.lite.TFLiteConverter.from_keras_model(model)
converter.optimizations = []  # no quantization, safest
tflite_model = converter.convert()

# Save
with open("./results/Unet_v3_CompoundLoss/Unet_v3_CompoundLoss.tflite", "wb") as f:
    f.write(tflite_model)
