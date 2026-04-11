import argparse
import sys
import traceback
#from PIL import Image, ImageFilter
import cv2
import numpy as np

#edge preserving smoothing
#smooth = cv2.bilateralFilter(img, d=9, sigmaColor=75, sigmaSpace=75)

#unsharp masking
#blur = cv2.GaussianBlur(img, (0,0), sigmaX=3)
#sharp = cv2.addWeighted(img, 1.5, blur, -0.5, 0)

#laplacian smoothing
#lap = cv2.Laplacian(img, cv2.CV_16S, ksize=3)
#lap = cv2.convertScaleAbs(lap)
#enhanced = cv2.addWeighted(img, 1.0, lap, 0.3, 0)

#multi-scale wavelet-like detail boosting
#low = cv2.GaussianBlur(img, (0,0), sigmaX=5)
#mid = cv2.GaussianBlur(img, (0,0), sigmaX=2)
#high = img - mid
#mid_detail = mid - low
#result = img + 0.5*mid_detail + 1.0*high

#CLAHE adaptive contrast enhanceent
#clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8,8))
#enhanced = clahe.apply(img)

#sobel-based edge enhancement
#sobelx = cv2.Sobel(img, cv2.CV_16S, 1, 0)
#sobely = cv2.Sobel(img, cv2.CV_16S, 0, 1)
#edges = cv2.convertScaleAbs(sobelx + sobely)
#enhanced = cv2.addWeighted(img, 1.0, edges, 0.2, 0)

#load image (no greyscale)
#upscale with lanczos
#edge-preserving smoothing
#unsharp mask
#laplacial detail boost
#clahe
#save resultGive 


def upscale_image(input_path, output_path, width, height):
    try:
        #load image (with no changes) - creates a NumPy array
        img = cv2.imread(input_path, cv2.IMREAD_UNCHANGED)

        #upscale image using Lanczos
        img = cv2.resize(img, (width,height), interpolation=cv2.INTER_LANCZOS4)        


        # Apply smoothing
        img = cv2.GaussianBlur(img, (5, 5), 0)

        # Save output
        cv2.imwrite(output_path, img)

        print(f"[OK] Generated: {output_path}")
        return 0

    except Exception as e:
        print(f"[ERROR] {sys.stderr(e)}")
        return 1


def main():
    parser = argparse.ArgumentParser(description="Simple terrain upscaler")
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--width", type=int, required=True)
    parser.add_argument("--height", type=int, required=True)

    args = parser.parse_args()

    exit_code = upscale_image(args.input, args.output, args.width, args.height)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
