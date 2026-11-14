namespace SevenPages.Core
{
    /// <summary>
    /// 效果解释器：把「效果数据」翻译成对战场的影响。
    /// 新增一张卡不需要改这里——除非引入新的 EffectKind。
    /// </summary>
    public sealed partial class BattleEngine
    {
        private void ResolveCard(PlayerState caster, CardInstance card)
        {
            PlayerState target = card.Def.Target == TargetKind.Enemy
                ? OpponentOf(caster)
                : caster;

            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                ResolveEffect(caster, target, card, card.Def.Effects[i]);
                CheckOutcome();
                if (State.IsOver) return;
            }
        }

        private void ResolveEffect(PlayerState caster, PlayerState target, CardInstance card, EffectSpec e)
        {
            switch (e.Kind)
            {
                case EffectKind.Damage:
                    ApplyDamage(target, e.Amount, card.Name);
                    break;

                case EffectKind.Heal:
                    target.Heal(e.Amount);
                    break;

                case EffectKind.Draw:
                    DrawCards(caster, e.Amount);
                    break;

                case EffectKind.GainSpirit:
                    caster.Spirit += e.Amount;
                    AppendLog(string.Format("{0} 借来 {1} 点灵。", caster.Name, e.Amount));
                    break;

                case EffectKind.ApplyRite:
                    ApplyRite(target, card, e);
                    break;

                case EffectKind.Ward:
                    target.DamageWard += e.Amount;
                    AppendLog(string.Format("{0} 凝结墨壁，减伤 {1}。", target.Name, target.DamageWard));
                    break;

                case EffectKind.ConditionalDamage:
                {
                    bool triggered = target.Ink <= e.Threshold;
                    int amount = triggered ? e.Amount + e.ExtraAmount : e.Amount;
                    if (triggered)
                        AppendLog(string.Format("目标墨痕将尽，「{0}」更添杀意。", card.Name));
                    ApplyDamage(target, amount, card.Name);
                    break;
                }

                case EffectKind.Lifesteal:
                {
                    int before = target.TotalInk;
                    ApplyDamage(target, e.Amount, card.Name);
                    int dealt = before - target.TotalInk;
                    if (dealt > 0)
                    {
                        caster.Heal(dealt);
                        AppendLog(string.Format("墨迹倒流，{0} 吸回 {1} 点墨。", caster.Name, dealt));
                    }
                    break;
                }
            }
        }

        private void ApplyDamage(PlayerState target, int amount, string sourceName)
        {
            int before = target.TotalInk;
            target.TakeDamage(amount);
            int lost = before - target.TotalInk;
            if (lost > 0)
                AppendLog(string.Format("【{0}】对 {1} 造成 {2} 点墨伤。", sourceName, target.Name, lost));
            else if (amount > 0)
                AppendLog(string.Format("【{0}】被挡下了。", sourceName));
        }

        private void ApplyRite(PlayerState target, CardInstance card, EffectSpec e)
        {
            var rite = new RiteState(
                string.IsNullOrEmpty(e.RiteId) ? card.Id : e.RiteId,
                string.IsNullOrEmpty(e.RiteName) ? card.Name : e.RiteName,
                card.Def.Text,
                e.RiteTurns,
                e.RiteDamage,
                e.RiteDraw);

            // 同名式叠加：刷新持续回合
            for (int i = 0; i < target.Rites.Count; i++)
            {
                if (target.Rites[i].DefId == rite.DefId)
                {
                    target.Rites[i].TurnsRemaining = rite.TurnsRemaining;
                    AppendLog(string.Format("{0} 身上的「{1}」被延续。", target.Name, rite.Name));
                    return;
                }
            }

            target.Rites.Add(rite);
            AppendLog(string.Format("{0} 被结附「{1}」。", target.Name, rite.Name));
        }
    }
}
