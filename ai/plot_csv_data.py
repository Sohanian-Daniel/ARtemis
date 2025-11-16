import pandas as pd
import matplotlib.pyplot as plt
import os
import yaml
import sys

CONFIG_FILE = sys.argv[1]

def checkIfFolderExists(folderPath):
    if not os.path.isdir(folderPath):
        os.makedirs(folderPath)
        print(f"The folder '{folderPath}' has been created.")

def read_config(file_path):
    with open(file_path, 'r') as file:
        try:
            return yaml.safe_load(file)
        except yaml.YAMLError as exc:
            print(f"Error reading YAML file: {exc}")
            return None

config_params = read_config(CONFIG_FILE)
if config_params == None:
    raise Exception("Could not read the config file!")

log_path = os.path.join("results", config_params["name"], "training_log.csv")
# Read the CSV file
df = pd.read_csv(log_path)

# Create a single figure for all graphs
plt.figure(figsize=(8, 6))

# Plot each column on the same plot with a different color and add a legend
for i, column in enumerate(df.columns):
    if not (column == "val_loss" or column == "loss"):
        continue
    plt.plot(df[column], label=column)

plt.title("Log")
plt.xlabel("Epochs")
plt.ylabel("Value")
plt.grid(True)
plt.legend()
batch_size = int(config_params["batch_size"])
lr = float(config_params["lr"])
num_epochs = int(config_params["num_epochs"])
model_name = config_params["name"]
parameters_text = \
f" Batch Size: {batch_size}\n \
Learning Rate: {lr}\n \
Num Epochs: {num_epochs}"
plt.text(0.05, 0.95, parameters_text, transform=plt.gca().transAxes,
         fontsize=10, verticalalignment='top', bbox=dict(facecolor='white', alpha=0.5))
plt_path = os.path.join("results", config_params["name"], "loss_val_loss_graph.png")
plt.savefig(plt_path)