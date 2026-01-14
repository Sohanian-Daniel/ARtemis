import tensorflow as tf
import tf2onnx
import onnx

# =========================
# CONFIGURARE
# =========================
H5_MODEL_PATH = "./results/Unet_v3_CompoundLoss/Unet_v3_CompoundLoss.h5"
ONNX_OUTPUT_PATH = "./results/Unet_v3_CompoundLoss/Unet_v3_CompoundLoss.onnx"

# 🔒 FIXED INPUT SHAPE
BATCH_SIZE = 1
HEIGHT = 256
WIDTH = 256
CHANNELS = 3

OPSET = 13

# =========================
# LOAD MODEL (.h5)
# =========================
print("[INFO] Loading Keras model...")
model = tf.keras.models.load_model(
    H5_MODEL_PATH,
    compile=False   # 🔴 IMPORTANT (ignora loss / metrics custom)
)

print("[INFO] Model loaded")
print("[INFO] Keras input shape:", model.input_shape)

# =========================
# DEFINE FIXED SHAPE
# =========================
input_signature = (
    tf.TensorSpec(
        [BATCH_SIZE, HEIGHT, WIDTH, CHANNELS],
        tf.float32,
        name="input"
    ),
)

# =========================
# CONVERT TO ONNX
# =========================
print("[INFO] Converting to ONNX with FIXED SHAPE...")

tf2onnx.convert.from_keras(
    model,
    input_signature=input_signature,
    opset=OPSET,
    output_path=ONNX_OUTPUT_PATH
)

print("[INFO] ONNX model saved to:", ONNX_OUTPUT_PATH)

# =========================
# VERIFY ONNX SHAPES
# =========================
print("[INFO] Verifying ONNX input shape...")

onnx_model = onnx.load(ONNX_OUTPUT_PATH)
onnx.checker.check_model(onnx_model)

for inp in onnx_model.graph.input:
    dims = [d.dim_value for d in inp.type.tensor_type.shape.dim]
    print("ONNX input:", inp.name, dims)

print("[SUCCESS] ONNX model is VALID and FIXED SHAPE")