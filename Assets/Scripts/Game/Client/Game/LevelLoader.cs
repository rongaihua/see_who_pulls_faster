//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 读原作导出的关卡数据
//---------------------------------------------------------------------------------------

using SeeWho.Common.Balls;
using UnityEngine;

namespace SeeWho.Client
{
    /// <summary>
    /// 原作关卡 JSON 的结构。字段名和导出脚本里的一致。
    /// </summary>
    [System.Serializable]
    public sealed class LevelData
    {
        /// <summary>
        /// 原作关卡 id，比如 1022
        /// </summary>
        public int id;

        /// <summary>
        /// 球盘宽
        /// </summary>
        public int width;

        /// <summary>
        /// 球盘高
        /// </summary>
        public int height;

        /// <summary>
        /// 每个格子的颜色，-1 是空
        /// </summary>
        public int[] cells;

        /// <summary>
        /// 车队
        /// </summary>
        public DeckEntry[] deck;
    }

    /// <summary>
    /// 一辆车的序列化形式
    /// </summary>
    [System.Serializable]
    public sealed class DeckEntry
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public int color;

        /// <summary>
        /// 容量
        /// </summary>
        public int capacity;
    }

    /// <summary>
    /// 读 Resources/SeeWho/Levels/level_XXXX.json
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>
        /// 试试读原作关卡，读不到就返回 false，让调用方回退到程序生成
        /// </summary>
        /// <param name="number">第几关（界面上从 1 开始）</param>
        /// <param name="level">读到的关卡</param>
        /// <returns>读到了为 true</returns>
        public static bool TryLoad(int number, out BallLevel level)
        {
            level = null;

            var id = 1000 + number;
            var asset = Resources.Load<TextAsset>("SeeWho/Levels/level_" + id);
            if (asset == null)
            {
                return false;
            }

            LevelData data;
            try
            {
                data = JsonUtility.FromJson<LevelData>(asset.text);
            }
            catch
            {
                return false;
            }

            if (data == null || data.cells == null || data.deck == null || data.deck.Length == 0)
            {
                return false;
            }

            if (data.width < 1 || data.height < 1 || data.cells.Length != data.width * data.height)
            {
                return false;
            }

            var deck = new BusCard[data.deck.Length];
            for (var i = 0; i < data.deck.Length; i++)
            {
                deck[i] = new BusCard(data.deck[i].color, data.deck[i].capacity);
            }

            // 导出时球是随机铺的，这里让每列先落到底，避免空位夹在中间
            var cells = (int[])data.cells.Clone();
            for (var x = 0; x < data.width; x++)
            {
                var write = 0;
                for (var y = 0; y < data.height; y++)
                {
                    var index = y * data.width + x;
                    if (cells[index] < 0)
                    {
                        continue;
                    }

                    var target = write * data.width + x;
                    if (target != index)
                    {
                        cells[target] = cells[index];
                        cells[index] = -1;
                    }

                    write++;
                }
            }

            var colorCount = 2;
            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] + 1 > colorCount)
                {
                    colorCount = cells[i] + 1;
                }
            }
            var result = new BallLevel
            {
                Id = "real-" + id,
                Width = data.width,
                Height = data.height,
                ColorCount = colorCount,
                Columns = 4,
                Visible = 3,
                Cells = cells,
                Deck = deck,
            };

            if (!result.IsValid(out _))
            {
                return false;
            }

            level = result;
            return true;
        }
    }
}
