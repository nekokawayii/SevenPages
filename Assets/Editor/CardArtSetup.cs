using System.IO;
using UnityEditor;
using UnityEngine;

namespace SevenPages.EditorTools
{
    /// <summary>把卡图批量设为 Sprite 并压缩，供 uGUI 直接使用。</summary>
    public static class CardArtSetup
    {
        public const string CardArtFolder = "Assets/Resources/Art/Cards";

        [MenuItem("七页轮回/⑤ 设置卡图导入")]
        public static void SetupCardArt()
        {
            if (!Directory.Exists(CardArtFolder))
            {
                Debug.LogWarning("[七页轮回] 卡图目录不存在：" + CardArtFolder);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CardArtFolder });
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (importer.maxTextureSize != 512)
                {
                    importer.maxTextureSize = 512;
                    dirty = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Compressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log(string.Format("[七页轮回] 卡图导入设置完成，处理 {0} 张（变更 {1} 张）", guids.Length, changed));
        }
    }
}
