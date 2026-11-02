from __future__ import annotations

from pathlib import Path
import math
import sys
from PIL import Image, ImageDraw, ImageFilter

CANVAS = 1024
SCALE = 2
W = H = CANVAS * SCALE
ICO_SIZES = [
    (256, 256), (128, 128), (64, 64), (48, 48), (40, 40),
    (32, 32), (24, 24), (20, 20), (16, 16)
]


def sc(value: float) -> int:
    return int(round(value * SCALE))


def linear_gradient(size, c1, c2, horizontal=False):
    width, height = size
    image = Image.new("RGBA", size)
    pixels = image.load()
    length = width if horizontal else height
    for index in range(length):
        t = index / max(1, length - 1)
        color = tuple(int(c1[channel] * (1 - t) + c2[channel] * t) for channel in range(4))
        if horizontal:
            for y in range(height):
                pixels[index, y] = color
        else:
            for x in range(width):
                pixels[x, index] = color
    return image


def rounded_mask(size, radius):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        (0, 0, size[0] - 1, size[1] - 1), radius=radius, fill=255
    )
    return mask


def cubic(p0, p1, p2, p3, steps=100):
    points = []
    for index in range(steps + 1):
        t = index / steps
        u = 1 - t
        x = u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0]
        y = u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]
        points.append((sc(x), sc(y)))
    return points


def draw_icon_symbol(draw, kind, box, color=(255, 255, 255, 245), width=7):
    x0, y0, x1, y1 = box
    center_x = (x0 + x1) // 2
    center_y = (y0 + y1) // 2
    width_box = x1 - x0
    height_box = y1 - y0
    line_width = max(2, sc(width / 2))

    if kind == "globe":
        radius = min(width_box, height_box) * 0.32
        draw.ellipse(
            (center_x - radius, center_y - radius, center_x + radius, center_y + radius),
            outline=color,
            width=line_width,
        )
        draw.line((center_x - radius, center_y, center_x + radius, center_y), fill=color, width=line_width)
        draw.arc(
            (center_x - radius * 0.55, center_y - radius, center_x + radius * 0.55, center_y + radius),
            80,
            280,
            fill=color,
            width=line_width,
        )
        draw.arc(
            (center_x - radius * 0.55, center_y - radius, center_x + radius * 0.55, center_y + radius),
            260,
            100,
            fill=color,
            width=line_width,
        )
    elif kind == "mail":
        padding = width_box * 0.20
        rectangle = (x0 + padding, y0 + height_box * 0.28, x1 - padding, y1 - height_box * 0.25)
        draw.rounded_rectangle(rectangle, radius=sc(10), outline=color, width=line_width)
        draw.line(
            (rectangle[0], rectangle[1], center_x, center_y + sc(7), rectangle[2], rectangle[1]),
            fill=color,
            width=line_width,
        )
    elif kind == "chat":
        rectangle = (x0 + width_box * 0.17, y0 + height_box * 0.22, x1 - width_box * 0.17, y1 - height_box * 0.28)
        draw.rounded_rectangle(rectangle, radius=sc(16), fill=color)
        draw.polygon(
            [
                (rectangle[0] + width_box * 0.18, rectangle[3]),
                (rectangle[0] + width_box * 0.12, rectangle[3] + height_box * 0.18),
                (rectangle[0] + width_box * 0.35, rectangle[3]),
            ],
            fill=color,
        )
        dot_color = (40, 126, 160, 255)
        for offset in (-0.16, 0, 0.16):
            radius = sc(6)
            draw.ellipse(
                (
                    center_x + width_box * offset - radius,
                    center_y - radius,
                    center_x + width_box * offset + radius,
                    center_y + radius,
                ),
                fill=dot_color,
            )
    elif kind == "folder":
        draw.rounded_rectangle(
            (x0 + width_box * 0.14, y0 + height_box * 0.34, x1 - width_box * 0.12, y1 - height_box * 0.18),
            radius=sc(12),
            fill=color,
        )
        draw.rounded_rectangle(
            (x0 + width_box * 0.20, y0 + height_box * 0.23, x0 + width_box * 0.52, y0 + height_box * 0.47),
            radius=sc(10),
            fill=color,
        )
    elif kind == "calendar":
        rectangle = (x0 + width_box * 0.18, y0 + height_box * 0.20, x1 - width_box * 0.18, y1 - height_box * 0.15)
        draw.rounded_rectangle(rectangle, radius=sc(12), outline=color, width=line_width)
        draw.line(
            (rectangle[0], rectangle[1] + height_box * 0.20, rectangle[2], rectangle[1] + height_box * 0.20),
            fill=color,
            width=line_width,
        )
        for column in range(3):
            for row in range(2):
                radius = sc(5)
                x = rectangle[0] + width_box * (0.18 + 0.20 * column)
                y = rectangle[1] + height_box * (0.40 + 0.18 * row)
                draw.rounded_rectangle(
                    (x - radius, y - radius, x + radius, y + radius),
                    radius=radius // 2,
                    fill=color,
                )


def card_layer(kind, accent, angle=-7):
    layer = Image.new("RGBA", (sc(520), sc(190)), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.rounded_rectangle((sc(30), sc(34), sc(490), sc(170)), radius=sc(34), fill=(4, 19, 70, 100))
    card = (sc(16), sc(12), sc(476), sc(150))
    draw.rounded_rectangle(
        card,
        radius=sc(32),
        fill=(214, 235, 255, 225),
        outline=(255, 255, 255, 220),
        width=sc(3),
    )
    draw.rounded_rectangle((sc(38), sc(31), sc(160), sc(133)), radius=sc(27), fill=accent)
    draw_icon_symbol(draw, kind, (sc(38), sc(31), sc(160), sc(133)))
    draw.rounded_rectangle((sc(190), sc(48), sc(420), sc(68)), radius=sc(10), fill=(91, 133, 205, 180))
    draw.rounded_rectangle((sc(190), sc(87), sc(365), sc(106)), radius=sc(9), fill=(117, 151, 205, 155))
    return layer.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)


def build_icon():
    base = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    background = linear_gradient((W, H), (4, 30, 86, 255), (29, 62, 162, 255))
    atmosphere = linear_gradient((W, H), (0, 190, 255, 90), (184, 59, 255, 115), horizontal=True)
    background = Image.alpha_composite(background, atmosphere)
    mask = rounded_mask((W, H), sc(205))
    frame = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    frame.paste(background, (0, 0), mask)
    base.alpha_composite(frame)

    sun = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sun_draw = ImageDraw.Draw(sun)
    sun_x, sun_y = sc(800), sc(225)
    sun_draw.ellipse(
        (sun_x - sc(95), sun_y - sc(95), sun_x + sc(95), sun_y + sc(95)),
        fill=(255, 232, 105, 255),
    )
    base.alpha_composite(sun.filter(ImageFilter.GaussianBlur(sc(45))))
    base.alpha_composite(sun)

    hills = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hill_draw = ImageDraw.Draw(hills)
    hill_draw.polygon(
        [(0, sc(370)), (sc(220), sc(300)), (sc(420), sc(365)), (sc(620), sc(295)), (sc(820), sc(360)), (W, sc(310)), (W, H), (0, H)],
        fill=(18, 54, 147, 255),
    )
    hill_draw.polygon(
        [(0, sc(520)), (sc(230), sc(430)), (sc(420), sc(515)), (sc(655), sc(390)), (sc(840), sc(505)), (W, sc(440)), (W, H), (0, H)],
        fill=(23, 72, 177, 255),
    )
    hill_draw.polygon(
        [(0, sc(710)), (sc(240), sc(610)), (sc(470), sc(720)), (sc(700), sc(575)), (sc(860), sc(690)), (W, sc(600)), (W, H), (0, H)],
        fill=(15, 87, 167, 255),
    )
    base.alpha_composite(hills)

    path_points = []
    path_points += cubic((390, 1080), (420, 920), (880, 950), (690, 720), 90)[:-1]
    path_points += cubic((690, 720), (500, 570), (940, 520), (790, 350), 90)[:-1]
    path_points += cubic((790, 350), (740, 295), (810, 255), (825, 245), 45)

    glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(glow_layer).line(path_points, fill=(255, 160, 52, 150), width=sc(155), joint="curve")
    base.alpha_composite(glow_layer.filter(ImageFilter.GaussianBlur(sc(26))))

    road = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    road_draw = ImageDraw.Draw(road)
    road_draw.line(path_points, fill=(255, 191, 72, 255), width=sc(132), joint="curve")
    road_draw.line(path_points, fill=(44, 116, 230, 255), width=sc(112), joint="curve")
    road_draw.line(path_points, fill=(51, 137, 245, 230), width=sc(92), joint="curve")
    base.alpha_composite(road)

    lengths = [0.0]
    for first, second in zip(path_points, path_points[1:]):
        lengths.append(lengths[-1] + math.dist(first, second))

    def point_at(distance):
        for index in range(1, len(lengths)):
            if lengths[index] >= distance:
                t = (distance - lengths[index - 1]) / max(1, lengths[index] - lengths[index - 1])
                first, second = path_points[index - 1], path_points[index]
                return (
                    first[0] + (second[0] - first[0]) * t,
                    first[1] + (second[1] - first[1]) * t,
                )
        return path_points[-1]

    dash_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dash_draw = ImageDraw.Draw(dash_layer)
    position = sc(35)
    while position < lengths[-1] - sc(25):
        start = point_at(position)
        end = point_at(min(lengths[-1], position + sc(34)))
        dash_draw.line((start, end), fill=(255, 247, 224, 255), width=sc(13))
        position += sc(72)
    base.alpha_composite(dash_layer)

    cards = [
        ("globe", (22, 126, 238, 255), 145, -8),
        ("mail", (27, 105, 230, 255), 275, -7),
        ("chat", (10, 176, 128, 255), 405, -6),
        ("folder", (247, 159, 22, 255), 535, -5),
        ("calendar", (123, 57, 219, 255), 665, -4),
    ]
    for kind, accent, y, angle in cards:
        card = card_layer(kind, accent, angle)
        base.alpha_composite(card, (sc(55), sc(y)))

    clock_glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    clock_glow_draw = ImageDraw.Draw(clock_glow)
    center_x, center_y, radius = sc(745), sc(748), sc(160)
    clock_glow_draw.ellipse(
        (center_x - radius, center_y - radius, center_x + radius, center_y + radius),
        fill=(37, 220, 255, 190),
    )
    base.alpha_composite(clock_glow.filter(ImageFilter.GaussianBlur(sc(28))))

    clock = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    clock_draw = ImageDraw.Draw(clock)
    clock_draw.ellipse(
        (center_x - radius, center_y - radius, center_x + radius, center_y + radius),
        fill=(40, 210, 245, 255),
        outline=(224, 252, 255, 255),
        width=sc(13),
    )
    clock_draw.ellipse(
        (center_x - sc(128), center_y - sc(128), center_x + sc(128), center_y + sc(128)),
        fill=(9, 48, 132, 255),
    )
    clock_draw.line((center_x, center_y, center_x, center_y - sc(72)), fill=(255, 255, 255, 255), width=sc(20))
    clock_draw.line((center_x, center_y, center_x + sc(58), center_y + sc(46)), fill=(255, 255, 255, 255), width=sc(20))
    clock_draw.ellipse(
        (center_x - sc(13), center_y - sc(13), center_x + sc(13), center_y + sc(13)),
        fill=(255, 255, 255, 255),
    )
    arc_box = (center_x - sc(115), center_y - sc(115), center_x + sc(115), center_y + sc(115))
    clock_draw.arc(arc_box, 130, 330, fill=(255, 255, 255, 255), width=sc(18))
    arrow_x, arrow_y = center_x - sc(105), center_y + sc(24)
    clock_draw.polygon(
        [
            (arrow_x - sc(25), arrow_y - sc(8)),
            (arrow_x + sc(18), arrow_y - sc(40)),
            (arrow_x + sc(14), arrow_y + sc(16)),
        ],
        fill=(255, 255, 255, 255),
    )
    base.alpha_composite(clock)

    border = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(border).rounded_rectangle(
        (sc(8), sc(8), W - sc(8), H - sc(8)),
        radius=sc(198),
        outline=(124, 227, 255, 180),
        width=sc(8),
    )
    base.alpha_composite(border)
    return base.resize((CANVAS, CANVAS), Image.Resampling.LANCZOS)


def main():
    root = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    assets = root / "Assets"
    assets.mkdir(parents=True, exist_ok=True)

    image = build_icon()
    master = assets / "MemoryTrailIconMaster.png"
    preview = assets / "MemoryTrailIcon.png"
    ico = assets / "MemoryTrailApp.ico"

    image.save(master, "PNG", optimize=True)
    image.resize((256, 256), Image.Resampling.LANCZOS).save(preview, "PNG", optimize=True)
    image.save(ico, "ICO", sizes=ICO_SIZES, bitmap_format="png")

    with Image.open(ico) as icon:
        available_sizes = set(icon.ico.sizes())
    missing_sizes = set(ICO_SIZES) - available_sizes
    if missing_sizes:
        raise RuntimeError(f"Missing ICO sizes: {sorted(missing_sizes)}")

    print(f"Generated {master} ({master.stat().st_size} bytes)")
    print(f"Generated {ico} ({ico.stat().st_size} bytes) with {len(available_sizes)} sizes")


if __name__ == "__main__":
    main()
