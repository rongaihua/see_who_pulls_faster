//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 一辆停靠在车位的公交车
//---------------------------------------------------------------------------------------

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 一辆公交车。颜色决定谁能上，容量决定能上几个。
    /// 结构体，复制即快照，求解时不用深拷贝。
    /// </summary>
    public struct Bus
    {
        /// <summary>
        /// 颜色索引，和乘客的颜色对应
        /// </summary>
        public int Color;

        /// <summary>
        /// 座位数
        /// </summary>
        public int Capacity;

        /// <summary>
        /// 已经上车的人数
        /// </summary>
        public int Loaded;

        /// <summary>
        /// 造一辆车
        /// </summary>
        /// <param name="color">颜色索引</param>
        /// <param name="capacity">座位数</param>
        /// <param name="loaded">已上车人数</param>
        public Bus(int color, int capacity, int loaded = 0)
        {
            Color = color;
            Capacity = capacity;
            Loaded = loaded;
        }

        /// <summary>
        /// 还有空座
        /// </summary>
        public bool HasSeat => Loaded < Capacity;

        /// <summary>
        /// 坐满了，该发车了
        /// </summary>
        public bool IsFull => Loaded >= Capacity;
    }
}
