using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Arcade's Treasure Hunt as a metal detector game (docs/kids-games/arcade-redesign.md): the child slides the detector over a big
    // stretch of sand, it beeps faster the closer it gets to something buried and goes continuous right over it; the spot is then
    // marked with an X and every tap digs, a number counting the taps, until the buried thing comes out. One game is levels 1-6 in a
    // row like the other Arcade games (a smaller hot zone, more treasures, more taps, decoys, no glow from level 3); it always starts
    // at level 1 and ends after level 6 or when the child leaves. Nothing is ever lost: junk is a funny find. All the rules live in
    // TreasureHuntDirector (Rules/TreasureHunt.cs); this screen only draws them and feeds in the frame time, the finger and the taps.
    public sealed class TreasureHuntScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The whole field is one touch pad: the finger drags the detector, a press is also a tap for digging.
        private sealed class Pad : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            public System.Action<Vector2, bool> Touched;

            public void OnPointerDown(PointerEventData eventData) => Report(eventData, true);
            public void OnDrag(PointerEventData eventData) => Report(eventData, false);

            private void Report(PointerEventData eventData, bool down)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local))
                    Touched?.Invoke(local, down);
            }
        }

        private static readonly string[] TreasureSprites = { "treasure/chest", "treasure/crown", "treasure/gem", "treasure/coins", "treasure/ring", "treasure/key" };
        private static readonly string[] JunkSprites = { "treasure/can", "treasure/boot", "treasure/bottle" };

        private const float CoilLift = 90f; // the search coil sits this far above the fingertip so the finger does not hide it
        private const float DetectorHeight = 330f;
        private static readonly Vector2 CoilPivot = new Vector2(0.39f, 0.16f); // the middle of the red coil in the detector picture
        private const float MarkSize = 170f;
        private const float HoleSize = 230f;
        private const float ShovelHeight = 210f;
        private const float ItemSize = 190f;
        private const float NumeralSize = 130f;
        private static readonly Vector2 ParkedDetector = new Vector2(0f, -150f);
        private static readonly Vector2 BesideTheSpot = new Vector2(-210f, 30f); // where the detector leans while the child digs
        private static readonly Color Sand = new Color(0.95f, 0.78f, 0.45f, 1f);

        // Idle help, in two steps that never play the game for the child: Eva repeats what to do, then the detector (or the spot) pulses.
        private const float RemindAfterSeconds = 8f;
        private const float PulseAfterSeconds = 14f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private static Sprite _ringSprite;

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _field;
        private RectTransform _pad;
        private RectTransform _detector;
        private RectTransform _glow;
        private Image _glowImage;
        private RectTransform _mark;
        private RectTransform _hole;
        private RectTransform _shovel;
        private TextMeshProUGUI _numeral;
        private GameObject _endPanel;
        private ArcadeProgress _progress;

        private readonly List<RectTransform> _holes = new List<RectTransform>();

        private System.Random _rng;
        private TreasureHuntDirector _director;
        private Vector2 _pointer;
        private bool _hasPointer;
        private bool _tapped;
        private bool _active;
        private bool _paid;
        private bool _toldHowToDig;
        private float _idle;
        private int _idleStage;
        private bool _pulse;
        private int _digCount;
        private DetectorSignal _lastSignal;
        private float _glowPulse;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground();
            BuildField();
            _progress = ArcadeProgress.Create(Root, TreasureHuntDirector.MaxLevel);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.TreasureHunt);
            StartNewGame();
        }

        public override void OnHide()
        {
            _active = false;
            _game.Sfx.ReelStop();
            PayIfPlayed();
        }

        private void StartNewGame()
        {
            _rng = new System.Random();
            _paid = false;
            _toldHowToDig = false;
            _hasPointer = false;
            _tapped = false;
            _lastSignal = DetectorSignal.Silent;
            _director = new TreasureHuntDirector(_rng);
            _director.MoveDetector(ParkedDetector.x, ParkedDetector.y);
            ClearSpot();
            ClearHoles();
            SetGameEnded(false);
            _runner.StartCoroutine(RunGame());
        }

        // --- The whole game: levels 1-6 -----------------------------------------------------------------------

        private IEnumerator RunGame()
        {
            var director = _director;
            for (var level = TreasureHuntDirector.MinLevel; level <= TreasureHuntDirector.MaxLevel; level++)
            {
                director.StartLevel(level);
                ClearSpot();
                ClearHoles();
                ResetIdle();
                _progress.Show(level, 0f);

                if (level == TreasureHuntDirector.MinLevel)
                {
                    _active = false;
                    Draw();
                    yield return _game.Voice.SayAndWait("treasure_prompt");
                }
                else
                {
                    _game.Sfx.LevelUp(); // a sound tells the child that a new beach starts
                }

                _active = true;
                while (!director.LevelDone)
                {
                    var dt = Time.deltaTime;
                    if (director.Phase == HuntPhase.Searching && _hasPointer)
                        director.MoveDetector(_pointer.x, _pointer.y + CoilLift);
                    var tap = _tapped;
                    _tapped = false;
                    var events = director.Tick(dt, tap);
                    UpdateHum(director);
                    if ((events & HuntEvents.Beep) != 0)
                    {
                        _game.Sfx.DetectorBeep(director.NearestIsJunk);
                        _glowPulse = 1f;
                    }
                    if ((events & HuntEvents.Marked) != 0) OnMarked(director.Marked);
                    if ((events & HuntEvents.Dug) != 0) OnDug(director);
                    if ((events & HuntEvents.Revealed) != 0)
                    {
                        yield return Reveal(director.LastRevealed);
                        _progress.Show(level, Mathf.Min(1f, director.Found / (float)TreasureHuntDirector.TreasuresToFind(level)));
                        ResetIdle();
                    }
                    _idle += dt;
                    MaybeHelp(director);
                    Draw();
                    yield return null;
                }
                yield return new WaitForSeconds(0.5f);
            }
            _active = false;
            _game.Sfx.ReelStop();
            yield return EndGame();
        }

        // The continuous tone is a loop that runs exactly while the signal is continuous.
        private void UpdateHum(TreasureHuntDirector director)
        {
            if (director.Signal == _lastSignal) return;
            if (director.Signal == DetectorSignal.Continuous) _game.Sfx.DetectorHot(director.NearestIsJunk);
            else if (_lastSignal == DetectorSignal.Continuous) _game.Sfx.ReelStop();
            _lastSignal = director.Signal;
        }

        private void MaybeHelp(TreasureHuntDirector director)
        {
            if (_idleStage == 0 && _idle >= RemindAfterSeconds)
            {
                _idleStage = 1;
                _game.Voice.Say(director.Phase == HuntPhase.Marked ? "treasure_dig" : "treasure_prompt");
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

        // --- What happens at the spot ---------------------------------------------------------------------------------

        private void OnMarked(HiddenItem item)
        {
            _game.Sfx.Place();
            _digCount = 0;
            var position = new Vector2(item.X, item.Y);
            _mark.gameObject.SetActive(true);
            _mark.anchoredPosition = position;
            _mark.localScale = Vector3.zero;
            _runner.StartCoroutine(Grow(_mark, 0.25f));
            _shovel.gameObject.SetActive(true);
            _shovel.anchoredPosition = position + new Vector2(80f, 70f);
            _hole.gameObject.SetActive(false);
            _numeral.gameObject.SetActive(false);
            if (!_toldHowToDig)
            {
                _toldHowToDig = true;
                _game.Voice.Say("treasure_dig");
            }
            ResetIdle();
        }

        private void OnDug(TreasureHuntDirector director)
        {
            _digCount++;
            ResetIdle();
            _game.Sfx.Dig();
            var spot = _mark.anchoredPosition;
            var needed = director.Marked != null && director.Marked.Junk ? TreasureHuntDirector.JunkDigs : TreasureHuntDirector.DigsNeeded(director.Level);
            if (director.Marked == null) needed = _digCount; // the last tap: the item is already out of the director
            _mark.gameObject.SetActive(false);
            _hole.gameObject.SetActive(true);
            _hole.anchoredPosition = spot;
            _hole.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(_digCount / (float)Mathf.Max(1, needed)));
            _numeral.gameObject.SetActive(true);
            _numeral.text = _digCount.ToString();
            _numeral.rectTransform.anchoredPosition = spot + new Vector2(0f, 170f);
            _numeral.rectTransform.localScale = Vector3.one;
            _runner.StartCoroutine(Bump(_numeral.rectTransform, 0.2f));
            _runner.StartCoroutine(Swing(_shovel, spot + new Vector2(80f, 70f)));
            _runner.StartCoroutine(Puff(spot));
            if (_digCount <= 10) _game.Voice.Say("num_" + _digCount);
        }

        private IEnumerator Reveal(HiddenItem item)
        {
            _shovel.gameObject.SetActive(false);
            _numeral.gameObject.SetActive(false);
            var spot = new Vector2(item.X, item.Y);

            var kept = NewPicture(_field, "DugHole", "treasure/hole", Vector2.one * HoleSize, spot);
            kept.GetComponent<Image>().raycastTarget = false;
            kept.SetSiblingIndex(_hole.GetSiblingIndex());
            _holes.Add(kept);
            _hole.gameObject.SetActive(false);

            var sprite = item.Junk ? JunkSprites[item.Kind] : TreasureSprites[item.Kind];
            var picture = NewPicture(_field, "Find", sprite, Vector2.one * ItemSize, spot);
            picture.GetComponent<Image>().raycastTarget = false;
            var image = picture.GetComponent<Image>();
            if (item.Junk) _game.Sfx.Drop();
            else _game.Sfx.Right();
            _game.Voice.Say(item.Junk ? "treasure_junk" : "treasure_found");

            // Pops out of the hole with a little bounce.
            const float popSeconds = 0.45f;
            for (var t = 0f; t < popSeconds; t += Time.deltaTime)
            {
                var k = t / popSeconds;
                picture.localScale = Vector3.one * (Mathf.Sin(k * Mathf.PI * 0.5f) * 1.2f - 0.2f * k * k);
                picture.anchoredPosition = spot + new Vector2(0f, 90f * Mathf.Sin(k * Mathf.PI * 0.5f));
                yield return null;
            }
            picture.localScale = Vector3.one;

            if (item.Junk)
            {
                for (var t = 0f; t < 1.2f; t += Time.deltaTime)
                {
                    picture.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * 8f * (1f - t / 1.2f));
                    yield return null;
                }
                for (var t = 0f; t < 0.4f; t += Time.deltaTime)
                {
                    image.color = new Color(1f, 1f, 1f, 1f - t / 0.4f);
                    yield return null;
                }
            }
            else
            {
                for (var t = 0f; t < 0.9f; t += Time.deltaTime)
                {
                    picture.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * 12f));
                    yield return null;
                }
                // The treasure flies up to the progress display.
                var from = picture.anchoredPosition;
                var to = new Vector2(0f, 395f);
                for (var t = 0f; t < 0.6f; t += Time.deltaTime)
                {
                    var k = t / 0.6f;
                    picture.anchoredPosition = Vector2.Lerp(from, to, k * k);
                    picture.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
                    yield return null;
                }
                _game.Sfx.Coin();
            }
            Object.Destroy(picture.gameObject);
        }

        private void ClearSpot()
        {
            _mark.gameObject.SetActive(false);
            _hole.gameObject.SetActive(false);
            _shovel.gameObject.SetActive(false);
            _numeral.gameObject.SetActive(false);
        }

        private void ClearHoles()
        {
            foreach (var hole in _holes)
                if (hole != null) Object.Destroy(hole.gameObject);
            _holes.Clear();
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

            var sand = new GameObject("Sand", typeof(RectTransform), typeof(Image));
            sand.transform.SetParent(rect, false);
            var sandRect = (RectTransform)sand.transform;
            sandRect.anchorMin = Vector2.zero;
            sandRect.anchorMax = Vector2.one;
            sandRect.offsetMin = sandRect.offsetMax = Vector2.zero;
            var image = sand.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("treasure/bg");
            image.preserveAspect = false;
            image.raycastTarget = false;
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
            pad.GetComponent<Pad>().Touched = (position, down) =>
            {
                _pointer = position;
                _hasPointer = true;
                if (down) _tapped = true;
                if (_active) ResetIdle();
            };

            _hole = NewPicture(_field, "Hole", "treasure/hole", Vector2.one * HoleSize, Vector2.zero);
            _hole.GetComponent<Image>().raycastTarget = false;
            _mark = NewPicture(_field, "Mark", "treasure/mark", Vector2.one * MarkSize, Vector2.zero);
            _mark.GetComponent<Image>().raycastTarget = false;

            var shovelSprite = EvaUi.Sprite("treasure/shovel");
            _shovel = NewPicture(_field, "Shovel", "treasure/shovel", new Vector2(ShovelHeight * shovelSprite.rect.width / shovelSprite.rect.height, ShovelHeight), Vector2.zero);
            _shovel.GetComponent<Image>().raycastTarget = false;

            _numeral = EvaUi.Numeral(_field, "DigCount", 150);
            _numeral.color = Color.white;
            _numeral.fontStyle = FontStyles.Bold;
            _numeral.rectTransform.sizeDelta = new Vector2(NumeralSize * 1.6f, NumeralSize * 1.3f);

            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(_field, false);
            _glow = (RectTransform)glow.transform;
            _glow.anchorMin = _glow.anchorMax = _glow.pivot = new Vector2(0.5f, 0.5f);
            _glowImage = glow.GetComponent<Image>();
            _glowImage.sprite = RingSprite();
            _glowImage.raycastTarget = false;

            var detectorSprite = EvaUi.Sprite("treasure/detector");
            var size = new Vector2(DetectorHeight * detectorSprite.rect.width / detectorSprite.rect.height, DetectorHeight);
            _detector = NewPicture(_field, "Detector", "treasure/detector", size, ParkedDetector);
            _detector.pivot = CoilPivot; // positioned by the coil, so the beep distance is measured from what the child sees
            _detector.GetComponent<Image>().raycastTarget = false;
        }

        // A soft ring, drawn in code and tinted: it shows how close the detector is on the first levels.
        private static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var half = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var r = Mathf.Sqrt((x - half + 0.5f) * (x - half + 0.5f) + (y - half + 0.5f) * (y - half + 0.5f)) / half;
                var ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
                var fill = Mathf.Clamp01(1f - r) * 0.25f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(ring, fill)));
            }
            texture.Apply();
            _ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _ringSprite;
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

        // --- Drawing a frame ------------------------------------------------------------------------------------

        private void Draw()
        {
            var d = _director;
            var scale = 1f;
            if (_pulse) scale = 1f + 0.08f * Mathf.Sin(Time.time * 9f);
            var coil = d.Phase == HuntPhase.Marked && d.Marked != null
                ? new Vector2(d.Marked.X, d.Marked.Y) + BesideTheSpot
                : new Vector2(d.DetectorX, d.DetectorY);
            _detector.anchoredPosition = coil;
            _detector.localScale = Vector3.one * scale;
            _detector.localRotation = Quaternion.Euler(0f, 0f, d.Phase == HuntPhase.Marked ? 14f : 0f);

            _glowPulse = Mathf.Max(0f, _glowPulse - Time.deltaTime * 6f);
            var showGlow = _active && TreasureHuntDirector.ShowsGlow(d.Level) && d.Phase == HuntPhase.Searching && d.Signal != DetectorSignal.Silent;
            _glow.gameObject.SetActive(showGlow);
            if (showGlow)
            {
                var close = d.Closeness;
                var diameter = Mathf.Lerp(150f, 330f, close) * (1f + 0.15f * _glowPulse);
                _glow.sizeDelta = new Vector2(diameter, diameter);
                _glow.anchoredPosition = coil;
                var tint = Color.Lerp(new Color(0.3f, 0.9f, 0.4f), new Color(1f, 0.3f, 0.2f), close);
                tint.a = Mathf.Lerp(0.35f, 0.95f, close);
                _glowImage.color = tint;
            }

            if (_mark.gameObject.activeSelf && _pulse) _mark.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(Time.time * 9f));
        }

        private static IEnumerator Grow(RectTransform rect, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Sin(t / seconds * Mathf.PI * 0.5f) * 1.1f;
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private static IEnumerator Bump(RectTransform rect, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(t / seconds * Mathf.PI));
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        // One scoop: the shovel dips towards the hole and comes back.
        private IEnumerator Swing(RectTransform shovel, Vector2 home)
        {
            const float seconds = 0.22f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = Mathf.Sin(t / seconds * Mathf.PI);
                shovel.localRotation = Quaternion.Euler(0f, 0f, 28f * k);
                shovel.anchoredPosition = home + new Vector2(-30f, -45f) * k;
                yield return null;
            }
            shovel.localRotation = Quaternion.identity;
            shovel.anchoredPosition = home;
        }

        // A puff of sand where the shovel went in.
        private IEnumerator Puff(Vector2 position)
        {
            var puff = NewPicture(_field, "Puff", "arcade/prop_pop", new Vector2(240f, 240f), position);
            var image = puff.GetComponent<Image>();
            image.raycastTarget = false;
            const float seconds = 0.4f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                puff.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.1f, k);
                var color = Sand;
                color.a = 1f - k * k;
                image.color = color;
                yield return null;
            }
            Object.Destroy(puff.gameObject);
        }

        // --- End of the game, the one coin --------------------------------------------------------------------

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        // Leaving early still pays the coin if the child found anything (no animation: the screen is going away).
        private void PayIfPlayed()
        {
            if (_paid || _director == null || _director.TotalFound == 0) return;
            _paid = true;
            _game.Progress.AddCoins(TreasureHuntDirector.SessionCoins);
            _game.Commit();
        }

        private IEnumerator EndGame()
        {
            if (!_paid)
            {
                _paid = true;
                _runner.StartCoroutine(PayCoins(TreasureHuntDirector.SessionCoins, _progress.Root.transform.position));
            }
            ClearSpot();
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

        // The beach stays visible behind the end panel; only the progress display, taps and the detector go away.
        private void SetGameEnded(bool ended)
        {
            _pad.gameObject.SetActive(!ended);
            _progress.Root.SetActive(!ended);
            _detector.gameObject.SetActive(!ended);
            _glow.gameObject.SetActive(false);
            _endPanel.SetActive(ended);
        }
    }
}
