//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 一步操作
//---------------------------------------------------------------------------------------

namespace SeeWho.Common.Level
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public enum MoveKind
    {
        /// <summary>
        /// 队头直接上车
        /// </summary>
        BoardFromQueue = 0,

        /// <summary>
        /// 队头去候车区
        /// </summary>
        ParkFromQueue = 1,

        /// <summary>
        /// 候车区里的人上车
        /// </summary>
        BoardFromWaiting = 2,
    }

    /// <summary>
    /// 一步操作
    /// </summary>
    public readonly struct Move
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public readonly MoveKind Kind;

        /// <summary>
        /// 颜色
        /// </summary>
        public readonly int Color;

        /// <summary>
        /// 上的哪个车位，ParkFromQueue 时为 -1
        /// </summary>
        public readonly int Dock;

        /// <summary>
        /// 造一步操作
        /// </summary>
        /// <param name="kind">类型</param>
        /// <param name="color">颜色</param>
        /// <param name="dock">车位下标</param>
        public Move(MoveKind kind, int color, int dock)
        {
            Kind = kind;
            Color = color;
            Dock = dock;
        }

        /// <summary>
        /// 打印成短串，日志和测试用
        /// </summary>
        /// <returns>可读的操作</returns>
        public override string ToString()
        {
            switch (Kind)
            {
                case MoveKind.BoardFromQueue:
                    return "上车(队头, 色" + Color + ", 位" + Dock + ")";
                case MoveKind.ParkFromQueue:
                    return "候车(队头, 色" + Color + ")";
                default:
                    return "上车(候车, 色" + Color + ", 位" + Dock + ")";
            }
        }
    }
}
