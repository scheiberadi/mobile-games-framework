using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's twelfth game (spec 4.1: Rotate the Piece). Wired into Activities.cs.
    //
    // The plan's one new mechanic between the NAVIGATION cluster and the DRAG&DROP cluster: a rotate handle
    // orbiting a piece (RotateDragger, App/Ui) rather than a slide (PathDragger) or a free drag (DragItem). The
    // child drags the handle around until the piece matches the target orientation. No PointerHand here - unlike
    // every earlier game, this one's own Demo animates the piece itself rather than a hand miming a tap or drag,
    // exactly per the plan's own note for this game. Its Hint (an arrow, per the plan) is stood in for with a
    // small wobble toward the correct direction instead of a real directional-arrow sprite, since no placeholder
    // art can actually convey a rotation direction until real art exists - flagged here the same way every other
    // placeholder-art gap has been this session.
    public sealed class RotateThePieceScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private static readonly Vector2 PiecePosition = new Vector2(-160f, -60f);
        private const float PieceSize = 220f;
        private const float HandleSize = EvaUi.MinTap;
        private const float HandleRadius = 170f;

        private const float HintWobbleSeconds = 1.2f;
        private const float HintRestSeconds = 0.4f;
        private const float DemoDegreesPerSecond = 90f;
        private const float MinDemoSeconds = 0.5f;
        private const float MaxDemoSeconds = 2.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private RectTransform _pieceRect;
        private Image _pieceImage;
        private RotateDragger _dragger;
        private GameObject _endPanel;

        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private RotateThePieceRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _field = CreateFullRectContainer("RotateField");
            BuildPiece();
            _dragger = RotateDragger.Create(_field, EvaUi.Sprite("rotatepiece/handle"), HandleSize);
            _dragger.PieceRect = _pieceRect;
            _dragger.Radius = HandleRadius;
            _dragger.Rotated += OnRotated;
            _dragger.Released += OnReleased;
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.RotateThePiece);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = RotateThePieceRoundGenerator.Create(_game.Progress.RotateThePieceLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;

            _pieceImage.sprite = EvaUi.Sprite(_round.PieceSprite);
            _dragger.StepDegrees = _round.StepDegrees;
            _dragger.SnapTo(_round.StartAngle);
            _dragger.Enabled = false;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("rotatepiece_find");
            _eva.SetTalking(false);
            _dragger.Enabled = true;
        }

        // --- Piece ------------------------------------------------------------------------------------------

        private void BuildPiece()
        {
            var go = new GameObject("Piece", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_field, false);
            _pieceRect = (RectTransform)go.transform;
            _pieceRect.anchorMin = _pieceRect.anchorMax = _pieceRect.pivot = new Vector2(0.5f, 0.5f);
            _pieceRect.anchoredPosition = PiecePosition;
            _pieceRect.sizeDelta = new Vector2(PieceSize, PieceSize);

            _pieceImage = go.GetComponent<Image>();
            _pieceImage.preserveAspect = true;
        }

        // --- Handle events ------------------------------------------------------------------------------------

        private void OnRotated(float angle)
        {
            if (_round == null || _roundOver) return;
            if (RotateThePieceRoundGenerator.IsMatch(_round, angle)) _runner.StartCoroutine(OnMatched());
        }

        private void OnReleased()
        {
            if (_round == null || _roundOver) return;
            if (!RotateThePieceRoundGenerator.IsMatch(_round, _dragger.Angle)) HandleMistake();
        }

        private void HandleMistake()
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

        // +1 clockwise, -1 counter-clockwise - whichever way around from `from` to `to` is the shorter turn.
        private static int ShorterDirection(float from, float to)
        {
            var diff = RotateThePieceRoundGenerator.Normalize(to - from);
            return diff <= 180f ? 1 : -1;
        }

        // 2nd mistake: the piece leans a little toward the correct direction and back, repeatedly (a stand-in
        // for the plan's "an arrow shows which way to rotate" - see the class comment).
        private IEnumerator RunHint()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("rotatepiece_hint");
            var lead = _game.Voice.Duration("rotatepiece_hint") + Voice.BreathSeconds;

            var direction = ShorterDirection(_dragger.Angle, _round.TargetAngle);
            _runner.StartCoroutine(WobbleTowardDirection(direction, Mathf.Max(lead, HintWobbleSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintWobbleSeconds);
            _dragger.Enabled = true;
        }

        private IEnumerator WobbleTowardDirection(int direction, float duration)
        {
            const float amplitude = 18f;
            const float cycles = 3f;
            var baseAngle = _dragger.Angle;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var lean = Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * 0.5f + 0.5f;
                _dragger.SnapTo(baseAngle + direction * amplitude * lean);
                yield return null;
            }
            _dragger.SnapTo(baseAngle);
        }

        // 3rd mistake: the piece animates through the correct rotation itself, then resets so the child repeats
        // the gesture (per the plan's own Demo note for this game).
        private IEnumerator RunDemonstrate()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("rotatepiece_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("rotatepiece_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var startAngle = _dragger.Angle;
            var direction = ShorterDirection(startAngle, _round.TargetAngle);
            var distance = RotateThePieceRoundGenerator.AngleDifference(startAngle, _round.TargetAngle);
            var duration = Mathf.Clamp(distance / DemoDegreesPerSecond, MinDemoSeconds, MaxDemoSeconds);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var angle = startAngle + direction * distance * Mathf.Clamp01(t / duration);
                _dragger.SnapTo(angle);
                yield return null;
            }
            _dragger.SnapTo(_round.TargetAngle);
            yield return new WaitForSeconds(HintRestSeconds);

            _dragger.SnapTo(_round.StartAngle);
            _dragger.Enabled = true;
        }

        private IEnumerator OnMatched()
        {
            _roundOver = true;
            _dragger.Enabled = false;
            _dragger.SnapTo(_round.TargetAngle);

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_pieceRect, _pieceImage, 1.2f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.RotateThePieceLevel = DifficultyLadder.RecordRound(_game.Progress.RotateThePieceBuffer, _game.Progress.RotateThePieceLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _pieceRect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= RotateThePieceRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

        private void SetSessionEnded(bool ended)
        {
            _field.gameObject.SetActive(!ended);
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

        private RectTransform CreateFullRectContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void AddPlaygroundBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/playground_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
