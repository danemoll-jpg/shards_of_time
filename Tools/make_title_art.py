"""Shards of Time (Round 1, item 3): placeholder title art made from a game screenshot, until Dan's own key art exists.

python Tools/make_title_art.py <menu-free screenshot.png>

Writes (all plain PNG, the game's name painted on in code):
  SourceArt/Title/Steam/ShardsOfTime-Cover.png     600 x 900  the launcher's cover (= Portrait)
  SourceArt/Title/Steam/ShardsOfTime-Portrait.png  600 x 900  Steam library capsule
  SourceArt/Title/Steam/ShardsOfTime-Wide.png      920 x 430  Steam wide capsule
  SourceArt/Title/Steam/ShardsOfTime-Hero.png     1920 x 620  Steam hero (no title on it: Steam draws the logo over it)
  SourceArt/Title/Steam/ShardsOfTime-Logo.png      transparent logo
  SourceArt/Title/Steam/ShardsOfTime-Icon.png      256 x 256  the shard icon
  Launcher/ShardsOfTime.ico                        the launcher's icon
  Assets/HandMade/Title/GameIcon.png               the game exe's icon (PlayerBuild)
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
STEAM = ROOT / "SourceArt" / "Title" / "Steam"
FONT = "C:/Windows/Fonts/arialbd.ttf"
GOLD, INK = (255, 226, 140), (30, 22, 60)


def font(size):
    try: return ImageFont.truetype(FONT, size)
    except OSError: return ImageFont.load_default()


def cover(img, w, h):
    s = max(w / img.width, h / img.height)
    r = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
    x, y = (r.width - w) // 2, (r.height - h) // 2
    return r.crop((x, y, x + w, y + h))


def title(img, size, y, lines=("SHARDS", "OF TIME")):
    d = ImageDraw.Draw(img)
    f = font(size)
    for i, line in enumerate(lines):
        tw = d.textlength(line, font=f)
        x = (img.width - tw) / 2; yy = y + i * size * 1.05
        for dx, dy in ((-4, 0), (4, 0), (0, -4), (0, 4), (-3, -3), (3, 3), (-3, 3), (3, -3)):
            d.text((x + dx, yy + dy), line, font=f, fill=INK)
        d.text((x, yy), line, font=f, fill=GOLD)
    return img


def shard_icon(n):
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((n * .04, n * .04, n * .96, n * .96), radius=n * .2, fill=(40, 52, 120, 255))
    cx, cy = n / 2, n / 2
    glow = Image.new("RGBA", (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(glow)
    g.ellipse((cx - n * .34, cy - n * .34, cx + n * .34, cy + n * .34), fill=(110, 220, 255, 120))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(n * .06)))
    top, bot, l, r = (cx, n * .12), (cx, n * .9), (cx - n * .2, n * .46), (cx + n * .2, n * .46)
    d = ImageDraw.Draw(img)
    d.polygon([top, r, bot, l], fill=(120, 225, 255, 255))
    d.polygon([top, (cx, n * .46), bot, l], fill=(80, 180, 240, 255))
    d.polygon([top, r, (cx, n * .46)], fill=(200, 245, 255, 255))
    return img


def main():
    if len(sys.argv) < 2: sys.exit(__doc__)
    shot = Image.open(sys.argv[1]).convert("RGB")
    STEAM.mkdir(parents=True, exist_ok=True)
    portrait = title(cover(shot, 600, 900), 92, 90)
    portrait.save(STEAM / "ShardsOfTime-Portrait.png"); portrait.save(STEAM / "ShardsOfTime-Cover.png")
    title(cover(shot, 920, 430), 84, 40, ("SHARDS OF TIME",)).save(STEAM / "ShardsOfTime-Wide.png")
    cover(shot, 1920, 620).save(STEAM / "ShardsOfTime-Hero.png")
    logo = Image.new("RGBA", (1280, 360), (0, 0, 0, 0)); title(logo, 150, 20)
    logo.crop(logo.getbbox()).save(STEAM / "ShardsOfTime-Logo.png")
    icon = shard_icon(256); icon.save(STEAM / "ShardsOfTime-Icon.png")
    (ROOT / "Assets" / "HandMade" / "Title").mkdir(parents=True, exist_ok=True)
    icon.save(ROOT / "Assets" / "HandMade" / "Title" / "GameIcon.png")
    icon.save(ROOT / "Launcher" / "ShardsOfTime.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    print("title art written to", STEAM)


if __name__ == "__main__":
    main()
