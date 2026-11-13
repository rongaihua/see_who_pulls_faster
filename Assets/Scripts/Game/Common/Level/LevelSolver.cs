//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 关卡求解器，广度优先找最少步数的解
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 求解结果
    /// </summary>
    public sealed class SolveResult
    {
        /// <summary>
        /// 有没有解
        /// </summary>
        public bool Solvable;

        /// <summary>
        /// 搜索量超过上限、放弃判断
        /// </summary>
        public bool Aborted;

        /// <summary>
        /// 搜过的局面数
        /// </summary>
        public int Visited;

        /// <summary>
        /// 最少步数的解，无解或放弃时为空
        /// </summary>
        public List<Move> Moves = new List<Move>();
    }

    /// <summary>
    /// 求解器。广度优先，第一次搜到通关局面的路径就是最少步数的解。
    /// </summary>
    public static class LevelSolver
    {
        /// <summary>
        /// 默认的局面数上限，超过就返回 Aborted，避免卡死
        /// </summary>
        public const int DefaultMaxStates = 200000;

        /// <summary>
        /// 求最少步数的解
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="maxStates">最多搜多少个局面</param>
        /// <returns>求解结果</returns>
        public static SolveResult Solve(LevelDefinition level, int maxStates = DefaultMaxStates)
        {
            if (!GameRules.IsSupported(level, out var error))
            {
                throw new ArgumentException("关卡不受支持：" + error, nameof(level));
            }

            var start = GameRules.CreateInitial(level);
            if (GameRules.IsWin(level, start))
            {
                return new SolveResult { Solvable = true, Visited = 1 };
            }

            var states = new List<GameState>(256) { start };
            var parents = new List<int>(256) { -1 };
            var moves = new List<Move>(256) { default };
            var visited = new Dictionary<StateKey, int>(256) { [StateKey.From(start)] = 0 };
            var queue = new Queue<int>(256);
            queue.Enqueue(0);

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var state = states[index];

                foreach (var move in GameRules.LegalMoves(level, state))
                {
                    var next = state.Clone();
                    if (!GameRules.Apply(level, next, move))
                    {
                        continue;
                    }

                    var key = StateKey.From(next);
                    if (visited.ContainsKey(key))
                    {
                        continue;
                    }

                    var nextIndex = states.Count;
                    visited[key] = nextIndex;
                    states.Add(next);
                    parents.Add(index);
                    moves.Add(move);

                    if (GameRules.IsWin(level, next))
                    {
                        return new SolveResult
                        {
                            Solvable = true,
                            Visited = visited.Count,
                            Moves = Reconstruct(parents, moves, nextIndex),
                        };
                    }

                    if (states.Count >= maxStates)
                    {
                        return new SolveResult { Aborted = true, Visited = visited.Count };
                    }

                    queue.Enqueue(nextIndex);
                }
            }

            return new SolveResult { Solvable = false, Visited = visited.Count };
        }

        /// <summary>
        /// 只判断有没有解，不看步数
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="maxStates">最多搜多少个局面</param>
        /// <returns>有解为 true</returns>
        public static bool HasSolution(LevelDefinition level, int maxStates = DefaultMaxStates)
        {
            return Solve(level, maxStates).Solvable;
        }

        /// <summary>
        /// 沿父指针倒着把操作串起来
        /// </summary>
        private static List<Move> Reconstruct(List<int> parents, List<Move> moves, int node)
        {
            var path = new List<Move>();
            for (var i = node; i > 0; i = parents[i])
            {
                path.Add(moves[i]);
            }

            path.Reverse();
            return path;
        }
    }
}
