//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 客户端入口
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace SeeWho.Client
{
    /// <summary>
    /// 客户端入口，挂在 entry 场景的 Entry 物体上。
    /// 目前只负责初始化 libra 的 App 和准备相机，玩法后面再挂到 Main 模块上。
    /// </summary>
    public class GameEntry : MonoBehaviour
    {
        /// <summary>
        /// 当前实例
        /// </summary>
        private static GameEntry sInstance;

        /// <summary>
        /// 当前实例
        /// </summary>
        public static GameEntry Instance => sInstance;

        /// <summary>
        /// 主相机
        /// </summary>
        public Camera MainCamera { get; private set; }

        /// <summary>
        /// 唤醒
        /// </summary>
        private void Awake()
        {
            sInstance = this;
            MyGame.G.Init();
            SetupCamera();
        }

        /// <summary>
        /// 每帧
        /// </summary>
        private void Update()
        {
            if (MyGame.G.IsReady)
            {
                MyGame.G.App.Update(Mathf.RoundToInt(Time.deltaTime * 1000f));
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            sInstance = null;
            MyGame.G.Destroy();
        }

        /// <summary>
        /// 准备正交相机
        /// </summary>
        private void SetupCamera()
        {
            var mainCamera = Camera.main;
            if (!mainCamera)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            MainCamera = mainCamera;
        }
    }
}
