"""Draw the package icon: an eight-pointed star, the thing you steer by.

Drawn at 8x and downsampled, because the star's points are thin and a
128-pixel canvas has no room for a jagged edge.
"""
import math
import pathlib
from PIL import Image, ImageDraw

SIZE = 128
SCALE = 8
NIGHT = (17, 30, 56)      # deep navy: the sky, and enough contrast for a dark theme
STAR = (247, 246, 240)    # warm off-white, not pure white
GLOW = (104, 146, 220)    # the cardinal rays, one step out of the background


def star_points(cx, cy, long_r, short_r, count=8, turn=0.0):
    points = []
    for i in range(count * 2):
        angle = turn + i * math.pi / count
        radius = long_r if i % 2 == 0 else short_r
        points.append((cx + radius * math.sin(angle), cy - radius * math.cos(angle)))
    return points


def draw(path: pathlib.Path) -> None:
    side = SIZE * SCALE
    image = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    radius = side * 0.16
    draw.rounded_rectangle([0, 0, side - 1, side - 1], radius=radius, fill=NIGHT)

    centre = side / 2
    # A four-pointed star rotated 45 degrees under the main one: the diagonal
    # rays read as a compass rather than as a sparkle.
    draw.polygon(star_points(centre, centre, side * 0.34, side * 0.085, 4, math.pi / 4), fill=GLOW)
    draw.polygon(star_points(centre, centre, side * 0.44, side * 0.105, 4, 0.0), fill=STAR)

    image.resize((SIZE, SIZE), Image.LANCZOS).save(path)
    print(f"{path} : {SIZE}x{SIZE}")


ROOT = pathlib.Path(__file__).resolve().parent.parent

if __name__ == "__main__":
    draw(ROOT / "assets" / "icon.png")
