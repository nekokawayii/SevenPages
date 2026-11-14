using System;
using SevenPages.Core;
using UnityEngine;

namespace SevenPages.Game
{
    /// <summary>
    /// 卡牌数据加载。优先读 Resources/cards.json（由编辑器脚本同步），
    /// 其次读工程根 cards/cards.json，保证模板期只有一份卡池数据。
    /// </summary>
    public static class CardDataLoader
    {
        public static CardSetDto Load()
        {
            string json = null;

            TextAsset asset = Resources.Load<TextAsset>("cards");
            if (asset != null) json = asset.text;

            if (string.IsNullOrEmpty(json))
            {
                string path = System.IO.Path.Combine(Application.dataPath, "../cards/cards.json");
                if (System.IO.File.Exists(path)) json = System.IO.File.ReadAllText(path);
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[七页轮回] 找不到 cards.json（Resources/cards.json 或 ../cards/cards.json）");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<CardSetDto>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[七页轮回] cards.json 解析失败：" + ex.Message);
                return null;
            }
        }

        public static CardDatabase BuildDatabase(CardSetDto set)
        {
            var db = new CardDatabase();
            for (int i = 0; i < set.cards.Length; i++)
            {
                db.Add(set.cards[i].ToCardDef());
            }
            return db;
        }
    }
}
