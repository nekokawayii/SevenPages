using System.Collections.Generic;

namespace SevenPages.Core
{
    /// <summary>
    /// 规则引擎：回合调度、抽牌、出牌、效果结算、胜负判定。
    /// 不依赖 UnityEngine，可被 Unity / 服务器 / 单元测试 / 无头模拟器共同使用。
    /// </summary>
    public sealed partial class BattleEngine
    {
        private readonly CardDatabase _db;
        private readonly DeterministicRng _rng;

        public BattleState State { get; private set; }
        public CardDatabase Database { get { return _db; } }
        public DeterministicRng Rng { get { return _rng; } }

        public BattleEngine(CardDatabase database, int seed)
        {
            _db = database;
            _rng = new DeterministicRng(seed);
            State = new BattleState();
        }

        // ———————————————————————————— 开局 ————————————————————————————

        public void StartBattle(
            string p1Name, bool p1IsAi, IList<string> deck1,
            string p2Name, bool p2IsAi, IList<string> deck2)
        {
            State = new BattleState();

            State.Players[0] = CreatePlayer(p1Name, p1IsAi, deck1);
            State.Players[1] = CreatePlayer(p2Name, p2IsAi, deck2);

            // 先手 4 张，后手 5 张（后手补偿）
            DrawCards(State.Players[0], BattleConfig.InitialHandFirst);
            DrawCards(State.Players[1], BattleConfig.InitialHandSecond);

            State.CurrentPlayerIndex = 0;
            State.TurnNumber = 1;
            StartTurn(0);
        }

        private PlayerState CreatePlayer(string name, bool isAi, IList<string> recipe)
        {
            var p = new PlayerState(name, isAi);

            p.Log = AppendLog;
            p.OnReincarnate = OnPlayerReincarnated;

            List<CardInstance> deck = _db.BuildDeck(recipe);
            _rng.Shuffle(deck);
            p.Deck.AddRange(deck);

            return p;
        }

        private void OnPlayerReincarnated(PlayerState p)
        {
            CardDef karma = _db.Get("karma");
            if (karma != null)
            {
                for (int i = 0; i < BattleConfig.KarmaPerReincarnation; i++)
                    p.Deck.Add(new CardInstance(karma));

                _rng.Shuffle(p.Deck);
                if (p.Reincarnations < BattleConfig.MaxReincarnations)
                    AppendLog(string.Format("【业障】{0} 张「业障」缠入 {1} 的牌库。", BattleConfig.KarmaPerReincarnation, p.Name));
            }
        }

        // ———————————————————————————— 回合 ————————————————————————————

        private void StartTurn(int playerIndex)
        {
            if (State.IsOver) return;

            State.CurrentPlayerIndex = playerIndex;
            PlayerState p = State.Players[playerIndex];

            p.BeginTurn();

            AppendLog(string.Format("—— 第 {0} 回合 · {1} ——", State.TurnNumber, p.Name));

            // 触发结附的「式」
            TriggerRites(p);
            if (State.IsOver) return;

            // 抽牌
            DrawCards(p, BattleConfig.DrawPerTurn);
            CheckOutcome();
        }

        private void TriggerRites(PlayerState p)
        {
            if (p.Rites.Count == 0) return;

            for (int i = 0; i < p.Rites.Count; i++)
            {
                RiteState rite = p.Rites[i];
                if (rite.DamagePerTurn > 0)
                {
                    AppendLog(string.Format("「{0}」发作，{1} 承受 {2} 点墨伤。", rite.Name, p.Name, rite.DamagePerTurn));
                    p.TakeDamage(rite.DamagePerTurn);
                    if (p.IsDead) return;
                }
                if (rite.DrawPerTurn > 0)
                {
                    AppendLog(string.Format("「{0}」牵引墨迹，{1} 额外抽取 {2} 张。", rite.Name, p.Name, rite.DrawPerTurn));
                    DrawCards(p, rite.DrawPerTurn);
                    CheckOutcome();
                    if (p.IsDead) return;
                }
                if (rite.TurnsRemaining > 0) rite.TurnsRemaining--;
            }
        }

        /// <summary>结束当前回合，交给对手。</summary>
        public void EndTurn()
        {
            if (State.IsOver) return;

            int next = 1 - State.CurrentPlayerIndex;
            State.TurnNumber++;

            if (State.TurnNumber > BattleConfig.MaxTurns)
            {
                // 超时保护：按剩余总墨量判胜负
                PlayerState a = State.Players[0];
                PlayerState b = State.Players[1];
                if (a.TotalInk > b.TotalInk) State.Outcome = BattleOutcome.Player1Win;
                else if (b.TotalInk > a.TotalInk) State.Outcome = BattleOutcome.Player2Win;
                else State.Outcome = BattleOutcome.Draw;
                AppendLog("【时限】回合数达上限，按剩余墨量裁定。");
                return;
            }

            StartTurn(next);
        }

        // ———————————————————————————— 抽牌 ————————————————————————————

        public void DrawCards(PlayerState p, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (State.IsOver) return;

                if (p.Deck.Count == 0)
                {
                    // 书尽：牌库抽空后承受递增疲劳伤
                    p.FatigueCounter++;
                    AppendLog(string.Format("{0} 的书页已尽，承受 {1} 点疲劳之伤。", p.Name, p.FatigueCounter));
                    p.TakeDamage(p.FatigueCounter);
                    CheckOutcome();
                    continue;
                }

                CardInstance card = p.Deck[0];
                p.Deck.RemoveAt(0);

                if (p.Hand.Count >= BattleConfig.MaxHand)
                {
                    p.Discard.Add(card);
                    AppendLog(string.Format("{0} 手牌已满，【{1}】化为灰烬。", p.Name, card.Name));
                }
                else
                {
                    p.Hand.Add(card);
                }
            }
        }

        // ———————————————————————————— 出牌 ————————————————————————————

        public bool CanPlayCard(PlayerState p, CardInstance card)
        {
            if (card == null || !card.Def.IsPlayable) return false;
            if (p.Spirit < card.Def.Cost) return false;
            return true;
        }

        /// <summary>打出当前玩家手牌中的第 handIndex 张牌。目标由卡牌自身决定。</summary>
        public bool TryPlayCard(int handIndex)
        {
            if (State.IsOver) return false;

            PlayerState p = State.Current;
            if (handIndex < 0 || handIndex >= p.Hand.Count) return false;

            CardInstance card = p.Hand[handIndex];
            if (!CanPlayCard(p, card)) return false;

            p.Spirit -= card.Def.Cost;
            p.Hand.RemoveAt(handIndex);
            p.Discard.Add(card);

            // 记录到台面（界面用）
            State.RecentPlays.Add(new PlayRecord
            {
                PlayerIndex = State.CurrentPlayerIndex,
                Card = card,
                Turn = State.TurnNumber
            });
            if (State.RecentPlays.Count > 40) State.RecentPlays.RemoveAt(0);

            AppendLog(string.Format("{0} 打出【{1}】", p.Name, card.Name));

            ResolveCard(p, card);
            CheckOutcome();
            return true;
        }

        // ———————————————————————————— 胜负 ————————————————————————————

        public void CheckOutcome()
        {
            if (State.IsOver) return;

            bool d0 = State.Players[0].IsDead;
            bool d1 = State.Players[1].IsDead;

            if (d0 && d1) State.Outcome = BattleOutcome.Draw;
            else if (d0) State.Outcome = BattleOutcome.Player2Win;
            else if (d1) State.Outcome = BattleOutcome.Player1Win;
        }

        public PlayerState OpponentOf(PlayerState p)
        {
            return ReferenceEquals(p, State.Players[0]) ? State.Players[1] : State.Players[0];
        }

        public BattleResult BuildResult()
        {
            var r = new BattleResult();
            r.Outcome = State.Outcome;
            r.Turns = State.TurnNumber;

            if (State.Outcome == BattleOutcome.Player1Win || State.Outcome == BattleOutcome.Player2Win)
            {
                PlayerState w = State.Outcome == BattleOutcome.Player1Win ? State.Players[0] : State.Players[1];
                PlayerState l = State.Outcome == BattleOutcome.Player1Win ? State.Players[1] : State.Players[0];
                r.Winner = w.Name;
                r.Loser = l.Name;
                r.WinnerInkLeft = w.TotalInk;
                r.LoserInkLeft = l.TotalInk;
                r.WinnerReincarnations = w.Reincarnations;
                r.LoserReincarnations = l.Reincarnations;
            }
            else if (State.Outcome == BattleOutcome.Draw)
            {
                r.Winner = "平局";
                r.Loser = "平局";
            }
            return r;
        }

        private void AppendLog(string line)
        {
            if (State.Log.Count > 4000) State.Log.RemoveRange(0, 1000);
            State.Log.Add(line);
        }
    }
}
