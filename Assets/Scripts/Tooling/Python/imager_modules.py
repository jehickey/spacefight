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

        #MODULES.append({
        #    "name": name,
        #    "parameters": params
        #})
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
    
@register_module("gaussian_blur")
def module_gaussian(img, size=5, sigma=0.25):
    #print (f"Applying Gaussian blur with size={size} and sigma={sigma}")
    return cv2.GaussianBlur(img, (size, size), sigma)

@register_module("wavelet_boost")
def module_wavelet_boost(img):
    low = cv2.GaussianBlur(img, (0,0), sigmaX=5)
    mid = cv2.GaussianBlur(img, (0,0), sigmaX=2)
    high = img - mid
    mid_detail = mid - low
    return img + 0.5*mid_detail + 1.0*high
