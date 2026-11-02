from __future__ import annotations

from pathlib import Path
import sys
from PIL import Image

SIZES = [
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


def main() -> int:
    root = (
        Path(sys.argv[1]).resolve()
        if len(sys.argv) > 1
        else Path(__file__).resolve().parents[1]
    )
    assets = root / "Assets"
    source = assets / "MemoryTrailIconMaster.png"
    ico_path = assets / "MemoryTrailApp.ico"
    preview_path = assets / "MemoryTrailIcon.png"

    if not source.is_file():
        raise FileNotFoundError(f"Missing icon master: {source}")

    with Image.open(source) as opened:
        image = opened.convert("RGBA")

    if image.width != image.height:
        side = max(image.width, image.height)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.alpha_composite(
            image,
            ((side - image.width) // 2, (side - image.height) // 2),
        )
        image = canvas

    if image.width < 512:
        image = image.resize((512, 512), Image.Resampling.LANCZOS)

    # Keep a healthy PNG copy for WPF, onboarding, and tray rendering.
    image.save(preview_path, format="PNG", optimize=True)

    # Pillow writes a standards-compliant multi-image ICO directory. The
    # previous hand-built ICO mixed frame offsets and produced static/noise in
    # the lower half when Windows selected certain resolutions.
    image.save(
        ico_path,
        format="ICO",
        sizes=SIZES,
        bitmap_format="png",
    )

    with Image.open(ico_path) as icon:
        available = set(icon.ico.sizes())

    required = set(SIZES)
    missing = required - available
    if missing:
        raise RuntimeError(
            f"Generated ICO is missing required sizes: {sorted(missing)}"
        )

    print(f"Generated {ico_path} ({ico_path.stat().st_size} bytes)")
    print(
        "ICO sizes: "
        + ", ".join(
            f"{width}x{height}"
            for width, height in sorted(available, reverse=True)
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
