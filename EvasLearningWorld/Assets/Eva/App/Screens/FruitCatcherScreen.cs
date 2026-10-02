using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Fruit Catcher as a real arcade game (docs/kids-games/arcade-redesign.md): fruit falls from the top, the child touches or
    // slides a finger anywhere and the basket follows it left and right to catch the fruit. One game is levels 1-6 in a row like the
    // other Arcade games: each needs more catches and the fruit falls faster, a short sound says "faster now", the fruit still falling
    // when a level ends goes on into the next, it always starts at level 1 and ends after level 6 or when the child leaves. Nothing is
    // ever lost: a fruit that is not caught just falls away. All pacing lives in FruitCatcherDirector (Rules/FruitCatcher.cs); this
    // screen only draws it and feeds it the frame time and the basket position.
    public sealed class FruitCatcherScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: wherever the finger is, the basket goes there.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            public System.Action<float> Moved;

            public void OnPointerDown(PointerEventData eventData) => Report(eventData);
            public void OnDrag(PointerEventData eventData) => Report(eventData);

            private void Report(PointerEventData eventData)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local))
                    Moved?.Invoke(local.x);
            }
        }

        private sealed class FruitView
        {
            public RectTransform Rect;
            public Image Image;
            public FallingFruit Model;
            public bool Dropping; // a caught fruit still sinking into the basket
        }

        private static readonly string[] FruitSprites =
            { "arcade/fruit_apple", "arcade/fruit_banana", "arcade/fruit_grape", "arcade/fruit_orange", "arcade/fruit_pear", "arcade/fruit_plum" };

        // Fruit falls from above the top to below the basket; it is caught when it passes the basket's mouth (CatchAt of the way).
        private const float FruitSize = 170f;
        private const float StartY = 500f;
        private const float EndY = -440f;
        private const int PoolSize = 18;

        private const float BasketSize = 280f;
        private const float BasketY = -330f;
        private const float BasketLimit = 580f; // the basket's middle never goes closer than this to the side edge
        private const float BasketSpeed = 2600f; // units per second, so it glides to the finger instead of jumping

        private const float CatchPopSeconds = 0.35f;
        private const float DropSeconds = 0.26f; // a caught fruit sinks into the basket this long
        private const float BumpSeconds = 0.22f; // and the basket gives a little

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the basket pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private RectTransform _basket;
        private GameObject _endPanel;
        private ArcadeProgress _progress;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly List<FruitView> _views = new List<FruitView>();

        private System.Random _rng;
        private FruitCatcherDirector _director;
        private float _basketX;
        private float _targetX;
        private int _totalHits;
        private bool _paid;
        private bool _active;
        private float _idle;
        private int _idleStage;
        private bool _pulse;
        private float _bump;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
            BuildField();
            _progress = ArcadeProgress.Create(Root, FruitCatcherDirector.MaxLevel); // after the field so the fruit falls behind the bar
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.FruitCatcher);
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
            _basketX = _targetX = 0f;
            PlaceBasket();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = FruitCatcherDirector.MinLevel; level <= FruitCatcherDirector.MaxLevel; level++)
            {
                // The fruit still falling when a level ends goes on into the next one: no clearing, no pause.
                _director = new FruitCatcherDirector(level, _rng, _director?.Up.ToArray());
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == FruitCatcherDirector.MinLevel)
                {
                    HideAllFruit();
                    _active = false;
                    _eva.SetTalking(true);
                    yield return _game.Voice.SayAndWait("fruitcatch_find");
                    _eva.SetTalking(false);
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                }

                _active = true;
                var spawned = new List<FallingFruit>();
                var caught = new List<FallingFruit>();
                var gone = new List<FallingFruit>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    MoveBasket(dt);
                    spawned.Clear();
                    caught.Clear();
                    gone.Clear();
                    _director.Tick(dt, _basketX, spawned, caught, gone);
                    foreach (var fruit in spawned) ShowFruit(fruit);
                    foreach (var fruit in caught) OnCaught(fruit);
                    foreach (var fruit in gone) ReleaseView(fruit);
                    _idle += dt;
                    MaybeHelp();
                    MoveFruit();
                    yield return null;
                }
            }
            _active = false;
            yield return EndGame();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the basket pulses.
        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("fruitcatch_find");
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

        // --- Basket and fruit --------------------------------------------------------------------------------

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
            pad.GetComponent<Pad>().Moved = x =>
            {
                _targetX = Mathf.Clamp(x, -BasketLimit, BasketLimit);
                if (_active) ResetIdle();
            };

            _basket = NewPicture(_field, "Basket", "arcade/prop_basket", new Vector2(BasketSize, BasketSize), new Vector2(0f, BasketY));
            _basket.GetComponent<Image>().raycastTarget = false;

            // The fruit is drawn in front of the basket, so a caught one is seen dropping into it instead of vanishing behind it.
            for (var i = 0; i < PoolSize; i++)
            {
                var rect = NewPicture(_field, "Fruit" + i, null, new Vector2(FruitSize, FruitSize), Vector2.zero);
                var image = rect.GetComponent<Image>();
                image.raycastTarget = false;
                _views.Add(new FruitView { Rect = rect, Image = image });
                rect.gameObject.SetActive(false);
            }

        }

        private void MoveBasket(float dt)
        {
            _basketX = Mathf.MoveTowards(_basketX, _targetX, BasketSpeed * dt);
            _bump = Mathf.Max(0f, _bump - dt);
            PlaceBasket();
        }

        private void PlaceBasket()
        {
            if (_basket == null) return;
            _basket.anchoredPosition = new Vector2(_basketX, BasketY);
            var pulse = _pulse ? 1f + 0.08f * Mathf.Sin(Time.time * 9f) : 1f;
            var bump = Mathf.Clamp01(_bump / BumpSeconds);
            _basket.localScale = new Vector3(1f + 0.06f * bump, 1f - 0.1f * bump, 1f) * pulse;
        }

        private void ShowFruit(FallingFruit fruit)
        {
            foreach (var view in _views)
            {
                if (view.Model != null || view.Dropping) continue;
                view.Model = fruit;
                view.Image.sprite = EvaUi.Sprite(FruitSprites[fruit.Look]);
                view.Image.color = Color.white;
                view.Rect.gameObject.SetActive(true);
                Place(view);
                return;
            }
        }

        private void OnCaught(FallingFruit fruit)
        {
            _totalHits++;
            var view = ViewOf(fruit);
            var from = view != null ? view.Rect.position : _basket.position;
            if (view != null)
            {
                view.Model = null;
                _runner.StartCoroutine(SinkIntoBasket(view));
            }
            _bump = BumpSeconds;
            _game.Sfx.Pop();
            _runner.StartCoroutine(CatchPop(from));
            _progress.Show(_director.Level, _director.Hits / (float)FruitCatcherDirector.HitsToPass(_director.Level));
        }

        private FruitView ViewOf(FallingFruit fruit)
        {
            foreach (var view in _views) if (view.Model == fruit) return view;
            return null;
        }

        private void ReleaseView(FallingFruit fruit)
        {
            var view = ViewOf(fruit);
            if (view == null) return;
            view.Model = null;
            view.Rect.gameObject.SetActive(false);
        }

        private void HideAllFruit()
        {
            foreach (var view in _views)
            {
                view.Model = null;
                view.Dropping = false;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void MoveFruit()
        {
            foreach (var view in _views) if (view.Model != null) Place(view);
        }

        // Falls with a little tumble; fades out as it drops past the basket.
        private static void Place(FruitView view)
        {
            var fruit = view.Model;
            var progress = Mathf.Clamp01(fruit.Progress);
            view.Rect.anchoredPosition = new Vector2(fruit.X, Mathf.Lerp(StartY, EndY, progress));
            view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fruit.Age * 3f + fruit.X) * 12f);
            var alpha = progress < FruitCatcherDirector.CatchAt ? 1f : 1f - (progress - FruitCatcherDirector.CatchAt) / (1f - FruitCatcherDirector.CatchAt);
            view.Image.color = new Color(1f, 1f, 1f, alpha);
        }

        // A caught fruit sinks into the basket's mouth, shrinking and fading, while it is still drawn in front of the basket.
        private IEnumerator SinkIntoBasket(FruitView view)
        {
            view.Dropping = true;
            var start = view.Rect.anchoredPosition;
            var rotation = view.Rect.localRotation;
            for (var t = 0f; t < DropSeconds; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / DropSeconds);
                var eased = k * k;
                view.Rect.anchoredPosition = new Vector2(Mathf.Lerp(start.x, _basketX, eased), Mathf.Lerp(start.y, BasketY + 40f, eased));
                view.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, k);
                view.Rect.localRotation = Quaternion.Slerp(rotation, Quaternion.identity, k);
                view.Image.color = new Color(1f, 1f, 1f, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
                yield return null;
            }
            view.Rect.localScale = Vector3.one;
            view.Rect.gameObject.SetActive(false);
            view.Dropping = false;
        }

        // A few sparkles at the basket's mouth when something is caught.
        private IEnumerator CatchPop(Vector3 worldPosition)
        {
            const int stars = 4;
            var pieces = new RectTransform[stars];
            var directions = new Vector2[stars];
            for (var i = 0; i < stars; i++)
            {
                var piece = NewPicture(_field, "Spark", "arcade/prop_sparkle", new Vector2(56f, 56f), Vector2.zero);
                piece.GetComponent<Image>().raycastTarget = false;
                piece.position = worldPosition;
                pieces[i] = piece;
                var angle = (45f + i * 90f) * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 200f;
            }
            for (var t = 0f; t < CatchPopSeconds; t += Time.deltaTime)
            {
                var k = t / CatchPopSeconds;
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
            _game.Progress.AddCoins(FruitCatcherDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(FruitCatcherDirector.SessionCoins, _progress.Root.transform.position));
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
            image.sprite = EvaUi.Sprite("world/arcade_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
