using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shared TRACE presenter (Art Studio, M4.7, docs/kids-games/full-catalogue-plan.md "9. Art Studio") - Art
    // Studio's one new must-have mechanic: the finger follows a fixed path within tolerance. Built directly on
    // FingerMazeScreen's own drag-along-a-path shape (read that one first) - PathDragger and
    // FingerMazePath.NearestFraction are reused exactly as they are; the only real difference is what happens
    // to the path visually as the child progresses (each segment lights up as "drawn" rather than staying a
    // plain corridor) and that there is no character, only a pencil-tip drag icon. One instance is built per
    // game (registered under its own ScreenId, same as `new MatchScreen(...)`), configured with that game's
    // TraceGameKind and level/buffer accessors.
    public sealed class TraceScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float PencilSize = EvaUi.MinTap;
        private const float LineWidth = 60f;

        private const float HintGlowSeconds = 1.2f;
        private const float HintRestSeconds = 0.4f;
        private const float DemoUnitsPerSecond = 220f;
        private const float MinDemoSeconds = 1.2f;
        private const float MaxDemoSeconds = 4.5f;

        // A drag counts as reaching the end once it snaps this close to it - same reasoning as FingerMaze's own
        // FinishFraction (a finger rarely lands on the exact last point).
        private const float FinishFraction = 0.97f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly TraceGameKind _kind;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly string _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _traceField, _lineField;
        private PathDragger _dragger;
        private GameObject _endPanel;

        private readonly List<Image> _segmentImages = new List<Image>();
        private float[] _waypointFractions;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousId;
        private TraceRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public TraceScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite, TraceGameKind kind,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _kind = kind;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _traceField = CreateFullRectContainer("TraceField");
            _lineField = CreateFullRectContainer("LineField", _traceField);
            _dragger = PathDragger.Create(_traceField, EvaUi.Sprite("artstudio/pencil_tip"), PencilSize);
            _dragger.Progressed += OnDraggerProgressed;
            _dragger.Released += OnDraggerReleased;
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousId = null;
            _rightLineIndex = 0;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = TraceRoundGenerator.Create(_kind, _getLevel(_game.Progress), _rng, _previousId);
            _previousId = _round.Id;
            _ladder = new HelpLadder();
            _roundOver = false;

            DrawPath(_round.Path);
            _dragger.SetPath(_round.Path);
            _dragger.SnapTo(0f);
            _dragger.Enabled = false;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.PromptVoiceKey);
            _eva.SetTalking(false);
            _dragger.Enabled = true;
        }

        // --- Path -------------------------------------------------------------------------------------------

        private void DrawPath(WorldPoint[] path)
        {
            ClearLineField(_lineField);
            _segmentImages.Clear();
            for (var i = 1; i < path.Length; i++)
                _segmentImages.Add(BuildSegmentTile(path[i - 1], path[i]));
            _waypointFractions = WaypointFractions(path);
        }

        private Image BuildSegmentTile(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_lineField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((a.X + b.X) / 2f, (a.Y + b.Y) / 2f);
            rect.sizeDelta = new Vector2(length, LineWidth);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("artstudio/trace_path");
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0.4f);
            return image;
        }

        private static float[] WaypointFractions(WorldPoint[] path)
        {
            var total = MapPath.Length(path);
            var fractions = new float[path.Length];
            var walked = 0f;
            for (var i = 0; i < path.Length; i++)
            {
                if (i > 0) walked += Distance(path[i - 1], path[i]);
                fractions[i] = total <= 0f ? 0f : walked / total;
            }
            return fractions;
        }

        private static float Distance(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private int NextSegmentIndex()
        {
            var fraction = _dragger.Fraction;
            for (var i = 0; i < _waypointFractions.Length - 1; i++)
                if (fraction < _waypointFractions[i + 1] - 0.001f) return i;
            return Mathf.Max(0, _waypointFractions.Length - 2);
        }

        // A segment already passed by the drag stays lit (the "drawn" trail) rather than reverting - the one
        // thing this presenter adds beyond FingerMazeScreen's plain corridor.
        private void RevealSegmentsUpTo(float fraction)
        {
            for (var i = 0; i < _segmentImages.Count; i++)
            {
                var passed = fraction >= _waypointFractions[i + 1] - 0.02f;
                _segmentImages[i].color = passed ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
        }

        private static void ClearLineField(RectTransform field)
        {
            for (var i = field.childCount - 1; i >= 0; i--)
            {
                var child = field.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnDraggerProgressed(float fraction)
        {
            if (_round == null || _roundOver) return;
            RevealSegmentsUpTo(fraction);
            if (fraction >= FinishFraction) _runner.StartCoroutine(OnReachedEnd());
        }

        private void OnDraggerReleased()
        {
            if (_round == null || _roundOver) return;
            OnGaveUpBeforeEnd();
        }

        // Letting go before the end is this game's only "mistake" - identical shape to FingerMaze's own.
        private void OnGaveUpBeforeEnd()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the next stretch of the path glows brighter (the plan's own Hint wording).
        private IEnumerator RunHint()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);
            var lead = _game.Voice.Duration(_hintVoiceKey) + Voice.BreathSeconds;

            var segmentIndex = NextSegmentIndex();
            _runner.StartCoroutine(GlowSegment(segmentIndex, Mathf.Max(lead, HintGlowSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            _dragger.Enabled = true;
        }

        private IEnumerator GlowSegment(int index, float duration)
        {
            if (index < 0 || index >= _segmentImages.Count) yield break;
            var image = _segmentImages[index];
            var original = image.color;
            var glow = Color.Lerp(original, Color.white, 0.5f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        // 3rd mistake: the path traces itself once (a moving dot), per the plan's own Demo note, then the
        // pencil resets to the start so the child repeats it themselves.
        private IEnumerator RunDemonstrate()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);

            _dragger.SnapTo(0f);
            RevealSegmentsUpTo(0f);
            _hand.Rect.gameObject.SetActive(true);
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);

            var duration = Mathf.Clamp(MapPath.Length(_round.Path) / DemoUnitsPerSecond, MinDemoSeconds, MaxDemoSeconds);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var fraction = Mathf.Clamp01(t / duration);
                _dragger.SnapTo(fraction);
                RevealSegmentsUpTo(fraction);
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);
                yield return null;
            }
            _dragger.SnapTo(1f);
            RevealSegmentsUpTo(1f);
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Hide();

            _dragger.SnapTo(0f);
            RevealSegmentsUpTo(0f);
            _dragger.Enabled = true;
        }

        private IEnumerator OnReachedEnd()
        {
            _roundOver = true;
            _dragger.Enabled = false;
            _dragger.SnapTo(1f);
            RevealSegmentsUpTo(1f);

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_dragger.Rect, _dragger.Image, 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            var progress = _game.Progress;
            _setLevel(progress, DifficultyLadder.RecordRound(_getBuffer(progress), _getLevel(progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _dragger.Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= TraceRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

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

        // --- End of session -----------------------------------------------------------------------------

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
                EndButtonPositions[1], EndButtonSize, GoHomeAfterSession);

            _endPanel.SetActive(false);
        }

        private void GoHomeAfterSession() => _game.Navigator.Show(_homeScreenId);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _traceField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens (identical shapes to NumberHuntScreen's - see there for the reasoning behind each) ---

        private static IEnumerator PopPulse(RectTransform target, Image tint, float peakScale, float duration)
        {
            var original = tint != null ? tint.color : Color.white;
            var glow = Color.Lerp(original, Color.white, 0.6f);
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, k);
                if (tint != null) tint.color = Color.Lerp(original, glow, k);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, k);
                if (tint != null) tint.color = Color.Lerp(glow, original, k);
                yield return null;
            }
            target.localScale = Vector3.one;
            if (tint != null) tint.color = original;
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

        // --- Helpers ----------------------------------------------------------------------------------------

        private RectTransform CreateFullRectContainer(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void AddPictureBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(_backgroundSprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
