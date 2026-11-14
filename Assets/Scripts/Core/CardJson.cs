using System;

namespace SevenPages.Core
{
    /// <summary>
    /// 卡牌 JSON 的传输对象（DTO）。
    /// Unity 端用 JsonUtility 解析，.NET 端用 System.Text.Json 解析，
    /// 两边都转成 Core 的 CardDef——保证规则只有一个来源。
    /// </summary>
    [Serializable]
    public sealed class CardSetDto
    {
        public CardDto[] cards;
        public string[] starterDeck;
    }

    [Serializable]
    public sealed class CardDto
    {
        public string id;
        public string name;
        public string type;      // Spell | Curse | Rite
        public string target;    // None | Self | Enemy
        public int cost;
        public string text;
        public string art;
        public EffectDto[] effects;

        public CardDef ToCardDef()
        {
            var def = new CardDef();
            def.Id = id;
            def.Name = name;
            def.Cost = cost;
            def.Text = text;
            def.Art = art;
            def.Type = ParseEnum(type, CardType.Spell);
            def.Target = ParseEnum(target, TargetKind.None);

            if (effects != null)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    EffectSpec spec = effects[i].ToSpec();
                    if (spec != null) def.Effects.Add(spec);
                }
            }
            return def;
        }

        private static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            try
            {
                if (Enum.IsDefined(typeof(T), raw)) return (T)Enum.Parse(typeof(T), raw);
            }
            catch
            {
                // 忽略非法值，回退默认
            }
            return fallback;
        }
    }

    [Serializable]
    public sealed class EffectDto
    {
        public string kind;      // Damage | Heal | Draw | GainSpirit | ApplyRite | ConditionalDamage | Ward
        public int amount;
        public int threshold;
        public int extraAmount;
        public string riteId;
        public string riteName;
        public int riteTurns;
        public int riteDamage;
        public int riteDraw;

        public EffectSpec ToSpec()
        {
            EffectKind parsed;
            if (string.IsNullOrEmpty(kind) || !Enum.IsDefined(typeof(EffectKind), kind)) return null;

            parsed = (EffectKind)Enum.Parse(typeof(EffectKind), kind);
            var spec = new EffectSpec(parsed, amount);
            spec.Threshold = threshold;
            spec.ExtraAmount = extraAmount;
            spec.RiteId = riteId;
            spec.RiteName = riteName;
            spec.RiteTurns = riteTurns;
            spec.RiteDamage = riteDamage;
            spec.RiteDraw = riteDraw;
            return spec;
        }
    }
}
