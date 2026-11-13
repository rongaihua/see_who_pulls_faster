#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从《看谁先上车》的微信小游戏缓存里拆美术资源。

用法：
    python extract_art.py <缓存目录> <工程 Assets 目录>

    <缓存目录> 指 ...\local\wx641f825d5dce9ea9\usr\gamecaches

拆出来的东西：
    Resources/SeeWho/Art/bg_level.png        主背景（原文件是 webp，这里转成真 PNG）
    Resources/SeeWho/Art/People/person_XX.png  18 个小人精灵（按颜色）
"""

import os
import shutil
import sys
from collections import deque

from PIL import Image

# 小人精灵图：8 列 × 2 行，列宽 48，人物区从 y=200 开始
SHEET_COLS = 10
SHEET_PITCH = 48
SHEET_TOP = 200
SHEET_ROW_HEIGHT = 64


def largest_component(cell):
    """返回 cell 里最大一块不透明区域的包围盒，用来丢掉相邻图块的碎片。"""
    width, height = cell.size
    alpha = cell.split()[3].load()
    seen = [[False] * width for _ in range(height)]
    best = []

    for y0 in range(height):
        for x0 in range(width):
            if seen[y0][x0] or alpha[x0, y0] < 32:
                continue

            queue = deque([(x0, y0)])
            seen[y0][x0] = True
            component = []

            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < width and 0 <= ny < height and not seen[ny][nx] and alpha[nx, ny] >= 32:
                        seen[ny][nx] = True
                        queue.append((nx, ny))

            if len(component) > len(best):
                best = component

    if not best:
        return None

    xs = [p[0] for p in best]
    ys = [p[1] for p in best]
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def extract_background(cache_dir, art_dir):
    """背景图。缓存里的扩展名是 .png，内容其实是 webp，必须转码。"""
    source = os.path.join(cache_dir, "prefabs", "179444969224831.png")
    image = Image.open(source).convert("RGBA")
    image.save(os.path.join(art_dir, "bg_level.png"))
    print("背景 %dx%d" % image.size)


def extract_people(cache_dir, art_dir):
    """小人精灵图按网格切开，每格取最大连通域，去掉串进来的相邻像素。"""
    source = os.path.join(cache_dir, "prefabs", "179428657819468.png")
    sheet = Image.open(source).convert("RGBA")

    people_dir = os.path.join(art_dir, "People")
    if os.path.isdir(people_dir):
        shutil.rmtree(people_dir)
    os.makedirs(people_dir)

    count = 0
    for row in range(2):
        for col in range(SHEET_COLS):
            box = (
                col * SHEET_PITCH,
                SHEET_TOP + row * SHEET_ROW_HEIGHT,
                min((col + 1) * SHEET_PITCH, sheet.width),
                SHEET_TOP + (row + 1) * SHEET_ROW_HEIGHT,
            )
            cell = sheet.crop(box)
            bbox = largest_component(cell)
            if not bbox:
                continue

            piece = cell.crop(bbox)
            if piece.width < 20 or piece.height < 20:
                continue

            piece.save(os.path.join(people_dir, "person_%02d.png" % count))
            count += 1

    print("小人 %d 个" % count)


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1

    cache_dir = sys.argv[1]
    assets_dir = sys.argv[2]
    art_dir = os.path.join(assets_dir, "Resources", "SeeWho", "Art")
    os.makedirs(art_dir, exist_ok=True)

    extract_background(cache_dir, art_dir)
    extract_people(cache_dir, art_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
