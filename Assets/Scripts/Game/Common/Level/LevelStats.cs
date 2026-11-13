//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 一关的统计指标，用来标难度
//---------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 关卡的量化指标。除了步数，主要看候车区用得有多满，越满越难。
    /// </summary>
    public sealed class LevelStats
    {
        /// <summary>
        /// 最少步数
        /// </summary>
        public int Moves;

        /// <summary>
        /// 其中去候车区的次数
        /// </summary>
        public int Parks;

        /// <summary>
        /// 过程中候车区最多同时站了几个人
        /// </summary>
        public int PeakWaiting;

        /// <summary>
        /// 统计一关按最优解走完的各项指标
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="moves">最优解</param>
        /// <returns>指标</returns>
        public static LevelStats Compute(LevelDefinition level, List<Move> moves)
        {
            var stats = new LevelStats { Moves = moves.Count };
            var state = GameRules.CreateInitial(level);

            for (var i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                if (move.Kind == MoveKind.ParkFromQueue)
                {
                    stats.Parks++;
                }

                if (!GameRules.Apply(level, state, move))
                {
                    break;
                }

                var waiting = state.WaitingCount();
                if (waiting > stats.PeakWaiting)
                {
                    stats.PeakWaiting = waiting;
                }
            }

            return stats;
        }

        /// <summary>
        /// 打印成一行
        /// </summary>
        /// <returns>摘要</returns>
        public override string ToString()
        {
            return "步数 " + Moves + "，候车 " + Parks + " 次，峰值 " + PeakWaiting;
        }
    }
}
