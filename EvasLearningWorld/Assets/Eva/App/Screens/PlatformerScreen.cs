using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Platformer as Bunny Run (docs/kids-games/arcade-redesign.md): the bunny hops along a trail while the ground and a slow
    // background slide past, a tap anywhere makes it jump over what lies on the trail, carrots and golden stars are collected by touching
    // them. One game is levels 1-6 in a row like the other Arcade games, each level a world with its own scenery (the trail just goes on,
    // the pace goes up, a short sound says "faster now"); it always starts at level 1 and ends after level 6 or when the child leaves.
    // Nothing is ever lost: a bunny that bumps something stumbles and is slid back a little. All the rules live in BunnyRunDirector
    // (Rules/BunnyRun.cs); this screen only draws them and feeds in the frame time and the taps.
    public sealed class PlatformerScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one big button: a tap anywhere is a jump.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler
        {
            public System.Action Tapped;
            public void OnPointerDown(PointerEventData eventData) => Tapped?.Invoke();
        }

        // A picture repeated side by side to cover the screen, every second copy mirrored so the copies always join seamlessly.
        private sealed class Strip
        {
            public readonly List<RectTransform> Slots = new List<RectTransform>();
            public readonly List<Image> Images = new List<Image>();
            public float Width;

            public void SetSprite(Sprite sprite)
            {
                foreach (var image in Images) image.sprite = sprite;
            }

            public void Place(float offset)
            {
                var first = Mathf.FloorToInt(offset / Width);
                for (var i = 0; i < Slots.Count; i++)
                {
                    var k = first + i;
                    var left = k * Width - offset - Width * 1.2f;
                    Slots[i].anchoredPosition = new Vector2(left + Width * 0.5f, Slots[i].anchoredPosition.y);
                    Slots[i].localScale = new Vector3(((k % 2) + 2) % 2 == 0 ? 1f : -1f, 1f, 1f);
                }
            }
        }

        private sealed class Friend
        {
            public RectTransform Rect;
            public float Start;
            public float Drift;
            public float Height;
            public float Phase;
        }

        private const float GroundY = -230f; // the surface the bunny runs on
        private const float StripWidth = 3271f; // the strip picture is 1568x139, so 3271x290 keeps its proportions
        private const float StripHeight = 290f;
        private const float BackgroundWidth = 1920f;
        private const float BackgroundParallax = 0.1f;
        private const float BunnyScreenX = -380f;
        private const float BunnyScale = 0.54f; // units per pixel of every bunny picture, so all poses keep the same size
        private const float StarSize = 130f;
        private const float CarrotScale = 0.29f;
        private const float VisibleHalf = 1150f; // beyond this much sideways from the middle nothing is drawn
        private const float HopsPerSecond = 2f; // the small hops of the running bunny, at level 1
        private const float HopHeight = 34f;

        private const int ObstaclePool = 6;
        private const int CarrotPool = 16;
        private static readonly string[] FriendSprites = { "platformer/friend_bird", "platformer/friend_butterfly", "platformer/friend_bee" };

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the bunny pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private static readonly Dictionary<string, Sprite> WorldSprites = new Dictionary<string, Sprite>();

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _field;
        private RectTransform _bunny;
        private Image _bunnyImage;
        private RectTransform _pad;
        private GameObject _endPanel;
        private ArcadeProgress _progress;
        private Strip _background;
        private Strip _ground;

        private readonly List<RectTransform> _obstacleViews = new List<RectTransform>();
        private readonly List<RectTransform> _carrotViews = new List<RectTransform>();
        private readonly List<Friend> _friends = new List<Friend>();

        private System.Random _rng;
        private BunnyRunDirector _director;
        private int _world;
        private bool _tapped;
        private bool _active;
        private bool _sitting;
        private bool _cheering;
        private bool _paid;
        private float _idle;
        private int _idleStage;
        private bool _pulse;
        private float _hopClock;

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
            _sitting = true;
            _tapped = false;
            _director = new BunnyRunDirector(_rng);
            _world = 0;
            SetWorld(1);
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
                SetWorld(level);
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == BunnyRunDirector.MinLevel)
                {
                    _active = false;
                    _sitting = true;
                    Draw();
                    yield return _game.Voice.SayAndWait("platformer_prompt");
                    _sitting = false;
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
                    if ((events & BunnyEvents.Bumped) != 0) _game.Voice.Say("bunny_ouch"); // the bump is just "Ouch!", no other sound, none when it runs again
                    foreach (var carrot in taken) OnTaken(carrot);
                    if (taken.Count > 0) _progress.Show(level, Mathf.Min(1f, director.Hits / (float)BunnyRunDirector.HitsToPass(level)));
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

        // --- Building the scene ---------------------------------------------------------------------------------

        private void AddBackground()
        {
            var container = new GameObject("Background", typeof(RectTransform));
            container.transform.SetParent(Root, false);
            container.transform.SetAsFirstSibling();
            var rect = (RectTransform)container.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            container.AddComponent<RectMask2D>();
            _background = NewStrip(rect, "Sky", BackgroundWidth, 0f, true);
        }

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

            for (var i = 0; i < FriendSprites.Length; i++)
            {
                var rect = NewPicture(_field, "Friend" + i, null, Vector2.one * 100f, Vector2.zero);
                rect.GetComponent<Image>().raycastTarget = false;
                _friends.Add(new Friend { Rect = rect, Start = i * 900f, Drift = 22f + i * 9f, Height = 150f + i * 55f, Phase = i * 2.1f });
            }

            _ground = NewStrip(_field, "Ground", StripWidth, GroundY + 60f, false); // the grass starts 60 units above the line the feet stand on, so feet and rocks are planted in it

            for (var i = 0; i < ObstaclePool; i++)
            {
                var rect = NewPicture(_field, "Obstacle" + i, null, Vector2.one * 100f, Vector2.zero);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.GetComponent<Image>().raycastTarget = false;
                _obstacleViews.Add(rect);
                rect.gameObject.SetActive(false);
            }
            for (var i = 0; i < CarrotPool; i++)
            {
                var rect = NewPicture(_field, "Carrot" + i, "platformer/carrot", EvaUi.Sprite("platformer/carrot").rect.size * CarrotScale, Vector2.zero);
                rect.GetComponent<Image>().raycastTarget = false;
                _carrotViews.Add(rect);
                rect.gameObject.SetActive(false);
            }

            _bunny = NewPicture(_field, "Bunny", "platformer/bunny_sit", Vector2.one * 100f, new Vector2(BunnyScreenX, GroundY));
            _bunny.pivot = new Vector2(0.5f, 0f); // it stands, hops and squashes on its feet
            _bunnyImage = _bunny.GetComponent<Image>();
            _bunnyImage.raycastTarget = false;
        }

        private static Strip NewStrip(RectTransform parent, string name, float width, float topY, bool stretchHeight)
        {
            var strip = new Strip { Width = width };
            var count = Mathf.CeilToInt(2 * VisibleHalf / width) + 3;
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject(name + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = false;
                if (stretchHeight)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(width + 6f, 0f); // 3 units of overlap each side hide the seam
                    rect.anchoredPosition = Vector2.zero;
                }
                else
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.sizeDelta = new Vector2(width + 6f, StripHeight);
                    rect.anchoredPosition = new Vector2(0f, topY);
                }
                strip.Slots.Add(rect);
                strip.Images.Add(image);
            }
            return strip;
        }

        // The art of world N (meadow, forest, autumn, beach, snow, twilight); a world whose art has not been made yet shows world 1's.
        private static Sprite WorldSprite(int world, string name)
        {
            var key = world + "/" + name;
            if (WorldSprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>("Art/platformer/w" + world + "_" + name);
            if (sprite == null) sprite = EvaUi.Sprite("platformer/w1_" + name);
            WorldSprites[key] = sprite;
            return sprite;
        }

        private void SetWorld(int world)
        {
            if (_world == world) return;
            _world = world;
            _background.SetSprite(WorldSprite(world, "bg"));
            _ground.SetSprite(WorldSprite(world, "ground"));
        }

        // --- Drawing a frame ------------------------------------------------------------------------------------

        private static float ScreenX(float worldX, float scroll) => worldX - scroll + BunnyScreenX;

        private void Draw()
        {
            var d = _director;
            _background.Place(d.Scroll * BackgroundParallax);
            _ground.Place(d.Scroll);
            DrawFriends(d);
            DrawObstacles(d);
            DrawCarrots(d);
            DrawBunny(d);
        }

        private void DrawFriends(BunnyRunDirector d)
        {
            for (var i = 0; i < _friends.Count; i++)
            {
                var f = _friends[i];
                const float span = 2800f;
                var along = (f.Start + Time.time * f.Drift + d.Scroll * 0.3f) % span;
                var x = 1400f - along;
                var y = 60f + f.Height * 0.55f + Mathf.Sin(Time.time * 2f + f.Phase) * 18f; // below the progress bar
                var sprite = EvaUi.Sprite(FriendSprites[i]);
                f.Rect.GetComponent<Image>().sprite = sprite;
                f.Rect.sizeDelta = sprite.rect.size * 0.26f;
                f.Rect.anchoredPosition = new Vector2(x, y);
                f.Rect.localScale = new Vector3(-1f, 1f, 1f); // they fly towards the left, as the world slides
            }
        }

        private void DrawObstacles(BunnyRunDirector d)
        {
            var used = 0;
            foreach (var o in d.Obstacles)
            {
                var x = ScreenX(o.X, d.Scroll);
                if (x < -VisibleHalf || x > VisibleHalf || used >= _obstacleViews.Count) continue;
                var view = _obstacleViews[used++];
                view.gameObject.SetActive(true);
                var sprite = WorldSprite(_world, "obstacle" + (o.Kind + 1));
                view.GetComponent<Image>().sprite = sprite;
                var scale = o.Height * 1.15f / sprite.rect.height; // the picture is a little taller than what the bunny bumps into
                view.sizeDelta = sprite.rect.size * scale;
                view.anchoredPosition = new Vector2(x, GroundY - 6f);
            }
            for (var i = used; i < _obstacleViews.Count; i++) _obstacleViews[i].gameObject.SetActive(false);
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
                var sprite = EvaUi.Sprite(carrot.Star ? "platformer/star" : "platformer/carrot");
                view.GetComponent<Image>().sprite = sprite;
                view.sizeDelta = carrot.Star ? Vector2.one * StarSize : sprite.rect.size * CarrotScale;
                view.anchoredPosition = new Vector2(x, GroundY + carrot.Y + bob);
            }
            for (var i = used; i < _carrotViews.Count; i++) _carrotViews[i].gameObject.SetActive(false);
        }

        // The pose follows what the bunny is doing: sitting while Eva speaks, small hops while it runs, a long hop when the child taps,
        // a wobble after a bump, a cheer at the end.
        private void DrawBunny(BunnyRunDirector d)
        {
            var scale = 1f;
            var y = GroundY;
            var tilt = 0f;
            string pose;
            if (_cheering)
            {
                pose = "cheer";
                y += Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 90f;
            }
            else if (_sitting)
            {
                pose = "sit";
            }
            else if (d.State == BunnyState.Stumbling)
            {
                pose = "stumble";
                tilt = Mathf.Sin(d.StumbleProgress * 25f) * 8f * (1f - d.StumbleProgress);
            }
            else if (d.Jumping)
            {
                var u = d.JumpProgress;
                pose = u < 0.1f ? "push" : u < 0.45f ? "tuck" : u < 0.9f ? "fall" : "land";
                y += d.JumpHeight;
            }
            else
            {
                // One small hop after another: crouch, push off, tuck, fall, land.
                _hopClock += Time.deltaTime * HopsPerSecond * BunnyRunDirector.Speed(d.Level) / BunnyRunDirector.Speed(1);
                var p = _hopClock - Mathf.Floor(_hopClock);
                pose = p < 0.12f ? "push" : p < 0.4f ? "tuck" : p < 0.65f ? "fall" : p < 0.78f ? "land" : "crouch";
                if (p < 0.65f) y += HopHeight * 4f * (p / 0.65f) * (1f - p / 0.65f);
                if (_pulse) scale = 1f + 0.08f * Mathf.Sin(Time.time * 9f);
            }
            var picture = EvaUi.Sprite("platformer/bunny_" + pose);
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
