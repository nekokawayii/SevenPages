using System.Collections.Generic;

namespace SevenPages.Core
{
    /// <summary>卡牌大类。</summary>
    public enum CardType
    {
        /// <summary>术：即时生效。</summary>
        Spell,
        /// <summary>咒：负面牌，抽到后不可主动打出。</summary>
        Curse,
        /// <summary>式：结附状态。</summary>
        Rite
    }

    /// <summary>目标类型。</summary>
    public enum TargetKind
    {
        None,
        Self,
        Enemy
    }

    /// <summary>效果种类。效果是数据，由 EffectResolver 统一解释。</summary>
    public enum EffectKind
    {
        /// <summary>造成墨伤。</summary>
        Damage,
        /// <summary>恢复墨（上限为本页墨量）。</summary>
        Heal,
        /// <summary>抽牌。</summary>
        Draw,
        /// <summary>获得灵。</summary>
        GainSpirit,
        /// <summary>结附一个「式」到目标身上。</summary>
        ApplyRite,
        /// <summary>条件伤害：目标当前页墨量 ≤ Threshold 时，额外造成 ExtraAmount。</summary>
        ConditionalDamage,
        /// <summary>获得本回合减伤护盾。</summary>
        Ward,
        /// <summary>吸血：对目标造成伤害，并为自己恢复等量墨。</summary>
        Lifesteal
    }

    /// <summary>一条效果的数据描述。</summary>
    public sealed class EffectSpec
    {
        public EffectKind Kind;
        public int Amount;
        public int Threshold;
        public int ExtraAmount;
        public string RiteId;
        public string RiteName;
        public int RiteTurns;
        public int RiteDamage;
        public int RiteDraw;

        public EffectSpec() { }

        public EffectSpec(EffectKind kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    /// <summary>卡牌定义（不可变模板）。</summary>
    public sealed class CardDef
    {
        public string Id = "";
        public string Name = "";
        public CardType Type = CardType.Spell;
        public int Cost;
        public TargetKind Target = TargetKind.None;
        public string Text = "";
        public string Art = "";
        public List<EffectSpec> Effects = new List<EffectSpec>();

        /// <summary>诅咒类卡牌不能主动打出。</summary>
        public bool IsPlayable { get { return Type != CardType.Curse; } }

        public int TotalDamage
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < Effects.Count; i++)
                {
                    if (Effects[i].Kind == EffectKind.Damage) sum += Effects[i].Amount;
                    else if (Effects[i].Kind == EffectKind.ConditionalDamage) sum += Effects[i].Amount + Effects[i].ExtraAmount;
                }
                return sum;
            }
        }
    }

    /// <summary>场上的一张具体卡牌（带唯一实例 ID，为以后做「卡牌强化/附魔」留位）。</summary>
    public sealed class CardInstance
    {
        private static int _nextId = 1;

        public readonly int InstanceId;
        public readonly CardDef Def;

        public CardInstance(CardDef def)
        {
            Def = def;
            InstanceId = _nextId++;
        }

        public string Id { get { return Def.Id; } }
        public string Name { get { return Def.Name; } }
        public int Cost { get { return Def.Cost; } }
    }
}
