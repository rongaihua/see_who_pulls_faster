//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 生成入口场景
//---------------------------------------------------------------------------------------

using SeeWho.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeeWho.Editor
{
    /// <summary>
    /// 生成入口场景。菜单在 SeeWho/生成入口场景，也可以命令行 -executeMethod 调用。
    /// </summary>
    public static class EntrySceneBuilder
    {
        /// <summary>
        /// 入口场景路径
        /// </summary>
        private const string ScenePath = "Assets/Scenes/entry.unity";

        /// <summary>
        /// 新建一个空场景，摆上 Entry 物体，存成入口场景并加进 Build Settings
        /// </summary>
        [MenuItem("SeeWho/生成入口场景")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var entryObject = new GameObject("Entry");
            entryObject.AddComponent<GameEntry>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("已生成入口场景：" + ScenePath);
        }
    }
}
