//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 关卡核心的离线自检工具，不依赖 Unity，直接 dotnet run
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SeeWho.Common.Balls;
using SeeWho.Common.Level;

namespace LevelLab
{
    /// <summary>
    /// 命令行自检：规则、求解器、生成器、球堆玩法
    /// </summary>
    internal static class Program
    {
        private static int sFailures;

        private static void Main()
        {
            TestStraightBoarding();
            TestUnsolvableColor();
            TestParkThenBoard();
            TestWaitingOverflow();
            TestIncompleteBusDeadlock();
            TestReplayGeneratedSolutions();
            TestGeneratorDeterminism();
            TestGeneratedLevelsAreSolvable();
            TestLargeLevelPerformance();
            PrintSamples();

            TestBallAbsorbAndDrop();
            TestBallGeneration();
            PrintBallSample();

            Console.WriteLine();
            Console.WriteLine(sFailures == 0 ? "全部通过" : "失败 " + sFailures + " 项");
            Environment.Exit(sFailures == 0 ? 0 : 1);
        }

        /// <summary>
        /// 三个人一辆车，一步一个，三步通关
        /// </summary>
        private static void TestStraightBoarding()
        {
            var level = MakeLevel(3, 3, 1, 5, new[] { 0, 0, 0 }, new[] { 0 });
            var result = LevelSolver.Solve(level);

            Check("直接上车：有解", result.Solvable);
            Check("直接上车：三步", result.Moves.Count == 3);
            Check("直接上车：全是队头上车",
                result.Moves.Count == 3 && result.Moves.All(m => m.Kind == MoveKind.BoardFromQueue));
        }

        /// <summary>
        /// 队伍里只有蓝色，车全是红色，永远上不去
        /// </summary>
        private static void TestUnsolvableColor()
        {
            var level = MakeLevel(3, 2, 1, 5, new[] { 1, 1 }, new[] { 0 });
            var result = LevelSolver.Solve(level);

            Check("颜色对不上：无解", !result.Solvable && !result.Aborted);
        }

        /// <summary>
        /// 蓝色的人先站到候车区，等红色车装满开走、蓝色车进站之后再上
        /// </summary>
        private static void TestParkThenBoard()
        {
            var level = MakeLevel(3, 3, 1, 5, new[] { 1, 1, 1, 0, 0, 0 }, new[] { 0, 1 });
            var result = LevelSolver.Solve(level);

            Check("候车转运：有解", result.Solvable);
            Check("候车转运：九步", result.Moves.Count == 9);
            Check("候车转运：前三步是候车",
                result.Moves.Count == 9 && result.Moves.Take(3).All(m => m.Kind == MoveKind.ParkFromQueue));
            Check("候车转运：最后三步从候车区上车",
                result.Moves.Count == 9 && result.Moves.Skip(6).All(m => m.Kind == MoveKind.BoardFromWaiting));
        }

        /// <summary>
        /// 候车区只有一个位置，第三个人没地方站就卡死
        /// </summary>
        private static void TestWaitingOverflow()
        {
            var level = MakeLevel(3, 3, 1, 1, new[] { 1, 1, 1 }, new[] { 0 });
            var result = LevelSolver.Solve(level);

            Check("候车区溢出：无解", !result.Solvable && !result.Aborted);
        }

        /// <summary>
        /// 前一色人不够坐满车时，那辆车一直占着车位，后面的颜色永远等不到车
        /// </summary>
        private static void TestIncompleteBusDeadlock()
        {
            var level = MakeLevel(3, 3, 1, 5, new[] { 0, 0, 1, 1, 1 }, new[] { 0, 1 });
            var result = LevelSolver.Solve(level);

            Check("车等不满：无解", !result.Solvable && !result.Aborted);
        }

        /// <summary>
        /// 生成的解必须真能一步步走通
        /// </summary>
        private static void TestReplayGeneratedSolutions()
        {
            foreach (var difficulty in new[] { LevelDifficulty.Easy, LevelDifficulty.Medium, LevelDifficulty.Hard })
            {
                for (var seed = 1; seed <= 3; seed++)
                {
                    var generated = LevelGenerator.Generate(LevelGeneratorOptions.For(difficulty, seed));
                    Check(difficulty + " #" + seed + "：生成成功", generated != null);
                    if (generated == null)
                    {
                        continue;
                    }

                    Check(difficulty + " #" + seed + "：回放能通关",
                        Replay(generated.Level, generated.Solution.Moves, out var reason), reason);
                }
            }
        }

        /// <summary>
        /// 同一个种子每次生成的关卡要一样
        /// </summary>
        private static void TestGeneratorDeterminism()
        {
            var options = LevelGeneratorOptions.For(LevelDifficulty.Medium, 42);
            var a = LevelGenerator.Build(options, 42);
            var b = LevelGenerator.Build(options, 42);

            Check("生成器：同种子同关卡",
                a.Passengers.SequenceEqual(b.Passengers) && a.BusDeck.SequenceEqual(b.BusDeck));
        }

        /// <summary>
        /// 三档难度各生成若干关，全部有解，且候车区确实被用上
        /// </summary>
        private static void TestGeneratedLevelsAreSolvable()
        {
            var watch = Stopwatch.StartNew();

            foreach (var difficulty in new[] { LevelDifficulty.Easy, LevelDifficulty.Medium, LevelDifficulty.Hard })
            {
                for (var seed = 1; seed <= 5; seed++)
                {
                    var generated = LevelGenerator.Generate(LevelGeneratorOptions.For(difficulty, seed));
                    Check(difficulty + " #" + seed + "：有解", generated != null && generated.Solution.Solvable);
                    if (generated == null)
                    {
                        continue;
                    }

                    Check(difficulty + " #" + seed + "：回放能通关",
                        Replay(generated.Level, generated.Solution.Moves, out var reason), reason);

                    if (difficulty != LevelDifficulty.Easy)
                    {
                        Check(difficulty + " #" + seed + "：用到了候车区", generated.Stats.PeakWaiting > 0);
                    }
                }
            }

            watch.Stop();
            Console.WriteLine("生成 + 求解 15 关耗时 " + watch.ElapsedMilliseconds + " ms");
        }

        /// <summary>
        /// 大关的求解耗时，顺便压一下搜索上限
        /// </summary>
        private static void TestLargeLevelPerformance()
        {
            var options = new LevelGeneratorOptions
            {
                Seed = 3, ColorCount = 4, PassengerCount = 48, DockCount = 3,
                WaitingSlots = 5, MinParks = 2, MaxAttempts = 50,
            };

            var watch = Stopwatch.StartNew();
            var generated = LevelGenerator.Generate(options);
            watch.Stop();

            Check("48 人关卡：生成成功", generated != null);
            if (generated == null)
            {
                return;
            }

            Console.WriteLine("  48 人关卡：" + generated.Stats + "，搜索 " + generated.Solution.Visited +
                              " 个局面，耗时 " + watch.ElapsedMilliseconds + " ms");
        }

        /// <summary>
        /// 打几关出来看看手感
        /// </summary>
        private static void PrintSamples()
        {
            Console.WriteLine();
            Console.WriteLine("样例关卡：");

            foreach (var difficulty in new[] { LevelDifficulty.Easy, LevelDifficulty.Medium, LevelDifficulty.Hard })
            {
                var generated = LevelGenerator.Generate(LevelGeneratorOptions.For(difficulty, 7));
                if (generated == null)
                {
                    Console.WriteLine("  " + difficulty + "：生成失败");
                    continue;
                }

                var level = generated.Level;
                Console.WriteLine("  " + difficulty + " " + level + " " + generated.Stats);
                Console.WriteLine("    队伍 " + string.Join(",", level.Passengers));
                Console.WriteLine("    车队 " + string.Join(",", level.BusDeck));
            }
        }

        /// <summary>
        /// 从开局把操作重放一遍，看是否每步合法、最后是否通关
        /// </summary>
        private static bool Replay(LevelDefinition level, List<Move> moves, out string reason)
        {
            reason = null;
            var state = GameRules.CreateInitial(level);

            for (var i = 0; i < moves.Count; i++)
            {
                if (!GameRules.Apply(level, state, moves[i]))
                {
                    reason = "第 " + (i + 1) + " 步非法：" + moves[i];
                    return false;
                }
            }

            if (!GameRules.IsWin(level, state))
            {
                reason = "走完没通关，队头 " + state.QueueIndex + "，候车 " + state.WaitingCount() + " 人";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 造一个测试关卡
        /// </summary>
        private static LevelDefinition MakeLevel(int colors, int capacity, int docks, int waiting, int[] passengers, int[] deck)
        {
            return new LevelDefinition
            {
                Id = "test",
                ColorCount = colors,
                BusCapacity = capacity,
                DockCount = docks,
                WaitingSlots = waiting,
                Passengers = passengers,
                BusDeck = deck,
            };
        }

        /// <summary>
        /// 球堆：吸球和下落
        /// </summary>
        private static void TestBallAbsorbAndDrop()
        {
            // 3 宽 3 高，第 0 行在最下面
            // 行 2: 0 1 1
            // 行 1: 1 1 0
            // 行 0: 0 0 1
            var level = new BallLevel
            {
                Id = "ball-test",
                Width = 3,
                Height = 3,
                ColorCount = 2,
                Columns = 2,
                Visible = 2,
                Cells = new[] { 0, 0, 1, 1, 1, 0, 0, 1, 1 },
                Deck = new[] { new BusCard(0, 10), new BusCard(1, 10) },
            };

            var game = BallGame.Create(level);
            Check("球堆：开局 9 个球", game.Remaining() == 9);
            Check("球堆：预览能吸 4 个", game.PreviewAbsorb(0) == 4);

            var got = game.Dispatch(0);
            Check("球堆：吸到 4 个", got == 4);
            Check("球堆：剩 5 个", game.Remaining() == 5);

            // 第 0 列原来的 1 落到最底下，上面空了
            var bottomLeft = game.Cells[level.Index(0, 0)];
            var middleLeft = game.Cells[level.Index(0, 1)];
            Check("球堆：吸完会下落", bottomLeft == 1 && middleLeft == -1);
        }

        /// <summary>
        /// 三档难度各生成几关，全部能打通
        /// </summary>
        private static void TestBallGeneration()
        {
            var watch = Stopwatch.StartNew();

            foreach (var difficulty in new[] { BallDifficulty.Easy, BallDifficulty.Medium, BallDifficulty.Hard })
            {
                for (var seed = 1; seed <= 4; seed++)
                {
                    var level = BallGenerator.Generate(BallGeneratorOptions.For(difficulty, seed));
                    Check(difficulty + " 球堆 #" + seed + "：生成成功", level != null);
                    if (level == null)
                    {
                        continue;
                    }

                    Check(difficulty + " 球堆 #" + seed + "：贪心可通关", BallGenerator.Verify(level));

                    var seats = 0;
                    for (var i = 0; i < level.Deck.Length; i++)
                    {
                        seats += level.Deck[i].Capacity;
                    }

                    Check(difficulty + " 球堆 #" + seed + "：座位够用",
                        seats >= level.Width * level.Height &&
                        level.Deck.Length >= level.Columns * level.Visible);
                }
            }

            watch.Stop();
            Console.WriteLine("球堆生成耗时 " + watch.ElapsedMilliseconds + " ms");
        }

        /// <summary>
        /// 用字符把球堆画出来，看看色块形状
        /// </summary>
        private static void PrintBallSample()
        {
            var level = BallGenerator.Generate(BallGeneratorOptions.For(BallDifficulty.Medium, 22));
            if (level == null)
            {
                Console.WriteLine("球堆样例生成失败");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("球堆样例 " + level + "，车 " + level.Deck.Length + " 辆：");
            for (var y = level.Height - 1; y >= 0; y--)
            {
                var line = new System.Text.StringBuilder("    ");
                for (var x = 0; x < level.Width; x++)
                {
                    line.Append("0123456789"[level.Cells[level.Index(x, y)]]);
                    line.Append(' ');
                }

                Console.WriteLine(line.ToString());
            }
        }

        /// <summary>
        /// 断言
        /// </summary>
        private static void Check(string name, bool ok, string detail = null)
        {
            if (ok)
            {
                Console.WriteLine("  ok   " + name);
                return;
            }

            sFailures++;
            Console.WriteLine("  FAIL " + name + (string.IsNullOrEmpty(detail) ? string.Empty : "（" + detail + "）"));
        }
    }
}
