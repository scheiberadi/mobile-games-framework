using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Space Shooter as a calm real arcade game (docs/kids-games/arcade-redesign.md): asteroids drift down the night sky, the child
    // slides a finger and the rocket follows it left and right and fires stars by itself; a shape a star touches pops. One game is
    // levels 1-6 in a row like the other Arcade games: each needs more pops and the shapes come faster, a short sound says "faster
    // now", what is still on screen when a level ends goes on into the next, it always starts at level 1 and ends after level 6 or when
    // the child leaves. Nothing is ever lost: a shape that is not hit just drifts away. All pacing lives in SpaceShooterDirector
    // (Rules/SpaceShooter.cs); this screen only draws it and feeds it the frame time and the ship position. The night sky is drawn here.
    public sealed class SpaceShooterScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: wherever the finger is, the rocket goes there.
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

        private sealed class ShapeView
        {
            public RectTransform Rect;
            public Image Image;
            public SpaceShape Model;
            public bool Popping; // a popped shape still bursting
        }

        private sealed class ShotView
        {
            public RectTransform Rect;
            public SpaceShot Model;
        }

        private sealed class Twinkle
        {
            public RectTransform Rect;
            public Image Image;
            public float Speed;
            public float Phase;
            public float Size;
        }

        private static readonly string[] ShapeSprites =
            { "arcade/asteroid_a", "arcade/asteroid_b", "arcade/asteroid_c", "arcade/asteroid_d", "arcade/asteroid_e", "arcade/asteroid_f" };

        private const float ShapeSize = 190f;
        private const float ShotSize = 80f;
        private const float ShipSize = 230f;
        private const int ShapePool = 14;
        private const int ShotPool = 14;

        private const float ShipLimit = 580f; // the rocket's middle never goes closer than this to the side edge
        private const float ShipSpeed = 2600f; // units per second, so it glides to the finger instead of jumping

        private const float BurstSeconds = 0.4f;

        // The child and Eva watch from the bottom right in space suits (still pictures, feet on the same line).
        private const float AstronautFeetY = -440f;
        private const float PlayerHeight = 215f;
        private const float PlayerAspect = 0.46f;
        private const float PlayerX = 440f;
        private const float EvaHeight = 200f;
        private const float EvaAspect = 0.83f;
        private const float EvaX = 585f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the rocket pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const int StarCount = 40;
        private const float StarDrift = 14f; // units per second the stars of the sky slip down

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _player;
        private RectTransform _evaSuit;
        private bool _evaTalking;
        private bool _cheering;
        private RectTransform _field;
        private RectTransform _ship;
        private GameObject _endPanel;
        private ArcadeProgress _progress;

        private readonly List<ShapeView> _shapeViews = new List<ShapeView>();
        private readonly List<ShotView> _shotViews = new List<ShotView>();
        private readonly List<Twinkle> _sky = new List<Twinkle>();

        private System.Random _rng;
        private SpaceShooterDirector _director;
        private float _shipX;
        private float _targetX;
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

            AddSky();
            BuildAstronauts();
            BuildField();
            _progress = ArcadeProgress.Create(Root, SpaceShooterDirector.MaxLevel); // after the field so the shapes drift behind the bar
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.SpaceShooter);
            _runner.StartCoroutine(AnimateAstronauts()); // dies with the screen when it is hidden, so it starts again here each time
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
            _shipX = _targetX = 0f;
            PlaceShip();
            _evaTalking = false;
            _cheering = false;
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            for (var level = SpaceShooterDirector.MinLevel; level <= SpaceShooterDirector.MaxLevel; level++)
            {
                // What is still on screen when a level ends goes on into the next one: no clearing, no pause.
                _director = new SpaceShooterDirector(level, _rng, _director?.Up.ToArray(), _director?.Shots.ToArray());
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == SpaceShooterDirector.MinLevel)
                {
                    HideAll();
                    _active = false;
                    _evaTalking = true;
                    yield return _game.Voice.SayAndWait("spaceshooter_find");
                    _evaTalking = false;
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                    Haptics.Win();
                }

                _active = true;
                var fired = new List<SpaceShot>();
                var spawned = new List<SpaceShape>();
                var popped = new List<SpaceShape>();
                var gone = new List<SpaceShape>();
                var spent = new List<SpaceShot>();
                while (!_director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    MoveShip(dt);
                    fired.Clear();
                    spawned.Clear();
                    popped.Clear();
                    gone.Clear();
                    spent.Clear();
                    _director.Tick(dt, _shipX, fired, spawned, popped, gone, spent);
                    foreach (var shot in fired) ShowShot(shot);
                    foreach (var shape in spawned) ShowShape(shape);
                    foreach (var shape in popped) OnPopped(shape);
                    foreach (var shape in gone) ReleaseShape(shape);
                    foreach (var shot in spent) ReleaseShot(shot);
                    _idle += dt;
                    MaybeHelp();
                    MoveThings(dt);
                    yield return null;
                }
            }
            _active = false;
            yield return EndGame();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the rocket pulses.
        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("spaceshooter_find");
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

        // --- The two astronauts ------------------------------------------------------------------------------

        private void BuildAstronauts()
        {
            _player = AddAstronaut("Player", "arcade/astronaut_player", PlayerX, PlayerHeight, PlayerAspect);
            _evaSuit = AddAstronaut("Eva", "arcade/astronaut_eva", EvaX, EvaHeight, EvaAspect);
        }

        private RectTransform AddAstronaut(string name, string sprite, float x, float height, float aspect)
        {
            var rect = NewPicture(Root, name + "Astronaut", sprite, new Vector2(height * aspect, height), new Vector2(x, AstronautFeetY + height * 0.5f));
            rect.pivot = new Vector2(0.5f, 0f); // hops and squashes stand on the feet
            rect.anchoredPosition = new Vector2(x, AstronautFeetY);
            rect.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        // Eva bobs while she speaks; both hop when the game is won.
        private IEnumerator AnimateAstronauts()
        {
            while (true)
            {
                var t = Time.time;
                Hop(_evaSuit, EvaX, _evaTalking ? Mathf.Abs(Mathf.Sin(t * 9f)) * 14f : (_cheering ? Mathf.Abs(Mathf.Sin(t * 6f)) * 40f : 0f));
                Hop(_player, PlayerX, _cheering ? Mathf.Abs(Mathf.Sin(t * 6f + 1f)) * 40f : 0f);
                yield return null;
            }
        }

        private static void Hop(RectTransform rect, float x, float lift) => rect.anchoredPosition = new Vector2(x, AstronautFeetY + lift);

        // --- Ship, shapes and shots ---------------------------------------------------------------------------

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
                _targetX = Mathf.Clamp(x, -ShipLimit, ShipLimit);
                if (_active) ResetIdle();
            };

            for (var i = 0; i < ShotPool; i++)
            {
                var rect = NewPicture(_field, "Shot" + i, "sciencelab/space_star", new Vector2(ShotSize, ShotSize), Vector2.zero);
                rect.GetComponent<Image>().raycastTarget = false;
                _shotViews.Add(new ShotView { Rect = rect });
                rect.gameObject.SetActive(false);
            }

            _ship = NewPicture(_field, "Ship", "arcade/ship_star", new Vector2(ShipSize, ShipSize), new Vector2(0f, SpaceShooterDirector.ShipY));
            _ship.GetComponent<Image>().raycastTarget = false;

            for (var i = 0; i < ShapePool; i++)
            {
                var rect = NewPicture(_field, "Shape" + i, null, new Vector2(ShapeSize, ShapeSize), Vector2.zero);
                var image = rect.GetComponent<Image>();
                image.raycastTarget = false;
                _shapeViews.Add(new ShapeView { Rect = rect, Image = image });
                rect.gameObject.SetActive(false);
            }
        }

        private void MoveShip(float dt)
        {
            _shipX = Mathf.MoveTowards(_shipX, _targetX, ShipSpeed * dt);
            PlaceShip();
        }

        private void PlaceShip()
        {
            if (_ship == null) return;
            var bob = Mathf.Sin(Time.time * 4f) * 6f;
            var pulse = _pulse ? 1f + 0.08f * Mathf.Sin(Time.time * 9f) : 1f;
            _ship.anchoredPosition = new Vector2(_shipX, SpaceShooterDirector.ShipY + bob);
            _ship.localScale = Vector3.one * pulse;
            // It leans a little into the direction it is gliding.
            _ship.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp((_shipX - _targetX) * 0.04f, -12f, 12f));
        }

        private void ShowShape(SpaceShape shape)
        {
            foreach (var view in _shapeViews)
            {
                if (view.Model != null || view.Popping) continue;
                view.Model = shape;
                view.Image.sprite = EvaUi.Sprite(ShapeSprites[shape.Look]);
                view.Image.color = Color.white;
                view.Rect.gameObject.SetActive(true);
                Place(view);
                return;
            }
        }

        private void ShowShot(SpaceShot shot)
        {
            foreach (var view in _shotViews)
            {
                if (view.Model != null) continue;
                view.Model = shot;
                view.Rect.gameObject.SetActive(true);
                view.Rect.anchoredPosition = new Vector2(shot.X, shot.Y);
                return;
            }
        }

        private void OnPopped(SpaceShape shape)
        {
            _totalHits++;
            var view = ViewOf(shape);
            var from = view != null ? view.Rect.position : _field.position;
            if (view != null)
            {
                view.Model = null;
                _runner.StartCoroutine(Burst(view, from));
            }
            _game.Sfx.Pop();
            Haptics.Tap();
            _progress.Show(_director.Level, _director.Hits / (float)SpaceShooterDirector.HitsToPass(_director.Level));
        }

        private ShapeView ViewOf(SpaceShape shape)
        {
            foreach (var view in _shapeViews) if (view.Model == shape) return view;
            return null;
        }

        private void ReleaseShape(SpaceShape shape)
        {
            var view = ViewOf(shape);
            if (view == null) return;
            view.Model = null;
            view.Rect.gameObject.SetActive(false);
        }

        private void ReleaseShot(SpaceShot shot)
        {
            foreach (var view in _shotViews)
            {
                if (view.Model != shot) continue;
                view.Model = null;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void HideAll()
        {
            foreach (var view in _shapeViews)
            {
                view.Model = null;
                view.Popping = false;
                view.Rect.gameObject.SetActive(false);
            }
            foreach (var view in _shotViews)
            {
                view.Model = null;
                view.Rect.gameObject.SetActive(false);
            }
        }

        private void MoveThings(float dt)
        {
            foreach (var view in _shapeViews) if (view.Model != null) Place(view);
            foreach (var view in _shotViews) if (view.Model != null) view.Rect.anchoredPosition = new Vector2(view.Model.X, view.Model.Y);
            DriftSky(dt);
        }

        // Drifts down with a slow spin; fades out as it passes the bottom of the screen.
        private static void Place(ShapeView view)
        {
            var shape = view.Model;
            view.Rect.anchoredPosition = new Vector2(shape.X, shape.Y);
            view.Rect.localRotation = Quaternion.Euler(0f, 0f, shape.Age * (shape.Look % 2 == 0 ? 20f : -20f) + shape.Phase * 57f);
            var alpha = Mathf.Clamp01((1f - shape.Progress) / 0.1f);
            view.Image.color = new Color(1f, 1f, 1f, alpha);
        }

        // A popped shape swells and fades while a burst of confetti flies out of it.
        private IEnumerator Burst(ShapeView view, Vector3 worldPosition)
        {
            view.Popping = true;
            var confetti = NewPicture(_field, "Burst", "arcade/prop_pop", new Vector2(300f, 300f), Vector2.zero);
            confetti.GetComponent<Image>().raycastTarget = false;
            confetti.position = worldPosition;
            var start = view.Rect.localScale;
            for (var t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                var k = t / BurstSeconds;
                view.Rect.localScale = start * Mathf.Lerp(1f, 1.5f, k);
                view.Image.color = new Color(1f, 1f, 1f, 1f - k);
                confetti.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.1f, k);
                confetti.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - k * k);
                yield return null;
            }
            Object.Destroy(confetti.gameObject);
            view.Rect.localScale = Vector3.one;
            view.Rect.gameObject.SetActive(false);
            view.Popping = false;
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

        // --- The night sky, drawn here --------------------------------------------------------------------------

        private void AddSky()
        {
            var backdrop = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(Root, false);
            backdrop.transform.SetAsFirstSibling();
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = backdrop.GetComponent<Image>();
            image.sprite = SkyGradient();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            var layer = new GameObject("Sky", typeof(RectTransform));
            layer.transform.SetParent(backdrop.transform, false);
            var layerRect = (RectTransform)layer.transform;
            layerRect.anchorMin = layerRect.anchorMax = layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);

            // A few big, faint planets and moons behind the stars.
            AddPlanet(layerRect, "sciencelab/space_moon", new Vector2(-520f, 200f), 260f, 0.5f);
            AddPlanet(layerRect, "sciencelab/space_saturn", new Vector2(540f, 40f), 300f, 0.45f);

            var random = new System.Random(7);
            for (var i = 0; i < StarCount; i++)
            {
                var size = 14f + (float)random.NextDouble() * 22f;
                var star = NewPicture(layerRect, "Star" + i, "sciencelab/space_star", new Vector2(size, size),
                    new Vector2((float)(random.NextDouble() * 2.0 - 1.0) * EvaLayout.DesignWidth * 0.5f,
                        (float)(random.NextDouble() * 2.0 - 1.0) * EvaLayout.DesignHeight * 0.5f));
                star.GetComponent<Image>().raycastTarget = false;
                _sky.Add(new Twinkle
                {
                    Rect = star,
                    Image = star.GetComponent<Image>(),
                    Speed = 1f + (float)random.NextDouble() * 2f,
                    Phase = (float)random.NextDouble() * 6.28f,
                    Size = size,
                });
            }
        }

        private static void AddPlanet(RectTransform parent, string sprite, Vector2 position, float size, float alpha)
        {
            var planet = NewPicture(parent, sprite, sprite, new Vector2(size, size), position);
            var image = planet.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, alpha);
        }

        private void DriftSky(float dt)
        {
            var half = EvaLayout.DesignHeight * 0.5f;
            foreach (var star in _sky)
            {
                var position = star.Rect.anchoredPosition;
                position.y -= StarDrift * star.Speed * dt;
                if (position.y < -half - 20f) position.y = half + 20f;
                star.Rect.anchoredPosition = position;
                var twinkle = 0.55f + 0.45f * Mathf.Sin(Time.time * star.Speed * 2f + star.Phase);
                star.Image.color = new Color(1f, 1f, 1f, twinkle);
            }
        }

        // A dark blue sky, lighter at the horizon, as a 1 x 128 picture stretched over the screen.
        private static Sprite SkyGradient()
        {
            const int height = 128;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var top = new Color(0.04f, 0.05f, 0.18f);
            var bottom = new Color(0.20f, 0.12f, 0.42f);
            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (height - 1f));
                texture.SetPixel(0, y, color);
                texture.SetPixel(1, y, color);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, height), new Vector2(0.5f, 0.5f), 100f);
        }

        // --- End of the game, the one coin, Eva ---------------------------------------------------------------

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        // Leaving early still pays the coin if the child popped anything (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _totalHits == 0) return;
            _paid = true;
            _game.Progress.AddCoins(SpaceShooterDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(SpaceShooterDirector.SessionCoins, _progress.Root.transform.position));
            }
            _cheering = true;
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
    }
}
