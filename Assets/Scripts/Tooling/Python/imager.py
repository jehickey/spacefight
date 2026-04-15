#!/usr/bin/env python3
import argparse
import json
import os
import numpy as np
#from turtle import update
import cv2
from pprint import pprint

from imager_modules import MODULES


def get_modules():
    return MODULES

def run_pipeline(img, module_list, output_path):
    for entry in module_list:
        name = entry["name"]
        params = entry["params"]
        try:
            updateImage = MODULES[name]["func"](img, **params)
        except cv2.error as e:
            print(f"Error occurred in CV2 while running module '{name}': {e}")
            continue
        except Exception as e:
            print(f"Unexpected error occurred while running module '{name}': {e}")
            continue
        if is_valid_image(updateImage):
            img = updateImage.astype(np.uint8)

    print(f"Writing output image to {output_path}")
    cv2.imwrite(output_path, img)
    return img




def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--list-modules", action="store_true")
    parser.add_argument("--input-path", action="store", help="Path to read the input image from")
    parser.add_argument("--output-path", action="store", help="Path to write the resulting image to")
    parser.add_argument("--modules", type=str, help="List of modules to execute in pipeline")
    parser.add_argument("--modules-file", type=str, help="Path to JSON file containing list of modules to execute in pipeline")
    parser.add_argument("--module", action="append", help="Add an individual module to the pipeline")
    args = parser.parse_args()

    img=None

    if (args.list_modules):
        #print (json.dumps(get_modules(), indent=2, default=str))
        print (json.dumps(get_modules(), default=str))
        return

    if (args.input_path is None or args.output_path is None):
        print("Input and output paths are required")
        return

    if (args.input_path):
        img = cv2.imread(args.input_path)
        if img is None:
            print(f"Failed to read image from {args.input_path}")
            return

    if (args.output_path):
        if not is_writeable_path(args.output_path):
            print(f"Unable to write to {args.output_path}")
            return

    if (args.modules_file):
        try:
            with open(args.modules_file, "r") as f:
                module_list = json.load(f)
                img = run_pipeline(img, module_list, args.output_path)
        except Exception as e:
            print(f"Failed to read modules from {args.modules_file}: {e}")
            return;

    if (args.modules):
        print (f"Got module list: {args.modules}")
        module_list = json.loads(args.modules)
        img = run_pipeline(img, module_list, args.output_path)


def is_writeable_path(path):
    directory = os.path.dirname(path) or "."
    if not os.path.exists(directory):
        return False
    return os.access(directory, os.W_OK)

def is_valid_image(arr):
    if not isinstance(arr, np.ndarray):
        return False
    if arr.ndim not in (2, 3):
        return False
    if arr.ndim == 3 and arr.shape[2] not in (1, 3, 4):
        return False
    if arr.size == 0:
        return False
    if arr.dtype not in (np.uint8, np.uint16, np.float32, np.float64):
        return False
    return True



if __name__ == "__main__":
    main()


