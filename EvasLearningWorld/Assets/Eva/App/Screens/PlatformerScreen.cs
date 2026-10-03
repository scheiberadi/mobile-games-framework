using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Platformer as Bunny Run (docs/kids-games/arcade-redesign.md): the bunny runs on the spot while the ground slides past, a tap
    // anywhere makes it jump over the rivers and carrots are collected by touching them. One game is levels 1-6 in a row like the other
    // Arcade games (the track just goes on, only the pace changes, a short sound says "faster now"); it always starts at level 1 and ends
    // after level 6 or when the child leaves. Nothing is ever lost: a bunny that falls in swims a moment and a lily pad sets it back on the
    // bank. All the rules live in BunnyRunDirector (Rules/BunnyRun.cs); this screen only draws them and feeds in the frame time and the taps.
    public sealed class PlatformerScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one big button: a tap anywhere is a jump.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler
        {
            public System.Action Tapped;
            public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke();
        }

        private const float GroundY = -230f; // the top of the grass
        private const float LandHeight = 210f;
        private const float GrassOverhang = 18f; // the grass tufts of the ground picture stand this far above the surface the bunny runs on
        private const float BunnyScreenX = -380f;
        private const float BunnyScale = 0.54f; // units per pixel of every bunny picture, so all poses keep the same size
        private const float CarrotScale = 0.29f;
        private const float LilyScale = 0.54f;
        private const float WaterBottom = -450f;
        private const float VisibleHalf = 1150f; // beyond this much sideways from the middle nothing is drawn

        private const int LandPool = 6;
        private const int CarrotPool = 16;
        private const int RipplePool = 9;

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the bunny pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _field;
        private RectTransform _bunny;
        private RectTransform _lily;
        private Image _bunnyImage;
        private RectTransform _pad;
        private GameObject _endPanel;
        private ArcadeProgress _progress;

        private readonly List<RectTransform> _landViews = new List<RectTransform>();
        private readonly List<RectTransform> _carrotViews = new List<RectTransform>();
        private readonly List<RectTransform> _ripples = new List<RectTransform>();

        private System.Random _rng;
        private BunnyRunDirector _director;
        private bool _tapped;
        private bool _active;
        private bool _cheering;
        private float _idle;
        private int _idleStage;
        private bool _pulse;
        private float _runClock;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            BuildField();
            _progress = ArcadeProgress.Create(Root, BunnyRunDirector.MaxLevel);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Platformer);
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
            _paid = false;
            _cheering = false;
            _tapped = false;
            _director = new BunnyRunDirector(_rng);
            Draw();
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            var director = _director;
            var taken = new List<RunCarrot>();
            for (var level = BunnyRunDirector.MinLevel; level <= BunnyRunDirector.MaxLevel; level++)
            {
                director.StartLevel(level);
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == BunnyRunDirector.MinLevel)
                {
                    _active = false;
                    yield return _game.Voice.SayAndWait("platformer_prompt");
                }
                else
                {
                    // Only a sound tells the child that it gets faster.
                    _game.Sfx.LevelUp();
                }

                _active = true;
                while (!director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    taken.Clear();
                    var tap = _tapped;
                    _tapped = false;
                    var events = director.Tick(dt, tap, taken);
                    if ((events & BunnyEvents.Jumped) != 0) _game.Sfx.Pick();
                    if ((events & BunnyEvents.Splashed) != 0) _game.Sfx.Drop();
                    if ((events & BunnyEvents.Rescued) != 0) _game.Sfx.Place();
                    foreach (var carrot in taken) OnTaken(carrot);
                    if (taken.Count > 0) _progress.Show(level, director.Hits / (float)BunnyRunDirector.HitsToPass(level));
                    _idle += dt;
                    MaybeHelp();
                    Draw();
                    yield return null;
                }
            }
            _active = false;
            yield return EndGame();
        }

        private void MaybeHelp()
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say("platformer_prompt");
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

        // --- Drawing the world ----------------------------------------------------------------------------------

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
            _pad = (RectTransform)pad.transform;
            _pad.anchorMin = Vector2.zero;
            _pad.anchorMax = Vector2.one;
            _pad.offsetMin = _pad.offsetMax = Vector2.zero;
            var padImage = pad.GetComponent<Image>();
            padImage.color = new Color(1f, 1f, 1f, 0f);
            padImage.raycastTarget = true;
            pad.GetComponent<Pad>().Tapped = () =>
            {
                _tapped = true;
                if (_active) ResetIdle();
            };

            AddWater();
            for (var i = 0; i < LandPool; i++)
            {
                var rect = NewPicture(_field, "Land" + i, "platformer/ground", new Vector2(100f, LandHeight), Vector2.zero);
                rect.pivot = new Vector2(0f, 1f);
                var image = rect.GetComponent<Image>();
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.raycastTarget = false;
                _landViews.Add(rect);
                rect.gameObject.SetActive(false);
            }
            for (var i = 0; i < CarrotPool; i++)
            {
                var rect = NewPicture(_field, "Carrot" + i, "platformer/carrot", EvaUi.Sprite("platformer/carrot").rect.size * CarrotScale, Vector2.zero);
                rect.GetComponent<Image>().raycastTarget = false;
                _carrotViews.Add(rect);
                rect.gameObject.SetActive(false);
            }

            _lily = NewPicture(_field, "LilyPad", "platformer/lilypad", EvaUi.Sprite("platformer/lilypad").rect.size * LilyScale, Vector2.zero);
            _lily.GetComponent<Image>().raycastTarget = false;
            _lily.gameObject.SetActive(false);

            _bunny = NewPicture(_field, "Bunny", "platformer/bunny_run1", Vector2.one * 100f, new Vector2(BunnyScreenX, GroundY));
            _bunny.pivot = new Vector2(0.5f, 0f); // it stands, hops and squashes on its feet
            _bunnyImage = _bunny.GetComponent<Image>();
            _bunnyImage.raycastTarget = false;
        }

        // The rivers: a gradient of water under the whole track, with ripples sliding along with the ground. The land is drawn over it.
        private void AddWater()
        {
            var water = new GameObject("Water", typeof(RectTransform), typeof(Image));
            water.transform.SetParent(_field, false);
            var rect = (RectTransform)water.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(EvaLayout.DesignWidth + 1000f, GroundY - WaterBottom + 130f);
            rect.anchoredPosition = new Vector2(0f, GroundY - 10f);
            var image = water.GetComponent<Image>();
            image.sprite = WaterGradient();
            image.raycastTarget = false;

            for (var i = 0; i < RipplePool; i++)
            {
                var ripple = new GameObject("Ripple" + i, typeof(RectTransform), typeof(Image));
                ripple.transform.SetParent(_field, false);
                var r = (RectTransform)ripple.transform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(90f + (i % 3) * 40f, 10f);
                ripple.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
                ripple.GetComponent<Image>().raycastTarget = false;
                _ripples.Add(r);
            }
        }

        private static Sprite WaterGradient()
        {
            const int height = 64;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var top = new Color(0.45f, 0.85f, 0.95f);
            var bottom = new Color(0.10f, 0.40f, 0.70f);
            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (height - 1f));
                texture.SetPixel(0, y, color);
                texture.SetPixel(1, y, color);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void AddBackground()
        {
            var backdrop = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(Root, false);
            backdrop.transform.SetAsFirstSibling();
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = backdrop.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("platformer/bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }

        private static float ScreenX(float worldX, float scroll) => worldX - scroll + BunnyScreenX;

        private void Draw()
        {
            var d = _director;
            DrawLand(d);
            DrawCarrots(d);
            DrawRipples(d);
            DrawBunny(d);
        }

        private void DrawLand(BunnyRunDirector d)
        {
            var used = 0;
            foreach (var land in d.Lands)
            {
                var left = ScreenX(land.Start, d.Scroll);
                var right = ScreenX(land.End, d.Scroll);
                if (right < -VisibleHalf || left > VisibleHalf || used >= _landViews.Count) continue;
                var view = _landViews[used++];
                view.gameObject.SetActive(true);
                view.anchoredPosition = new Vector2(left, GroundY + GrassOverhang);
                view.sizeDelta = new Vector2(right - left, LandHeight);
            }
            for (var i = used; i < _landViews.Count; i++) _landViews[i].gameObject.SetActive(false);
        }

        private void DrawCarrots(BunnyRunDirector d)
        {
            var used = 0;
            var bob = Mathf.Sin(Time.time * 5f) * 6f;
            foreach (var carrot in d.Carrots)
            {
                if (carrot.Taken) continue;
                var x = ScreenX(carrot.X, d.Scroll);
                if (x < -VisibleHalf || x > VisibleHalf || used >= _carrotViews.Count) continue;
                var view = _carrotViews[used++];
                view.gameObject.SetActive(true);
                view.anchoredPosition = new Vector2(x, GroundY + carrot.Y + bob);
            }
            for (var i = used; i < _carrotViews.Count; i++) _carrotViews[i].gameObject.SetActive(false);
        }

        // Ripples are spread over a stretch of track and slide with it, so the river looks like it flows past.
        private void DrawRipples(BunnyRunDirector d)
        {
            const float span = 2400f;
            for (var i = 0; i < _ripples.Count; i++)
            {
                var along = (i * 263f - d.Scroll * 1.0f) % span;
                if (along < 0f) along += span;
                var x = along - span * 0.5f - 200f;
                var y = GroundY - 50f - (i * 37f) % 150f + Mathf.Sin(Time.time * 2f + i) * 4f;
                _ripples[i].anchoredPosition = new Vector2(x, y);
            }
        }

        private void DrawBunny(BunnyRunDirector d)
        {
            var scale = 1f;
            var y = GroundY;
            var tilt = 0f;
            var lily = false;
            string sprite;
            if (_cheering)
            {
                sprite = "platformer/bunny_jump";
                y += Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 110f;
            }
            else if (d.State == BunnyState.Swimming)
            {
                // Splashing about in the river, then carried back on a lily pad and put down on the bank.
                var carry = d.CarryProgress;
                sprite = "platformer/bunny_swim";
                y = GroundY - 90f + Mathf.Sin(Time.time * 7f) * 5f;
                if (carry > 0f)
                {
                    lily = true;
                    _lily.anchoredPosition = new Vector2(BunnyScreenX, y - 5f);
                    if (carry > 0.8f) y = Mathf.Lerp(y, GroundY, (carry - 0.8f) / 0.2f);
                }
            }
            else if (d.Jumping)
            {
                sprite = "platformer/bunny_jump";
                y += d.JumpHeight;
                tilt = Mathf.Lerp(15f, -15f, d.JumpProgress); // nose up going up, nose down coming down
            }
            else
            {
                _runClock += Time.deltaTime * BunnyRunDirector.Speed(d.Level) / 230f;
                sprite = (int)(_runClock / 0.13f) % 2 == 0 ? "platformer/bunny_run1" : "platformer/bunny_run2";
                if (_pulse) scale = 1f + 0.08f * Mathf.Sin(Time.time * 9f);
            }
            _lily.gameObject.SetActive(lily);
            var picture = EvaUi.Sprite(sprite);
            _bunnyImage.sprite = picture;
            _bunny.sizeDelta = picture.rect.size * BunnyScale;
            _bunny.anchoredPosition = new Vector2(BunnyScreenX, y);
            _bunny.localScale = Vector3.one * scale;
            _bunny.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private void OnTaken(RunCarrot carrot)
        {
            _game.Sfx.Pop();
            var x = ScreenX(carrot.X, _director.Scroll);
            _runner.StartCoroutine(Poof(new Vector2(x, GroundY + carrot.Y)));
        }

        // A little burst where a carrot was taken.
        private IEnumerator Poof(Vector2 position)
        {
            var confetti = NewPicture(_field, "Poof", "arcade/prop_pop", new Vector2(220f, 220f), position);
            confetti.GetComponent<Image>().raycastTarget = false;
            const float seconds = 0.35f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                confetti.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.1f, k);
                confetti.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - k * k);
                yield return null;
            }
            Object.Destroy(confetti.gameObject);
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

        // --- End of the game, the one coin --------------------------------------------------------------------

        private bool _paid;

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        // Leaving early still pays the coin if the child collected anything (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _director == null || _director.TotalHits == 0) return;
            _paid = true;
            _game.Progress.AddCoins(BunnyRunDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(BunnyRunDirector.SessionCoins, _progress.Root.transform.position));
            }
            _cheering = true;
            SetGameEnded(true);
            while (_cheering)
            {
                Draw();
                yield return null;
            }
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

        // The field stays visible behind the end panel so the cheering bunny can be seen; only the progress display and taps go away.
        private void SetGameEnded(bool ended)
        {
            _pad.gameObject.SetActive(!ended);
            _progress.Root.SetActive(!ended);
            _endPanel.SetActive(ended);
        }
    }
}
