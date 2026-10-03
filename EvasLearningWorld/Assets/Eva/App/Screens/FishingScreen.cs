using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Fishing as a real arcade game (docs/kids-games/arcade-redesign.md): the child and Eva sit in a boat on a lake seen from the
    // side, fish of three sizes swim across in rows, a tap in the water sends the hook (on the line from the rod) there; a fish it touches
    // anywhere on its body is pulled to the boat and jumps in, scoring 1, 2 or 3 points by its size. Fish never stop, so the child aims a
    // little ahead; a hook that touches nothing comes back empty. One game is levels 1-6 in a row like the other Arcade games: each needs
    // more points and the fish swim faster, a short sound says "faster now", the fish still swimming when a level ends go on into the next,
    // it always starts at level 1 and ends after level 6 or when the child leaves. Nothing is ever lost. All pacing lives in
    // FishingDirector (Rules/Fishing.cs); this screen only draws it and feeds it the frame time and the taps.
    public sealed class FishingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: a tap sends the hook to that spot.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler
        {
            public System.Action<Vector2> Tapped;

            public void OnPointerDown(PointerEventData eventData)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local))
                    Tapped?.Invoke(local);
            }
        }

        private sealed class FishView
        {
            public RectTransform Rect;
            public Image Image;
            public SwimmingFish Model;
            public bool Jumping; // a landed fish still leaping into the boat
        }

        private const int PoolSize = 26;

        private static readonly Vector2 HookSize = new Vector2(60f, 120f); // the picture is 1:2, its line on top, the hook and worm below
        private const float LineWidth = 4f;
        private static readonly Vector2 RodTip = new Vector2(FishingDirector.RodTipX, FishingDirector.RodTipY);
        private static readonly Vector2 RodSize = new Vector2(330f, 161f); // the picture is 2.05:1, mirrored so its tip points at the water
        private static readonly Vector2 RodPosition = new Vector2(190f, 284f);

        private static readonly Vector2 BoatSize = new Vector2(420f, 109f);
        private static readonly Vector2 BoatPosition = new Vector2(330f, 190f); // the top of the boat sits at y 245, its front rim hides their feet
        private static readonly Vector2 BoatMouth = new Vector2(330f, 262f); // where a caught fish jumps to

        private const float JumpSeconds = 0.55f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the fish nearest the boat pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private RectTransform _hook;
        private RectTransform _line;
        private GameObject _endPanel;
        private ArcadeProgress _progress;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly List<FishView> _views = new List<FishView>();

        private System.Random _rng;
        private FishingDirector _director;
        private int _totalPoints;
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
            BuildField();
            _eva = AddCompanionPair(_game, CompanionLayout.Boat);
            _evaBaseScale = _eva.Root.localScale;
            AddBoatAndRod();
            _progress = ArcadeProgress.Create(Root, FishingDirector.MaxLevel);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Fishing);
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
            _totalPoints = 0;
            _paid = false;
            _director = null; // nothing carries over from a game before
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            ShowHook(null);
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                // The fish still swimming when a level ends go on into the next one, hook and all: no clearing, no pause.
                _director = new FishingDirector(level, _rng, _director?.Fish.ToArray(), _director?.Hook);
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == FishingDirector.MinLevel)
                {
                    HideAllFish();
                    ShowFishNow();
                    _active = false;
                    _eva.SetTalking(true);
                    yield return _game.Voice.SayAndWait("fishing_find");
                    _eva.SetTalking(false);
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                }

                _active = true;
                var spawned = new List<SwimmingFish>();
                var hooked = new List<SwimmingFish>();
                var landed = new List<SwimmingFish>();
                var gone = new List<SwimmingFish>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    spawned.Clear();
                    hooked.Clear();
                    landed.Clear();
                    gone.Clear();
                    _director.Tick(dt, spawned, hooked, landed, gone);
                    foreach (var fish in spawned) ShowFish(fish);
                    if (hooked.Count > 0) _game.Sfx.Pick();
                    foreach (var fish in landed) OnLanded(fish);
                    foreach (var fish in gone) ReleaseView(fish);
                    _idle += dt;
                    MaybeHelp();
                    MoveFish();
                    ShowHook(_director.Hook);
                    yield return null;
                }
            }
            _active = false;
            yield return EndGame();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the fish nearest the boat pulses.
        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("fishing_find");
            }
            else if (_idleStage == 1 && _idle >= PulseAfterSeconds)
            {
                _idleStage = 2;
                _pulse = true;
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
            }
        }

        // Any tap counts as the child playing: the idle clock and the pulse start over.
        private void ResetIdle()
        {
            _idle = 0f;
            _idleStage = 0;
            _pulse = false;
        }

        // --- Lake, hook and fish ------------------------------------------------------------------------------

        private void BuildField()
        {
            var go = new GameObject("Field", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _field = (RectTransform)go.transform;
            _field.anchorMin = Vector2.zero;
            _field.anchorMax = Vector2.one;
            _field.offsetMin = _field.offsetMax = Vector2.zero;

            var pad = new GameObject("Pad", typeof(RectTransform), typeof(Image), typeof(Pad));
            pad.transform.SetParent(_field, false);
            var padRect = (RectTransform)pad.transform;
            padRect.anchorMin = Vector2.zero;
            padRect.anchorMax = Vector2.one;
            padRect.offsetMin = padRect.offsetMax = Vector2.zero;
            var padImage = pad.GetComponent<Image>();
            padImage.color = new Color(1f, 1f, 1f, 0f);
            padImage.raycastTarget = true;
            pad.GetComponent<Pad>().Tapped = point =>
            {
                if (!_active || _director == null) return;
                ResetIdle();
                if (_director.Cast(point.x, point.y)) _game.Sfx.Drop();
            };

            for (var i = 0; i < PoolSize; i++)
            {
                var rect = NewPicture(_field, "Fish" + i, null, new Vector2(100f, 50f), Vector2.zero);
                var image = rect.GetComponent<Image>();
                image.raycastTarget = false;
                _views.Add(new FishView { Rect = rect, Image = image });
                rect.gameObject.SetActive(false);
            }

            // The line from the rod tip to the hook, and the hook.
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(_field, false);
            _line = (RectTransform)line.transform;
            _line.anchorMin = _line.anchorMax = _line.pivot = new Vector2(0.5f, 0.5f);
            var lineImage = line.GetComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.9f);
            lineImage.raycastTarget = false;

            _hook = NewPicture(_field, "Hook", null, HookSize, Vector2.zero);
            _hook.pivot = new Vector2(0.5f, 0.12f); // the point of the hook, near the bottom of its picture, is where the hook is
            var hookImage = _hook.GetComponent<Image>();
            hookImage.sprite = EvaUi.Sprite("fishing/hook");
            hookImage.raycastTarget = false;
            ShowHook(null);
        }

        // The boat in front of the two sitting in it, and the rod from the child's hands to the tip the line hangs from.
        private void AddBoatAndRod()
        {
            var boat = NewPicture(Root, "Boat", "fishing/boat", BoatSize, BoatPosition);
            boat.GetComponent<Image>().raycastTarget = false;

            var rod = NewPicture(Root, "Rod", "fishing/rod", RodSize, RodPosition);
            rod.localScale = new Vector3(-1f, 1f, 1f);
            rod.GetComponent<Image>().raycastTarget = false;
        }

        // The hook and its line show only while the hook is out of the rod.
        private void ShowHook(FishingHook hook)
        {
            if (_hook == null) return;
            var visible = hook != null && hook.State != HookState.Idle;
            _hook.gameObject.SetActive(visible);
            _line.gameObject.SetActive(visible);
            if (!visible) return;
            var point = new Vector2(hook.X, hook.Y);
            _hook.anchoredPosition = point;
            var top = point + new Vector2(0f, HookSize.y * 0.8f); // the line is tied to the top of the hook picture
            var along = top - RodTip;
            _line.anchoredPosition = (top + RodTip) * 0.5f;
            _line.sizeDelta = new Vector2(LineWidth, along.magnitude);
            _line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg - 90f);
        }

        private void ShowFishNow()
        {
            foreach (var fish in _director.Fish) ShowFish(fish);
        }

        private void ShowFish(SwimmingFish fish)
        {
            foreach (var view in _views)
            {
                if (view.Model != null || view.Jumping) continue;
                view.Model = fish;
                view.Image.sprite = EvaUi.Sprite("fishing/fish_" + (char)('a' + fish.Look));
                view.Image.color = Color.white;
                view.Rect.sizeDelta = new Vector2(fish.Width, fish.Height);
                view.Rect.gameObject.SetActive(true);
                Place(view);
                return;
            }
        }

        private void OnLanded(SwimmingFish fish)
        {
            _totalPoints += fish.Points;
            var view = ViewOf(fish);
            if (view != null)
            {
                view.Model = null;
                _runner.StartCoroutine(JumpIntoBoat(view));
            }
            _game.Sfx.Pop();
            _progress.Show(_director.Level, Mathf.Min(1f, _director.Points / (float)FishingDirector.PointsToPass(_director.Level)));
        }

        private FishView ViewOf(SwimmingFish fish)
        {
            foreach (var view in _views) if (view.Model == fish) return view;
            return null;
        }

        private void ReleaseView(SwimmingFish fish)
        {
            var view = ViewOf(fish);
            if (view == null) return;
            view.Model = null;
            view.Rect.gameObject.SetActive(false);
        }

        private void HideAllFish()
        {
            foreach (var view in _views)
            {
                view.Model = null;
                view.Jumping = false;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void MoveFish()
        {
            var nearest = _pulse ? NearestToBoat() : null;
            foreach (var view in _views) if (view.Model != null) Place(view, view == nearest);
        }

        private FishView NearestToBoat()
        {
            FishView best = null;
            var bestDistance = float.MaxValue;
            foreach (var view in _views)
            {
                if (view.Model == null || view.Model.Hooked) continue;
                var distance = Vector2.Distance(new Vector2(view.Model.X, view.Model.Y), RodTip);
                if (distance < bestDistance) { best = view; bestDistance = distance; }
            }
            return best;
        }

        // Swims along its row facing the way it goes with a little wobble; once hooked it trails behind the hook, mouth first.
        private void Place(FishView view, bool pulse = false)
        {
            var fish = view.Model;
            if (fish.Hooked)
            {
                var toRod = (RodTip - new Vector2(fish.X, fish.Y)).normalized;
                var angle = Mathf.Atan2(toRod.y, toRod.x) * Mathf.Rad2Deg;
                view.Rect.anchoredPosition = new Vector2(fish.X, fish.Y) - toRod * (fish.Width * 0.4f);
                view.Rect.localScale = new Vector3(fish.Dir, 1f, 1f);
                view.Rect.localRotation = Quaternion.Euler(0f, 0f, fish.Dir > 0f ? angle : angle - 180f);
                return;
            }
            var bob = Mathf.Sin(fish.Age * 2f + fish.Lane) * 5f;
            view.Rect.anchoredPosition = new Vector2(fish.X, fish.Y + bob);
            var scale = pulse ? 1f + 0.1f * Mathf.Sin(Time.time * 9f) : 1f;
            view.Rect.localScale = new Vector3(fish.Dir * scale, scale, 1f);
            view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fish.Age * 2.4f + fish.Lane) * 3f);
        }

        // A landed fish leaps from the rod tip into the boat in an arc, shrinking, and is gone.
        private IEnumerator JumpIntoBoat(FishView view)
        {
            view.Jumping = true;
            var start = view.Rect.anchoredPosition;
            var scale = view.Rect.localScale;
            for (var t = 0f; t < JumpSeconds; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / JumpSeconds);
                var arc = 4f * k * (1f - k) * 160f;
                view.Rect.anchoredPosition = Vector2.Lerp(start, BoatMouth, k) + new Vector2(0f, arc);
                view.Rect.localScale = scale * Mathf.Lerp(1f, 0.35f, k);
                view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 6.28f) * 25f);
                view.Image.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                yield return null;
            }
            view.Rect.localScale = Vector3.one;
            view.Rect.localRotation = Quaternion.identity;
            view.Rect.gameObject.SetActive(false);
            view.Jumping = false;
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

        // --- End of the game, the one coin, Eva ---------------------------------------------------------------

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        // Leaving early still pays the coin if the child caught anything (no animation: the screen is going away). Whether it should is
        // decided later with the rest of the coin economy.
        private void PayIfPlayed()
        {
            if (_paid || _totalPoints == 0) return;
            _paid = true;
            _game.Progress.AddCoins(FishingDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(FishingDirector.SessionCoins, _progress.Root.transform.position));
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
            _progress.Root.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // The underwater scene (fishing/bg), scaled to cover the whole screen: its horizon is at 28% from the top.
        private void AddBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);

            var scene = new GameObject("Scene", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            scene.transform.SetParent(background.transform, false);
            var sceneRect = (RectTransform)scene.transform;
            sceneRect.anchorMin = Vector2.zero;
            sceneRect.anchorMax = Vector2.one;
            sceneRect.offsetMin = sceneRect.offsetMax = Vector2.zero;
            var image = scene.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("fishing/bg");
            image.raycastTarget = false;
            var fitter = scene.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 900f;
        }
    }
}
