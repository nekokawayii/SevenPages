using System;

namespace SevenPages.Core
{
    /// <summary>
    /// 贪心 AI：能斩杀就斩杀，否则按「价值 / 费用」挑最赚的牌。
    /// 目标选择由卡牌自身决定（Enemy / Self），所以 AI 只需选牌。
    /// 够用就好——它的作用是验证规则闭环与提供单人陪练。
    /// </summary>
    public sealed class GreedyAi
    {
        public int ThinkSeed { get; set; }

        /// <summary>打完整个回合（会自行调用 EndTurn）。</summary>
        public void TakeTurn(BattleEngine engine)
        {
            if (engine == null || engine.State.IsOver) return;

            for (int guard = 0; guard < BattleConfig.MaxActionsPerTurn; guard++)
            {
                if (engine.State.IsOver) break;

                int index = ChooseBestCard(engine);
                if (index < 0) break;
                if (!engine.TryPlayCard(index)) break;
            }

            if (!engine.State.IsOver) engine.EndTurn();
        }

        /// <summary>返回要打出的手牌下标；-1 表示这回合不出牌了。</summary>
        public int ChooseBestCard(BattleEngine engine)
        {
            BattleState st = engine.State;
            PlayerState me = st.Current;
            PlayerState foe = engine.OpponentOf(me);

            int bestIndex = -1;
            double bestScore = 0.0;

            for (int i = 0; i < me.Hand.Count; i++)
            {
                CardInstance card = me.Hand[i];
                if (!card.Def.IsPlayable) continue;
                if (card.Def.Cost > me.Spirit) continue;

                double score = ScoreCard(me, foe, card);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private double ScoreCard(PlayerState me, PlayerState foe, CardInstance card)
        {
            double raw = 0.0;
            bool lethal = false;

            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                EffectSpec e = card.Def.Effects[i];

                switch (e.Kind)
                {
                    case EffectKind.Damage:
                        raw += e.Amount * 10.0;
                        if (e.Amount >= foe.TotalInk) lethal = true;
                        break;

                    case EffectKind.ConditionalDamage:
                    {
                        int amount = foe.Ink <= e.Threshold ? e.Amount + e.ExtraAmount : e.Amount;
                        raw += amount * 10.0;
                        if (amount >= foe.TotalInk) lethal = true;
                        break;
                    }

                    case EffectKind.Heal:
                    {
                        int missing = BattleConfig.InkPerPage - me.Ink;
                        int effective = Math.Min(missing, e.Amount);
                        // 残血时治疗价值更高
                        raw += effective * (me.TotalInk <= 8 ? 12.0 : 5.0);
                        break;
                    }

                    case EffectKind.Draw:
                        raw += me.Hand.Count < BattleConfig.MaxHand - 1 ? 6.0 : 1.0;
                        break;

                    case EffectKind.GainSpirit:
                        raw += e.Amount * 3.0;
                        break;

                    case EffectKind.ApplyRite:
                        raw += 8.0 + e.Amount * 4.0;
                        break;

                    case EffectKind.Ward:
                        raw += Math.Min(e.Amount, 6) * 2.0;
                        break;
                }
            }

            if (lethal) raw += 100000.0;

            // 费用效率：同等价值下优先低费牌
            return raw / (card.Cost * 0.5 + 1.0);
        }
    }
}
