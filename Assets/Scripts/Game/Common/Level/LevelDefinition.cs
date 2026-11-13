//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 关卡的静态数据
//---------------------------------------------------------------------------------------

using System;

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 一关的静态数据。纯数据，不带任何运行时状态，方便编辑器导出 JSON、也方便单测构造。
    /// </summary>
    public sealed class LevelDefinition
    {
        /// <summary>
        /// 关卡编号，给日志和存档用
        /// </summary>
        public string Id = string.Empty;

        /// <summary>
        /// 颜色总数，乘客和车的颜色索引都落在 [0, ColorCount) 里
        /// </summary>
        public int ColorCount = 3;

        /// <summary>
        /// 每辆车的座位数
        /// </summary>
        public int BusCapacity = 3;

        /// <summary>
        /// 同时能停几辆车
        /// </summary>
        public int DockCount = 3;

        /// <summary>
        /// 候车区能站几个人
        /// </summary>
        public int WaitingSlots = 5;

        /// <summary>
        /// 队伍里每个人的颜色，按站位从前往后
        /// </summary>
        public int[] Passengers = Array.Empty<int>();

        /// <summary>
        /// 车的出场顺序，颜色索引。前 DockCount 辆一开场就停在车位上
        /// </summary>
        public int[] BusDeck = Array.Empty<int>();

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>新的关卡对象</returns>
        public LevelDefinition Clone()
        {
            return new LevelDefinition
            {
                Id = Id,
                ColorCount = ColorCount,
                BusCapacity = BusCapacity,
                DockCount = DockCount,
                WaitingSlots = WaitingSlots,
                Passengers = (int[])Passengers.Clone(),
                BusDeck = (int[])BusDeck.Clone(),
            };
        }

        /// <summary>
        /// 检查数据是否自洽
        /// </summary>
        /// <param name="error">不自洽时的原因</param>
        /// <returns>自洽为 true</returns>
        public bool IsValid(out string error)
        {
            error = null;

            if (ColorCount < 1)
            {
                error = "ColorCount 必须大于 0";
                return false;
            }

            if (BusCapacity < 1)
            {
                error = "BusCapacity 必须大于 0";
                return false;
            }

            if (DockCount < 1)
            {
                error = "DockCount 必须大于 0";
                return false;
            }

            if (WaitingSlots < 0)
            {
                error = "WaitingSlots 不能为负";
                return false;
            }

            if (Passengers == null || BusDeck == null)
            {
                error = "Passengers / BusDeck 不能为空";
                return false;
            }

            for (var i = 0; i < Passengers.Length; i++)
            {
                if (Passengers[i] < 0 || Passengers[i] >= ColorCount)
                {
                    error = "Passengers[" + i + "] 颜色越界";
                    return false;
                }
            }

            for (var i = 0; i < BusDeck.Length; i++)
            {
                if (BusDeck[i] < 0 || BusDeck[i] >= ColorCount)
                {
                    error = "BusDeck[" + i + "] 颜色越界";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 这关最多能装下多少人，用来快速判断车够不够
        /// </summary>
        /// <returns>座位总数</returns>
        public int TotalSeats => BusDeck.Length * BusCapacity;

        /// <summary>
        /// 每种颜色的乘客各有多少
        /// </summary>
        /// <returns>长度为 ColorCount 的计数数组</returns>
        public int[] CountPassengersByColor()
        {
            var counts = new int[ColorCount];
            for (var i = 0; i < Passengers.Length; i++)
            {
                counts[Passengers[i]]++;
            }

            return counts;
        }

        /// <summary>
        /// 打印成一行，调试用
        /// </summary>
        /// <returns>关卡摘要</returns>
        public override string ToString()
        {
            return "Level(" + Id + ", 乘客 " + Passengers.Length + ", 车 " + BusDeck.Length +
                   ", 颜色 " + ColorCount + ", 座位 " + BusCapacity + ", 车位 " + DockCount +
                   ", 候车 " + WaitingSlots + ")";
        }
    }
}
