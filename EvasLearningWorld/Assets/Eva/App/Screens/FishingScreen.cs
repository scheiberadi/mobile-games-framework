using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Fishing as a real arcade game (docs/kids-games/arcade-redesign.md): fish swim to and fro in a pond, the child puts a
    // finger down and drags a hook through the water; a fish the hook touches is caught and jumps out of the water. One game is levels
    // 1-6 in a row like the other Arcade games: each needs more fish and they swim faster and stay shorter, a short sound says "faster
    // now", the fish still swimming when a level ends go on into the next, it always starts at level 1 and ends after level 6 or when
    // the child leaves. Nothing is ever lost: a fish that is not caught just dives away. All pacing lives in FishingDirector
    // (Rules/Fishing.cs); this screen only draws it and feeds it the frame time and the hook.
    public sealed class FishingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: the hook hangs under the finger while it is down.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public System.Action<Vector2> Moved;
            public System.Action Released;

            public void OnPointerDown(PointerEventData eventData) => Report(eventData);
            public void OnDrag(PointerEventData eventData) => Report(eventData);
            public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();

            private void Report(PointerEventData eventData)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local))
                    Moved?.Invoke(local);
            }
        }

        private sealed class FishView
        {
            public RectTransform Rect;
            public Image Image;
            public SwimmingFish Model;
            public bool Jumping; // a caught fish still leaping out of the water
        }

        private static readonly string[] FishSprites =
            { "arcade/fish_red", "arcade/fish_orange", "arcade/fish_yellow", "arcade/fish_green", "arcade/fish_blue", "arcade/fish_purple" };

        private const float FishSize = 200f;
        private const int PoolSize = 12;
        private const float SurfaceSeconds = 0.4f; // a fish grows into view this long, and shrinks away the same way before it dives
        private const float DiveSeconds = 0.5f;

        private const float HookSize = 240f;
        private const float HookTipAboveFinger = 70f; // the hook hangs a little above the finger so the finger never hides it
        private const float HookLingerSeconds = 0.25f; // a quick tap still counts: the hook stays in the water this long
        private const float LineTop = 480f;
        private static readonly Vector2 PondSize = new Vector2(1300f, 777f);
        private static readonly Vector2 PondPosition = new Vector2(0f, -20f);

        private const float JumpSeconds = 0.5f;
        private const float SplashSeconds = 0.35f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then a fish pulses.
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
        private bool _touching;
        private float _lingerLeft;
        private Vector2 _hookPoint; // where the hook's tip is
        private int _totalHits;
        private bool _paid;
        private bool _active;
        private float _idle;
        private int _idleStage;
        private bool _pulse;

        private bool HookInWater => _touching || _lingerLeft > 0f;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
            BuildField();
            _progress = ArcadeProgress.Create(Root, FishingDirector.MaxLevel); // after the field so the fish swim behind the bar
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
            _totalHits = 0;
            _paid = false;
            _director = null; // nothing carries over from a game before
            _touching = false;
            _lingerLeft = 0f;
            ShowHook();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                // The fish still swimming when a level ends go on into the next one: no clearing, no pause.
                _director = new FishingDirector(level, _rng, _director?.Up.ToArray());
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == FishingDirector.MinLevel)
                {
                    HideAllFish();
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
                var caught = new List<SwimmingFish>();
                var gone = new List<SwimmingFish>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    if (!_touching && _lingerLeft > 0f) _lingerLeft -= dt;
                    spawned.Clear();
                    caught.Clear();
                    gone.Clear();
                    _director.Tick(dt, _active && HookInWater, _hookPoint.x, _hookPoint.y, spawned, caught, gone);
                    foreach (var fish in spawned) ShowFish(fish);
                    foreach (var fish in caught) OnCaught(fish);
                    foreach (var fish in gone) ReleaseView(fish);
                    _idle += dt;
                    MaybeHelp();
                    MoveFish();
                    ShowHook();
                    yield return null;
                }
            }
            _active = false;
            yield return EndGame();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, a fish pulses.
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

        // Any touch counts as the child playing: the idle clock and the pulse start over.
        private void ResetIdle()
        {
            _idle = 0f;
            _idleStage = 0;
            _pulse = false;
        }

        // --- Pond, hook and fish ------------------------------------------------------------------------------

        private void BuildField()
        {
            var go = new GameObject("Field", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _field = (RectTransform)go.transform;
            _field.anchorMin = Vector2.zero;
            _field.anchorMax = Vector2.one;
            _field.offsetMin = _field.offsetMax = Vector2.zero;

            var pond = NewPicture(_field, "Pond", "world/pond", PondSize, PondPosition);
            pond.GetComponent<Image>().raycastTarget = false;

            var pad = new GameObject("Pad", typeof(RectTransform), typeof(Image), typeof(Pad));
            pad.transform.SetParent(_field, false);
            var padRect = (RectTransform)pad.transform;
            padRect.anchorMin = Vector2.zero;
            padRect.anchorMax = Vector2.one;
            padRect.offsetMin = padRect.offsetMax = Vector2.zero;
            var padImage = pad.GetComponent<Image>();
            padImage.color = new Color(1f, 1f, 1f, 0f);
            padImage.raycastTarget = true;
            var padBehaviour = pad.GetComponent<Pad>();
            padBehaviour.Moved = finger =>
            {
                _touching = true;
                _lingerLeft = HookLingerSeconds;
                _hookPoint = finger + new Vector2(0f, HookTipAboveFinger);
                if (_active) ResetIdle();
            };
            padBehaviour.Released = () => _touching = false;

            for (var i = 0; i < PoolSize; i++)
            {
                var rect = NewPicture(_field, "Fish" + i, null, new Vector2(FishSize, FishSize), Vector2.zero);
                var image = rect.GetComponent<Image>();
                image.raycastTarget = false;
                _views.Add(new FishView { Rect = rect, Image = image });
                rect.gameObject.SetActive(false);
            }

            // The hook is drawn over the fish: a thin line from above the screen down to the hook.
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(_field, false);
            _line = (RectTransform)line.transform;
            _line.anchorMin = _line.anchorMax = new Vector2(0.5f, 0.5f);
            _line.pivot = new Vector2(0.5f, 0f);
            var lineImage = line.GetComponent<Image>();
            lineImage.color = new Color(0.55f, 0.55f, 0.58f, 1f);
            lineImage.raycastTarget = false;

            _hook = NewPicture(_field, "Hook", "arcade/prop_hook", new Vector2(HookSize, HookSize), Vector2.zero);
            _hook.pivot = new Vector2(0.5f, 0f);
            _hook.GetComponent<Image>().raycastTarget = false;
            ShowHook();
        }

        private void ShowHook()
        {
            if (_hook == null) return;
            var visible = _active && HookInWater;
            _hook.gameObject.SetActive(visible);
            _line.gameObject.SetActive(visible);
            if (!visible) return;
            _hook.anchoredPosition = _hookPoint - new Vector2(0f, 12f);
            var top = _hookPoint.y + HookSize * 0.85f;
            _line.anchoredPosition = new Vector2(_hookPoint.x, top);
            _line.sizeDelta = new Vector2(4f, Mathf.Max(0f, LineTop - top));
        }

        private void ShowFish(SwimmingFish fish)
        {
            foreach (var view in _views)
            {
                if (view.Model != null || view.Jumping) continue;
                view.Model = fish;
                view.Image.sprite = EvaUi.Sprite(FishSprites[fish.Look]);
                view.Image.color = Color.white;
                view.Rect.gameObject.SetActive(true);
                Place(view);
                return;
            }
        }

        private void OnCaught(SwimmingFish fish)
        {
            _totalHits++;
            var view = ViewOf(fish);
            var from = view != null ? view.Rect.position : _field.position;
            if (view != null)
            {
                view.Model = null;
                _runner.StartCoroutine(JumpOut(view));
            }
            _game.Sfx.Pop();
            _runner.StartCoroutine(Splash(from));
            _progress.Show(_director.Level, _director.Hits / (float)FishingDirector.HitsToPass(_director.Level));
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
            foreach (var view in _views) if (view.Model != null) Place(view);
        }

        // Swims facing the way it goes; grows into view when it surfaces and shrinks away before it dives.
        private void Place(FishView view)
        {
            var fish = view.Model;
            view.Rect.anchoredPosition = new Vector2(fish.X, fish.Y);
            var life = fish.LifeSeconds;
            var grow = Mathf.Clamp01(fish.Age / SurfaceSeconds);
            var shrink = Mathf.Clamp01((life - fish.Age) / DiveSeconds);
            var size = Mathf.Min(grow, shrink);
            var pulse = _pulse && view == OldestFish() ? 1f + 0.1f * Mathf.Sin(Time.time * 9f) : 1f;
            view.Rect.localScale = new Vector3(fish.Dir >= 0f ? size : -size, size, 1f) * pulse;
            view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fish.Age * 2.2f + fish.Phase) * 6f);
        }

        // The fish that pulses as help: the one that has been in the pond longest.
        private FishView OldestFish()
        {
            FishView best = null;
            foreach (var view in _views)
                if (view.Model != null && (best == null || view.Model.Age > best.Model.Age)) best = view;
            return best;
        }

        // A caught fish leaps out of the pond in an arc, spinning a little, and fades.
        private IEnumerator JumpOut(FishView view)
        {
            view.Jumping = true;
            var start = view.Rect.anchoredPosition;
            var scale = view.Rect.localScale;
            var side = scale.x >= 0f ? 1f : -1f;
            for (var t = 0f; t < JumpSeconds; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / JumpSeconds);
                var height = 4f * k * (1f - k) * 300f;
                view.Rect.anchoredPosition = start + new Vector2(side * 140f * k, height);
                view.Rect.localScale = new Vector3(side, 1f, 1f) * Mathf.Lerp(1f, 1.25f, 4f * k * (1f - k));
                view.Rect.localRotation = Quaternion.Euler(0f, 0f, -side * 360f * k);
                view.Image.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                yield return null;
            }
            view.Rect.localScale = Vector3.one;
            view.Rect.localRotation = Quaternion.identity;
            view.Rect.gameObject.SetActive(false);
            view.Jumping = false;
        }

        // A few sparkles where the fish was when it is caught.
        private IEnumerator Splash(Vector3 worldPosition)
        {
            const int stars = 5;
            var pieces = new RectTransform[stars];
            var directions = new Vector2[stars];
            for (var i = 0; i < stars; i++)
            {
                var piece = NewPicture(_field, "Splash", "arcade/prop_sparkle", new Vector2(56f, 56f), Vector2.zero);
                piece.GetComponent<Image>().raycastTarget = false;
                piece.position = worldPosition;
                pieces[i] = piece;
                var angle = (20f + i * 72f) * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 220f;
            }
            for (var t = 0f; t < SplashSeconds; t += Time.deltaTime)
            {
                var k = t / SplashSeconds;
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

        // Leaving early still pays the coin if the child caught anything (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _totalHits == 0) return;
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
            image.sprite = EvaUi.Sprite("world/map_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
