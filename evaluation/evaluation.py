
#!/usr/bin/env python3
import os
import re
import math
import csv
from pathlib import Path

import numpy as np
from PIL import Image
import matplotlib.pyplot as plt

# -------------------------
# CONFIG
# -------------------------

# Folder containing all your Unity renders for ONE scene / setting
IMAGE_DIR = Path("experiment_1", "renders")

# Which file is the "ground truth"?
REFERENCE_SAMPLER = "LDBN"
REFERENCE_SAMPLES = 576   # your chosen "ground truth" sample count

# If images are sRGB PNG/JPG from Unity, do we linearize?
ASSUME_SRGB = True

# Hardcoded list of sample counts to compare.
# If empty or None, ALL sample counts are used.
ALLOWED_SAMPLES = [64, 256, 576]

FILENAME_RE = re.compile(
    r"^(?P<samples>\d+)_samples_(?P<sampler>[A-Za-z0-9]+)_.*\.(png|jpg|jpeg)$"
)

# helpers
def load_image_as_float(path: Path) -> np.ndarray:
    """
    Load image as float32 array in [0,1], shape (H,W,3).
    Converts to RGB if needed.
    Optionally converts from sRGB to linear.
    """
    img = Image.open(path).convert("RGB")
    arr = np.asarray(img, dtype=np.float32) / 255.0  # [0,1]

    if ASSUME_SRGB:
        # simple sRGB -> linear approximation
        # (good enough for our eval purposes)
        arr = np.where(
            arr <= 0.04045,
            arr / 12.92,
            ((arr + 0.055) / 1.055) ** 2.4,
        )

    return arr


def compute_metrics(img: np.ndarray, ref: np.ndarray) -> dict:
    """
    Compute MSE, RMSE, PSNR, and error variance between
    img and reference (both shape (H,W,3) in linear [0,1]).
    """
    assert img.shape == ref.shape, f"Shape mismatch: {img.shape} vs {ref.shape}"
    # error per channel
    diff = img - ref
    sq = diff ** 2

    mse = float(np.mean(sq))
    rmse = float(math.sqrt(mse))

    # error variance (how spread-out is the error)
    err_var = float(np.var(diff))

    # PSNR in dB
    if mse == 0:
        psnr = float("inf")
    else:
        psnr = 10.0 * math.log10(1.0 / mse)  # assuming max=1.0 in linear

    return {
        "mse": mse,
        "rmse": rmse,
        "err_var": err_var,
        "psnr": psnr,
    }


def parse_filename(path: Path):
    """
    Attempt to extract (sampler, samples) from filename.
    Expects something like '256_samples_LDBN_....png'.
    """
    m = FILENAME_RE.match(path.name)
    if not m:
        return None

    samples = int(m.group("samples"))
    sampler = m.group("sampler")
    return sampler, samples


def main():
    # tag used in output filenames
    if ALLOWED_SAMPLES:
        samples_tag = "_".join(str(s) for s in sorted(set(ALLOWED_SAMPLES)))
    else:
        samples_tag = "all"

    # 1. Scan all candidate images
    entries = []
    for fname in os.listdir(IMAGE_DIR):
        fpath = IMAGE_DIR / fname
        if not fpath.is_file():
            continue
        if not fname.lower().endswith((".png", ".jpg", ".jpeg")):
            continue

        parsed = parse_filename(fpath)
        if parsed is None:
            print(f"[WARN] Skipping file (name pattern not recognized): {fname}")
            continue

        sampler, samples = parsed

        # Filter by ALLOWED_SAMPLES if specified
        if ALLOWED_SAMPLES and samples not in ALLOWED_SAMPLES:
            print(f"[INFO] Skipping {fname} (samples={samples} not in ALLOWED_SAMPLES={ALLOWED_SAMPLES})")
            continue

        entries.append({
            "path": fpath,
            "sampler": sampler,
            "samples": samples,
        })

    if not entries:
        print("No valid images found in", IMAGE_DIR, "after applying ALLOWED_SAMPLES filter.")
        return

    # 2. Find reference image: LDBN with REFERENCE_SAMPLES
    ref_entry = None
    for e in entries:
        if e["sampler"] == REFERENCE_SAMPLER and e["samples"] == REFERENCE_SAMPLES:
            ref_entry = e
            break

    if ref_entry is None:
        raise RuntimeError(
            f"No reference image found for sampler={REFERENCE_SAMPLER}, "
            f"samples={REFERENCE_SAMPLES} within filtered entries.\n"
            f"Check that REFERENCE_SAMPLES is included in ALLOWED_SAMPLES "
            f"and that the file exists."
        )

    print(f"[INFO] Using reference image: {ref_entry['path']}")

    ref_img = load_image_as_float(ref_entry["path"])

    # 3. Compute metrics for each image
    results = []
    for e in entries:
        img = load_image_as_float(e["path"])
        metrics = compute_metrics(img, ref_img)

        row = {
            "sampler": e["sampler"],
            "samples": e["samples"],
            "path": str(e["path"]),
            **metrics,
        }
        results.append(row)

        print(
            f"{e['sampler']:10s} N={e['samples']:4d}  "
            f"RMSE={metrics['rmse']:.6f}  "
            f"PSNR={metrics['psnr']:.2f} dB"
        )

    # 4. Save CSV (tagged by sample counts)
    csv_path = IMAGE_DIR / f"metrics_samples_{samples_tag}.csv"
    with open(csv_path, "w", newline="") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=["sampler", "samples", "mse", "rmse", "err_var", "psnr", "path"],
        )
        writer.writeheader()
        for r in results:
            writer.writerow(r)

    print(f"[INFO] Metrics written to {csv_path}")

    # 5. Plot RMSE vs N for each sampler (only for ALLOWED_SAMPLES)
    plot_rmse_vs_samples(results, IMAGE_DIR / f"rmse_vs_samples_{samples_tag}.png")


def plot_rmse_vs_samples(results, out_path: Path):
    # Group by sampler
    samplers = sorted({r["sampler"] for r in results})

    plt.figure()
    for sampler in samplers:
        # subset for this sampler
        pts = [r for r in results if r["sampler"] == sampler]
        pts_sorted = sorted(pts, key=lambda r: r["samples"])

        Ns = [p["samples"] for p in pts_sorted]
        RMSEs = [p["rmse"] for p in pts_sorted]

        # log-log plot is nice for convergence rate
        plt.loglog(Ns, RMSEs, marker="o", label=sampler)

    plt.xlabel("Samples per pixel")
    plt.ylabel(f"RMSE vs {REFERENCE_SAMPLER}@{REFERENCE_SAMPLES}")
    plt.title("DoF Sampler Convergence (RMSE vs sample count)")
    plt.grid(True, which="both", ls="--", alpha=0.3)
    plt.legend()
    plt.tight_layout()
    plt.savefig(out_path, dpi=200)
    print(f"[INFO] RMSE plot saved to {out_path}")


if __name__ == "__main__":
    main()
