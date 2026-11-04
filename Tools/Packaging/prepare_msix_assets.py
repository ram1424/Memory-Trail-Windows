from pathlib import Path
import sys
from PIL import Image

root = Path(sys.argv[1]).resolve()
out = Path(sys.argv[2]).resolve()
source = root / "Assets" / "MemoryTrailIconMaster.png"
if not source.exists():
    raise SystemExit(f"Missing approved master icon: {source}")
out.mkdir(parents=True, exist_ok=True)
img = Image.open(source).convert("RGBA")
targets = {
    "Square44x44Logo.png": (44, 44),
    "Square150x150Logo.png": (150, 150),
    "StoreLogo.png": (50, 50),
}
for name, size in targets.items():
    img.resize(size, Image.Resampling.LANCZOS).save(out / name, optimize=True)
print("Generated MSIX visual assets:", ", ".join(targets))
