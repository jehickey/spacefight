#!/usr/bin/env python3

import cv2
from dataclasses import dataclass

@dataclass
class Module:
    name: str
    func: callable
    params: dict


def module_upscale():
    return 

#@register_module("retinex")
def module_upscale(img, width, height):
    return upscale_image(img, width, height)

#@register_module("hybrid_noise")
def module_gaussian(img, size, sigma):
    return smooth_gaussian(img, size, sigma)


def upscale_image(im, width, height):
    img = cv2.resize(img, (width,height), interpolation=cv2.INTER_LANCZOS4)        
    return img

def smooth_gaussian(img, size, sigma):
    return cv2.GaussianBlur(img, (size, size), sigma)

def smooth_bilaterialFilter (img, d, sigmaColor, sigmaSpace):
    return cv2.bilateralFilter(img, d=d, sigmaColor=sigmaColor, sigmaSpace=sigmaSpace)

MODULES = [
    Module("upscale", module_upscale, {"width":1024, "height":1024}),
    Module("gaussian blur", module_gaussian, {"size":5, "sigma":0.25}),
]


print (MODULES)

