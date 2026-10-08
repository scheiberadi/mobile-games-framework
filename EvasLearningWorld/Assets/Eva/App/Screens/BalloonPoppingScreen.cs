using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Balloon Popping as a real arcade game (docs/kids-games/arcade-redesign.md): balloons of many colours rise, the child
    // pops them. One game is levels 1-6 in a row like Whack-a-Mole: each needs more pops and the
    // balloons rise faster, a short sound says "faster now", it always starts at level 1 and ends after level 6 or when the child
    // leaves. Nothing is ever lost: a balloon that is not popped just floats off the top. All pacing lives in
    // BalloonPoppingDirector (Rules/BalloonPopping.cs); this screen only draws it and feeds it the frame time.
    public sealed class BalloonPoppingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class BalloonView
        {
            public RectTransform Rect;
            public Image Image;
            public UpBalloon Model;
        }

        private static readonly string[] BalloonSprites = { "arcade/bal_a", "arcade/bal_b", "arcade/bal_c", "arcade/bal_d", "arcade/bal_e", "arcade/bal_f" };

        // The balloons rise in six lanes across the whole screen. A balloon is a full-size tap area (EvaUi.MinTap) with the picture filling it.
        private const float BalloonSize = 240f;
        private const float LaneSpacing = 240f;
        private const float StartY = -470f;
        private const float EndY = 400f;
        private const float WobbleWidth = 18f;
        private const float FadeFrom = 0.9f;
        private const int PoolSize = 18;

        // Progress: a bar for the pops of the current level and six stars for the levels, above the field.
        private const float BarWidth = 520f;
        private const float BarHeight = 38f;
        private const float BarY = 395f;
        private const float LevelStarSize = 60f;
        private const float LevelStarY = 335f;
        private const float LevelStarSpacing = 68f;

        private const float PopSeconds = 0.4f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the balloons pulse.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private GameObject _endPanel;
        private RectTransform _barFill;
        private GameObject _progress;
        private Image[] _levelStars;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly List<BalloonView> _views = new List<BalloonView>();

        private System.Random _rng;
        private BalloonPoppingDirector _director;
        private int _totalHits;
        private bool _paid;
        private bool _active;
        private float _idle;
        private int _idleStage;
        private bool _pulse;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
            BuildField();
            BuildProgress(); // after the field so the balloons float behind the bar
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.BalloonPopping);
            StartNewGame();
        }

        public override void OnHide()
        {
            _active = false;
            PayIfPlayed();
        }

        private void StartNewGame()
        {
            _rng = new System.Random();
            _totalHits = 0;
            _paid = false;
            _director = null; // nothing carries over from a game before
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = BalloonPoppingDirector.MinLevel; level <= BalloonPoppingDirector.MaxLevel; level++)
            {
                // The balloons still rising when a level ends go on into the next one: no clearing, no pause.
                _director = new BalloonPoppingDirector(level, _rng, _director?.Up.ToArray());
                ResetIdle();
                ShowProgress(level, 0);

                if (level == BalloonPoppingDirector.MinLevel)
                {
                    HideAllBalloons();
                    _active = false;
                    _eva.SetTalking(true);
                    yield return _game.Voice.SayAndWait("balloonpop_find");
                    _eva.SetTalking(false);
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                    Haptics.Win();
                }

                _active = true;
                var spawned = new List<UpBalloon>();
                var escaped = new List<UpBalloon>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    spawned.Clear();
                    escaped.Clear();
                    _director.Tick(dt, spawned, escaped);
                    foreach (var balloon in spawned) ShowBalloon(balloon);
                    foreach (var balloon in escaped) ReleaseView(balloon);
                    _idle += dt;
                    MaybeHelp();
                    MoveBalloons();
                    yield return null;
                }

            }
            _active = false;
            yield return EndGame();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the balloons on the screen pulse.
        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("balloonpop_find");
            }
            else if (_idleStage == 1 && _idle >= PulseAfterSeconds && _director.Up.Count > 0)
            {
                _idleStage = 2;
                _pulse = true;
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
            }
        }

        // Any tap on a balloon counts as the child playing: the idle clock and the pulse start over.
        private void ResetIdle()
        {
            _idle = 0f;
            _idleStage = 0;
            _pulse = false;
        }

        // --- Balloons ----------------------------------------------------------------------------------------

        private void BuildField()
        {
            var go = new GameObject("Field", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _field = (RectTransform)go.transform;
            _field.anchorMin = Vector2.zero;
            _field.anchorMax = Vector2.one;
            _field.offsetMin = _field.offsetMax = Vector2.zero;

            for (var i = 0; i < PoolSize; i++)
            {
                var balloon = new GameObject("Balloon" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget));
                balloon.transform.SetParent(_field, false);
                var rect = (RectTransform)balloon.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(BalloonSize, BalloonSize);

                var image = balloon.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = true;
                var button = balloon.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;

                var view = new BalloonView { Rect = rect, Image = image };
                button.onClick.AddListener(() => OnBalloonTapped(view));
                _views.Add(view);
                balloon.SetActive(false);
            }
        }

        private void ShowBalloon(UpBalloon balloon)
        {
            foreach (var view in _views)
            {
                if (view.Model != null) continue;
                view.Model = balloon;
                view.Image.sprite = EvaUi.Sprite(BalloonSprites[balloon.Look]);
                view.Image.color = Color.white;
                view.Rect.localScale = Vector3.one;
                view.Rect.gameObject.SetActive(true);
                Place(view);
                return;
            }
        }

        private void ReleaseView(UpBalloon balloon)
        {
            foreach (var view in _views)
            {
                if (view.Model != balloon) continue;
                view.Model = null;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void HideAllBalloons()
        {
            foreach (var view in _views)
            {
                view.Model = null;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void MoveBalloons()
        {
            foreach (var view in _views) if (view.Model != null) Place(view);
        }

        // Rises along its lane with a gentle sway; fades out near the top where the Hud is.
        private void Place(BalloonView view)
        {
            var balloon = view.Model;
            var progress = Mathf.Clamp01(balloon.Progress);
            var x = (balloon.Lane - (BalloonPoppingDirector.Lanes - 1) / 2f) * LaneSpacing + Mathf.Sin(balloon.Age * 1.6f + balloon.Lane * 1.7f) * WobbleWidth;
            view.Rect.anchoredPosition = new Vector2(x, Mathf.Lerp(StartY, EndY, progress));
            var pulse = _pulse ? 1f + 0.08f * Mathf.Sin(Time.time * 9f) : 1f;
            view.Rect.localScale = Vector3.one * pulse;
            var alpha = progress < FadeFrom ? 1f : 1f - (progress - FadeFrom) / (1f - FadeFrom);
            view.Image.color = new Color(1f, 1f, 1f, alpha);
        }

        // --- Tapping -----------------------------------------------------------------------------------------

        private void OnBalloonTapped(BalloonView view)
        {
            if (!_active || _director == null || _director.LevelDone || view.Model == null) return;
            var balloon = view.Model;
            ResetIdle();
            if (!_director.Pop(balloon)) return;
            _totalHits++;
            view.Model = null;
            view.Rect.gameObject.SetActive(false);
            _game.Sfx.Pop();
            Haptics.Tap();
            _runner.StartCoroutine(Burst(view.Rect.position));
            ShowProgress(_director.Level, _director.Hits);
        }

        // The confetti burst of a popped balloon grows a little and fades.
        private IEnumerator Burst(Vector3 worldPosition)
        {
            var piece = NewPicture(_field, "Burst", "arcade/prop_pop", new Vector2(BalloonSize, BalloonSize), Vector2.zero);
            var image = piece.GetComponent<Image>();
            image.raycastTarget = false;
            piece.position = worldPosition;
            for (var t = 0f; t < PopSeconds; t += Time.deltaTime)
            {
                var k = t / PopSeconds;
                piece.localScale = Vector3.one * (0.7f + 0.6f * k);
                image.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            Object.Destroy(piece.gameObject);
        }

        private static RectTransform NewPicture(RectTransform parent, string name, string sprite, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            if (sprite != null) image.sprite = EvaUi.Sprite(sprite);
            return rect;
        }

        // --- Progress: pops of this level, and which level --------------------------------------------------

        private void BuildProgress()
        {
            var container = new GameObject("Progress", typeof(RectTransform));
            container.transform.SetParent(Root, false);
            _progress = container;
            var containerRect = (RectTransform)container.transform;
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = containerRect.offsetMax = Vector2.zero;

            var back = new GameObject("BarBack", typeof(RectTransform), typeof(Image));
            back.transform.SetParent(containerRect, false);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(0f, BarY);
            backRect.sizeDelta = new Vector2(BarWidth, BarHeight);
            var backImage = back.GetComponent<Image>();
            backImage.color = new Color(0f, 0f, 0f, 0.55f);
            backImage.raycastTarget = false;

            var fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(backRect, false);
            _barFill = (RectTransform)fill.transform;
            _barFill.anchorMin = _barFill.anchorMax = new Vector2(0f, 0.5f);
            _barFill.pivot = new Vector2(0f, 0.5f);
            _barFill.anchoredPosition = new Vector2(4f, 0f);
            _barFill.sizeDelta = new Vector2(0f, BarHeight - 8f);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(1f, 0.82f, 0.15f, 1f);
            fillImage.raycastTarget = false;

            var starBack = new GameObject("StarsBack", typeof(RectTransform), typeof(Image));
            starBack.transform.SetParent(containerRect, false);
            var starBackRect = (RectTransform)starBack.transform;
            starBackRect.anchorMin = starBackRect.anchorMax = starBackRect.pivot = new Vector2(0.5f, 0.5f);
            starBackRect.anchoredPosition = new Vector2(0f, LevelStarY);
            starBackRect.sizeDelta = new Vector2(BarWidth, LevelStarSize + 10f);
            var starBackImage = starBack.GetComponent<Image>();
            starBackImage.color = new Color(0f, 0f, 0f, 0.4f);
            starBackImage.raycastTarget = false;

            _levelStars = new Image[BalloonPoppingDirector.MaxLevel];
            for (var i = 0; i < _levelStars.Length; i++)
            {
                var x = (i - (_levelStars.Length - 1) / 2f) * LevelStarSpacing;
                var star = NewPicture(containerRect, "Level" + (i + 1), "arcade/prop_sparkle", new Vector2(LevelStarSize, LevelStarSize), new Vector2(x, LevelStarY));
                _levelStars[i] = star.GetComponent<Image>();
                _levelStars[i].raycastTarget = false;
            }
        }

        private void ShowProgress(int level, int hits)
        {
            var fraction = Mathf.Clamp01(hits / (float)BalloonPoppingDirector.HitsToPass(level));
            _barFill.sizeDelta = new Vector2((BarWidth - 8f) * fraction, BarHeight - 8f);
            for (var i = 0; i < _levelStars.Length; i++)
                _levelStars[i].color = i < level ? Color.white : new Color(1f, 1f, 1f, 0.28f);
        }

        // --- End of the game, the one coin, Eva ---------------------------------------------------------------

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        // Leaving early still pays the coin if the child played at all (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _totalHits == 0) return;
            _paid = true;
            _game.Progress.AddCoins(BalloonPoppingDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(BalloonPoppingDirector.SessionCoins, _progress.transform.position));
            }
            _eva.Cheer();
            SetGameEnded(true);
            yield break;
        }

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(WinCelebration));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewGame);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(ScreenId.Arcade));

            _endPanel.SetActive(false);
        }

        private void SetGameEnded(bool ended)
        {
            _field.gameObject.SetActive(!ended);
            _progress.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        private void AddBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/arcade_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
