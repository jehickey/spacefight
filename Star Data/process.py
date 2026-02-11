import math
import csv

# Adjust this if your file is named differently
INPUT_FILE = "catalog.txt"
OUTPUT_FILE = "bsc_processed.csv"

# Column offsets (1-based in documentation, 0-based in Python)
RA_COL = (75, 83)     # HHMMSS.s
DEC_COL = (83, 91)    # ±DDMMSS
MAG_COL = (102, 107)  # V magnitude

def parse_ra(ra_str):
    # Example: "000001.1"
    hours = int(ra_str[0:2])
    minutes = int(ra_str[2:4])
    seconds = float(ra_str[4:])
    return (hours + minutes/60 + seconds/3600) * (math.pi / 12)

def parse_dec(dec_str):
    # Example: "+444022"
    sign = -1 if dec_str[0] == '-' else 1
    degrees = int(dec_str[1:3])
    minutes = int(dec_str[3:5])
    seconds = int(dec_str[5:7])
    return sign * (degrees + minutes/60 + seconds/3600) * (math.pi / 180)

def ra_dec_to_vector(ra, dec):
    x = math.cos(dec) * math.cos(ra)
    y = math.cos(dec) * math.sin(ra)
    z = math.sin(dec)
    return x, y, z

with open(INPUT_FILE, "r") as infile, open(OUTPUT_FILE, "w", newline="") as outfile:
    writer = csv.writer(outfile)
    writer.writerow(["x", "y", "z", "magnitude"])

    for line in infile:
        ra_raw = line[RA_COL[0]:RA_COL[1]].strip()
        dec_raw = line[DEC_COL[0]:DEC_COL[1]].strip()
        mag_raw = line[MAG_COL[0]:MAG_COL[1]].strip()

        if not ra_raw or not dec_raw or not mag_raw:
            continue

        try:
            ra = parse_ra(ra_raw)
            dec = parse_dec(dec_raw)
            mag = float(mag_raw)
        except ValueError:
            continue

        x, y, z = ra_dec_to_vector(ra, dec)
        writer.writerow([x, y, z, mag])

print("Processing complete. Output written to", OUTPUT_FILE)