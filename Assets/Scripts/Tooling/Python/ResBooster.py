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


#synthesis suggestions
#multi-scale Retinex (reveals hidden gradients)
#guided filter detail enhancement (adds fine-grain texture without halos)
#noise-guided micro-detail synthesis (adds plausible fine structure using modulated noise)
#wavelet boosting with tuned weights (adds fractal detail)


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

        #retinex
        #ret = retinex_msr(img, scales=[15, 80, 250], weight=.25)
        #ret = invert_polarity(ret)
        #img = blend_linear(img, 0.8, ret, 0.2)
        #img  = blend_highpass(img, ret, t=0.5)
        #img = blend_multiplicative(img, ret, t=0.5)
        #img = blend_soft_light(img, ret, t=0.5)

        img = np.clip(img, 0, 255).astype(np.uint8)

        #clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8,8))
        #img = clahe.apply(img)

                #sobel-based edge enhancement
        sobelx = cv2.Sobel(img, cv2.CV_16S, 1, 0)
        sobely = cv2.Sobel(img, cv2.CV_16S, 0, 1)
        edges = cv2.convertScaleAbs(sobelx + sobely)
        img = cv2.addWeighted(img, 1.0, edges, 0.2, 0)


        #img = hybrid_noise_guided(img, 0.25)


        #guided filter detail enhancement

        #wavelet boosting with tuned weights

        #noise-guided micro-detail synthesis

        #smoothing: unsharp, light laplacian


        # Apply smoothing
        #img = cv2.GaussianBlur(img, (5, 5), 0)
        #edge-preserving smoothing
        #img = cv2.bilateralFilter(img, d=9, sigmaColor=75, sigmaSpace=75)



        #multi-scale wavelet-like detail boosting
        low = cv2.GaussianBlur(img, (0,0), sigmaX=5)
        mid = cv2.GaussianBlur(img, (0,0), sigmaX=2)
        high = img - mid
        mid_detail = mid - low
        #img = img + 0.5*mid_detail + 1.0*high


        #unsharp masking
        blur = cv2.GaussianBlur(img, (0,0), sigmaX=3)
        img = cv2.addWeighted(img, 1.5, blur, -0.5, 0)

        #laplacian smoothing
        img = np.clip(img, 0, 255).astype(np.uint8)
        lap = cv2.Laplacian(img, cv2.CV_16S, ksize=3)
        lap = cv2.convertScaleAbs(lap)
        img = cv2.addWeighted(img, 1.0, lap, 0.3, 0)



        # Save output
        cv2.imwrite(output_path, img)

        print(f"[OK] Generated: {output_path}")
        return 0

    except Exception as e:
        print(f"[ERROR] {sys.stderr(e)}")
        return 1


def blend_linear (img1, weight1, img2, weight2):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = img1 * weight1 + img2 * weight2
    return np.clip(result, 0, 255).astype(np.uint8)

def blend_additive (img1, img2, weight=1.0):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = img1 + img2 * weight
    return np.clip(result, 0, 255).astype(np.uint8)

def blend_multiplicative (img1, img2, t):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = img1 * (1 + img2 * t)
    return np.clip(result, 0, 255).astype(np.uint8)

def blend_overlay (img1, img2):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    if img1 < 0.5: result = 2 * img1 * img2
    else:          result = 1 - 2*(1-img1)*(1-img2)
    return np.clip(result, 0, 255).astype(np.uint8)

def blend_soft_light (img1, img2, t):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = (1 - 2*img2)*img1**2 + 2*img2*img1
    return np.clip(result, 0, 255).astype(np.uint8)

def blend_highpass (img1, img2, t):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = img1 + (img2 - 128) * t
    return np.clip(result, 0, 255).astype(np.uint8)


def blend_screen (img1, img2):
    img1 = img1.astype(np.float32)
    img2 = img2.astype(np.float32)
    result = 1 - (1-img1)*(1-img2)
    return np.clip(result, 0, 255).astype(np.uint8)




def invert_polarity(img):
    img = img.astype(np.float32)
    mean = np.mean(img)
    inv = (mean * 2.0) - img
    return np.clip(inv, 0, 255).astype(np.uint8)



def retinex_msr(img, scales=[15, 80, 250], weight=1.0):
    # Convert to float and avoid log(0)
    img = img.astype(np.float32) + 1.0

    # Work in each channel independently
    retinex = np.zeros_like(img)

    for sigma in scales:
        blur = cv2.GaussianBlur(img, (0,0), sigma)
        retinex += np.log(img) - np.log(blur)

    retinex /= len(scales)

    # Normalize back to 0-255
    orig_mean = np.mean(img)
    ret_mean = np.mean(retinex)
    scale = orig_mean / (ret_mean + 1e-6)

    #retinex = cv2.normalize(retinex, None, 0, 255, cv2.NORM_MINMAX)
    retinex = np.clip(retinex * scale, 0, 255).astype(np.uint8)     #normally * weight

    return retinex



def perlin_noise(h, w, scale=50):
    y, x = np.mgrid[0:h, 0:w]
    return cv2.resize(np.random.rand(h//scale, w//scale).astype(np.float32),
                      (w, h), interpolation=cv2.INTER_CUBIC)

# --- fBm noise ---------------------------------------------------------------

def fbm_noise(h, w, octaves=4):
    noise = np.zeros((h, w), np.float32)
    freq = 1.0
    amp = 1.0
    for _ in range(octaves):
        noise += cv2.resize(np.random.rand(h//2, w//2).astype(np.float32),
                            (w, h), interpolation=cv2.INTER_LINEAR) * amp
        freq *= 2.0
        amp *= 0.5
    return noise / noise.max()

# --- Hybrid noise synthesis --------------------------------------------------

def hybrid_noise_guided(img, strength=0.25):
    if img.ndim == 2:
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)
    elif img.ndim == 3 and img.shape[2] == 1:
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)

    h, w = img.shape[:2]

    # Convert to grayscale for gradient analysis
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.float32)

    # Compute gradients
    gx = cv2.Sobel(gray, cv2.CV_32F, 1, 0, ksize=3)
    gy = cv2.Sobel(gray, cv2.CV_32F, 0, 1, ksize=3)
    grad_mag = np.sqrt(gx*gx + gy*gy)

    # Normalize gradient magnitude
    grad_mag = grad_mag / (grad_mag.max() + 1e-6)

    # Generate hybrid noise
    perlin = perlin_noise(h, w, scale=40)
    fbm = fbm_noise(h, w, octaves=5)
    hybrid = (perlin * 0.4 + fbm * 0.6)

    # Modulate noise by gradient magnitude
    micro = hybrid * (grad_mag ** 0.7)

    # Expand to 3 channels
    micro3 = np.repeat(micro[:, :, None], 3, axis=2)

    # Blend into original
    img_f = img.astype(np.float32)
    result = img_f + micro3 * (strength * 255.0)

    return np.clip(result, 0, 255).astype(np.uint8)




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
