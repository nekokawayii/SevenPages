namespace SevenPages.Core
{
    /// <summary>全局规则数值。所有可调参数集中在这里，方便日后平衡性调整。</summary>
    public static class BattleConfig
    {
        // —— 页 / 墨 ——
        public const int PageCount = 7;          // 每人书的总页数
        public const int InkPerPage = 3;         // 每页墨量（总血量 = 页数 × 墨量 × 轮回次数）
        public const int MaxReincarnations = 3;  // 第 3 次轮回即败北

        // —— 灵（费用） ——
        public const int BaseSpirit = 3;         // 每回合基础灵
        public const int MaxSpiritCap = 10;      // 灵上限
        public const int MaxInsight = 3;         // 宿慧最多 3 层

        // —— 手牌 / 抽牌 ——
        public const int MaxHand = 10;
        /// <summary>开局发给先手的牌数（首回合开始抽 2 张，故首回合手牌 4 张）。</summary>
        public const int InitialHandFirst = 2;
        /// <summary>开局发给后手的牌数（首回合开始抽 2 张，故首回合手牌 5 张）。</summary>
        public const int InitialHandSecond = 3;
        /// <summary>每回合抽牌数。抽得比打得多，才不会被手牌枯竭拖成疲劳局。</summary>
        public const int DrawPerTurn = 2;

        // —— 业障 ——
        public const int KarmaPerReincarnation = 1;

        // —— 安全阀 ——
        public const int MaxTurns = 200;         // 防止模拟死循环
        public const int MaxActionsPerTurn = 30;
    }
}
