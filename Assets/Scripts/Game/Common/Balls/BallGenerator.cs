//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 球堆关卡生成：先把球堆画成同色色块，再配一副够用的车队，最后用贪心验一遍能通关
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Balls
{
    /// <summary>
    /// 难度档位
    /// </summary>
    public enum BallDifficulty
    {
        /// <summary>
        /// 简单：盘小、颜色少
        /// </summary>
        Easy = 0,

        /// <summary>
        /// 普通
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 困难：盘大、颜色多、车更紧张
        /// </summary>
        Hard = 2,
    }

    /// <summary>
    /// 生成参数
    /// </summary>
    public sealed class BallGeneratorOptions
    {
        /// <summary>
        /// 随机种子
        /// </summary>
        public int Seed = 1;

        /// <summary>
        /// 球盘宽
        /// </summary>
        public int Width = 12;

        /// <summary>
        /// 球盘高
        /// </summary>
        public int Height = 9;

        /// <summary>
        /// 颜色数
        /// </summary>
        public int ColorCount = 5;

        /// <summary>
        /// 一个色块最少几个球
        /// </summary>
        public int MinBlob = 6;

        /// <summary>
        /// 一个色块最多几个球
        /// </summary>
        public int MaxBlob = 26;

        /// <summary>
        /// 大车容量
        /// </summary>
        public int BigCapacity = 20;

        /// <summary>
        /// 小车容量
        /// </summary>
        public int SmallCapacity = 10;

        /// <summary>
        /// 底部车位列数
        /// </summary>
        public int Columns = 4;

        /// <summary>
        /// 每列显示几辆车
        /// </summary>
        public int Visible = 3;

        /// <summary>
        /// 车队总容量相对球数的倍数，留点富余否则一步浪费就过不去
        /// </summary>
        public float SlackFactor = 1.35f;

        /// <summary>
        /// 最多重试几次
        /// </summary>
        public int MaxAttempts = 80;

        /// <summary>
        /// 按档位给一套默认参数
        /// </summary>
        /// <param name="difficulty">档位</param>
        /// <param name="seed">种子</param>
        /// <returns>参数</returns>
        public static BallGeneratorOptions For(BallDifficulty difficulty, int seed)
        {
            switch (difficulty)
            {
                case BallDifficulty.Easy:
                    return new BallGeneratorOptions
                    {
                        Seed = seed, Width = 14, Height = 10, ColorCount = 4,
                    };
                case BallDifficulty.Medium:
                    return new BallGeneratorOptions
                    {
                        Seed = seed, Width = 16, Height = 12, ColorCount = 5,
                    };
                default:
                    return new BallGeneratorOptions
                    {
                        Seed = seed, Width = 18, Height = 13, ColorCount = 6, MaxBlob = 20,
                    };
            }
        }
    }

    /// <summary>
    /// 球堆关卡生成器
    /// </summary>
    public static class BallGenerator
    {
        /// <summary>
        /// 生成一关，保证贪心能打通
        /// </summary>
        /// <param name="options">参数</param>
        /// <returns>关卡，造不出来时为空</returns>
        public static BallLevel Generate(BallGeneratorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            for (var attempt = 0; attempt < options.MaxAttempts; attempt++)
            {
                var level = Build(options, options.Seed * 7919 + attempt);
                if (Verify(level))
                {
                    return level;
                }
            }

            return null;
        }

        /// <summary>
        /// 随机造一关，不保证能过
        /// </summary>
        /// <param name="options">参数</param>
        /// <param name="seed">本题种子</param>
        /// <returns>关卡</returns>
        public static BallLevel Build(BallGeneratorOptions options, int seed)
        {
            var random = new Random(seed);
            var width = Math.Max(3, options.Width);
            var height = Math.Max(3, options.Height);
            var colors = Math.Max(2, Math.Min(options.ColorCount, 8));
            var total = width * height;

            var counts = SplitCounts(total, colors, random);
            var cells = FillBlobs(width, height, counts, options.MinBlob, options.MaxBlob, random);

            return new BallLevel
            {
                Id = "ball-" + seed,
                Width = width,
                Height = height,
                ColorCount = colors,
                Columns = Math.Max(1, options.Columns),
                Visible = Math.Max(1, options.Visible),
                Cells = cells,
                Deck = BuildDeck(counts, options, random),
            };
        }

        /// <summary>
        /// 把总数拆给每种颜色
        /// </summary>
        private static int[] SplitCounts(int total, int colors, Random random)
        {
            var counts = new int[colors];
            for (var i = 0; i < colors; i++)
            {
                counts[i] = 1;
            }

            var left = total - colors;
            for (var i = 0; i < left; i++)
            {
                counts[random.Next(colors)]++;
            }

            return counts;
        }

        /// <summary>
        /// 把球按色块铺满球盘：每次挑一个空格当种子，用 BFS 长出一块
        /// </summary>
        private static int[] FillBlobs(int width, int height, int[] counts, int minBlob, int maxBlob, Random random)
        {
            var cells = new int[width * height];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = -1;
            }

            var left = (int[])counts.Clone();
            var queue = new Queue<int>();

            while (true)
            {
                var color = PickColor(left, random);
                if (color < 0)
                {
                    break;
                }

                var seedCell = PickEmpty(cells, random);
                if (seedCell < 0)
                {
                    break;
                }

                var target = Math.Min(left[color], random.Next(minBlob, maxBlob + 1));
                cells[seedCell] = color;
                left[color]--;
                var placed = 1;

                queue.Clear();
                queue.Enqueue(seedCell);

                while (placed < target && queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    var x = cell % width;
                    var y = cell / width;

                    for (var dir = 0; dir < 4; dir++)
                    {
                        var nx = x + (dir == 0 ? 1 : dir == 1 ? -1 : 0);
                        var ny = y + (dir == 2 ? 1 : dir == 3 ? -1 : 0);
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        {
                            continue;
                        }

                        var index = ny * width + nx;
                        if (cells[index] >= 0)
                        {
                            continue;
                        }

                        cells[index] = color;
                        left[color]--;
                        placed++;
                        queue.Enqueue(index);

                        if (placed >= target)
                        {
                            break;
                        }
                    }
                }
            }

            // 兜底：还有剩的球就随便找空位塞
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] >= 0)
                {
                    continue;
                }

                var color = PickColor(left, random);
                if (color < 0)
                {
                    color = random.Next(counts.Length);
                }

                cells[i] = color;
                if (left[color] > 0)
                {
                    left[color]--;
                }
            }

            return cells;
        }

        /// <summary>
        /// 挑一个还有剩的球色
        /// </summary>
        private static int PickColor(int[] left, Random random)
        {
            var totalLeft = 0;
            for (var i = 0; i < left.Length; i++)
            {
                totalLeft += left[i];
            }

            if (totalLeft <= 0)
            {
                return -1;
            }

            // 按剩余数量加权，数量多的更容易被选中，视觉上更成块
            var roll = random.Next(totalLeft);
            for (var i = 0; i < left.Length; i++)
            {
                roll -= left[i];
                if (roll < 0)
                {
                    return i;
                }
            }

            return left.Length - 1;
        }

        /// <summary>
        /// 随机找一个空格
        /// </summary>
        private static int PickEmpty(int[] cells, Random random)
        {
            var empty = 0;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] < 0)
                {
                    empty++;
                }
            }

            if (empty == 0)
            {
                return -1;
            }

            var roll = random.Next(empty);
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] < 0)
                {
                    if (roll == 0)
                    {
                        return i;
                    }

                    roll--;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按每种颜色的球数配车，然后把各色的车按轮次混排，
        /// 避免开局四个车位全是已经吸完的颜色。
        /// </summary>
        private static BusCard[] BuildDeck(int[] counts, BallGeneratorOptions options, Random random)
        {
            var byColor = new List<BusCard>[counts.Length];
            var big = options.BigCapacity;
            var small = options.SmallCapacity;

            for (var color = 0; color < counts.Length; color++)
            {
                byColor[color] = new List<BusCard>();
                var target = (int)Math.Ceiling(counts[color] * options.SlackFactor / small) * small;
                var left = target;
                var useBig = random.Next(2) == 0;

                while (left > 0)
                {
                    var capacity = left >= big && useBig ? big : Math.Min(small, left);
                    byColor[color].Add(new BusCard(color, capacity));
                    left -= capacity;
                    useBig = !useBig;
                }
            }

            // 轮转混排：每轮把还有车的颜色各取一辆，顺序随机
            var deck = new List<BusCard>();
            var remaining = new int[counts.Length];
            var total = 0;
            for (var color = 0; color < counts.Length; color++)
            {
                remaining[color] = byColor[color].Count;
                total += remaining[color];
            }

            while (total > 0)
            {
                var round = new List<int>(counts.Length);
                for (var color = 0; color < counts.Length; color++)
                {
                    if (remaining[color] > 0)
                    {
                        round.Add(color);
                    }
                }

                for (var i = round.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    var temp = round[i];
                    round[i] = round[j];
                    round[j] = temp;
                }

                for (var i = 0; i < round.Count; i++)
                {
                    var color = round[i];
                    var index = byColor[color].Count - remaining[color];
                    deck.Add(byColor[color][index]);
                    remaining[color]--;
                    total--;
                }
            }

            // 至少要让所有车位都摆满
            var minimum = Math.Max(1, options.Columns) * Math.Max(1, options.Visible);
            while (deck.Count < minimum)
            {
                deck.Add(new BusCard(random.Next(counts.Length), small));
            }

            return deck.ToArray();
        }

        /// <summary>
        /// 用“每次放大吸得最多的那辆车”的贪心策略验一遍
        /// </summary>
        /// <param name="level">关卡</param>
        /// <returns>能打通为 true</returns>
        public static bool Verify(BallLevel level)
        {
            if (level == null || !level.IsValid(out _))
            {
                return false;
            }

            var game = BallGame.Create(level);

            while (!game.IsWin)
            {
                var best = -1;
                var bestAbsorb = 0;

                for (var column = 0; column < level.Columns; column++)
                {
                    var absorb = game.PreviewAbsorb(column);
                    if (absorb > bestAbsorb)
                    {
                        bestAbsorb = absorb;
                        best = column;
                    }
                }

                if (best < 0)
                {
                    return false;
                }

                game.Dispatch(best);
            }

            return true;
        }
    }
}
