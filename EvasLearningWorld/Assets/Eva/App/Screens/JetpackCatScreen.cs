using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Jetpack Cat, a Flappy-Bird-like game: Eva flies sideways with a jetpack past cloud pillars. A finger held anywhere lifts her
    // (the flame shows and the jetpack hums), letting go lets her sink, and she flies through the gaps. Nothing is ever lost: a pillar she
    // touches only holds her inside its gap (a bump, no star). Levels 1-6 in a row like the other Arcade games, always from level 1; the
    // game pays one coin at the end. Until the first touch Eva hovers and nothing moves. All the rules live in JetpackDirector
    // (Rules/JetpackFlight.cs); this screen draws them and feeds them the frame time and whether a finger is down.
    public sealed class JetpackCatScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: a finger down anywhere means "fly up".
        private sealed class Pad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public System.Action<bool> Held;

            public void OnPointerDown(PointerEventData eventData) => Held?.Invoke(true);
            public void OnPointerUp(PointerEventData eventData) => Held?.Invoke(false);
        }

        private sealed class PillarView
        {
            public RectTransform Root;
            public RectTransform Top;
            public RectTransform Bottom;
            public RectTransform Star;
            public Image StarImage;
            public JetpackPillar Model;
            public bool StarTaken;
        }

        private const float PillarWidth = 190f;
        private const float PillarHeight = PillarWidth * 1198f / 300f; // the sprite's own proportions: long enough to reach past the screen edge
        private static readonly Vector2 CatSize = new Vector2(300f, 172f);
        private static readonly Vector2 FlameSize = new Vector2(90f, 182f);
        private static readonly Vector2 Nozzle = new Vector2(-6f, -1f); // where the jetpack's nozzle is, from the middle of the cat picture
        private const float StarSize = 96f;
        private const int PoolSize = 6;
        private const float TiltDegrees = 14f;
        private const float BumpSeconds = 0.35f;
        private const float StarPopSeconds = 0.35f;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the cat pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _field;
        private RectTransform _cat;
        private RectTransform _flame;
        private GameObject _endPanel;
        private ArcadeProgress _progress;

        private readonly List<PillarView> _views = new List<PillarView>();

        private System.Random _rng;
        private JetpackDirector _director;
        private bool _holding;
        private bool _paid;
        private bool _active;
        private float _idle;
        private int _idleStage;
        private bool _pulse;
        private float _bump;
        private float _tilt;
        private float _flameLevel;

        public JetpackDirector Director => _director;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            BuildField();
            _progress = ArcadeProgress.Create(Root, JetpackDirector.MaxLevel, 280f); // clear of the cat's flight path on the left
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.JetpackCat);
            StartNewGame();
        }

        public override void OnHide()
        {
            _active = false;
            _holding = false;
            _game.Sfx.ThrustStop();
            PayIfPlayed();
        }

        private void StartNewGame()
        {
            _rng = new System.Random();
            _paid = false;
            _holding = false;
            _bump = 0f;
            _tilt = 0f;
            _flameLevel = 0f;
            _director = new JetpackDirector(_rng);
            HideAllPillars();
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            var director = _director;
            ResetIdle();
            _progress.Show(JetpackDirector.MinLevel, 0f);
            _active = true;
            _game.Voice.Say("jetpack_find"); // nothing waits for it: the cat hovers until the first touch anyway
            var passed = new List<JetpackPillar>();
            var bumped = new List<JetpackPillar>();
            for (var level = JetpackDirector.MinLevel; level <= JetpackDirector.MaxLevel; level++)
            {
                if (level > JetpackDirector.MinLevel)
                {
                    director.SetLevel(level);
                    _progress.Show(level, 0f);
                    _game.Sfx.LevelUp(); // only a sound tells the child that it gets faster
                }
                while (!director.LevelDone)
                {
                    if (_director != director) yield break; // a new game began (replay) while this one still ran
                    var dt = Time.deltaTime;
                    passed.Clear();
                    bumped.Clear();
                    director.Tick(dt, _holding, passed, bumped);
                    foreach (var pillar in bumped) OnBumped(pillar);
                    foreach (var pillar in passed) OnPassed(director);
                    _idle += dt;
                    MaybeHelp();
                    Draw(dt);
                    yield return null;
                }
            }
            _active = false;
            _game.Sfx.ThrustStop();
            yield return EndGame();
        }

        // A finger down is the first thing the child does and every later one counts as playing.
        private void SetHeld(bool held)
        {
            if (!_active) return;
            if (held && _director.Hovering) _director.Hovering = false;
            if (held != _holding)
            {
                _holding = held;
                if (held) _game.Sfx.ThrustStart();
                else _game.Sfx.ThrustStop();
            }
            ResetIdle();
        }

        // After a quiet spell Eva first repeats what to do; if the quiet goes on, the cat pulses.
        private void MaybeHelp()
        {
            if (_holding) { ResetIdle(); return; }
            if (!_director.Hovering && _idleStage == 0) return; // while she is flying on her own the child is clearly playing
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("jetpack_find");
            }
            else if (_idleStage == 1 && _idle >= PulseAfterSeconds)
            {
                _idleStage = 2;
                _pulse = true;
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
            }
        }

        private void ResetIdle()
        {
            _idle = 0f;
            _idleStage = 0;
            _pulse = false;
        }

        private void OnBumped(JetpackPillar pillar)
        {
            _bump = BumpSeconds;
            _game.Sfx.Drop();
            var view = ViewOf(pillar);
            if (view != null) view.StarImage.color = new Color(1f, 1f, 1f, 0.25f); // the star is missed
        }

        private void OnPassed(JetpackDirector director)
        {
            _progress.Show(director.Level, director.PassedInLevel / (float)JetpackDirector.PillarsToPass(director.Level));
        }

        // --- Drawing -----------------------------------------------------------------------------------------

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
            pad.GetComponent<Pad>().Held = SetHeld;

            for (var i = 0; i < PoolSize; i++) _views.Add(NewPillarView(i));

            // The cat: the flame is drawn first so it comes out from behind her belly.
            var catGo = new GameObject("Cat", typeof(RectTransform));
            catGo.transform.SetParent(_field, false);
            _cat = (RectTransform)catGo.transform;
            _cat.anchorMin = _cat.anchorMax = _cat.pivot = new Vector2(0.5f, 0.5f);
            _cat.sizeDelta = CatSize;
            var flame = NewPicture(_cat, "Flame", "arcade/jetpack_flame", FlameSize, Nozzle);
            flame.pivot = new Vector2(0.5f, 1f);
            flame.anchoredPosition = Nozzle;
            _flame = flame;
            NewPicture(_cat, "Body", "arcade/jetpack_cat", CatSize, Vector2.zero);
        }

        private PillarView NewPillarView(int index)
        {
            var rootGo = new GameObject("Pillar" + index, typeof(RectTransform));
            rootGo.transform.SetParent(_field, false);
            var root = (RectTransform)rootGo.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;

            var top = NewPicture(root, "Top", "arcade/jetpack_pillar", new Vector2(PillarWidth, PillarHeight), Vector2.zero);
            top.pivot = new Vector2(0.5f, 0f); // the picture's cap is its lower end: it hangs from the top with the cap at the gap
            var bottom = NewPicture(root, "Bottom", "arcade/jetpack_pillar", new Vector2(PillarWidth, PillarHeight), Vector2.zero);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.localScale = new Vector3(1f, -1f, 1f); // upside down: standing on the ground with the cap at the gap
            var star = NewPicture(root, "Star", "arcade/prop_sparkle", new Vector2(StarSize, StarSize), Vector2.zero);

            var view = new PillarView { Root = root, Top = top, Bottom = bottom, Star = star, StarImage = star.GetComponent<Image>() };
            root.gameObject.SetActive(false);
            return view;
        }

        private void Draw(float dt)
        {
            SyncPillars();
            foreach (var view in _views)
            {
                if (view.Model == null) continue;
                var pillar = view.Model;
                view.Root.anchoredPosition = new Vector2(pillar.X, 0f);
                view.Top.anchoredPosition = new Vector2(0f, pillar.GapY + pillar.GapHeight * 0.5f);
                view.Bottom.anchoredPosition = new Vector2(0f, pillar.GapY - pillar.GapHeight * 0.5f);
                view.Star.anchoredPosition = new Vector2(0f, pillar.GapY);
                view.Star.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f);
                if (!view.StarTaken && !pillar.Bumped && pillar.X <= JetpackDirector.CatX) TakeStar(view);
            }
            DrawCat(dt);
        }

        // Every pillar the director holds gets a view and every view whose pillar is gone is freed.
        private void SyncPillars()
        {
            foreach (var view in _views)
                if (view.Model != null && !_director.Pillars.Contains(view.Model))
                {
                    view.Model = null;
                    view.Root.gameObject.SetActive(false);
                }
            foreach (var pillar in _director.Pillars)
            {
                if (ViewOf(pillar) != null) continue;
                foreach (var view in _views)
                {
                    if (view.Model != null) continue;
                    view.Model = pillar;
                    view.StarTaken = false;
                    view.StarImage.color = Color.white;
                    view.Star.gameObject.SetActive(true);
                    view.Star.localScale = Vector3.one;
                    view.Root.gameObject.SetActive(true);
                    break;
                }
            }
        }

        private PillarView ViewOf(JetpackPillar pillar)
        {
            foreach (var view in _views) if (view.Model == pillar) return view;
            return null;
        }

        private void HideAllPillars()
        {
            foreach (var view in _views)
            {
                view.Model = null;
                view.Root.gameObject.SetActive(false);
            }
        }

        private void DrawCat(float dt)
        {
            var director = _director;
            var bump = Mathf.Clamp01(_bump / BumpSeconds);
            _bump = Mathf.Max(0f, _bump - dt);
            var targetTilt = Mathf.Clamp(director.VelocityY / JetpackDirector.RiseSpeed, -1f, 1f) * TiltDegrees;
            _tilt = Mathf.Lerp(_tilt, targetTilt, 10f * dt);
            var bob = director.Hovering ? Mathf.Sin(Time.time * 3f) * 10f : 0f;
            var wobble = Mathf.Sin(bump * 25f) * 8f * bump;
            var pulse = _pulse ? 1f + 0.06f * Mathf.Sin(Time.time * 9f) : 1f;
            _cat.anchoredPosition = new Vector2(JetpackDirector.CatX, director.CatY + bob);
            _cat.localRotation = Quaternion.Euler(0f, 0f, _tilt + wobble);
            _cat.localScale = new Vector3(1f + 0.05f * bump, 1f - 0.08f * bump, 1f) * pulse;

            // The flame swells while a finger is down and dies down when it is let go.
            _flameLevel = Mathf.MoveTowards(_flameLevel, _holding ? 1f : 0f, dt * (_holding ? 12f : 7f));
            var flicker = 1f + 0.12f * Mathf.Sin(Time.time * 45f);
            _flame.localScale = new Vector3(_flameLevel * (1f + 0.06f * Mathf.Sin(Time.time * 37f)), _flameLevel * flicker, 1f);
            _flame.gameObject.SetActive(_flameLevel > 0.01f);
        }

        // A clean crossing: the star in the gap pops into a few sparkles.
        private void TakeStar(PillarView view)
        {
            view.StarTaken = true;
            view.Star.gameObject.SetActive(false);
            _game.Sfx.Pop();
            _runner.StartCoroutine(StarPop(view.Star.position));
        }

        private IEnumerator StarPop(Vector3 worldPosition)
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
            for (var t = 0f; t < StarPopSeconds; t += Time.deltaTime)
            {
                var k = t / StarPopSeconds;
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
            image.raycastTarget = false;
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

        // Leaving early still pays the coin if the child got past anything (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _director == null || _director.TotalPassed == 0) return;
            _paid = true;
            _game.Progress.AddCoins(JetpackDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(JetpackDirector.SessionCoins, _progress.Root.transform.position));
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
            image.sprite = EvaUi.Sprite("world/jetpack_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
