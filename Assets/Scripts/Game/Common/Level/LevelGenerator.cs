//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 鍏冲崱鐢熸垚鍣細闅忔満閫犲叧鍗★紝鐢ㄦ眰瑙ｅ櫒楠岃瘉鏈夎В锛屽啀鎸夋鏁板拰鍊欒溅鍘嬪姏鎸戦毦搴?//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 闅惧害妗ｄ綅
    /// </summary>
    public enum LevelDifficulty
    {
        /// <summary>
        /// 绠€鍗曪細杞﹀銆佸€欒溅鍖哄ぇ
        /// </summary>
        Easy = 0,

        /// <summary>
        /// 鏅€?        /// </summary>
        Medium = 1,

        /// <summary>
        /// 鍥伴毦锛氬€欒溅鍖哄皬锛屽繀椤荤簿鎵撶粏绠?        /// </summary>
        Hard = 2,
    }

    /// <summary>
    /// 杞﹂槦鍑哄満椤哄簭鐨勬帓娉?    /// </summary>
    public enum BusOrdering
    {
        /// <summary>
        /// 杞浆锛氭瘡杞妸杩樻湁杞︾殑棰滆壊鍚勫嚭涓€杈嗐€傚紑灞€杞︿綅涓婇鑹叉渶鍏紝闅惧害姣旇緝娓╁拰
        /// </summary>
        RoundRobin = 0,

        /// <summary>
        /// 鎴愬潡锛氬悓鑹茬殑杞﹁繛鐫€鍑恒€傚紑灞€杞︿綅涓婇鑹插皯锛岄€肩帺瀹跺ぇ閲忓€欒溅锛屾渶闅?        /// </summary>
        Grouped = 1,

        /// <summary>
        /// 瀹屽叏鎵撲贡
        /// </summary>
        Shuffled = 2,
    }

    /// <summary>
    /// 鐢熸垚鍙傛暟
    /// </summary>
    public sealed class LevelGeneratorOptions
    {
        /// <summary>
        /// 闅忔満绉嶅瓙锛屽悓涓€涓瀛愮敓鎴愬悓涓€鍏?        /// </summary>
        public int Seed = 1;

        /// <summary>
        /// 棰滆壊鏁?        /// </summary>
        public int ColorCount = 3;

        /// <summary>
        /// 涔樺鏁帮紝鐢熸垚鏃朵細鍚戜笂鍑戞垚搴т綅鏁扮殑鏁存暟鍊?        /// </summary>
        public int PassengerCount = 12;

        /// <summary>
        /// 搴т綅鏁?        /// </summary>
        public int BusCapacity = 3;

        /// <summary>
        /// 杞︿綅鏁?        /// </summary>
        public int DockCount = 3;

        /// <summary>
        /// 鍊欒溅鍖哄ぇ灏?        /// </summary>
        public int WaitingSlots = 5;

        /// <summary>
        /// 杞﹂槦鍑哄満椤哄簭鐨勬帓娉?        /// </summary>
        public BusOrdering BusOrdering = BusOrdering.RoundRobin;

        /// <summary>
        /// 鏈€灏戞鏁?        /// </summary>
        public int MinMoves = 0;

        /// <summary>
        /// 鏈€澶氭鏁?        /// </summary>
        public int MaxMoves = int.MaxValue;

        /// <summary>
        /// 鑷冲皯瑕佸€欒溅鍑犳
        /// </summary>
        public int MinParks = 0;

        /// <summary>
        /// 鍊欒溅鍖哄悓鏃惰嚦灏戠珯鍑犱釜浜猴紙瓒婃帴杩戝€欒溅鍖哄閲忚秺绱э級
        /// </summary>
        public int MinPeakWaiting = 0;

        /// <summary>
        /// 鏈€澶氳瘯鍑犳
        /// </summary>
        public int MaxAttempts = 300;

        /// <summary>
        /// 鎸夋。浣嶇粰涓€濂楅粯璁ゅ弬鏁?        /// </summary>
        /// <param name="difficulty">妗ｄ綅</param>
        /// <param name="seed">绉嶅瓙</param>
        /// <returns>鍙傛暟</returns>
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
                        BusOrdering = BusOrdering.Grouped,
                    };
            }
        }
    }

    /// <summary>
    /// 鐢熸垚缁撴灉
    /// </summary>
    public sealed class GeneratedLevel
    {
        /// <summary>
        /// 鍏冲崱
        /// </summary>
        public LevelDefinition Level;

        /// <summary>
        /// 鏈€浼樿В
        /// </summary>
        public SolveResult Solution;

        /// <summary>
        /// 闅惧害鎸囨爣
        /// </summary>
        public LevelStats Stats;
    }

    /// <summary>
    /// 鍏冲崱鐢熸垚鍣ㄣ€傞殢鏈洪€犱竴鍏筹紝鐢ㄦ眰瑙ｅ櫒楠屼竴閬嶏紝鏈夎В涓旀寚鏍囪揪鏍囧氨鏀躲€?    /// 鐢熸垚鏃朵細淇濊瘉姣忕棰滆壊鐨勪汉鏁版濂芥槸搴т綅鏁扮殑鏁存暟鍊嶏紝杩欐牱涓嶄細鍑虹幇涓€杈嗚溅姘歌繙绛変笉婊°€?    /// 鎶婅溅浣嶄竴鐩村崰浣忕殑鎯呭喌銆?    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// 鐢熸垚涓€鍏?        /// </summary>
        /// <param name="options">鍙傛暟</param>
        /// <returns>鐢熸垚缁撴灉锛岄€犱笉鍑烘潵鏃朵负绌?/returns>
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
        /// 闅忔満閫犱竴鍏筹紝涓嶄繚璇佹湁瑙?        /// </summary>
        /// <param name="options">鍙傛暟</param>
        /// <param name="seed">杩欎竴鍏崇殑绉嶅瓙</param>
        /// <returns>鍏冲崱</returns>
        public static LevelDefinition Build(LevelGeneratorOptions options, int seed)
        {
            var random = new Random(seed);
            var colorCount = Math.Max(1, Math.Min(options.ColorCount, GameRules.MaxColors));
            var capacity = Math.Max(1, Math.Min(options.BusCapacity, GameRules.MaxCapacity));
            var dockCount = Math.Max(1, Math.Min(options.DockCount, GameRules.MaxDocks));
            var waitingSlots = Math.Max(0, Math.Min(options.WaitingSlots, GameRules.MaxWaiting));

            // 姣忎汉涓€鏍煎骇浣嶏紝鍧楁暟鑷冲皯鍜岄鑹叉暟涓€鏍峰锛屼繚璇佹瘡绉嶉鑹查兘鏈変汉
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
        /// 鎸夋寚瀹氭帓娉曟妸杞︽帓鎴愬嚭鍦洪『搴?        /// </summary>
        /// <param name="counts">姣忕棰滆壊鍚勬湁鍑犺締杞?/param>
        /// <param name="ordering">鎺掓硶</param>
        /// <param name="random">闅忔満鏁?/param>
        /// <returns>杞﹂槦</returns>
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
                    // 杞浆锛氭瘡杞妸杩樻湁杞︾殑棰滆壊鍚勫嚭涓€杈嗭紝淇濊瘉寮€灞€杞︿綅涓婇鑹插敖閲忛綈
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
        /// 杩樻湁杞︾殑棰滆壊
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
        /// Fisher-Yates 娲楃墝
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
