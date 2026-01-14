import os
from train import train_model
from test import test_model
from plot_csv_data import plot_model
import glob

config_files = glob.glob("configs/*")

for file in config_files:
    print("Processing", file)
    train_model(file)
    test_model(file)
    plot_model(file)