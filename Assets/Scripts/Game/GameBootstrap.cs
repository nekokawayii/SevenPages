using System.Collections;
using SevenPages.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SevenPages.Game
{
    /// <summary>
    /// 入口：创建摄像机 / Canvas / EventSystem，装载卡池，开启一局「玩家 vs AI」。
    /// 放在场景里任意空物体上即可；场景本身可以是空的。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public int Seed = 20261113;

        /// <summary>演示模式：开局后自动出几张牌并结束回合，用于无交互截图验证。</summary>
        private bool _autoDemo;

        private BattleEngine _engine;
        private BattleView _view;
        private GreedyAi _ai;
        private bool _busy;

        private void Start()
        {
            CardSetDto set = CardDataLoader.Load();
            if (set == null) return;

            CardDatabase db = CardDataLoader.BuildDatabase(set);
            _ai = new GreedyAi();

            _engine = new BattleEngine(db, Seed);
            _engine.StartBattle("你", false, set.starterDeck, "书魇", true, set.starterDeck);

            Canvas canvas = EnsureCanvas();
            EnsureEventSystem();
            EnsureCamera();

            var viewGo = new GameObject("BattleView");
            viewGo.transform.SetParent(canvas.transform, false);
            _view = viewGo.AddComponent<BattleView>();
            _view.Init(_engine, HandlePlayCard, HandleEndTurn, canvas);

            Debug.Log("[七页轮回] 开局完成，玩家先手");

            string[] cliArgs = System.Environment.GetCommandLineArgs();
            _autoDemo = System.Array.IndexOf(cliArgs, "-sevenpages-demo") >= 0;
            if (_autoDemo)
            {
                Debug.Log("[七页轮回] 演示模式：自动出牌");
                StartCoroutine(DemoRoutine());
            }
        }

        private System.Collections.IEnumerator DemoRoutine()
        {
            yield return new WaitForSeconds(1.2f);

            for (int guard = 0; guard < 3; guard++)
            {
                if (_engine.State.IsOver) yield break;
                int index = FindPlayableHandIndex();
                if (index < 0) break;
                HandlePlayCard(index);
                yield return new WaitForSeconds(0.9f);
            }

            yield return new WaitForSeconds(0.6f);
            if (!_engine.State.IsOver) HandleEndTurn();
        }

        private int FindPlayableHandIndex()
        {
            PlayerState me = _engine.State.Players[0];
            for (int i = 0; i < me.Hand.Count; i++)
            {
                CardInstance card = me.Hand[i];
                if (card.Def.IsPlayable && card.Def.Cost <= me.Spirit) return i;
            }
            return -1;
        }

        private void HandlePlayCard(int handIndex)
        {
            if (_busy || _engine == null || _engine.State.IsOver) return;

            bool ok = _engine.TryPlayCard(handIndex);
            _view.Refresh();

            if (ok && _engine.State.IsOver) FinishGame();
        }

        private void HandleEndTurn()
        {
            if (_busy || _engine == null || _engine.State.IsOver) return;

            _engine.EndTurn();
            _view.Refresh();

            if (_engine.State.IsOver)
            {
                FinishGame();
                return;
            }

            if (_engine.State.Current.IsAi) StartCoroutine(AiTurnRoutine());
        }

        private IEnumerator AiTurnRoutine()
        {
            _busy = true;
            _view.SetEndTurnInteractable(false);

            yield return new WaitForSeconds(0.55f);

            if (!_engine.State.IsOver && _engine.State.Current.IsAi)
            {
                _ai.TakeTurn(_engine);
            }

            _view.Refresh();
            _busy = false;

            if (_engine.State.IsOver)
            {
                FinishGame();
            }
            else
            {
                _view.SetEndTurnInteractable(true);
            }
        }

        private void FinishGame()
        {
            BattleResult result = _engine.BuildResult();
            string text;

            if (result.Outcome == BattleOutcome.Player1Win)
                text = "你的书页尚存。\n「书魇」第三次轮回了。";
            else if (result.Outcome == BattleOutcome.Player2Win)
                text = "你第三次轮回。\n墨迹已干，无人记得这一页。";
            else
                text = "两本书同时燃尽。\n平局。";

            _view.ShowResult(text);
            Debug.Log("[七页轮回] 对局结束：" + result);
        }

        // —————————————————————— 基础设施 ——————————————————————

        private static Canvas EnsureCanvas()
        {
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null) return existing;

            var go = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";

            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
            cam.orthographic = true;
        }
    }
}
