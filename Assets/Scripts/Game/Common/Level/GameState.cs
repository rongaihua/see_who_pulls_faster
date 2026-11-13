//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 运行中的局面，以及局面去重用的 Key
//---------------------------------------------------------------------------------------

using System;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 一局进行到某一步的局面。
    /// 队伍是先进先出的：只有队头那个人能上车或者去候车区，前面不走后面动不了。
    /// 候车区里的人则可以随时上车，这是玩家唯一的调度余地。
    /// </summary>
    public sealed class GameState
    {
        /// <summary>
        /// 队头在 LevelDefinition.Passengers 里的下标
        /// </summary>
        public int QueueIndex;

        /// <summary>
        /// 候车区，长度 WaitingSlots，-1 表示空位。
        /// 始终按颜色升序、-1 排在最后，保证同一种站法只有一种表示。
        /// </summary>
        public int[] Waiting;

        /// <summary>
        /// 停靠位上的车，null 表示这个车位空着
        /// </summary>
        public Bus?[] Docks;

        /// <summary>
        /// 下一辆要进站的车在 BusDeck 里的下标
        /// </summary>
        public int DeckIndex;

        /// <summary>
        /// 候车区现在站了几个人
        /// </summary>
        /// <returns>人数</returns>
        public int WaitingCount()
        {
            var count = 0;
            for (var i = 0; i < Waiting.Length; i++)
            {
                if (Waiting[i] >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>新的局面</returns>
        public GameState Clone()
        {
            return new GameState
            {
                QueueIndex = QueueIndex,
                Waiting = (int[])Waiting.Clone(),
                Docks = (Bus?[])Docks.Clone(),
                DeckIndex = DeckIndex,
            };
        }

        /// <summary>
        /// 候车区排序：颜色升序，空位（-1）挪到最后
        /// </summary>
        public void NormalizeWaiting()
        {
            Array.Sort(Waiting);
        }

        /// <summary>
        /// 候车区里第一个空位，没有返回 -1
        /// </summary>
        /// <returns>空位下标</returns>
        public int FirstEmptyWaiting()
        {
            for (var i = 0; i < Waiting.Length; i++)
            {
                if (Waiting[i] < 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 候车区里第一个这色的人，没有返回 -1
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>下标</returns>
        public int FirstWaitingColor(int color)
        {
            for (var i = 0; i < Waiting.Length; i++)
            {
                if (Waiting[i] == color)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// 局面去重用的键。
    /// 限制：颜色不超过 8 种、车位不超过 8 个、座位不超过 7 个、候车区不超过 15 个。
    /// </summary>
    public readonly struct StateKey : IEquatable<StateKey>
    {
        /// <summary>
        /// 队头下标
        /// </summary>
        private readonly int mQueueIndex;

        /// <summary>
        /// 候车区各色人数，每色 4 位
        /// </summary>
        private readonly ulong mWaiting;

        /// <summary>
        /// 各车位的车，每个车位 6 位（颜色 3 位 + 已载 3 位），颜色 7 表示空车位
        /// </summary>
        private readonly ulong mDocks;

        /// <summary>
        /// 下一辆进站车的下标
        /// </summary>
        private readonly int mDeckIndex;

        /// <summary>
        /// 造一个键
        /// </summary>
        private StateKey(int queueIndex, ulong waiting, ulong docks, int deckIndex)
        {
            mQueueIndex = queueIndex;
            mWaiting = waiting;
            mDocks = docks;
            mDeckIndex = deckIndex;
        }

        /// <summary>
        /// 从局面算键
        /// </summary>
        /// <param name="state">局面</param>
        /// <returns>键</returns>
        public static StateKey From(GameState state)
        {
            var waitingCounts = new int[8];
            for (var i = 0; i < state.Waiting.Length; i++)
            {
                var color = state.Waiting[i];
                if (color >= 0 && color < 8)
                {
                    waitingCounts[color]++;
                }
            }

            var waiting = 0UL;
            for (var i = 0; i < 8; i++)
            {
                waiting |= (ulong)(waitingCounts[i] & 0x0F) << (i * 4);
            }

            var docks = 0UL;
            for (var i = 0; i < state.Docks.Length && i < 8; i++)
            {
                var bus = state.Docks[i];
                var packed = 0x07UL;
                if (bus.HasValue)
                {
                    packed = (ulong)((bus.Value.Color & 0x07) | ((bus.Value.Loaded & 0x07) << 3));
                }

                docks |= packed << (i * 6);
            }

            return new StateKey(state.QueueIndex, waiting, docks, state.DeckIndex);
        }

        /// <summary>
        /// 相等
        /// </summary>
        public bool Equals(StateKey other)
        {
            return mQueueIndex == other.mQueueIndex
                   && mWaiting == other.mWaiting
                   && mDocks == other.mDocks
                   && mDeckIndex == other.mDeckIndex;
        }

        /// <summary>
        /// 相等
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is StateKey other && Equals(other);
        }

        /// <summary>
        /// 哈希
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = mQueueIndex;
                hash = (hash * 397) ^ (int)mWaiting;
                hash = (hash * 397) ^ (int)(mWaiting >> 32);
                hash = (hash * 397) ^ (int)mDocks;
                hash = (hash * 397) ^ (int)(mDocks >> 32);
                hash = (hash * 397) ^ mDeckIndex;
                return hash;
            }
        }
    }
}
