//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 关卡生成器：随机造关卡，用求解器验证有解，再按步数和候车压力挑难度
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 难度档位
    /// </summary>
    public enum LevelDifficulty
    {
        /// <summary>
        /// 简单：车多、候车区大
        /// </summary>
        Easy = 0,

        /// <summary>
        /// 普通
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 困难：候车区小，必须精打细算
        /// </summary>
        Hard = 2,
    }

    /// <summary>
    /// 车队出场顺序的排法
    /// </summary>
    public enum BusOrdering
    {
        /// <summary>
        /// 轮转：每轮把还有车的颜色各出一辆。开局车位上颜色最全，难度比较温和
        /// </summary>
        RoundRobin = 0,

        /// <summary>
        /// 成块：同色的车连着出。开局车位上颜色少，逼玩家大量候车，最难
        /// </summary>
        Grouped = 1,

        /// <summary>
        /// 完全打乱
        /// </summary>
        Shuffled = 2,
    }

    /// <summary>
    /// 生成参数
    /// </summary>
    public sealed class LevelGeneratorOptions
    {
        /// <summary>
        /// 随机种子，同一个种子生成同一关
        /// </summary>
        public int Seed = 1;

        /// <summary>
        /// 颜色数
        /// </summary>
        public int ColorCount = 3;

        /// <summary>
        /// 乘客数，生成时会向上凑成座位数的整数倍
        /// </summary>
        public int PassengerCount = 12;

        /// <summary>
        /// 座位数
        /// </summary>
        public int BusCapacity = 3;

        /// <summary>
        /// 车位数
        /// </summary>
        public int DockCount = 3;

        /// <summary>
        /// 候车区大小
        /// </summary>
        public int WaitingSlots = 5;

        /// <summary>
        /// 车队出场顺序的排法
        /// </summary>
        public BusOrdering BusOrdering = BusOrdering.RoundRobin;

        /// <summary>
        /// 最少步数
        /// </summary>
        public int MinMoves = 0;

        /// <summary>
        /// 最多步数
        /// </summary>
        public int MaxMoves = int.MaxValue;

        /// <summary>
        /// 至少要候车几次
        /// </summary>
        public int MinParks = 0;

        /// <summary>
        /// 候车区同时至少站几个人（越接近候车区容量越紧）
        /// </summary>
        public int MinPeakWaiting = 0;

        /// <summary>
        /// 最多试几次
        /// </summary>
        public int MaxAttempts = 300;

        /// <summary>
        /// 按档位给一套默认参数
        /// </summary>
        /// <param name="difficulty">档位</param>
        /// <param name="seed">种子</param>
        /// <returns>参数</returns>
        public static LevelGeneratorOptions For(LevelDifficulty difficulty, int seed)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    return new LevelGeneratorOptions
                    {
                        Seed = seed, ColorCount = 3, PassengerCount = 12, DockCount = 2,
                        WaitingSlots = 5, MinParks = 1, MinPeakWaiting = 2,
                    };
                case LevelDifficulty.Medium:
                    return new LevelGeneratorOptions
                    {
                        Seed = seed, ColorCount = 4, PassengerCount = 18, DockCount = 3,
                        WaitingSlots = 4, MinParks = 2, MinPeakWaiting = 3,
                    };
                default:
                    return new LevelGeneratorOptions
                    {
                        Seed = seed, ColorCount = 4, PassengerCount = 24, DockCount = 3,
                        WaitingSlots = 3, MinParks = 3, MinPeakWaiting = 3,
                        // 困难档靠小候车区施压，车队仍用轮转，保证开局三个车位颜色不重样
                        BusOrdering = BusOrdering.RoundRobin,
                    };
            }
        }
    }

    /// <summary>
    /// 生成结果
    /// </summary>
    public sealed class GeneratedLevel
    {
        /// <summary>
        /// 关卡
        /// </summary>
        public LevelDefinition Level;

        /// <summary>
        /// 最优解
        /// </summary>
        public SolveResult Solution;

        /// <summary>
        /// 难度指标
        /// </summary>
        public LevelStats Stats;
    }

    /// <summary>
    /// 关卡生成器。随机造一关，用求解器验一遍，有解且指标达标就收。
    /// 生成时会保证每种颜色的人数正好是座位数的整数倍，这样不会出现一辆车永远等不满、
    /// 把车位一直占住的情况。
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// 生成一关
        /// </summary>
        /// <param name="options">参数</param>
        /// <returns>生成结果，造不出来时为空</returns>
        public static GeneratedLevel Generate(LevelGeneratorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            for (var attempt = 0; attempt < options.MaxAttempts; attempt++)
            {
                var level = Build(options, options.Seed * 7919 + attempt);
                var result = LevelSolver.Solve(level);
                if (!result.Solvable)
                {
                    continue;
                }

                if (result.Moves.Count < options.MinMoves || result.Moves.Count > options.MaxMoves)
                {
                    continue;
                }

                var stats = LevelStats.Compute(level, result.Moves);
                if (stats.Parks < options.MinParks || stats.PeakWaiting < options.MinPeakWaiting)
                {
                    continue;
                }

                return new GeneratedLevel { Level = level, Solution = result, Stats = stats };
            }

            return null;
        }

        /// <summary>
        /// 随机造一关，不保证有解
        /// </summary>
        /// <param name="options">参数</param>
        /// <param name="seed">这一关的种子</param>
        /// <returns>关卡</returns>
        public static LevelDefinition Build(LevelGeneratorOptions options, int seed)
        {
            var random = new Random(seed);
            var colorCount = Math.Max(1, Math.Min(options.ColorCount, GameRules.MaxColors));
            var capacity = Math.Max(1, Math.Min(options.BusCapacity, GameRules.MaxCapacity));
            var dockCount = Math.Max(1, Math.Min(options.DockCount, GameRules.MaxDocks));
            var waitingSlots = Math.Max(0, Math.Min(options.WaitingSlots, GameRules.MaxWaiting));

            // 每人一格座位，块数至少和颜色数一样多，保证每种颜色都有人
            var chunks = (options.PassengerCount + capacity - 1) / capacity;
            chunks = Math.Max(chunks, colorCount);

            var counts = new int[colorCount];
            for (var color = 0; color < colorCount; color++)
            {
                counts[color] = 1;
            }

            for (var i = colorCount; i < chunks; i++)
            {
                counts[random.Next(colorCount)]++;
            }

            var queue = new List<int>(chunks * capacity);
            for (var color = 0; color < colorCount; color++)
            {
                for (var i = 0; i < counts[color] * capacity; i++)
                {
                    queue.Add(color);
                }
            }

            Shuffle(queue, random);
            var deck = BuildDeck(counts, options.BusOrdering, random);

            return new LevelDefinition
            {
                Id = "gen-" + seed,
                ColorCount = colorCount,
                BusCapacity = capacity,
                DockCount = dockCount,
                WaitingSlots = waitingSlots,
                Passengers = queue.ToArray(),
                BusDeck = deck.ToArray(),
            };
        }

        /// <summary>
        /// 按指定排法把车排成出场顺序
        /// </summary>
        /// <param name="counts">每种颜色各有几辆车</param>
        /// <param name="ordering">排法</param>
        /// <param name="random">随机数</param>
        /// <returns>车队</returns>
        private static List<int> BuildDeck(int[] counts, BusOrdering ordering, Random random)
        {
            var total = 0;
            for (var color = 0; color < counts.Length; color++)
            {
                total += counts[color];
            }

            var deck = new List<int>(total);

            switch (ordering)
            {
                case BusOrdering.Grouped:
                {
                    var colorOrder = LiveColors(counts);
                    Shuffle(colorOrder, random);
                    for (var i = 0; i < colorOrder.Count; i++)
                    {
                        var color = colorOrder[i];
                        for (var j = 0; j < counts[color]; j++)
                        {
                            deck.Add(color);
                        }
                    }

                    break;
                }

                case BusOrdering.Shuffled:
                {
                    for (var color = 0; color < counts.Length; color++)
                    {
                        for (var i = 0; i < counts[color]; i++)
                        {
                            deck.Add(color);
                        }
                    }

                    Shuffle(deck, random);
                    break;
                }

                default:
                {
                    // 轮转：每轮把还有车的颜色各出一辆，保证开局车位上颜色尽量齐
                    var remaining = (int[])counts.Clone();
                    var left = total;

                    while (left > 0)
                    {
                        var round = LiveColors(remaining);
                        Shuffle(round, random);

                        for (var i = 0; i < round.Count; i++)
                        {
                            var color = round[i];
                            deck.Add(color);
                            remaining[color]--;
                            left--;
                        }
                    }

                    break;
                }
            }

            return deck;
        }

        /// <summary>
        /// 还有车的颜色
        /// </summary>
        private static List<int> LiveColors(int[] counts)
        {
            var colors = new List<int>(counts.Length);
            for (var color = 0; color < counts.Length; color++)
            {
                if (counts[color] > 0)
                {
                    colors.Add(color);
                }
            }

            return colors;
        }

        /// <summary>
        /// Fisher-Yates 洗牌
        /// </summary>
        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
