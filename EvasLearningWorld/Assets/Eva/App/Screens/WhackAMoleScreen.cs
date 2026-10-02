using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Whack-a-Mole as a real arcade game (docs/kids-games/arcade-redesign.md): nine holes, moles pop up and hide again in
    // real time, the child taps the ones that look like the mole on the card. A decoy that is tapped shakes its head, nothing is ever
    // lost. All pacing lives in WhackAMoleDirector (Rules/WhackAMole.cs); this screen only draws it and feeds it the frame time.
    public sealed class WhackAMoleScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private enum Phase { Hidden, Rising, Up, Hiding, Squashed }

        private const int RoundsPerSession = 3;

        // The 3 x 3 field sits left of Eva. Every cell is a full-size tap area (EvaUi.MinTap); the hole picture is drawn smaller.
        private const float CellSize = 240f;
        private const float FieldCenterX = -150f;
        private static readonly float[] RowY = { 150f, -90f, -330f };
        private static readonly float[] ColX = { -240f, 0f, 240f };
        private const float HolePictureWidth = 210f;
        private const float MolePictureSize = 230f;

        // The target card and the hit counter sit right of the field, above Eva.
        private const float CardX = 440f;
        private const float CardY = 170f;
        private const float CardSize = 200f;
        private const float StarSize = 56f;
        private const float StarY = 30f;

        private const float RiseSeconds = 0.14f;
        private const float HideSeconds = 0.16f;
        private const float SquashSeconds = 0.22f;
        private const float HintAfterSeconds = 9f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private GameObject _endPanel;
        private Image _card;
        private Image[] _stars;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly RectTransform[] _moleRects = new RectTransform[WhackAMoleDirector.Holes];
        private readonly Image[] _moleImages = new Image[WhackAMoleDirector.Holes];
        private readonly Button[] _holeButtons = new Button[WhackAMoleDirector.Holes];
        private readonly Phase[] _phase = new Phase[WhackAMoleDirector.Holes];
        private readonly float[] _phaseTime = new float[WhackAMoleDirector.Holes];
        private readonly bool[] _isTarget = new bool[WhackAMoleDirector.Holes];

        private System.Random _rng;
        private WhackAMoleDirector _director;
        private int _roundIndex;
        private int _rightLineIndex;
        private string _previousTarget;
        private bool _active;
        private float _sinceHit;
        private float _lastHintAt;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
            BuildCard();
            BuildField();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.WhackAMole);
            StartNewSession();
        }

        public override void OnHide()
        {
            _active = false;
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            _previousTarget = null;
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        // --- One round -------------------------------------------------------------------------------------

        private IEnumerator RunRound()
        {
            var level = _game.Progress.WhackAMoleLevel;
            var target = WhackAMoleDirector.NextTarget(_previousTarget, _rng);
            _previousTarget = target;
            _director = new WhackAMoleDirector(level, target, _rng);

            HideAllMoles();
            _card.sprite = EvaUi.Sprite("arcade/molecard_" + target);
            ShowStars(WhackAMoleDirector.HitsPerRound(level), 0);
            _active = false;
            _sinceHit = 0f;
            _lastHintAt = -100f;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("whackamole_find");
            _eva.SetTalking(false);

            _active = true;
            var spawned = new List<UpMole>();
            var expired = new List<UpMole>();
            while (!_director.RoundDone)
            {
                var dt = Time.deltaTime;
                spawned.Clear();
                expired.Clear();
                _director.Tick(dt, spawned, expired);
                foreach (var mole in spawned) ShowMole(mole);
                foreach (var mole in expired) StartHiding(mole.Hole);
                _sinceHit += dt;
                MaybeHint();
                AnimateMoles(dt);
                yield return null;
            }

            _active = false;
            for (var h = 0; h < WhackAMoleDirector.Holes; h++)
                if (_phase[h] == Phase.Rising || _phase[h] == Phase.Up) StartHiding(h);
            yield return AnimateUntilQuiet();

            var clean = _director.WrongWhacks <= 2;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;
            _game.Progress.WhackAMoleLevel = DifficultyLadder.RecordRound(_game.Progress.WhackAMoleBuffer, _game.Progress.WhackAMoleLevel, clean);
            _runner.StartCoroutine(PayCoins(clean ? CoinPayout.Clean : CoinPayout.Assisted, _card.rectTransform.position));
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= RoundsPerSession) yield return EndSession();
            else yield return RunRound();
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

        // After a quiet spell Eva repeats what to look for and the target moles on the board pulse.
        private void MaybeHint()
        {
            if (_sinceHit < HintAfterSeconds || _sinceHit - _lastHintAt < HintAfterSeconds) return;
            var anyTarget = false;
            for (var h = 0; h < WhackAMoleDirector.Holes; h++)
                if (_isTarget[h] && (_phase[h] == Phase.Up || _phase[h] == Phase.Rising)) anyTarget = true;
            if (!anyTarget) return;
            _lastHintAt = _sinceHit;
            if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
            _game.Voice.Say("whackamole_hint");
        }

        private bool HintActive => _active && _sinceHit >= HintAfterSeconds;

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
                rect.sizeDelta = new Vector2(CellSize, CellSize);

                var background = cell.GetComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0f); // invisible, only the tap area
                background.raycastTarget = true;
                var button = cell.GetComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;
                var hole = h;
                button.onClick.AddListener(() => OnHoleTapped(hole));
                _holeButtons[h] = button;

                var hollow = NewPicture(rect, "Hollow", "arcade/prop_mole_hole", new Vector2(HolePictureWidth, HolePictureWidth * 0.72f), new Vector2(0f, -40f));
                hollow.GetComponent<Image>().raycastTarget = false;

                var mole = NewPicture(rect, "Mole", null, new Vector2(MolePictureSize, MolePictureSize), new Vector2(0f, -78f));
                mole.pivot = new Vector2(0.5f, 0f);
                mole.anchoredPosition = new Vector2(0f, -78f);
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
            _isTarget[h] = mole.IsTarget;
            _moleImages[h].sprite = EvaUi.Sprite("arcade/mole_" + mole.MoleId);
            _moleImages[h].color = Color.white;
            _moleRects[h].localRotation = Quaternion.identity;
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
                        rect.localScale = new Vector3(1f, PointerHand.EaseInOut(k), 1f);
                        if (k >= 1f) SetPhase(h, Phase.Up);
                        break;
                    }
                    case Phase.Up:
                    {
                        // A little life while waiting, and a stronger pulse on the targets once the child has been looking for a while.
                        var pulse = _isTarget[h] && HintActive ? 1f + 0.1f * Mathf.Sin(Time.time * 9f) : 1f;
                        rect.localScale = Vector3.one * pulse;
                        break;
                    }
                    case Phase.Hiding:
                    {
                        var k = Mathf.Clamp01(_phaseTime[h] / HideSeconds);
                        rect.localScale = new Vector3(1f, 1f - PointerHand.EaseInOut(k), 1f);
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
            if (!_active || _director == null || _director.RoundDone) return;
            switch (_director.Whack(hole))
            {
                case WhackResult.Target:
                    SetPhase(hole, Phase.Squashed);
                    _sinceHit = 0f;
                    _game.Sfx.Pop();
                    _runner.StartCoroutine(Sparkle(_moleRects[hole].position));
                    ShowStars(WhackAMoleDirector.HitsPerRound(_director.Level), _director.Hits);
                    break;
                case WhackResult.Decoy:
                    _game.Sfx.Retry();
                    _runner.StartCoroutine(ShakeHead(_moleRects[hole]));
                    break;
            }
        }

        private static IEnumerator ShakeHead(RectTransform mole)
        {
            const float duration = 0.4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var angle = Mathf.Sin(t / duration * 4f * Mathf.PI * 2f) * 10f * (1f - t / duration);
                mole.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            mole.localRotation = Quaternion.identity;
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

        // --- Target card and hit counter ---------------------------------------------------------------------

        private void BuildCard()
        {
            var card = NewPicture(Root, "TargetCard", null, new Vector2(CardSize, CardSize), new Vector2(CardX, CardY));
            _card = card.GetComponent<Image>();
            _card.raycastTarget = false;

            _stars = new Image[6];
            for (var i = 0; i < _stars.Length; i++)
            {
                var star = NewPicture(Root, "Star" + i, "arcade/prop_sparkle", new Vector2(StarSize, StarSize), Vector2.zero);
                _stars[i] = star.GetComponent<Image>();
                _stars[i].raycastTarget = false;
            }
        }

        private void ShowStars(int total, int filled)
        {
            var spacing = StarSize + 6f;
            var startX = CardX - (total - 1) * spacing / 2f;
            for (var i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(i < total);
                if (i >= total) continue;
                _stars[i].rectTransform.anchoredPosition = new Vector2(startX + i * spacing, StarY);
                _stars[i].color = i < filled ? Color.white : new Color(1f, 1f, 1f, 0.28f);
            }
        }

        // --- Session end, coins, Eva -------------------------------------------------------------------------

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        private IEnumerator EndSession()
        {
            yield return _game.Voice.SayAndWait("count_done");
            _eva.Cheer();
            SetSessionEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
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
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(ScreenId.Arcade));

            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            _field.gameObject.SetActive(!ended);
            _card.gameObject.SetActive(!ended);
            foreach (var star in _stars) if (ended) star.gameObject.SetActive(false);
            _endPanel.SetActive(ended);
        }

        private static IEnumerator BigCheer(RectTransform target, Vector3 baseScale)
        {
            const float peak = 1.18f, duration = 0.4f, half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = baseScale;
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
