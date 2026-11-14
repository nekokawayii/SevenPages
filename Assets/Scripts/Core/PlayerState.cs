using System;
using System.Collections.Generic;

namespace SevenPages.Core
{
    /// <summary>
    /// 一名玩家的全部状态：书页、墨、轮回、灵、手牌/牌库/弃牌堆、结附的式。
    /// 只负责「状态与伤害结算」，抽牌与回合调度交给 BattleEngine。
    /// </summary>
    public sealed class PlayerState
    {
        public readonly string Name;
        public readonly bool IsAi;

        // —— 书页 ——
        public int PageIndex;      // 0 .. PageCount-1，当前翻到的页
        public int Ink;            // 当前页的墨量
        public int Reincarnations; // 已轮回次数
        public int Insight;        // 宿慧层数

        // —— 资源 ——
        public int Spirit;         // 当前灵
        public int FatigueCounter; // 疲劳计数（全局递增）
        public int DamageWard;     // 本回合减伤护盾

        // —— 卡牌区 ——
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> Deck = new List<CardInstance>();
        public readonly List<CardInstance> Discard = new List<CardInstance>();

        // —— 式 ——
        public readonly List<RiteState> Rites = new List<RiteState>();

        public bool IsDead;

        // —— 由 BattleEngine 注入的回调 ——
        /// <summary>战斗日志。</summary>
        public Action<string> Log;
        /// <summary>轮回发生时回调（用于洗入业障、重排牌库）。</summary>
        public Action<PlayerState> OnReincarnate;

        public PlayerState(string name, bool isAi)
        {
            Name = name;
            IsAi = isAi;
            PageIndex = 0;
            Ink = BattleConfig.InkPerPage;
        }

        /// <summary>本回合灵上限：基础 2 + 宿慧层数，封顶 10。</summary>
        public int MaxSpirit
        {
            get
            {
                int v = BattleConfig.BaseSpirit + Insight;
                return v > BattleConfig.MaxSpiritCap ? BattleConfig.MaxSpiritCap : v;
            }
        }

        /// <summary>剩余总墨量（当前页墨 + 后续所有页的满墨）。</summary>
        public int TotalInk
        {
            get
            {
                int pagesAfterCurrent = BattleConfig.PageCount - 1 - PageIndex;
                if (pagesAfterCurrent < 0) pagesAfterCurrent = 0;
                return Ink + pagesAfterCurrent * BattleConfig.InkPerPage;
            }
        }

        public int PageDisplay { get { return PageIndex + 1; } }

        public void BeginTurn()
        {
            Spirit = MaxSpirit;
            DamageWard = 0;
            PruneRites();
        }

        private void PruneRites()
        {
            for (int i = Rites.Count - 1; i >= 0; i--)
            {
                if (Rites[i].TurnsRemaining == 0) Rites.RemoveAt(i);
            }
        }

        /// <summary>造成墨伤。返回实际造成的伤害（含被护盾吸收的部分）。</summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return 0;

            int total = amount;

            if (DamageWard > 0)
            {
                int absorbed = Math.Min(DamageWard, amount);
                DamageWard -= absorbed;
                amount -= absorbed;
                if (absorbed > 0 && Log != null)
                    Log(string.Format("「墨壁」吸收了 {0} 点伤害。", absorbed));
                if (amount <= 0) return total;
            }

            while (amount > 0 && !IsDead)
            {
                if (Ink > amount)
                {
                    Ink -= amount;
                    amount = 0;
                }
                else
                {
                    amount -= Ink;
                    Ink = 0;
                    TearPage();
                }
            }
            return total;
        }

        /// <summary>治疗：只能在当前页内恢复，不跨页。</summary>
        public int Heal(int amount)
        {
            if (amount <= 0 || IsDead) return 0;
            int before = Ink;
            Ink = Math.Min(BattleConfig.InkPerPage, Ink + amount);
            int healed = Ink - before;
            if (healed > 0 && Log != null) Log(string.Format("{0} 的墨迹复原了 {1} 点。", Name, healed));
            return healed;
        }

        /// <summary>撕掉当前页，翻到下一页；若是最后一页则触发轮回。</summary>
        private void TearPage()
        {
            int burntPage = PageIndex + 1;
            PageIndex++;

            if (PageIndex >= BattleConfig.PageCount)
            {
                if (Log != null) Log(string.Format("【撕页】{0} 的第 {1} 页（末页）被撕下。", Name, burntPage));
                Reincarnate();
                return;
            }

            Ink = BattleConfig.InkPerPage;
            if (Log != null) Log(string.Format("【撕页】{0} 的第 {1} 页被撕下，翻至第 {2} 页。", Name, burntPage, PageIndex + 1));
        }

        /// <summary>轮回：回到第一页，业障入牌库，宿慧 +1；第 3 次轮回即消亡。</summary>
        public void Reincarnate()
        {
            Reincarnations++;
            PageIndex = 0;
            Ink = BattleConfig.InkPerPage;

            if (Log != null)
                Log(string.Format("【轮回】{0} 的书页燃尽——第 {1} 次轮回。", Name, Reincarnations));

            if (OnReincarnate != null) OnReincarnate(this);

            if (Reincarnations >= BattleConfig.MaxReincarnations)
            {
                IsDead = true;
                if (Log != null) Log(string.Format("【永劫】{0} 第三次轮回，魂散于书页之间。", Name));
                return;
            }

            if (Insight < BattleConfig.MaxInsight) Insight++;
            if (Log != null)
                Log(string.Format("【宿慧】{0} 带着前世记忆归来，宿慧 {1} 层（每回合灵 +{2}）。", Name, Insight, Insight));
        }
    }
}
