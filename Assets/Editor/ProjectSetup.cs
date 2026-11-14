using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SevenPages.EditorTools
{
    /// <summary>
    /// 工程初始化工具：同步卡牌数据、生成对战场景。
    /// 可在编辑器菜单「七页轮回」里手动执行，也可用命令行：
    ///   Unity.exe -batchmode -projectPath &lt;工程&gt; -executeMethod SevenPages.EditorTools.ProjectSetup.SetupAll -quit
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("七页轮回/① 同步卡牌数据")]
        public static void SyncCards()
        {
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../cards/cards.json"));
            string resDir = Path.Combine(Application.dataPath, "Resources");
            string target = Path.Combine(resDir, "cards.json");

            if (!File.Exists(source))
            {
                Debug.LogError("[七页轮回] 找不到卡牌源文件：" + source);
                return;
            }

            Directory.CreateDirectory(resDir);
            File.Copy(source, target, true);
            AssetDatabase.Refresh();
            Debug.Log("[七页轮回] 卡牌数据已同步 -> " + target);
        }

        [MenuItem("七页轮回/② 生成对战场景")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相机（Bootstrap 也会兜底创建，这里先放一个让编辑器视图正常）
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();

            // 游戏入口
            var gameGo = new GameObject("Game");
            gameGo.AddComponent<SevenPages.Game.GameBootstrap>();

            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Debug.Log("[七页轮回] 场景已生成：" + ScenePath);
        }

        [MenuItem("七页轮回/③ 一键初始化")]
        public static void SetupAll()
        {
            SyncCards();
            BuildScene();
            Debug.Log("[七页轮回] 初始化完成 ✅");
        }
    }
}
