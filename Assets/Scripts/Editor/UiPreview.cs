//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 不开编辑器也能把界面渲染成 PNG，用来检查排版
//---------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using SeeWho.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeeWho.Editor
{
    /// <summary>
    /// 把 GameView 的界面渲染成 1080x1920 的 PNG。
    /// 菜单在 SeeWho/截界面预览，也可以命令行调用，方便没有显示器时检查排版。
    /// </summary>
    public static class UiPreview
    {
        /// <summary>
        /// 渲染三张预览图
        /// </summary>
        [MenuItem("SeeWho/截界面预览")]
        public static void Capture()
        {
            Render(1, "Preview/ui_easy.png", false);
            Render(13, "Preview/ui_hard.png", false);
            Render(6, "Preview/ui_mid.png", true);
        }

        /// <summary>
        /// 渲染指定关卡的界面
        /// </summary>
        /// <param name="level">关号</param>
        /// <param name="path">输出路径，相对工程根目录</param>
        /// <param name="playMoves">是否先走几步，用来检查中途状态</param>
        private static void Render(int level, string path, bool playMoves)
        {
            var root = new GameObject("PreviewRoot");
            Camera camera = null;
            RenderTexture target = null;

            try
            {
                var view = root.AddComponent<GameView>();

                // GameView 的 Awake 是私有的，编辑模式下不会自己跑，这里手动调一次
                var awake = typeof(GameView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                if (awake != null)
                {
                    awake.Invoke(view, null);
                }

                view.StartGame(level);

                if (playMoves)
                {
                    view.DebugAutoPlay(6);
                }

                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas == null)
                {
                    Debug.LogError("没找到 Canvas");
                    return;
                }

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f;
                }

                var cameraObject = new GameObject("PreviewCamera");
                camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 960f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.10f, 0.12f, 0.18f);
                camera.transform.position = new Vector3(540f, 960f, -10f);

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;

                Canvas.ForceUpdateCanvases();

                target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                target.Create();
                camera.targetTexture = target;
                camera.Render();

                var texture = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                Debug.Log("界面预览已输出：" + Path.GetFullPath(path));
            }
            finally
            {
                if (camera != null)
                {
                    camera.targetTexture = null;
                    Object.DestroyImmediate(camera.gameObject);
                }

                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }

                Object.DestroyImmediate(root);
            }
        }
    }
}
