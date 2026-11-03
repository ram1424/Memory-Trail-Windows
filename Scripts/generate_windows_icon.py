from __future__ import annotations

from pathlib import Path
import struct
import sys


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
REQUIRED_ICO_SIZES = {16, 32, 64, 128, 256}


def png_dimensions(path: Path) -> tuple[int, int]:
    raw = path.read_bytes()
    if not raw.startswith(PNG_SIGNATURE) or len(raw) < 24:
        raise RuntimeError(f"Invalid PNG asset: {path}")
    return int.from_bytes(raw[16:20], "big"), int.from_bytes(raw[20:24], "big")


def ico_sizes(path: Path) -> set[int]:
    raw = path.read_bytes()
    if len(raw) < 6:
        raise RuntimeError(f"Invalid ICO asset: {path}")

    reserved, icon_type, count = struct.unpack_from("<HHH", raw, 0)
    if reserved != 0 or icon_type != 1 or count < 1:
        raise RuntimeError(f"Invalid ICO header: {path}")

    sizes: set[int] = set()
    for index in range(count):
        offset = 6 + index * 16
        if offset + 16 > len(raw):
            raise RuntimeError(f"Truncated ICO directory: {path}")
        width = raw[offset] or 256
        height = raw[offset + 1] or 256
        if width != height:
            raise RuntimeError(f"Non-square ICO entry {width}x{height}: {path}")
        sizes.add(width)

    return sizes


def main() -> None:
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    assets = root / "Assets"

    master = assets / "MemoryTrailIconMaster.png"
    preview = assets / "MemoryTrailIcon.png"
    onboarding = assets / "OnboardingIcon.png"
    ico = assets / "MemoryTrailApp.ico"

    for path in (master, preview, onboarding, ico):
        if not path.exists():
            raise RuntimeError(f"Required branding asset is missing: {path}")

    master_size = png_dimensions(master)
    preview_size = png_dimensions(preview)
    onboarding_size = png_dimensions(onboarding)

    if master_size != (512, 512):
        raise RuntimeError(f"Expected exact 512x512 approved master artwork; got {master_size}")
    if preview_size != (256, 256):
        raise RuntimeError(f"Expected 256x256 UI icon; got {preview_size}")
    if onboarding_size != (512, 512):
        raise RuntimeError(f"Expected 512x512 onboarding icon; got {onboarding_size}")

    sizes = ico_sizes(ico)
    missing = REQUIRED_ICO_SIZES - sizes
    if missing:
        raise RuntimeError(f"Windows ICO is missing native sizes: {sorted(missing)}")

    print(
        "Verified exact Memory Trail artwork and multi-resolution Windows ICO: "
        f"master={master_size[0]}px, ico={sorted(sizes)}"
    )


if __name__ == "__main__":
    main()
