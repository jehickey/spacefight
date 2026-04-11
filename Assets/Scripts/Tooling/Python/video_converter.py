#!/usr/bin/env python3
import sys
import subprocess
import os
import traceback

print ("Test")
#sys.exit(1);

DEFAULT_OUTPUT_DIR = r"C:\Users\ehickey\Documents\Unity\spacefight\Assets\Videos"

UNITY_PRESET = [
    "-c:v", "libx264",
    "-profile:v", "baseline",
    "-level", "3.0",
    "-x264-params", "bframes=0:ref=1:force-cfr=1",
    "-vf", "setpts=PTS-STARTPTS",
    "-pix_fmt", "yuv420p",
    "-fps_mode", "cfr",
    "-an",
    "-r", "15",
    "-y",           #force overwrite (no Y/N)
    "-nostdin"      #no reading input
]

def process_file(input_path, output_dir):
    base = os.path.splitext(os.path.basename(input_path))[0]
    output_path = os.path.join(output_dir, f"{base}_unity.mp4")

    cmd = ["ffmpeg", "-fflags", "+genpts", "-i", input_path] + UNITY_PRESET + [output_path]

    try:
        proc = subprocess.run(cmd, capture_output=True, text=True)
        print(proc.stdout)
        if proc.stderr:
            print(proc.stderr, file=sys.stderr)
        return proc.returncode, output_path
    except Exception:
        traceback.print_exc()
        return 1, None

def main():
    if len(sys.argv) <= 1:
        print("Drag a video file onto this script, or run:")
        print("    python unity_encode.py inputfile")
        #input("Press Enter to exit...")
        return

    inputs = sys.argv[1:]
    os.makedirs(DEFAULT_OUTPUT_DIR, exist_ok=True)

    for f in inputs:
        code, outpath = process_file(f, DEFAULT_OUTPUT_DIR)
        print(f"Processed: {f} - {outpath} (code {code})")

    #input("Done. Press Enter to exit...")

if __name__ == "__main__":
    main()
