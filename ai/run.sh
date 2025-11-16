#!/bin/bash

# Directory containing the files
DIRECTORY="configs"

# Iterate over each file in the directory
for FILE in "$DIRECTORY"/*; do
    # Check if it's a file (not a directory)
    if [ -f "$FILE" ]; then
        echo "Processing file: $FILE"
        echo "Training model on file: $FILE"
        # Call the Python script with the file as an argument
        python3 ./train.py "$FILE"
        echo "Testing model on file: $FILE"
        python3 ./test.py "$FILE"
        echo "Creating drawings for model on file: $FILE"
        python3 ./plot_csv_data.py "$FILE"
    fi
done