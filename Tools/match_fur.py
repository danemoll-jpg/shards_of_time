"""Shards of Time, Fix 3.1: make a Kira form's fur the same gray as home Kira's (she is the same cat in every form).

python Tools/match_fur.py kira_sabertooth [--to kira]

Reads SourceArt/Meshy/<name>/base_color.png (Meshy's texture; the first run keeps it as base_color_meshy.png and always starts
from that), finds the fur (the low-colour pixels in the main gray band of each texture) and moves its colour and brightness to
home Kira's fur average. Eyes, nose, fangs and anything else away from the fur colour are left alone (soft edge). Writes
base_color.png (Tools/Blender/meshy_cleanup.py and Tools/import_characters.py use it) and prints the fur colours before / after.
"""
import argparse, shutil
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
MESHY = ROOT / "SourceArt" / "Meshy"


def ycc(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    y = .299 * r + .587 * g + .114 * b
    return np.stack([y, (b - y) * .564, (r - y) * .713], -1)


def rgb(c):
    y, cb, cr = c[..., 0], c[..., 1] / .564, c[..., 2] / .713
    r = y + cr; b = y + cb; g = (y - .299 * r - .114 * b) / .587
    return np.stack([r, g, b], -1)


def fur(c):
    """The fur: low colour, and brightness in the middle band (no near-black pupils, no near-white fangs)."""
    chroma = np.hypot(c[..., 1], c[..., 2])
    y = c[..., 0]
    m = (chroma < np.percentile(chroma, 60)) & (y > .12) & (y < .8)
    return m, c[m].mean(0), c[m][:, 0].std()


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("name"); ap.add_argument("--to", default="kira"); a = ap.parse_args()
    d = MESHY / a.name
    orig = d / "base_color_meshy.png"
    if not orig.exists(): shutil.copy2(d / "base_color.png", orig)
    src = np.asarray(Image.open(orig).convert("RGB"), np.float32) / 255
    ref = np.asarray(Image.open(MESHY / a.to / "base_color.png").convert("RGB"), np.float32) / 255
    cs, cr = ycc(src), ycc(ref)
    _, mean_s, sd_s = fur(cs)
    _, mean_r, sd_r = fur(cr)
    # weight: 1 near the source fur colour, 0 far from it (eyes, pink nose), soft in between
    dist = np.hypot(cs[..., 1] - mean_s[1], cs[..., 2] - mean_s[2])
    w = np.clip(1 - (dist - .03) / .05, 0, 1)
    w *= np.clip((cs[..., 0] - .04) / .06, 0, 1) * np.clip((.92 - cs[..., 0]) / .08, 0, 1)
    out = cs.copy()
    out[..., 0] = cs[..., 0] + w * ((cs[..., 0] - mean_s[0]) * (sd_r / sd_s) + mean_r[0] - cs[..., 0])
    out[..., 1:] = cs[..., 1:] + w[..., None] * (mean_r[1:] - mean_s[1:])
    res = np.clip(rgb(out), 0, 1)
    Image.fromarray((res * 255 + .5).astype(np.uint8)).save(d / "base_color.png")
    _, mean_o, _ = fur(ycc(res))
    f = lambda m: "rgb(" + ",".join(f"{v * 255:.0f}" for v in rgb(m[None])[0]) + ")"
    print(f"{a.name} fur {f(mean_s)} -> {f(mean_o)}; {a.to} fur {f(mean_r)}; {w.mean() * 100:.0f} % of the texture moved")


if __name__ == "__main__":
    main()
