//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: uGUI 的小工具，界面全部用代码搭
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SeeWho.Client
{
    /// <summary>
    /// 用代码搭 uGUI 的辅助方法，省得每个控件都写一遍样板
    /// </summary>
    public static class UiKit
    {
        /// <summary>
        /// 建一个空节点
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名字</param>
        /// <returns>矩形</returns>
        public static RectTransform CreateNode(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>
        /// 摆位置：锚点、轴心、坐标、尺寸
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pivot">轴心</param>
        /// <param name="position">锚点坐标系里的位置</param>
        /// <param name="size">尺寸</param>
        /// <returns>原矩形</returns>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>
        /// 铺满父节点
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <returns>原矩形</returns>
        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// 建一张图
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名字</param>
        /// <param name="sprite">图</param>
        /// <param name="color">颜色</param>
        /// <returns>Image</returns>
        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = CreateNode(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        /// <summary>
        /// 生成一张白色圆角九宫格图，运行时用，省得依赖美术资源。
        /// 边缘按距离做了一像素抗锯齿，放大后不会毛边。
        /// </summary>
        /// <param name="size">贴图边长</param>
        /// <param name="radius">圆角半径</param>
        /// <returns>Sprite</returns>
        public static Sprite CreateRoundedSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            var half = size * 0.5f;
            var inner = half - radius;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    var dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        /// <summary>
        /// 建一个文字
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名字</param>
        /// <param name="content">内容</param>
        /// <param name="size">字号</param>
        /// <param name="color">颜色</param>
        /// <param name="anchor">对齐</param>
        /// <param name="font">字体</param>
        /// <returns>Text</returns>
        public static Text CreateText(Transform parent, string name, string content, int size, Color color,
            TextAnchor anchor, Font font)
        {
            var rect = CreateNode(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// 建一个按钮
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名字</param>
        /// <param name="label">文字</param>
        /// <param name="size">字号</param>
        /// <param name="background">底色</param>
        /// <param name="backgroundSprite">底图</param>
        /// <param name="textColor">字色</param>
        /// <param name="font">字体</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>Button</returns>
        public static Button CreateButton(Transform parent, string name, string label, int size, Color background,
            Sprite backgroundSprite, Color textColor, Font font, UnityAction onClick)
        {
            var rect = CreateNode(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = backgroundSprite;
            image.color = background;
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (backgroundSprite != null && backgroundSprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var text = CreateText(rect, "Label", label, size, textColor, TextAnchor.MiddleCenter, font);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>
        /// 删掉所有子节点。运行时要走 Destroy，编辑模式下 Destroy 不生效，得用 DestroyImmediate。
        /// </summary>
        /// <param name="root">根节点</param>
        public static void Clear(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }
    }
}
