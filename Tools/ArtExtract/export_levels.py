#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把缓存里的原作关卡数据导成工程能读的 JSON。

用法：
    python export_levels.py <zs_game_config.json> <工程 Assets 目录> [起始 id] [结束 id]

原作的关卡结构（levelNCXHCfg）：
    queue  : 这一关所有乘客的颜色（按颜色分组排列）
    bus    : 分波的车队，每个元素是 "颜色_数字" 或 "颜色_数字_特殊类型"，
             数字 / 10 就是这辆车能装几个人（100 -> 10 人，200 -> 20 人）

导出的 JSON 给 BallLevel 用，球盘的排布是按颜色数量重新铺的色块（原作没存空间位置）。
"""

import json
import os
import random
import sys
from collections import deque

# 球盘宽度，高度按球数算
BOARD_WIDTH = 24
# 一个色块的大小范围
MIN_BLOB = 6
MAX_BLOB = 24
# 我们的调色板只支持 8 色，超过的先跳过
MAX_COLORS = 8


def largest_remap(counts):
    """颜色编号压缩成 0..n-1，保持顺序。"""
    colors = sorted(counts)
    return {c: i for i, c in enumerate(colors)}


def fill_blobs(width, height, counts, rng):
    """按颜色数量把球铺成色块，返回 cells（-1 表示空）。"""
    cells = [-1] * (width * height)
    left = dict(counts)
    total_left = sum(left.values())

    while total_left > 0:
        # 按剩余数量加权挑颜色
        roll = rng.randrange(total_left)
        color = None
        for c, n in left.items():
            if n <= 0:
                continue
            roll -= n
            if roll < 0:
                color = c
                break
        if color is None:
            break

        # 随机找一个空格当种子
        empties = [i for i, v in enumerate(cells) if v < 0]
        if not empties:
            break
        seed = rng.choice(empties)

        target = min(left[color], rng.randint(MIN_BLOB, MAX_BLOB))
        cells[seed] = color
        left[color] -= 1
        total_left -= 1
        placed = 1

        queue = deque([seed])
        while placed < target and queue:
            cell = queue.popleft()
            x, y = cell % width, cell // width
            neighbors = []
            if x + 1 < width:
                neighbors.append(cell + 1)
            if x > 0:
                neighbors.append(cell - 1)
            if y + 1 < height:
                neighbors.append(cell + width)
            if y > 0:
                neighbors.append(cell - width)

            for index in neighbors:
                if cells[index] >= 0:
                    continue
                cells[index] = color
                left[color] -= 1
                total_left -= 1
                placed += 1
                queue.append(index)
                if placed >= target:
                    break

    return cells


def parse_bus(entry):
    """把 "颜色_数字[_特殊]" 拆成 (颜色, 容量)。"""
    parts = entry.split("_")
    color = int(parts[0])
    capacity = max(1, int(parts[1]) // 10)
    return color, capacity


def board_size(count):
    """按球数挑一个大致接近方形的球盘，太小或太扁都不好看。"""
    width = int(round((count * 1.5) ** 0.5))
    width = max(5, min(BOARD_WIDTH, width))
    height = max(4, (count + width - 1) // width)
    return width, height


def export_level(level_id, level, out_dir):
    colors = [int(c) for c in level["queue"]]
    if not colors:
        return None

    counts = {}
    for c in colors:
        counts[c] = counts.get(c, 0) + 1

    if len(counts) > MAX_COLORS:
        return None

    mapping = largest_remap(counts)
    counts = {mapping[c]: n for c, n in counts.items()}

    deck = []
    for wave in level["bus"]:
        for entry in wave:
            color, capacity = parse_bus(entry)
            if color not in mapping:
                continue
            deck.append({"color": int(mapping[color]), "capacity": int(capacity)})

    if not deck:
        return None

    width, height = board_size(len(colors))
    rng = random.Random(level_id * 7919)
    cells = fill_blobs(width, height, counts, rng)

    data = {
        "id": int(level_id),
        "width": width,
        "height": height,
        "cells": [int(v) for v in cells],
        "deck": deck,
    }

    path = os.path.join(out_dir, "level_%d.json" % level_id)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))
    return len(deck), len(counts)


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1

    config_path = sys.argv[1]
    assets_dir = sys.argv[2]
    start = int(sys.argv[3]) if len(sys.argv) > 3 else 1001
    end = int(sys.argv[4]) if len(sys.argv) > 4 else 1060

    with open(config_path, encoding="utf-8") as handle:
        config = json.load(handle)

    levels = config["levelNCXHCfg"]
    out_dir = os.path.join(assets_dir, "Resources", "SeeWho", "Levels")
    os.makedirs(out_dir, exist_ok=True)

    exported = 0
    skipped = 0
    for level_id in range(start, end + 1):
        level = levels.get(str(level_id))
        if not level:
            continue
        result = export_level(level_id, level, out_dir)
        if result is None:
            skipped += 1
            continue
        exported += 1

    print("导出 %d 关，跳过 %d 关（颜色超过 %d 种或数据不全）" % (exported, skipped, MAX_COLORS))
    return 0


if __name__ == "__main__":
    sys.exit(main())
