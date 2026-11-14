namespace SevenPages.Core
{
    /// <summary>结附在玩家身上的持续状态（式）。</summary>
    public sealed class RiteState
    {
        public string DefId;
        public string Name;
        public string Text;
        /// <summary>剩余回合数（-1 表示永久）。</summary>
        public int TurnsRemaining;
        /// <summary>每回合开始时对持有者造成的墨伤。</summary>
        public int DamagePerTurn;
        /// <summary>每回合开始时为持有者额外抽取的牌数。</summary>
        public int DrawPerTurn;

        public RiteState() { }

        public RiteState(string defId, string name, string text, int turns, int damagePerTurn, int drawPerTurn = 0)
        {
            DefId = defId;
            Name = name;
            Text = text;
            TurnsRemaining = turns;
            DamagePerTurn = damagePerTurn;
            DrawPerTurn = drawPerTurn;
        }
    }
}
