#!/bin/bash
export NVIDIA_VISIBLE_DEVICES=all
export NVIDIA_DRIVER_CAPABILITIES=compute,utility
export LD_LIBRARY_PATH=/usr/local/cuda/lib64:$LD_LIBRARY_PATH

# Directory containing the files
DIRECTORY="configs"

# Iterate over each file in the directory
for FILE in "$DIRECTORY"/*; do
    # Check if it's a file (not a directory)
    if [ -f "$FILE" ]; then
        echo "----------------------------------------"
        echo "$(date +%T) - Processing file: $FILE"

        echo "$(date +%T) - Training model on file: $FILE"
        start=$(date +%s)
        python3 ./train.py "$FILE"
        end=$(date +%s)
        echo "$(date +%T) - Finished training in $((end - start)) seconds"

        echo "$(date +%T) - Testing model on file: $FILE"
        start=$(date +%s)
        python3 ./test.py "$FILE"
        end=$(date +%s)
        echo "$(date +%T) - Finished testing in $((end - start)) seconds"

        echo "$(date +%T) - Creating drawings for model on file: $FILE"
        start=$(date +%s)
        python3 ./plot_csv_data.py "$FILE"
        end=$(date +%s)
        echo "$(date +%T) - Finished creating drawings in $((end - start)) seconds"
    fi
done
echo "----------------------------------------"
echo "$(date +%T) - All files processed"
