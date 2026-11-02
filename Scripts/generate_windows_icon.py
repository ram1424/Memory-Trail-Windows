from __future__ import annotations

from pathlib import Path
import sys
from PIL import Image

ICO_SIZES = [
    (256, 256), (128, 128), (64, 64), (48, 48), (40, 40),
    (32, 32), (24, 24), (20, 20), (16, 16)
]

def main():
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    assets = root / "Assets"
    master_path = assets / "MemoryTrailIconMaster.png"

    if not master_path.exists():
        raise FileNotFoundError(f"Missing master icon: {master_path}")

    with Image.open(master_path) as source:
        image = source.convert("RGBA")
        if image.width != image.height:
            raise RuntimeError("Memory Trail master icon must be square.")

        # The master artwork is the single source of truth.
        preview = image.resize((256, 256), Image.Resampling.LANCZOS)
        onboarding = image.resize((512, 512), Image.Resampling.LANCZOS)

        preview.save(assets / "MemoryTrailIcon.png", "PNG", optimize=True)
        onboarding.save(assets / "OnboardingIcon.png", "PNG", optimize=True)
        preview.save(
            assets / "MemoryTrailApp.ico",
            "ICO",
            sizes=ICO_SIZES,
            bitmap_format="png"
        )

    with Image.open(assets / "MemoryTrailApp.ico") as icon:
        available_sizes = set(icon.ico.sizes())

    missing = set(ICO_SIZES) - available_sizes
    if missing:
        raise RuntimeError(f"Missing ICO sizes: {sorted(missing)}")

    print("Generated Windows icon assets from Assets/MemoryTrailIconMaster.png")
    print(f"ICO sizes: {sorted(available_sizes)}")

if __name__ == "__main__":
    main()
