from __future__ import annotations

from pathlib import Path
import shutil
import sys
from PIL import Image

ICO_SIZES = [
    (256, 256),
    (128, 128),
    (64, 64),
    (48, 48),
    (40, 40),
    (32, 32),
    (24, 24),
    (20, 20),
    (16, 16),
]


def main() -> None:
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    assets = root / "Assets"
    master_path = assets / "MemoryTrailIconMaster.png"

    if not master_path.exists():
        raise RuntimeError(f"Approved master icon is missing: {master_path}")

    # Keep every PNG branding surface byte-for-byte on the approved artwork.
    shutil.copyfile(master_path, assets / "MemoryTrailIcon.png")
    shutil.copyfile(master_path, assets / "OnboardingIcon.png")

    with Image.open(master_path) as source:
        if source.width != source.height:
            raise RuntimeError("Memory Trail master icon must be square.")

        if source.width < 256:
            raise RuntimeError(
                f"Memory Trail master icon is too small ({source.width}x{source.height}); "
                "the approved source must be at least 256x256."
            )

        image = source.convert("RGBA")
        source_size = (source.width, source.height)

    image.save(
        assets / "MemoryTrailApp.ico",
        "ICO",
        sizes=ICO_SIZES,
        bitmap_format="png",
    )

    with Image.open(assets / "MemoryTrailApp.ico") as icon:
        available = set(icon.ico.sizes())

    missing = set(ICO_SIZES) - available
    if missing:
        raise RuntimeError(f"Missing ICO sizes: {sorted(missing)}")

    print(
        "Generated Memory Trail Windows branding directly from "
        f"{master_path.name}; source={source_size[0]}x{source_size[1]}; "
        f"ICO sizes={sorted(available)}"
    )


if __name__ == "__main__":
    main()
