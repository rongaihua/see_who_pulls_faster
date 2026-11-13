//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 球堆玩法的数据：球盘、车队
//---------------------------------------------------------------------------------------

using System;

namespace SeeWho.Common.Balls
{
    /// <summary>
    /// 一辆车。颜色决定吸什么球，容量决定吸几个。
    /// </summary>
    public struct BusCard
    {
        /// <summary>
        /// 颜色索引
        /// </summary>
        public int Color;

        /// <summary>
        /// 能吸几个球
        /// </summary>
        public int Capacity;

        /// <summary>
        /// 造一辆车
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="capacity">容量</param>
        public BusCard(int color, int capacity)
        {
            Color = color;
            Capacity = capacity;
        }

        /// <summary>
        /// 车身上印的数字，和原作一样按 10 分一个人算
        /// </summary>
        public int Points => Capacity * 10;

        /// <summary>
        /// 打印成短串
        /// </summary>
        /// <returns>可读的车</returns>
        public override string ToString()
        {
            return "车(色" + Color + ", 容量" + Capacity + ")";
        }
    }

    /// <summary>
    /// 一关的静态数据：一个球盘加一副车队。
    /// </summary>
    public sealed class BallLevel
    {
        /// <summary>
        /// 关卡编号
        /// </summary>
        public string Id = string.Empty;

        /// <summary>
        /// 球盘宽（列数）
        /// </summary>
        public int Width = 12;

        /// <summary>
        /// 球盘高（行数），第 0 行在最下面
        /// </summary>
        public int Height = 9;

        /// <summary>
        /// 颜色数
        /// </summary>
        public int ColorCount = 5;

        /// <summary>
        /// 底部几列车位
        /// </summary>
        public int Columns = 4;

        /// <summary>
        /// 每列显示几辆车
        /// </summary>
        public int Visible = 3;

        /// <summary>
        /// 球的颜色，长度 Width * Height，-1 表示空
        /// </summary>
        public int[] Cells = Array.Empty<int>();

        /// <summary>
        /// 车队，按发车顺序
        /// </summary>
        public BusCard[] Deck = Array.Empty<BusCard>();

        /// <summary>
        /// 后续从顶部掉下来补位的人，按顺序
        /// </summary>
        public int[] Refill = Array.Empty<int>();

        /// <summary>
        /// 球格总数
        /// </summary>
        public int CellCount => Width * Height;

        /// <summary>
        /// 把二维坐标转成下标，y 从下往上
        /// </summary>
        /// <param name="x">列</param>
        /// <param name="y">行</param>
        /// <returns>下标</returns>
        public int Index(int x, int y)
        {
            return y * Width + x;
        }

        /// <summary>
        /// 检查数据是否自洽
        /// </summary>
        /// <param name="error">不自洽的原因</param>
        /// <returns>自洽为 true</returns>
        public bool IsValid(out string error)
        {
            error = null;

            if (Width < 1 || Height < 1)
            {
                error = "球盘尺寸必须大于 0";
                return false;
            }

            if (Cells == null || Cells.Length != CellCount)
            {
                error = "Cells 长度和球盘尺寸对不上";
                return false;
            }

            if (Deck == null || Deck.Length == 0)
            {
                error = "车队不能为空";
                return false;
            }

            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] < -1 || Cells[i] >= ColorCount)
                {
                    error = "Cells[" + i + "] 颜色越界";
                    return false;
                }
            }

            for (var i = 0; i < Deck.Length; i++)
            {
                if (Deck[i].Color < 0 || Deck[i].Color >= ColorCount)
                {
                    error = "Deck[" + i + "] 颜色越界";
                    return false;
                }

                if (Deck[i].Capacity < 1)
                {
                    error = "Deck[" + i + "] 容量必须大于 0";
                    return false;
                }
            }

            if (Refill != null)
            {
                for (var i = 0; i < Refill.Length; i++)
                {
                    if (Refill[i] < 0 || Refill[i] >= ColorCount)
                    {
                        error = "Refill[" + i + "] 颜色越界";
                        return false;
                    }
                }
            }

            if (Columns < 1 || Visible < 1)
            {
                error = "车位列数和可见数必须大于 0";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 盘上每种颜色还剩几个球
        /// </summary>
        /// <returns>长度为 ColorCount 的计数</returns>
        public int[] CountColors()
        {
            var counts = new int[ColorCount];
            for (var i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] >= 0)
                {
                    counts[Cells[i]]++;
                }
            }

            return counts;
        }

        /// <summary>
        /// 打印成一行
        /// </summary>
        /// <returns>摘要</returns>
        public override string ToString()
        {
            return "BallLevel(" + Id + ", " + Width + "x" + Height + ", 色 " + ColorCount +
                   ", 车 " + Deck.Length + ")";
        }
    }
}
