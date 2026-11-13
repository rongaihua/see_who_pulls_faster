//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 球堆玩法的局面：吸球、下落、发车
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SeeWho.Common.Balls
{
    /// <summary>
    /// 一局球堆游戏。
    /// 规则：底下一排是“能被吸到”的球，把车放出去只吸底排同色的，吸完上面的球往下落。
    /// 如果放出去的车一个都没吸到，这一次就白发了，所以要挑着放。
    /// </summary>
    public sealed class BallGame
    {
        /// <summary>
        /// 关卡数据
        /// </summary>
        public BallLevel Level { get; private set; }

        /// <summary>
        /// 当前盘面，和 Level.Cells 同构
        /// </summary>
        public int[] Cells { get; private set; }

        /// <summary>
        /// 每列车位上的车，下标 0 是最前面（能发的那辆）
        /// </summary>
        public List<BusCard>[] Stacks { get; private set; }

        /// <summary>
        /// 下一辆要补进车队的车
        /// </summary>
        public int DeckIndex { get; private set; }

        /// <summary>
        /// 已经发了几辆车
        /// </summary>
        public int Dispatched { get; private set; }

        /// <summary>
        /// 已经吸掉几个球
        /// </summary>
        public int Cleared { get; private set; }

        /// <summary>
        /// 造一局
        /// </summary>
        private BallGame()
        {
        }

        /// <summary>
        /// 按关卡摆好开局
        /// </summary>
        /// <param name="level">关卡</param>
        /// <returns>局面</returns>
        public static BallGame Create(BallLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (!level.IsValid(out var error))
            {
                throw new ArgumentException("关卡数据不对：" + error, nameof(level));
            }

            var game = new BallGame
            {
                Level = level,
                Cells = (int[])level.Cells.Clone(),
                Stacks = new List<BusCard>[level.Columns],
                DeckIndex = 0,
            };

            for (var i = 0; i < level.Columns; i++)
            {
                game.Stacks[i] = new List<BusCard>(level.Visible);
            }

            // 先给每列发满可见的车
            for (var i = 0; i < level.Visible; i++)
            {
                for (var column = 0; column < level.Columns; column++)
                {
                    game.PushNextBus(column);
                }
            }

            return game;
        }

        /// <summary>
        /// 盘上还剩几个球
        /// </summary>
        /// <returns>球数</returns>
        public int Remaining()
        {
            var count = 0;
            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 某列最前面的车，没有返回空
        /// </summary>
        /// <param name="column">列</param>
        /// <returns>车</returns>
        public BusCard? Front(int column)
        {
            if (column < 0 || column >= Stacks.Length || Stacks[column].Count == 0)
            {
                return null;
            }

            return Stacks[column][0];
        }

        /// <summary>
        /// 这辆车放出去能吸到几个球
        /// </summary>
        /// <param name="column">列</param>
        /// <returns>能吸的数量</returns>
        public int PreviewAbsorb(int column)
        {
            var bus = Front(column);
            if (!bus.HasValue)
            {
                return 0;
            }

            var want = bus.Value.Color;
            var capacity = bus.Value.Capacity;
            var taken = 0;

            for (var i = 0; i < Cells.Length && taken < capacity; i++)
            {
                if (Cells[i] == want)
                {
                    taken++;
                }
            }

            return taken;
        }

        /// <summary>
        /// 发一辆车，返回吸到的球数。吸 0 个也算发车。
        /// </summary>
        /// <param name="column">列</param>
        /// <returns>吸到的球数，不合法返回 -1</returns>
        public int Dispatch(int column)
        {
            var bus = Front(column);
            if (!bus.HasValue)
            {
                return -1;
            }

            Stacks[column].RemoveAt(0);

            var absorbed = Absorb(bus.Value);
            Drop();

            Cleared += absorbed;
            Dispatched++;
            PushNextBus(column);

            return absorbed;
        }

        /// <summary>
        /// 通关：球盘空了
        /// </summary>
        public bool IsWin => Remaining() == 0;

        /// <summary>
        /// 没车了但还有球
        /// </summary>
        public bool IsStuck
        {
            get
            {
                if (IsWin)
                {
                    return false;
                }

                for (var i = 0; i < Stacks.Length; i++)
                {
                    if (Stacks[i].Count > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 吸球：从下往上找同色的，最多吸 capacity 个
        /// </summary>
        private int Absorb(BusCard bus)
        {
            var taken = 0;

            for (var y = 0; y < Level.Height && taken < bus.Capacity; y++)
            {
                for (var x = 0; x < Level.Width && taken < bus.Capacity; x++)
                {
                    var index = Level.Index(x, y);
                    if (Cells[index] == bus.Color)
                    {
                        Cells[index] = -1;
                        taken++;
                    }
                }
            }

            return taken;
        }

        /// <summary>
        /// 下落：每列把剩下的球堆到底部
        /// </summary>
        private void Drop()
        {
            for (var x = 0; x < Level.Width; x++)
            {
                var write = 0;
                for (var y = 0; y < Level.Height; y++)
                {
                    var index = Level.Index(x, y);
                    var color = Cells[index];
                    if (color < 0)
                    {
                        continue;
                    }

                    var target = Level.Index(x, write);
                    if (target != index)
                    {
                        Cells[target] = color;
                        Cells[index] = -1;
                    }

                    write++;
                }
            }
        }

        /// <summary>
        /// 往某列尾部补一辆车
        /// </summary>
        private void PushNextBus(int column)
        {
            if (DeckIndex >= Level.Deck.Length)
            {
                return;
            }

            if (Stacks[column].Count >= Level.Visible)
            {
                return;
            }

            Stacks[column].Add(Level.Deck[DeckIndex]);
            DeckIndex++;
        }
    }
}
