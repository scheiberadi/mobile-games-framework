using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Whack-a-Mole as a real arcade game (docs/kids-games/arcade-redesign.md): nine holes, moles pop up out of them and hide
    // again in real time, the child taps them. One game is levels 1-6 in a row, each needing more hits and running faster; a short
    // sound says "faster now". It always starts at level 1 and ends after level 6 or when the child leaves. Nothing is ever lost.
    // All pacing lives in WhackAMoleDirector (Rules/WhackAMole.cs); this screen only draws it and feeds it the frame time.
    public sealed class WhackAMoleScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private enum Phase { Hidden, Rising, Up, Hiding, Squashed }

        // The 3 x 3 field sits left of Eva. Every cell is a full-size tap area (EvaUi.MinTap); the hole picture is drawn smaller.
        private const float CellWidth = 280f;
        private const float CellHeight = 240f;
        private const float FieldCenterX = -170f;
        private static readonly float[] RowY = { 150f, -90f, -330f };
        private static readonly float[] ColX = { -280f, 0f, 280f };

        // A mole rises out of the middle of its hole: the part below the hole's centre line is clipped away, and the front half of the
        // hole is drawn in front of it. Replace arcade/wam_hole and arcade/wam_mole to change the look (the mole must be about 60% of
        // the hole's width).
        private const string HoleSprite = "arcade/wam_hole";
        private const string MoleSprite = "arcade/wam_mole";
        private const float HoleWidth = 270f;
        private const float HoleHeight = 82f;
        private const float HoleY = -30f;
        private const float MoleWidth = 165f;
        private const float MoleHeight = 137f;
        private const float WindowBottomY = HoleY - 4f;

        // Progress: a bar for the hits of the current level and six stars for the levels, above the field.
        private const float BarWidth = 520f;
        private const float BarHeight = 38f;
        private const float BarY = 440f;
        private const float LevelStarSize = 52f;
        private const float LevelStarY = 388f;
        private const float LevelStarSpacing = 64f;

        private const float RiseSeconds = 0.14f;
        private const float HideSeconds = 0.16f;
        private const float SquashSeconds = 0.22f;
        private const float LevelPauseSeconds = 0.7f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then a mole pulses.
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

        private readonly RectTransform[] _moleRects = new RectTransform[WhackAMoleDirector.Holes];
        private readonly Image[] _moleImages = new Image[WhackAMoleDirector.Holes];
        private readonly Phase[] _phase = new Phase[WhackAMoleDirector.Holes];
        private readonly float[] _phaseTime = new float[WhackAMoleDirector.Holes];

        private System.Random _rng;
        private WhackAMoleDirector _director;
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
            BuildProgress();
            BuildField();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.WhackAMole);
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
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = WhackAMoleDirector.MinLevel; level <= WhackAMoleDirector.MaxLevel; level++)
            {
                _director = new WhackAMoleDirector(level, _rng);
                HideAllMoles();
                _active = false;
                ResetIdle();
                ShowProgress(level, 0);

                if (level == WhackAMoleDirector.MinLevel)
                {
                    _eva.SetTalking(true);
                    yield return _game.Voice.SayAndWait("whackamole_find");
                    _eva.SetTalking(false);
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                    yield return new WaitForSeconds(LevelPauseSeconds);
                }

                _active = true;
                var spawned = new List<UpMole>();
                var expired = new List<UpMole>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    spawned.Clear();
                    expired.Clear();
                    _director.Tick(dt, spawned, expired);
                    foreach (var mole in spawned) ShowMole(mole);
                    foreach (var mole in expired) StartHiding(mole.Hole);
                    _idle += dt;
                    MaybeHelp();
                    AnimateMoles(dt);
                    yield return null;
                }

                _active = false;
                for (var h = 0; h < WhackAMoleDirector.Holes; h++)
                    if (_phase[h] == Phase.Rising || _phase[h] == Phase.Up) StartHiding(h);
                yield return AnimateUntilQuiet();
            }
            yield return EndGame();
        }

        private IEnumerator AnimateUntilQuiet()
        {
            var busy = true;
            while (busy)
            {
                AnimateMoles(Time.deltaTime);
                busy = false;
                for (var h = 0; h < WhackAMoleDirector.Holes; h++) if (_phase[h] != Phase.Hidden) busy = true;
                yield return null;
            }
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the moles that are up pulse.
        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("whackamole_find");
            }
            else if (_idleStage == 1 && _idle >= PulseAfterSeconds && _director.Up.Count > 0)
            {
                _idleStage = 2;
                _pulse = true;
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
            }
        }

        // Any tap on the field counts as the child playing: the idle clock and the pulse start over.
        private void ResetIdle()
        {
            _idle = 0f;
            _idleStage = 0;
            _pulse = false;
        }

        // --- Holes and moles ---------------------------------------------------------------------------------

        private void BuildField()
        {
            var go = new GameObject("Field", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _field = (RectTransform)go.transform;
            _field.anchorMin = Vector2.zero;
            _field.anchorMax = Vector2.one;
            _field.offsetMin = _field.offsetMax = Vector2.zero;

            for (var h = 0; h < WhackAMoleDirector.Holes; h++)
            {
                var row = h / 3;
                var column = h % 3;
                var cell = new GameObject("Hole" + h, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                cell.transform.SetParent(_field, false);
                var rect = (RectTransform)cell.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(FieldCenterX + ColX[column], RowY[row]);
                rect.sizeDelta = new Vector2(CellWidth, CellHeight);

                var background = cell.GetComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0f); // invisible, only the tap area
                background.raycastTarget = true;
                var button = cell.GetComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;
                var hole = h;
                button.onClick.AddListener(() => OnHoleTapped(hole));

                var hollow = NewPicture(rect, "Hollow", HoleSprite, new Vector2(HoleWidth, HoleHeight), new Vector2(0f, HoleY));
                hollow.GetComponent<Image>().raycastTarget = false;

                // The window clips the mole at the hole's centre line; the part of the hole below that line stays in front of it.
                var window = new GameObject("Window", typeof(RectTransform), typeof(RectMask2D));
                window.transform.SetParent(rect, false);
                var windowRect = (RectTransform)window.transform;
                windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                windowRect.pivot = new Vector2(0.5f, 0f);
                windowRect.anchoredPosition = new Vector2(0f, WindowBottomY);
                windowRect.sizeDelta = new Vector2(MoleWidth + 40f, MoleHeight + 30f);

                var mole = NewPicture(windowRect, "Mole", MoleSprite, new Vector2(MoleWidth, MoleHeight), Vector2.zero);
                mole.anchorMin = mole.anchorMax = new Vector2(0.5f, 0f);
                mole.pivot = new Vector2(0.5f, 0f);
                mole.anchoredPosition = new Vector2(0f, -MoleHeight);
                var image = mole.GetComponent<Image>();
                image.raycastTarget = false;
                _moleRects[h] = mole;
                _moleImages[h] = image;
                mole.gameObject.SetActive(false);
            }
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

        private void ShowMole(UpMole mole)
        {
            var h = mole.Hole;
            _moleImages[h].color = Color.white;
            _moleRects[h].localScale = Vector3.one;
            _moleRects[h].anchoredPosition = new Vector2(0f, -MoleHeight);
            _moleRects[h].gameObject.SetActive(true);
            SetPhase(h, Phase.Rising);
        }

        private void StartHiding(int hole)
        {
            if (_phase[hole] == Phase.Hidden || _phase[hole] == Phase.Hiding || _phase[hole] == Phase.Squashed) return;
            SetPhase(hole, Phase.Hiding);
        }

        private void HideAllMoles()
        {
            for (var h = 0; h < WhackAMoleDirector.Holes; h++)
            {
                _phase[h] = Phase.Hidden;
                if (_moleRects[h] != null) _moleRects[h].gameObject.SetActive(false);
            }
        }

        private void SetPhase(int hole, Phase phase)
        {
            _phase[hole] = phase;
            _phaseTime[hole] = 0f;
        }

        private void AnimateMoles(float dt)
        {
            for (var h = 0; h < WhackAMoleDirector.Holes; h++)
            {
                if (_phase[h] == Phase.Hidden) continue;
                _phaseTime[h] += dt;
                var rect = _moleRects[h];
                switch (_phase[h])
                {
                    case Phase.Rising:
                    {
                        var k = Mathf.Clamp01(_phaseTime[h] / RiseSeconds);
                        rect.anchoredPosition = new Vector2(0f, -MoleHeight * (1f - PointerHand.EaseInOut(k)));
                        if (k >= 1f) SetPhase(h, Phase.Up);
                        break;
                    }
                    case Phase.Up:
                    {
                        // A pulse once the child has been looking for a while.
                        rect.anchoredPosition = Vector2.zero;
                        var pulse = _pulse ? 1f + 0.1f * Mathf.Sin(Time.time * 9f) : 1f;
                        rect.localScale = Vector3.one * pulse;
                        break;
                    }
                    case Phase.Hiding:
                    {
                        var k = Mathf.Clamp01(_phaseTime[h] / HideSeconds);
                        rect.anchoredPosition = new Vector2(0f, -MoleHeight * PointerHand.EaseInOut(k));
                        if (k >= 1f) Finish(h);
                        break;
                    }
                    case Phase.Squashed:
                    {
                        var k = Mathf.Clamp01(_phaseTime[h] / SquashSeconds);
                        rect.localScale = new Vector3(1f + 0.3f * k, Mathf.Lerp(1f, 0.08f, k), 1f);
                        _moleImages[h].color = new Color(1f, 1f, 1f, 1f - k * k);
                        if (k >= 1f) Finish(h);
                        break;
                    }
                }
            }
        }

        private void Finish(int hole)
        {
            _phase[hole] = Phase.Hidden;
            _moleRects[hole].gameObject.SetActive(false);
            _moleRects[hole].localScale = Vector3.one;
            _moleImages[hole].color = Color.white;
        }

        // --- Tapping -----------------------------------------------------------------------------------------

        private void OnHoleTapped(int hole)
        {
            if (!_active || _director == null || _director.LevelDone) return;
            ResetIdle();
            if (!_director.Whack(hole)) return;
            _totalHits++;
            SetPhase(hole, Phase.Squashed);
            _game.Sfx.Pop();
            _runner.StartCoroutine(Sparkle(_moleRects[hole].position));
            ShowProgress(_director.Level, _director.Hits);
        }

        // A few stars burst out of the whacked mole and fade.
        private IEnumerator Sparkle(Vector3 worldPosition)
        {
            const int stars = 5;
            const float duration = 0.6f;
            var pieces = new RectTransform[stars];
            var directions = new Vector2[stars];
            for (var i = 0; i < stars; i++)
            {
                var piece = NewPicture(_field, "Spark", "arcade/prop_sparkle", new Vector2(64f, 64f), Vector2.zero);
                piece.GetComponent<Image>().raycastTarget = false;
                piece.position = worldPosition;
                pieces[i] = piece;
                var angle = (60f + i * 60f) * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 220f;
            }
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var k = t / duration;
                for (var i = 0; i < stars; i++)
                {
                    pieces[i].anchoredPosition += directions[i] * Time.deltaTime;
                    pieces[i].localScale = Vector3.one * (1f - k * 0.6f);
                    pieces[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - k);
                }
                yield return null;
            }
            for (var i = 0; i < stars; i++) Object.Destroy(pieces[i].gameObject);
        }

        // --- Progress: hits of this level, and which level --------------------------------------------------

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

            _levelStars = new Image[WhackAMoleDirector.MaxLevel];
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
            var fraction = Mathf.Clamp01(hits / (float)WhackAMoleDirector.HitsToPass(level));
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
            _game.Progress.AddCoins(WhackAMoleDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(WhackAMoleDirector.SessionCoins, _progress.transform.position));
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
