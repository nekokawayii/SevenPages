using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SevenPages.EditorTools
{
    /// <summary>
    /// 命令行带 -sevenpages-autoplay 启动编辑器时，自动打开 Battle 场景并进入 Play Mode。
    /// 用于无交互环境下自动验证画面。
    /// </summary>
    [InitializeOnLoad]
    public static class AutoPlayOnLoad
    {
        private const string ScenePath = "Assets/Scenes/Battle.unity";
        private const string SessionKey = "SevenPages.AutoPlayed";

        static AutoPlayOnLoad()
        {
            EditorApplication.delayCall += TryAutoPlay;
        }

        /// <summary>供 -executeMethod 调用：打开 Battle 场景并进入 Play Mode。</summary>
        public static void StartPlay()
        {
            Debug.Log("[七页轮回] StartPlay 被调用");
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[七页轮回] 场景不存在：" + ScenePath);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[七页轮回] 场景已打开，准备进入 Play Mode");

            EditorApplication.delayCall += delegate
            {
                Debug.Log("[七页轮回] 进入 Play Mode");
                EditorApplication.isPlaying = true;
            };
        }

        /// <summary>菜单入口，方便人工触发。</summary>
        [MenuItem("七页轮回/④ 自动播放")]
        public static void MenuAutoPlay()
        {
            StartPlay();
        }

        private static void TryAutoPlay()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-sevenpages-autoplay") < 0) return;
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            Debug.Log("[七页轮回] 检测到自动播放参数，打开场景并进入 Play Mode");

            if (System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            else
            {
                Debug.LogError("[七页轮回] 场景不存在：" + ScenePath);
                return;
            }

            EditorApplication.delayCall += delegate
            {
                EditorApplication.isPlaying = true;
            };
        }
    }
}
