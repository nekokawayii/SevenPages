using System;
using System.Text;
using SevenPages.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SevenPages.Game
{
    /// <summary>
    /// 对战界面：完全用代码搭建 uGUI，卡牌使用 Resources/Art/Cards 下的卡图。
    /// 布局按 1920x1080 设计，CanvasScaler 自适应。
    /// </summary>
    public class BattleView : MonoBehaviour
    {
        private BattleEngine _engine;
        private Action<int> _onPlayCard;
        private Action _onEndTurn;

        private Text _enemyStatus;
        private Text _myStatus;
        private Text _turnLabel;
        private Text _logText;
        private RectTransform _handRoot;
        private RectTransform _enemyHandRoot;
        private Button _endTurnButton;
        private Image _logBg;
        private RectTransform _tableRoot;
        private int _lastPlayCount = -1;

        private GameObject _resultPanel;
        private Text _resultText;

        private static Sprite _cardBack;

        public void Init(BattleEngine engine, Action<int> onPlayCard, Action onEndTurn, Canvas canvas)
        {
            _engine = engine;
            _onPlayCard = onPlayCard;
            _onEndTurn = onEndTurn;

            BuildUI(canvas.transform);
            Refresh();
        }

        // —————————————————————— 搭建 ——————————————————————

        private void BuildUI(Transform root)
        {
            // 背景
            var bg = UIFactory.CreatePanel("Background", root, Theme.Background);
            bg.raycastTarget = false;

            // 敌方状态条
            RectTransform enemyBar = UIFactory.CreateRegion("EnemyBar", root, 0f, 0.915f, 1f, 1f, Theme.Panel);
            _enemyStatus = UIFactory.CreateText("EnemyStatus", enemyBar, "", 24, Theme.Ink, TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(_enemyStatus.rectTransform, 0f, 0f, 1f, 1f, 24f);

            // 敌方手牌（卡背）
            _enemyHandRoot = UIFactory.CreateRegion("EnemyHand", root, 0f, 0.785f, 1f, 0.915f, new Color(0, 0, 0, 0f));
            var enemyLayout = _enemyHandRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyLayout.childAlignment = TextAnchor.MiddleCenter;
            enemyLayout.spacing = 12f;
            enemyLayout.childForceExpandWidth = false;
            enemyLayout.childForceExpandHeight = false;
            enemyLayout.padding = new RectOffset(20, 20, 8, 8);

            // 中央日志区
            RectTransform logPanel = UIFactory.CreateRegion("LogPanel", root, 0f, 0.315f, 1f, 0.785f, new Color(0f, 0f, 0f, 0.25f));
            _logBg = logPanel.GetComponent<Image>();
            _logText = UIFactory.CreateText("LogText", logPanel, "", 17, Theme.InkDim, TextAnchor.LowerLeft);
            UIFactory.SetAnchors(_logText.rectTransform, 0f, 0f, 1f, 0.42f, 40f);
            _logText.lineSpacing = 1.16f;

            // 台面：打出的牌摆在这里
            _tableRoot = UIFactory.CreateRegion("TableArea", root, 0.12f, 0.44f, 0.88f, 0.78f, new Color(0, 0, 0, 0f));
            var tableLayout = _tableRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            tableLayout.childAlignment = TextAnchor.MiddleCenter;
            tableLayout.spacing = 16f;
            tableLayout.childForceExpandWidth = false;
            tableLayout.childForceExpandHeight = false;

            // 我方手牌
            _handRoot = UIFactory.CreateRegion("Hand", root, 0f, 0.085f, 1f, 0.315f, new Color(0, 0, 0, 0f));
            var handLayout = _handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.childAlignment = TextAnchor.MiddleCenter;
            handLayout.spacing = 10f;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            handLayout.padding = new RectOffset(24, 24, 8, 8);

            // 底部状态条
            RectTransform bottomBar = UIFactory.CreateRegion("BottomBar", root, 0f, 0f, 1f, 0.085f, Theme.Panel);
            _myStatus = UIFactory.CreateText("MyStatus", bottomBar, "", 24, Theme.Ink, TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(_myStatus.rectTransform, 0f, 0f, 0.62f, 1f, 24f);

            _turnLabel = UIFactory.CreateText("TurnLabel", bottomBar, "", 20, Theme.Accent, TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(_turnLabel.rectTransform, 0.56f, 0f, 0.80f, 1f);

            _endTurnButton = UIFactory.CreateButton("EndTurn", bottomBar, "结束回合", 22, Theme.Accent, Color.white);
            UIFactory.SetAnchors(_endTurnButton.GetComponent<RectTransform>(), 0.82f, 0.18f, 0.985f, 0.82f);
            _endTurnButton.onClick.AddListener(HandleEndTurnClick);

            // 结算遮罩
            RectTransform overlay = UIFactory.CreateRegion("Result", root, 0f, 0f, 1f, 1f, new Color(0f, 0f, 0f, 0.85f));
            _resultPanel = overlay.gameObject;
            _resultText = UIFactory.CreateText("ResultText", overlay, "", 46, Theme.Ink, TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(_resultText.rectTransform, 0.1f, 0.2f, 0.9f, 0.8f);
            _resultPanel.SetActive(false);
        }

        // —————————————————————— 刷新 ——————————————————————

        public void Refresh()
        {
            if (_engine == null) return;

            BattleState st = _engine.State;
            PlayerState me = st.Players[0];
            PlayerState foe = st.Players[1];

            _enemyStatus.text = Describe(foe);
            _myStatus.text = Describe(me) + string.Format("　灵 {0}/{1}", me.Spirit, me.MaxSpirit);

            bool myTurn = st.CurrentPlayerIndex == 0 && !st.IsOver;
            _turnLabel.text = st.IsOver ? "对局结束" : (myTurn ? "你的回合" : "对手回合");
            _endTurnButton.interactable = myTurn;

            RefreshEnemyHand(foe);
            RefreshHand(me, myTurn);
            RefreshTable();
            RefreshLog();
        }

        private string Describe(PlayerState p)
        {
            return string.Format("{0}　第 {1}/{2} 页　墨 {3}/{4}　轮回 {5}/{6}　宿慧 {7}",
                p.Name,
                p.PageDisplay, BattleConfig.PageCount,
                p.Ink, BattleConfig.InkPerPage,
                p.Reincarnations, BattleConfig.MaxReincarnations,
                p.Insight);
        }

        private void RefreshEnemyHand(PlayerState foe)
        {
            ClearChildren(_enemyHandRoot);
            if (_cardBack == null) _cardBack = Resources.Load<Sprite>("Art/Cards/card_back");

            for (int i = 0; i < foe.Hand.Count; i++)
            {
                RectTransform back = UIFactory.CreateRect("Back_" + i, _enemyHandRoot);
                var le = back.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 72;
                le.preferredHeight = 100;

                var img = back.gameObject.AddComponent<Image>();
                img.raycastTarget = false;

                if (_cardBack != null)
                {
                    img.sprite = _cardBack;
                    img.color = new Color(0.66f, 0.66f, 0.7f);
                }
                else
                {
                    img.color = new Color(0.18f, 0.17f, 0.24f);
                }
            }
        }

        private void RefreshHand(PlayerState me, bool myTurn)
        {
            ClearChildren(_handRoot);

            for (int i = 0; i < me.Hand.Count; i++)
            {
                CardInstance card = me.Hand[i];
                bool playable = myTurn && card.Def.IsPlayable && card.Def.Cost <= me.Spirit;
                CreateCardButton(i, card, playable);
            }
        }

        private void CreateCardButton(int handIndex, CardInstance card, bool interactable)
        {
            RectTransform rt = UIFactory.CreateRect("Card_" + handIndex, _handRoot);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 148;
            le.preferredHeight = 212;

            // 卡面美术
            Sprite sprite = Resources.Load<Sprite>("Art/Cards/" + card.Id);
            var bg = rt.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                bg.sprite = sprite;
                bg.color = interactable ? Color.white : new Color(0.5f, 0.5f, 0.55f);
            }
            else
            {
                bg.color = CardColor(card);
                if (!interactable) bg.color = new Color(bg.color.r * 0.55f, bg.color.g * 0.55f, bg.color.b * 0.6f);
            }

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = interactable;
            int captured = handIndex;
            btn.onClick.AddListener(delegate { if (_onPlayCard != null) _onPlayCard(captured); });

            // 底部信息条
            RectTransform info = UIFactory.CreateRect("Info", rt);
            UIFactory.SetAnchors(info, 0f, 0f, 1f, 0.42f);
            var infoImg = info.gameObject.AddComponent<Image>();
            infoImg.color = new Color(0.02f, 0.02f, 0.03f, 0.84f);
            infoImg.raycastTarget = false;

            // 名称
            Text name = UIFactory.CreateText("Name", info, card.Name, 19, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(name.rectTransform, 0.04f, 0.60f, 0.96f, 1f);

            // 描述
            Text desc = UIFactory.CreateText("Desc", info, card.Def.Text, 12, new Color(0.86f, 0.84f, 0.8f), TextAnchor.UpperCenter);
            UIFactory.SetAnchors(desc.rectTransform, 0.06f, 0f, 0.94f, 0.58f);

            // 费用徽章（左上）
            RectTransform badge = UIFactory.CreateRect("CostBadge", rt);
            UIFactory.SetAnchors(badge, -0.03f, 0.845f, 0.27f, 1.03f);
            var badgeImg = badge.gameObject.AddComponent<Image>();
            badgeImg.color = new Color(0.55f, 0.11f, 0.15f, 0.96f);
            badgeImg.raycastTarget = false;

            Text cost = UIFactory.CreateText("Cost", badge, card.Cost.ToString(), 22, Color.white, TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(cost.rectTransform, 0f, 0f, 1f, 1f);
        }

        private static string TypeLabel(CardType type)
        {
            switch (type)
            {
                case CardType.Curse: return "咒";
                case CardType.Rite: return "式";
                default: return "术";
            }
        }

        private static Color CardColor(CardInstance card)
        {
            switch (card.Def.Type)
            {
                case CardType.Curse: return Theme.Curse;
                case CardType.Rite: return new Color(0.22f, 0.42f, 0.32f);
                default: return new Color(0.16f, 0.24f, 0.42f);
            }
        }

        // —————————————————————— 台面 ——————————————————————

        private void RefreshTable()
        {
            ClearChildren(_tableRoot);

            var plays = _engine.State.RecentPlays;
            int total = plays.Count;
            int show = Mathf.Min(6, total);
            int start = total - show;
            bool hasNew = total > _lastPlayCount && _lastPlayCount >= 0;

            for (int i = start; i < total; i++)
            {
                float rank = (i - start + 1f) / show;   // 越新越亮
                float alpha = 0.38f + 0.62f * rank;
                bool newest = (i == total - 1);
                CreateTableCard(plays[i], alpha, newest && hasNew);
            }

            _lastPlayCount = total;
        }

        private void CreateTableCard(PlayRecord rec, float alpha, bool animate)
        {
            RectTransform rt = UIFactory.CreateRect("Table_" + rec.Card.InstanceId, _tableRoot);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 104;
            le.preferredHeight = 146;

            Sprite sprite = Resources.Load<Sprite>("Art/Cards/" + rec.Card.Id);
            var img = rt.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                img.color = new Color(0.22f, 0.28f, 0.45f, alpha);
            }
            img.raycastTarget = false;

            // 归属色条：蓝色=你，红色=对手
            RectTransform tag = UIFactory.CreateRect("Tag", rt);
            UIFactory.SetAnchors(tag, 0f, 0.93f, 1f, 1f);
            var tagImg = tag.gameObject.AddComponent<Image>();
            tagImg.color = rec.PlayerIndex == 0
                ? new Color(0.32f, 0.58f, 0.95f, alpha)
                : new Color(0.92f, 0.28f, 0.30f, alpha);
            tagImg.raycastTarget = false;

            // 底部名称条
            RectTransform foot = UIFactory.CreateRect("Foot", rt);
            UIFactory.SetAnchors(foot, 0f, 0f, 1f, 0.21f);
            var footImg = foot.gameObject.AddComponent<Image>();
            footImg.color = new Color(0f, 0f, 0f, 0.78f * alpha);
            footImg.raycastTarget = false;

            Text name = UIFactory.CreateText("Name", foot, rec.Card.Name, 14, new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleCenter);
            UIFactory.SetAnchors(name.rectTransform, 0f, 0f, 1f, 1f);

            if (animate) StartCoroutine(PopIn(rt));
        }

        private System.Collections.IEnumerator PopIn(RectTransform rt)
        {
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(0.55f, 1f, t / duration);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private void RefreshLog()
        {
            var log = _engine.State.Log;
            int start = Mathf.Max(0, log.Count - 12);

            var sb = new StringBuilder();
            for (int i = start; i < log.Count; i++)
            {
                sb.AppendLine(log[i]);
            }
            _logText.text = sb.ToString();
        }

        // —————————————————————— 交互 ——————————————————————

        private void HandleEndTurnClick()
        {
            if (_onEndTurn != null) _onEndTurn();
        }

        public void ShowResult(string text)
        {
            _resultPanel.SetActive(true);
            _resultText.text = text;
        }

        public void SetEndTurnInteractable(bool value)
        {
            if (_endTurnButton != null) _endTurnButton.interactable = value;
        }

        private static void ClearChildren(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                Destroy(child);
            }
        }
    }
}
