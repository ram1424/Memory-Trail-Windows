from __future__ import annotations

from pathlib import Path
import shutil
import struct
import sys


def main() -> None:
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    assets = root / "Assets"
    master = assets / "MemoryTrailIconMaster.png"

    if not master.exists():
        raise RuntimeError(f"Approved master icon is missing: {master}")

    png = master.read_bytes()
    png_signature = b"\x89PNG\r\n\x1a\n"
    if not png.startswith(png_signature):
        raise RuntimeError("MemoryTrailIconMaster.png is not a valid PNG file.")

    # PNG IHDR stores width/height big-endian at byte offsets 16 and 20.
    width = int.from_bytes(png[16:20], "big")
    height = int.from_bytes(png[20:24], "big")
    if width != height or width < 256:
        raise RuntimeError(
            f"Approved Memory Trail master icon must be square and at least 256px; got {width}x{height}."
        )

    # Every PNG branding surface uses the exact same approved bytes.
    shutil.copyfile(master, assets / "MemoryTrailIcon.png")
    shutil.copyfile(master, assets / "OnboardingIcon.png")

    # Standards-compliant ICO containing the approved 256px PNG payload.
    # Windows natively scales a PNG-compressed 256px ICO entry for shell sizes.
    header = struct.pack("<HHH", 0, 1, 1)
    entry = struct.pack(
        "<BBBBHHII",
        0,  # 0 means 256px in ICO directory entries
        0,
        0,
        0,
        1,
        32,
        len(png),
        22,
    )
    ico = header + entry + png
    (assets / "MemoryTrailApp.ico").write_bytes(ico)

    print(
        f"Generated Memory Trail branding from approved {width}x{height} master: "
        f"ICO bytes={len(ico)}"
    )


if __name__ == "__main__":
    main()
