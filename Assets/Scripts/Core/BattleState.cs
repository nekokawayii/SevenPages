using System.Collections.Generic;

namespace SevenPages.Core
{
    public enum BattleOutcome
    {
        Ongoing,
        Player1Win,
        Player2Win,
        Draw
    }

    /// <summary>一次出牌记录，供界面把打出的牌摆到台面上。</summary>
    public sealed class PlayRecord
    {
        public int PlayerIndex;
        public CardInstance Card;
        public int Turn;
    }

    /// <summary>一局对战的全局状态。</summary>
    public sealed class BattleState
    {
        public readonly PlayerState[] Players = new PlayerState[2];

        public int CurrentPlayerIndex;
        public int TurnNumber = 1;
        public BattleOutcome Outcome = BattleOutcome.Ongoing;

        /// <summary>完整战斗日志（含带【】的叙事性节点）。</summary>
        public readonly List<string> Log = new List<string>();

        /// <summary>最近打出的牌（按时间顺序，最多保留 40 条）。</summary>
        public readonly List<PlayRecord> RecentPlays = new List<PlayRecord>();

        public PlayerState Current { get { return Players[CurrentPlayerIndex]; } }
        public PlayerState Other { get { return Players[1 - CurrentPlayerIndex]; } }
        public bool IsOver { get { return Outcome != BattleOutcome.Ongoing; } }

        public int TurnsTaken
        {
            get { return TurnNumber; }
        }
    }

    /// <summary>一局对战的最终结果（给模拟器/AI 训练用的摘要）。</summary>
    public sealed class BattleResult
    {
        public BattleOutcome Outcome;
        public int Turns;
        public string Winner = "";
        public string Loser = "";
        public int WinnerInkLeft;
        public int LoserInkLeft;
        public int WinnerReincarnations;
        public int LoserReincarnations;

        public override string ToString()
        {
            return string.Format("{0} 胜（{1} 回合）| 胜者余墨 {2}、轮回 {3} 次；败者余墨 {4}、轮回 {5} 次",
                Winner, Turns, WinnerInkLeft, WinnerReincarnations, LoserInkLeft, LoserReincarnations);
        }
    }
}
