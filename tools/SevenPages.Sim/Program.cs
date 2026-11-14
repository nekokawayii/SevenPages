using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SevenPages.Core;

namespace SevenPages.Sim
{
    /// <summary>
    /// 无头工具：
    ///   sim  [局数]  —— 批量 AI 对战，验证规则闭环与数值
    ///   test         —— 规则单元自测
    ///   demo         —— 跑一局并打印完整日志
    /// </summary>
    internal static class Program
    {
        private static int _failures;

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "sim";
            string cardPath = ResolveCardPath(args.Length > 1 ? args[1] : null);

            if (!File.Exists(cardPath))
            {
                Console.WriteLine("[错误] 找不到卡牌数据：" + cardPath);
                return 2;
            }

            CardSetDto set = LoadCardSet(cardPath);
            CardDatabase db = BuildDatabase(set);
            List<string> starter = new List<string>(set.starterDeck);

            Console.WriteLine(string.Format("载入卡池 {0} 张，卡组 {1} 张", db.All.Count, starter.Count));

            switch (mode)
            {
                case "test":
                    return RunTests(db, starter, set);
                case "demo":
                    RunDemo(db, starter);
                    return 0;
                default:
                {
                    int games = 500;
                    if (args.Length > 2) int.TryParse(args[2], out games);
                    RunSimulation(db, starter, games);
                    return 0;
                }
            }
        }

        // —————————————————————————— 载入 ——————————————————————————

        private static string ResolveCardPath(string explicitPath)
        {
            if (!string.IsNullOrEmpty(explicitPath)) return explicitPath;

            // 从可执行目录向上找 cards/cards.json
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "cards", "cards.json");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return Path.Combine(AppContext.BaseDirectory, "cards.json");
        }

        private static CardSetDto LoadCardSet(string path)
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            return JsonSerializer.Deserialize<CardSetDto>(json, options);
        }

        private static CardDatabase BuildDatabase(CardSetDto set)
        {
            var db = new CardDatabase();
            for (int i = 0; i < set.cards.Length; i++)
            {
                db.Add(set.cards[i].ToCardDef());
            }
            return db;
        }

        // —————————————————————————— 自测 ——————————————————————————

        private static int RunTests(CardDatabase db, List<string> starter, CardSetDto set)
        {
            _failures = 0;
            Console.WriteLine("=== 规则自测 ===");

            // 1. 初始状态
            var e1 = new BattleEngine(db, 1);
            e1.StartBattle("甲", false, starter, "乙", false, starter);
            Assert(e1.State.Players[0].Ink == BattleConfig.InkPerPage, "先手初始墨 = 每页墨量");
            Assert(e1.State.Players[0].PageIndex == 0, "先手停在第 1 页");
            Assert(e1.State.Players[0].Hand.Count == 4, "先手首回合手牌 4 张");
            Assert(e1.State.Players[1].Hand.Count == 3, "后手开局手牌 3 张（等他的回合再抽 2）");
            Assert(e1.State.Players[0].Spirit == BattleConfig.BaseSpirit, "首回合灵 = 基础灵");
            Assert(e1.State.Players[0].Deck.Count == starter.Count - 4, "先手牌库 = 总牌数 - 4");

            e1.EndTurn();
            Assert(e1.State.Players[1].Hand.Count == 5, "后手首个回合开始后手牌 5 张（后手补偿生效）");

            // 2. 伤害与撕页（溢出伤害延续到下一页）
            int per = BattleConfig.InkPerPage;
            var p = new PlayerState("测", false);
            p.TakeDamage(2);
            Assert(p.Ink == per - 2, "受 2 伤后墨 = 每页墨量 - 2");
            p.TakeDamage(per);
            Assert(p.PageIndex == 1, "墨耗尽翻到第 2 页");
            Assert(p.Ink == per - 2, "溢出的伤害落在新页");

            // 3. 轮回
            var p2 = new PlayerState("测2", false);
            p2.TakeDamage(BattleConfig.PageCount * BattleConfig.InkPerPage);
            Assert(p2.Reincarnations == 1, "撕尽七页触发第 1 次轮回");
            Assert(p2.PageIndex == 0 && p2.Ink == BattleConfig.InkPerPage, "轮回后回到第 1 页满墨");
            Assert(p2.Insight == 1, "轮回后获得 1 层宿慧");

            // 4. 三度轮回即消亡
            var p3 = new PlayerState("测3", false);
            p3.TakeDamage(BattleConfig.PageCount * BattleConfig.InkPerPage * 3);
            Assert(p3.IsDead, "第三次轮回后消亡");

            // 5. 出牌结算
            var dbStrike = new CardDatabase();
            dbStrike.Add(db.Get("strike"));
            var strikeDeck = new List<string>();
            for (int i = 0; i < 10; i++) strikeDeck.Add("strike");

            var e2 = new BattleEngine(dbStrike, 3);
            e2.StartBattle("甲", false, strikeDeck, "乙", false, strikeDeck);
            int foeInkBefore = e2.State.Players[1].TotalInk;
            int spiritBefore = e2.State.Players[0].Spirit;
            bool played = e2.TryPlayCard(0);
            Assert(played, "可以打出墨刺");
            Assert(e2.State.Players[1].TotalInk == foeInkBefore - 2, "墨刺造成 2 点墨伤");
            Assert(e2.State.Players[0].Spirit == spiritBefore - 1, "打出 1 费牌后灵 -1");
            Assert(e2.State.Players[0].Hand.Count == 3, "手牌减少 1 张");
            Assert(e2.State.Players[0].Discard.Count == 1, "打出的牌进入弃牌堆");

            e2.State.Players[0].Spirit = 0;
            Assert(!e2.TryPlayCard(0), "灵不足时不能出牌");

            // 6. 诅咒不可打出
            var dbKarma = new CardDatabase();
            dbKarma.Add(db.Get("karma"));
            var karmaDeck = new List<string>();
            for (int i = 0; i < 10; i++) karmaDeck.Add("karma");
            var e3 = new BattleEngine(dbKarma, 4);
            e3.StartBattle("甲", false, karmaDeck, "乙", false, karmaDeck);
            Assert(!e3.TryPlayCard(0), "业障无法打出");

            // 7. 牌库耗尽 → 疲劳伤
            var e4 = new BattleEngine(dbStrike, 5);
            var fourCards = new List<string> { "strike", "strike", "strike", "strike" };
            e4.StartBattle("甲", false, fourCards, "乙", false, fourCards);
            Assert(e4.State.Players[0].Deck.Count == 0, "开局抽牌后先手牌库已空");
            int inkBefore = e4.State.Players[0].TotalInk;
            e4.DrawCards(e4.State.Players[0], 1);
            Assert(e4.State.Players[0].TotalInk == inkBefore - 1, "牌库空时抽牌受 1 点疲劳伤");

            // 8. 治疗不跨页
            var p4 = new PlayerState("测4", false);
            p4.TakeDamage(per + 1); // 撕一次页后当前页只剩 per-1
            p4.Heal(per + 5);
            Assert(p4.Ink == BattleConfig.InkPerPage, "治疗封顶于本页墨量");

            // 9. 护盾
            var p5 = new PlayerState("测5", false);
            p5.DamageWard = 3;
            p5.TakeDamage(per + 1);
            Assert(p5.Ink == per - 1, "护盾吸收 3 点后只受 1 点伤害");

            // 10. 冒烟：AI 对战 100 局全部正常收场
            int crashes = 0, unfinished = 0;
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var r = PlayOneGame(db, starter, starter, 1000 + i);
                    if (r.Outcome == BattleOutcome.Ongoing) unfinished++;
                }
                catch (Exception ex)
                {
                    crashes++;
                    if (crashes <= 2) Console.WriteLine("  [异常] " + ex.Message);
                }
            }
            Assert(crashes == 0, "100 局 AI 对战无异常");
            Assert(unfinished == 0, "100 局全部在回合上限内收场");

            Console.WriteLine();
            if (_failures == 0) Console.WriteLine(">>> 全部通过 ✅");
            else Console.WriteLine(string.Format(">>> {0} 项失败 ❌", _failures));
            return _failures == 0 ? 0 : 1;
        }

        private static void Assert(bool condition, string label)
        {
            if (condition)
            {
                Console.WriteLine("  [通过] " + label);
            }
            else
            {
                _failures++;
                Console.WriteLine("  [失败] " + label);
            }
        }

        // —————————————————————————— 模拟 ——————————————————————————

        private static BattleResult PlayOneGame(CardDatabase db, List<string> deck1, List<string> deck2, int seed)
        {
            var engine = new BattleEngine(db, seed);
            engine.StartBattle("墨守", true, deck1, "书魇", true, deck2);

            var ai = new GreedyAi();
            int guard = 0;
            while (!engine.State.IsOver && guard++ < 2000)
            {
                if (!engine.State.Current.IsAi) break;
                ai.TakeTurn(engine);
            }

            return engine.BuildResult();
        }

        private static void RunSimulation(CardDatabase db, List<string> starter, int games)
        {
            Console.WriteLine(string.Format("=== 模拟 {0} 局 AI 对战 ===", games));

            int p1 = 0, p2 = 0, draw = 0, unfinished = 0;
            long turns = 0;
            int minTurns = int.MaxValue, maxTurns = 0;
            long reinc = 0;
            var hist = new SortedDictionary<int, int>();

            for (int i = 0; i < games; i++)
            {
                BattleResult r;
                try
                {
                    r = PlayOneGame(db, starter, starter, i + 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[崩溃] 第 " + i + " 局：" + ex);
                    return;
                }

                if (r.Outcome == BattleOutcome.Player1Win) p1++;
                else if (r.Outcome == BattleOutcome.Player2Win) p2++;
                else if (r.Outcome == BattleOutcome.Draw) draw++;
                else unfinished++;

                turns += r.Turns;
                if (r.Turns < minTurns) minTurns = r.Turns;
                if (r.Turns > maxTurns) maxTurns = r.Turns;
                reinc += r.WinnerReincarnations + r.LoserReincarnations;

                int bucket = (r.Turns / 5) * 5;
                if (!hist.ContainsKey(bucket)) hist[bucket] = 0;
                hist[bucket]++;
            }

            Console.WriteLine("先手胜：" + p1 + "  后手胜：" + p2 + "  平局：" + draw + "  未收场：" + unfinished);
            Console.WriteLine(string.Format("平均回合 {0:F1}  最短 {1}  最长 {2}", (double)turns / Math.Max(1, games), minTurns, maxTurns));
            Console.WriteLine(string.Format("平均每局轮回次数 {0:F2}", (double)reinc / Math.Max(1, games)));
            Console.WriteLine("回合分布：");
            foreach (var kv in hist)
            {
                Console.Write(string.Format("  {0,3}-{1,3} 回合: {2,4} 局  ", kv.Key, kv.Key + 4, kv.Value));
                int bars = (int)Math.Round(kv.Value * 40.0 / Math.Max(1, games));
                Console.WriteLine(new string('█', Math.Max(0, bars)));
            }
        }

        private static void RunDemo(CardDatabase db, List<string> starter)
        {
            Console.WriteLine("=== 演示对局（AI vs AI）===");
            var engine = new BattleEngine(db, 20261113);
            engine.StartBattle("墨守", true, starter, "书魇", true, starter);

            var ai = new GreedyAi();
            int guard = 0;
            while (!engine.State.IsOver && guard++ < 2000)
            {
                ai.TakeTurn(engine);
            }

            foreach (string line in engine.State.Log)
                Console.WriteLine(line);

            Console.WriteLine();
            Console.WriteLine("结果：" + engine.BuildResult());
        }
    }
}
