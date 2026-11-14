using System;
using System.Collections.Generic;

namespace SevenPages.Core
{
    /// <summary>卡池。卡牌定义按 ID 索引，卡组用「ID 列表」描述（重复即多张）。</summary>
    public sealed class CardDatabase
    {
        private readonly Dictionary<string, CardDef> _byId = new Dictionary<string, CardDef>();
        private readonly List<CardDef> _all = new List<CardDef>();

        public IReadOnlyList<CardDef> All { get { return _all; } }

        public void Add(CardDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
                throw new ArgumentException("卡牌必须有 Id");
            if (_byId.ContainsKey(def.Id))
                throw new ArgumentException("卡牌 ID 重复：" + def.Id);

            _byId.Add(def.Id, def);
            _all.Add(def);
        }

        public CardDef Get(string id)
        {
            CardDef def;
            if (id != null && _byId.TryGetValue(id, out def)) return def;
            return null;
        }

        public bool Contains(string id)
        {
            return id != null && _byId.ContainsKey(id);
        }

        /// <summary>按配方构造一组卡牌实例（配方里重复的 ID 会生成多张）。</summary>
        public List<CardInstance> BuildDeck(IList<string> recipe)
        {
            var deck = new List<CardInstance>();
            if (recipe == null) return deck;

            for (int i = 0; i < recipe.Count; i++)
            {
                CardDef def = Get(recipe[i]);
                if (def == null)
                    throw new ArgumentException("卡组引用了不存在的卡牌：" + recipe[i]);
                deck.Add(new CardInstance(def));
            }
            return deck;
        }
    }
}
