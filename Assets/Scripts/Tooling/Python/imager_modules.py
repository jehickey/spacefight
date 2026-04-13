import numpy as np
import cv2

MODULES = {}

def register_module(name):
    def decorator(func):
        MODULES[name] = {
            "func": func,
            "params": func.__defaults__,
            "param_names": func.__code__.co_varnames[1:1+len(func.__defaults__)]
        }
        return func
    return decorator


@register_module("upscale")
def module_upscale(img, width=1025, height=1024):
    print (f"Upscaling image to {width}x{height}")
    return cv2.resize(img, (width,height), interpolation=cv2.INTER_LANCZOS4)        
    
@register_module("gaussian_blur")
def module_gaussian(img, size=5, sigma=0.25):
    print (f"Applying Gaussian blur with size={size} and sigma={sigma}")
    return cv2.GaussianBlur(img, (size, size), sigma)

@register_module("wavelet_boost")
def module_wavelet_boost(img):
    low = cv2.GaussianBlur(img, (0,0), sigmaX=5)
    mid = cv2.GaussianBlur(img, (0,0), sigmaX=2)
    high = img - mid
    mid_detail = mid - low
    return img + 0.5*mid_detail + 1.0*high
