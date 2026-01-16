# strip_taco_graph.py
import onnx
from onnx import helper, numpy_helper

model = onnx.load('taco_raw.onnx')

print(f"Original model has {len(model.graph.node)} nodes")

# Remove nodes with TensorArray or Preprocessor in the name
nodes_to_keep = []
for node in model. graph.node:
    if 'TensorArray' not in node.name and 'Preprocessor' not in node.name:
        nodes_to_keep.append(node)
    else:
        print(f"Removing:  {node.name}")

print(f"Keeping {len(nodes_to_keep)} nodes")

# Rebuild graph
del model.graph.node[:]
model.graph.node.extend(nodes_to_keep)

# Check the model
try:
    onnx.checker.check_model(model)
    print("Model is valid!")
except Exception as e:
    print(f"Model validation warning: {e}")

onnx.save(model, 'taco_stripped.onnx')
print("Saved taco_stripped.onnx")