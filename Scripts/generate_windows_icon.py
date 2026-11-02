from __future__ import annotations

from pathlib import Path
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

    with Image.open(master_path) as source:
        image = source.convert("RGBA")

        if image.width != image.height:
            raise RuntimeError("Memory Trail master icon must be square.")

        if image.width < 512:
            raise RuntimeError(
                f"Memory Trail master icon is too small ({image.width}x{image.height}); "
                "use the approved high-resolution master."
            )

        image.resize((256, 256), Image.Resampling.LANCZOS).save(
            assets / "MemoryTrailIcon.png",
            "PNG",
            optimize=True,
        )
        image.resize((512, 512), Image.Resampling.LANCZOS).save(
            assets / "OnboardingIcon.png",
            "PNG",
            optimize=True,
        )
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
        f"{master_path.name}; ICO sizes={sorted(available)}"
    )


if __name__ == "__main__":
    main()
