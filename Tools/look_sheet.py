"""Fix 10.1, item 3: one frame per look-at moment of each rendered cutscene (Recordings/<Scene>.mp4) -> Data/screenshots/fix101/looks.jpg.
python Tools/look_sheet.py   (render the scenes first: python Tools/render_cutscene.py <Scene> ...)"""
import pathlib, sys, tempfile, shutil
from PIL import Image, ImageDraw
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import film_sheet as fs

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOOKS = [
    ('TheStone', 12.0, 'Muscle Man to Dan: your turn'), ('TheStone', 32.0, 'Dan looks at him laughing'), ('TheStone', 34.4, '"My hero!" at Dan'),
    ('PirateArrival', 12.55, 'Dan drops into his barrel'), ('PirateArrival', 13.05, "Kira's barrel"), ('PirateArrival', 13.45, "Abi's barrel"),
    ('PirateArrival', 23.5, 'all three turn to Muscle Man'), ('PirateArrival', 31.6, 'Dan glances at Abi'),
    ('SphinxGag', 9.0, 'Dan looks down at Kira'), ('SphinxGag', 16.2, 'the look: Abi at Dan, Dan at Kira'),
    ('WiseMenArrive', 9.9, 'the Wise Men stare at Dan'),
    ('IceAgeArrival', 3.8, 'stunned: at the sabertooth'), ('IceAgeArrival', 19.0, 'Abi looks at Dan'),
    ('EgyptArrival', 22.4, "Where's Kira? (each other)"), ('EgyptArrival', 31.0, 'at mummy Kira'),
    ('BethlehemArrival', 19.5, 'Abi looks at Dan'),
    ('MedievalArrival', 17.5, 'they pop up: each other'), ('MedievalArrival', 22.5, 'at Kira (achoo)'), ('MedievalArrival', 28.5, 'watching the wagon'),
]

def main():
    tmp = pathlib.Path(tempfile.mkdtemp(prefix='looks_'))
    w = 480; th = int(w * 9 / 16); cols = 4; rows = (len(LOOKS) + cols - 1) // cols
    out = Image.new('RGB', (cols * w, 40 + rows * (th + 30)), (24, 24, 24)); d = ImageDraw.Draw(out)
    d.text((10, 8), 'Fix 10.1: look-at in the cutscenes (the game, rendered from the exe)', fill=(255, 255, 255), font=fs.font(22))
    for i, (scene, t, what) in enumerate(LOOKS):
        x = (i % cols) * w; y = 40 + (i // cols) * (th + 30)
        v = ROOT / 'Recordings' / f'{scene}.mp4'
        g = fs.grab(v, t, tmp / f'{i}.png', w) if v.exists() else None
        g = (g or Image.new('RGB', (w, th), (60, 0, 0))).resize((w, th))
        out.paste(fs.label(g, f'{scene} {t:.1f}s', 14), (x, y + 26)); d.text((x + 6, y + 4), what, fill=(255, 220, 120), font=fs.font(17))
    dst = ROOT / 'Data' / 'screenshots' / 'fix101' / 'looks.jpg'; dst.parent.mkdir(parents=True, exist_ok=True); out.save(dst, quality=88)
    shutil.rmtree(tmp, ignore_errors=True); print(dst.relative_to(ROOT))

if __name__ == '__main__':
    main()
