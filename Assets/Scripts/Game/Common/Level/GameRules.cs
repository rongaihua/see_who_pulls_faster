//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 规则判定：合法操作、执行操作、胜负判断
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 规则。求解器和游戏表现层都只通过这里动局面，保证两边判定一致。
    /// 规则要点：
    /// 1. 队伍先进先出，只有队头能动；
    /// 2. 队头颜色和某个车位上的车对得上、且那辆车有空座，就能上车；
    /// 3. 上不了车就去候车区站着，候车区满了还上不了车就卡死；
    /// 4. 候车区里的人随时可以补位上车；
    /// 5. 车坐满就发车，下一辆车立刻进站补位。
    /// </summary>
    public static class GameRules
    {
        /// <summary>
        /// 支持的颜色上限（StateKey 打包限制）
        /// </summary>
        public const int MaxColors = 8;

        /// <summary>
        /// 支持的车位上限
        /// </summary>
        public const int MaxDocks = 8;

        /// <summary>
        /// 支持的座位上限
        /// </summary>
        public const int MaxCapacity = 7;

        /// <summary>
        /// 支持的候车区上限
        /// </summary>
        public const int MaxWaiting = 15;

        /// <summary>
        /// 支持的队伍长度
        /// </summary>
        public const int MaxPassengers = 200;

        /// <summary>
        /// 检查关卡是否在求解器支持的范围内
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="error">不支持时的原因</param>
        /// <returns>支持为 true</returns>
        public static bool IsSupported(LevelDefinition level, out string error)
        {
            error = null;

            if (!level.IsValid(out error))
            {
                return false;
            }

            if (level.ColorCount > MaxColors)
            {
                error = "颜色数超过 " + MaxColors;
                return false;
            }

            if (level.DockCount > MaxDocks)
            {
                error = "车位数超过 " + MaxDocks;
                return false;
            }

            if (level.BusCapacity > MaxCapacity)
            {
                error = "座位数超过 " + MaxCapacity;
                return false;
            }

            if (level.WaitingSlots > MaxWaiting)
            {
                error = "候车区超过 " + MaxWaiting;
                return false;
            }

            if (level.Passengers.Length > MaxPassengers)
            {
                error = "队伍长度超过 " + MaxPassengers;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 摆出开局局面：队头指向第一个乘客，前 DockCount 辆车停在车位上
        /// </summary>
        /// <param name="level">关卡</param>
        /// <returns>开局局面</returns>
        public static GameState CreateInitial(LevelDefinition level)
        {
            var state = new GameState
            {
                QueueIndex = 0,
                Waiting = new int[level.WaitingSlots],
                Docks = new Bus?[level.DockCount],
                DeckIndex = 0,
            };

            for (var i = 0; i < state.Waiting.Length; i++)
            {
                state.Waiting[i] = -1;
            }

            for (var i = 0; i < state.Docks.Length; i++)
            {
                state.Docks[i] = TakeNextBus(level, state);
            }

            return state;
        }

        /// <summary>
        /// 列出当前局面所有合法操作
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="state">局面</param>
        /// <returns>合法操作</returns>
        public static List<Move> LegalMoves(LevelDefinition level, GameState state)
        {
            var moves = new List<Move>();

            if (state.QueueIndex < level.Passengers.Length)
            {
                var color = level.Passengers[state.QueueIndex];
                var canBoard = false;

                for (var dock = 0; dock < state.Docks.Length; dock++)
                {
                    var bus = state.Docks[dock];
                    if (bus.HasValue && bus.Value.Color == color && bus.Value.HasSeat)
                    {
                        moves.Add(new Move(MoveKind.BoardFromQueue, color, dock));
                        canBoard = true;
                    }
                }

                // 能直接上车就不用去候车区占位子，这一步没有收益
                if (!canBoard && state.FirstEmptyWaiting() >= 0)
                {
                    moves.Add(new Move(MoveKind.ParkFromQueue, color, -1));
                }
            }

            for (var color = 0; color < level.ColorCount; color++)
            {
                if (state.FirstWaitingColor(color) < 0)
                {
                    continue;
                }

                for (var dock = 0; dock < state.Docks.Length; dock++)
                {
                    var bus = state.Docks[dock];
                    if (bus.HasValue && bus.Value.Color == color && bus.Value.HasSeat)
                    {
                        moves.Add(new Move(MoveKind.BoardFromWaiting, color, dock));
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// 执行一步操作，就地改局面
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="state">局面</param>
        /// <param name="move">操作</param>
        /// <returns>操作合法并执行为 true</returns>
        public static bool Apply(LevelDefinition level, GameState state, Move move)
        {
            switch (move.Kind)
            {
                case MoveKind.BoardFromQueue:
                {
                    if (!IsQueueHead(state, level, move.Color) || !CanBoard(state, move))
                    {
                        return false;
                    }

                    state.QueueIndex++;
                    Board(state, level, move.Dock);
                    return true;
                }

                case MoveKind.ParkFromQueue:
                {
                    if (!IsQueueHead(state, level, move.Color))
                    {
                        return false;
                    }

                    var slot = state.FirstEmptyWaiting();
                    if (slot < 0)
                    {
                        return false;
                    }

                    state.QueueIndex++;
                    state.Waiting[slot] = move.Color;
                    state.NormalizeWaiting();
                    return true;
                }

                case MoveKind.BoardFromWaiting:
                {
                    var slot = state.FirstWaitingColor(move.Color);
                    if (slot < 0 || !CanBoard(state, move))
                    {
                        return false;
                    }

                    state.Waiting[slot] = -1;
                    state.NormalizeWaiting();
                    Board(state, level, move.Dock);
                    return true;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// 是否已经通关：队伍和候车区都空了
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="state">局面</param>
        /// <returns>通关为 true</returns>
        public static bool IsWin(LevelDefinition level, GameState state)
        {
            return state.QueueIndex >= level.Passengers.Length && state.WaitingCount() == 0;
        }

        /// <summary>
        /// 是否已经死局：没通关，而且一步都走不了
        /// </summary>
        /// <param name="level">关卡</param>
        /// <param name="state">局面</param>
        /// <returns>死局为 true</returns>
        public static bool IsStuck(LevelDefinition level, GameState state)
        {
            return !IsWin(level, state) && LegalMoves(level, state).Count == 0;
        }

        /// <summary>
        /// 这个人是不是队头
        /// </summary>
        private static bool IsQueueHead(GameState state, LevelDefinition level, int color)
        {
            return state.QueueIndex < level.Passengers.Length && level.Passengers[state.QueueIndex] == color;
        }

        /// <summary>
        /// 这个车位上是不是正好能上这色的人
        /// </summary>
        private static bool CanBoard(GameState state, Move move)
        {
            if (move.Dock < 0 || move.Dock >= state.Docks.Length)
            {
                return false;
            }

            var bus = state.Docks[move.Dock];
            return bus.HasValue && bus.Value.Color == move.Color && bus.Value.HasSeat;
        }

        /// <summary>
        /// 让一个人上车，坐满就换下一辆
        /// </summary>
        private static void Board(GameState state, LevelDefinition level, int dock)
        {
            var bus = state.Docks[dock].Value;
            bus.Loaded++;
            state.Docks[dock] = bus.IsFull ? TakeNextBus(level, state) : bus;
        }

        /// <summary>
        /// 取下一辆进站的车，车发完了就返回空车位
        /// </summary>
        private static Bus? TakeNextBus(LevelDefinition level, GameState state)
        {
            if (state.DeckIndex >= level.BusDeck.Length)
            {
                return null;
            }

            var color = level.BusDeck[state.DeckIndex];
            state.DeckIndex++;
            return new Bus(color, level.BusCapacity);
        }
    }
}
