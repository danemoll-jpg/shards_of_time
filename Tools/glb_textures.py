"""Shards of Time, Round 11: pull a GLB's embedded textures out as Meshy-style files (Tripo's models carry them inside; the rig, closed-mesh
and static-export tools read SourceArt/Meshy/<name>/base_color.png, normal.png, roughness.png, metallic.png).

python Tools/glb_textures.py <name> [<name> ...]     (reads SourceArt/Meshy/<name>/model.glb)
"""
import json, struct, sys, io
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent


def images(glb):
    data = glb.read_bytes()
    _, _, _ = struct.unpack_from("<III", data, 0)
    off, js, binc = 12, None, None
    while off < len(data):
        ln, typ = struct.unpack_from("<II", data, off); chunk = data[off + 8: off + 8 + ln]; off += 8 + ln
        if typ == 0x4E4F534A: js = json.loads(chunk)
        elif typ == 0x004E4942: binc = chunk
    def img(i):
        im = js["images"][js["textures"][i]["source"]]; bv = js["bufferViews"][im["bufferView"]]
        return Image.open(io.BytesIO(binc[bv.get("byteOffset", 0): bv.get("byteOffset", 0) + bv["byteLength"]]))
    m = js["materials"][0]; pbr = m.get("pbrMetallicRoughness", {}); out = {}
    if "baseColorTexture" in pbr: out["base_color"] = img(pbr["baseColorTexture"]["index"]).convert("RGB")
    if "normalTexture" in m: out["normal"] = img(m["normalTexture"]["index"]).convert("RGB")
    if "metallicRoughnessTexture" in pbr:
        mr = img(pbr["metallicRoughnessTexture"]["index"]).convert("RGB"); r, g, b = mr.split()
        out["roughness"] = g.convert("RGB"); out["metallic"] = b.convert("RGB")
    return out


for n in sys.argv[1:]:
    d = ROOT / "SourceArt" / "Meshy" / n
    got = images(d / "model.glb")
    for k, im in got.items(): im.save(d / f"{k}.png")
    print(n, "textures:", ", ".join(f"{k} {im.size[0]}x{im.size[1]}" for k, im in got.items()))
