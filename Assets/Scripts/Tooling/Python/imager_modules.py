from atexit import register
import numpy as np
import cv2
import json
import inspect

MODULES = {}

def register_module_old(name):
    def decorator(func):
        MODULES[name] = {
            "func": func,
            "params": func.__defaults__,
            "param_names": func.__code__.co_varnames[1:1+len(func.__defaults__)]
            #skips first parameter (image)
        }
        return func
    return decorator



MODULES = {}

def register_module(name):
    def decorator(func):
        sig = inspect.signature(func)
        params = []

        # Skip the first parameter (usually the image/context)
        for param_name, param in list(sig.parameters.items())[1:]:
            default = param.default
            annotation = param.annotation

            # Determine type name
            if annotation is not inspect._empty:
                type_name = annotation.__name__
            else:
                if isinstance(default, int):
                    type_name = "int"
                elif isinstance(default, float):
                    type_name = "float"
                elif isinstance(default, bool):
                    type_name = "bool"
                else:
                    type_name = "string"

            params.append({
                "name": param_name,
                "type": type_name,
                "defaultVal": default
            })

        MODULES[name] = {
            "name": name,
            "parameters": params,
            "func": func
        }

        return func
    return decorator


def get_modules_json():
    return json.dumps({"modules": MODULES}, separators=(',', ':'))

@register_module("upscale")
def module_upscale(img, width=1024, height=1024):
    width=int(width)
    height=int(height)
    #print (f"Upscaling image to {width}x{height}")
    return cv2.resize(img, (width,height), interpolation=cv2.INTER_LANCZOS4)        
    
@register_module("gaussian blur")
def module_gaussian(img, size=5, sigma=0.25):
    #print (f"Applying Gaussian blur with size={size} and sigma={sigma}")
    return cv2.GaussianBlur(img, (size, size), sigma)

@register_module("wavelet boost")
def module_wavelet_boost(img, low_sigma=5, mid_sigma=2, mid_weight=0.5, high_weight=1.0):
    low = cv2.GaussianBlur(img, (0,0), sigmaX=low_sigma)
    mid = cv2.GaussianBlur(img, (0,0), sigmaX=mid_sigma)
    high = img - mid
    mid_detail = mid - low
    return img + mid_weight*mid_detail + high_weight*high

#unsharp masking
@register_module("unsharp masking")
def unsharp_masking(img, radius=3, original_weight=1.5, blur_weight=-0.5):
    blur = cv2.GaussianBlur(img, (0,0), sigmaX=radius)
    sharp = cv2.addWeighted(img, original_weight, blur, blur_weight, 0)
    return sharp


@register_module("laplacian smoothing")
def laplacian_smoothing(img, edge_weight=0.3):
    lap = cv2.Laplacian(img, cv2.CV_16S, ksize=3)
    lap = cv2.convertScaleAbs(lap)
    enhanced = cv2.addWeighted(img, 1.0, lap, edge_weight, 0)
    return enhanced

@register_module("CLAHE adaptive contrast")
def clahe_contrast_enhancement(img, clip_limit=2.0, tile_grid_size=8):
     clahe = cv2.createCLAHE(clipLimit=clip_limit, tileGridSize=(tile_grid_size,tile_grid_size))
     if len(img.shape) == 2:  # Grayscale image
         return clahe.apply(img)
     elif len(img.shape) == 3 and img.shape[2] == 3:  # Color image
         lab = cv2.cvtColor(img, cv2.COLOR_BGR2LAB)
         l, a, b = cv2.split(lab)
         l = clahe.apply(l)
         lab = cv2.merge((l,a,b))
         return cv2.cvtColor(lab, cv2.COLOR_LAB2BGR)
     else:
         raise ValueError("Unsupported image format for CLAHE")

@register_module("sobel")
def sobel_edge_enhancement(img, edge_weight=0.2):
    sobelx = cv2.Sobel(img, cv2.CV_16S, 1, 0)
    sobely = cv2.Sobel(img, cv2.CV_16S, 0, 1)
    edges = cv2.convertScaleAbs(sobelx + sobely)
    enhanced = cv2.addWeighted(img, 1.0, edges, edge_weight, 0)
    return enhanced

@register_module("invert polarity")
def invert_polarity(img):
    img = img.astype(np.float32)
    mean = np.mean(img)
    inv = (mean * 2.0) - img
    return np.clip(inv, 0, 255).astype(np.uint8)

@register_module("multi-scale retinex")
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

@register_module("hybrid noise synthesis")
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













#noise generators
def perlin_noise(h, w, scale=50):
    y, x = np.mgrid[0:h, 0:w]
    return cv2.resize(np.random.rand(h//scale, w//scale).astype(np.float32),
                      (w, h), interpolation=cv2.INTER_CUBIC)

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

