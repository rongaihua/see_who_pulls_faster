#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把缓存里的 pixelMap（40x40 盘面点阵）渲染成 PNG，给游戏当球坑底图。

用法：
    python extract_pitmaps.py <gamecaches 目录> <工程 Assets 目录>

pixelMap 的 JSON 里 points 是 40x40 的颜色索引，11 表示空位（hasGap）。
"""

import json
import os
import sys

from PIL import Image, ImageDraw

# 18 种颜色的近似色（对应 colorNCXHCfg 里的名字）
PALETTE = {
    0: (255, 255, 255),
    1: (76, 175, 80),
    2: (0, 188, 212),
    3: (33, 110, 243),
    4: (121, 85, 72),
    5: (156, 39, 176),
    6: (255, 204, 188),
    7: (255, 193, 7),
    8: (229, 57, 53),
    9: (0, 150, 136),
    10: (255, 112, 67),
    12: (27, 94, 32),
    13: (136, 14, 79),
    14: (121, 134, 203),
    15: (179, 157, 219),
    16: (244, 143, 177),
    17: (0, 121, 107),
}

EMPTY = 11
CELL = 20
DOT = 17


def load_points(path):
    raw = json.load(open(path, encoding="utf-8"))
    info = raw[5][0][2]
    return info.get("points")


def render(points, out_path):
    height = len(points)
    width = len(points[0])
    image = Image.new("RGBA", (width * CELL, height * CELL), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    for y in range(height):
        for x in range(width):
            value = points[y][x]
            if value == EMPTY or value not in PALETTE:
                continue
            cx = x * CELL + CELL // 2
            cy = y * CELL + CELL // 2
            r = DOT // 2
            draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=PALETTE[value] + (255,))

    image.save(out_path)


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1

    cache_dir = sys.argv[1]
    assets_dir = sys.argv[2]
    src_dir = os.path.join(cache_dir, "pixelMap")
    out_dir = os.path.join(assets_dir, "Resources", "SeeWho", "Art")
    os.makedirs(out_dir, exist_ok=True)

    count = 0
    for name in sorted(os.listdir(src_dir)):
        if not name.endswith(".json"):
            continue
        path = os.path.join(src_dir, name)
        try:
            points = load_points(path)
        except Exception:
            continue
        if not points:
            continue

        render(points, os.path.join(out_dir, "pitmap_%02d.png" % count))
        count += 1

    print("渲染 %d 张盘面底图" % count)
    return 0


if __name__ == "__main__":
    sys.exit(main())
