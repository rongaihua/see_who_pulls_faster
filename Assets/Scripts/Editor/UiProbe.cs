//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 临时诊断：看看点位能不能被 UI 射线打到
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection;
using SeeWho.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeeWho.Editor
{
    /// <summary>
    /// 把界面搭出来，对每张巴士卡做一次 UI 射线，打印命中的对象和有没有 Button
    /// </summary>
    public static class UiProbe
    {
        /// <summary>
        /// 跑一遍诊断
        /// </summary>
        [MenuItem("SeeWho/诊断点击")]
        public static void Probe()
        {
            var root = new GameObject("ProbeRoot");
            var camera = new GameObject("ProbeCamera").AddComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);

            try
            {
                var view = root.AddComponent<GameView>();
                var awake = typeof(GameView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                awake?.Invoke(view, null);
                view.StartGame(1);

                var canvas = root.GetComponentInChildren<Canvas>(true);
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;

                camera.orthographic = true;
                camera.orthographicSize = 960f;
                camera.transform.position = new Vector3(540f, 960f, -10f);
                camera.targetTexture = target;
                target.Create();

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
                Canvas.ForceUpdateCanvases();

                var eventSystem = Object.FindObjectOfType<EventSystem>();
                Debug.Log("probe: EventSystem=" + (eventSystem != null) +
                          " 输入模块=" + (eventSystem != null ? eventSystem.currentInputModule?.GetType().Name : "null"));

                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                var buttons = canvas.GetComponentsInChildren<Button>(true);
                Debug.Log("probe: Button 数量=" + buttons.Length);

                var hits = new List<RaycastResult>();
                for (var i = 0; i < buttons.Length; i++)
                {
                    var rect = (RectTransform)buttons[i].transform;
                    var world = rect.TransformPoint(rect.rect.center);
                    var screen = camera.WorldToScreenPoint(world);

                    var pointer = new PointerEventData(EventSystem.current) { position = screen };
                    hits.Clear();
                    raycaster.Raycast(pointer, hits);

                    var top = hits.Count > 0 ? hits[0].gameObject.name : "无";
                    var hitButton = hits.Count > 0 && hits[0].gameObject.GetComponent<Button>() != null;
                    Debug.Log("probe: " + buttons[i].name + " 屏幕点 " + screen.ToString("F0") +
                              " 命中 " + top + " 是按钮=" + hitButton);
                }
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(camera.gameObject);
                Object.DestroyImmediate(root);
            }
        }
    }
}
