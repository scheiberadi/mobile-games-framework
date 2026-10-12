using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Day/Night: "Eva's day". The meadow is in the day or in the night (the whole picture; stars twinkle at night, the sun's
    // flowers and critters show by day) and Eva asks what she does then ("It is night! What do I do at night?"). Four round activity
    // cards lie along the bottom, two for the asked time and two for the other. The child drags the right ones onto the meadow: each flies
    // up into a little row in the sky. A wrong one goes back to the tray and Eva says when she really does it. Rules: Rules/DayNight.cs;
    // this draws them.
    //
    // Two wrong cards in a row, or a long quiet: the hand carries a right card onto the meadow (the only help; the coin is then Assisted).
    //
    // Layout in "picture space" like MakeWeatherScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py).
    public sealed class DayNightScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Card
        {
            public int Index;
            public DayNightActivity Activity;
            public DragItem Drag;
            public RectTransform Art;
            public Image ArtImage;
            public Vector2 Home;
            public bool Held, Flying;
            public float Wobble;
        }

        private sealed class Star
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home;
            public float Phase;
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);      // her feet, left of the tree
        public const float EvaHeight = 420f;
        public static readonly Vector2 MeadowCentre = new Vector2(-100f, 40f);    // where the hint hand carries the card
        public const float MeadowBottom = -190f;                                  // a card let go above this line is on the meadow
        public const float CardTapSize = 240f;
        private const float CardArt = 200f;
        private const float TrayY = -345f;
        private const float TrayStep = 300f;
        private const float ReturnSpeed = 2600f;
        private const float TimeFadeSeconds = 1.4f;
        private const float PlacedSize = 170f;
        private const float FlySeconds = 0.6f;
        private static readonly Vector2[] PlacedSlots = { new Vector2(-130f, 330f), new Vector2(130f, 330f) };

        // Twinkling stars of the night, clear of the moon (upper right), the Back button and the tree crown (x -550..100, up to y 370).
        private static readonly Vector2[] StarSpots =
        {
            new Vector2(-420f, 415f), new Vector2(-870f, 330f), new Vector2(-60f, 400f), new Vector2(90f, 250f), new Vector2(250f, 370f),
            new Vector2(380f, 280f), new Vector2(-770f, 420f), new Vector2(190f, 170f), new Vector2(330f, 180f), new Vector2(480f, 400f),
        };

        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };
        private const float MusicLevel = 0.3f;

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _dayBackground, _nightBackground;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _trayField, _placedField, _starField;
        private Image _backFade, _frontFade;
        private float _fade = 1f;
        private Sprite _daySprite, _nightSprite;
        private readonly List<Card> _cards = new List<Card>();
        private readonly List<Image> _placed = new List<Image>();
        private readonly List<Star> _stars = new List<Star>();
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private DayNightRound _round;
        private DayNightRound[] _session;
        private TimeOfDay _shown = TimeOfDay.Day;
        private int _roundIndex, _rightLineIndex, _flying;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time, _night;      // _night 0..1: how far the night's stars show
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;
        private SunnyLife _sunny;
        private float _life;                    // 0..1 how much of the sunny life shows (the day only)

        public DayNightScreen(ScreenId selfId, ScreenId homeScreenId, string dayBackground, string nightBackground)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _dayBackground = dayBackground;
            _nightBackground = nightBackground;
        }

        // Read-only state for tests.
        public DayNightRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public TimeOfDay ShownTime => _shown;
        public int PlacedBadges => _placed.FindAll(p => p.gameObject.activeSelf).Count;
        public IReadOnlyList<DragItem> CardDrags
        {
            get { var list = new List<DragItem>(); foreach (var card in _cards) list.Add(card.Drag); return list; }
        }

        // The card's middle, in picture space; PlaceCard puts it there.
        public Vector2 CardPoint(int index) => Scene(_cards[index].Drag.Rect.anchoredPosition);
        public void PlaceCard(int index, Vector2 point) => _cards[index].Drag.Rect.anchoredPosition = Place(point);

        public static bool UseDevicePlacement = true;
        private float _scaleX = 1f;
        private Vector2 _shift;

        private Vector2 Place(Vector2 scene) => new Vector2(scene.x * _scaleX - _shift.x, scene.y);

        private Vector2 Scene(Vector2 local) => new Vector2((local.x + _shift.x) / _scaleX, local.y);

        private void UpdatePlacement()
        {
            _scaleX = 1f;
            _shift = Vector2.zero;
            var canvas = UseDevicePlacement ? Root.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var size = ((RectTransform)canvas.rootCanvas.transform).rect.size;
                if (size.x > 0f) _scaleX = size.x / PictureWidth;
                _shift = FullBleed.CentreShift(FullBleed.CurrentInsets(Root));
            }
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            _daySprite = EvaUi.Sprite(_dayBackground);
            _nightSprite = EvaUi.Sprite(_nightBackground);
            AddPictureBackground();
            BuildEva();
            _sunny = new SunnyLife(Root, Place, float.NegativeInfinity, SeasonsScreen.FlowerSpots);
            _starField = Container("Stars");
            BuildStars();
            _placedField = Container("Placed");
            BuildPlaced();
            _trayField = Container("Tray");
            BuildTray();
            _hand = new PointerHand(Root, _runner);
            BuildConfetti();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            if (_game.Music != null) _game.Music.Level = MusicLevel;
            UpdatePlacement();
            StartNewSession();
        }

        private void StartNewSession()
        {
            _session = DayNight.CreateSession(new System.Random());
            _roundIndex = 0;
            _rightLineIndex = 0;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StopAllCoroutines();
            _runner.StartCoroutine(Loop());
            StartRound();
        }

        private void StartRound()
        {
            _round = _session[_roundIndex];
            ShowTime(_round.Asked, _roundIndex == 0);
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _flying = 0;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            foreach (var badge in _placed) badge.gameObject.SetActive(false);
            foreach (var card in _cards)
            {
                card.Activity = _round.Cards[card.Index];
                card.ArtImage.sprite = EvaUi.Sprite("sciencelab/daynightcard_" + card.Activity.Id);
                card.Drag.gameObject.SetActive(true);
                card.Held = false;
                card.Flying = false;
                card.Wobble = 0f;
                card.Drag.Rect.anchoredPosition = Place(card.Home);
                card.Art.localScale = Vector3.one;
                card.Art.localRotation = Quaternion.identity;
            }
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("dn_ask_" + DayNight.IdOf(_round.Asked));
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            var go = new GameObject("EvaAnchor", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _evaAnchor = (RectTransform)go.transform;
            _evaAnchor.anchorMin = _evaAnchor.anchorMax = _evaAnchor.pivot = new Vector2(0.5f, 0.5f);
            _evaAnchor.sizeDelta = Vector2.zero;
            _eva = RigFactory.CreateEva(_evaAnchor, EvaHeight);
            _evaBaseScale = _eva.Root.localScale;
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private void BuildStars()
        {
            var sprite = EvaUi.Sprite("sciencelab/senseeffect_sparkle");
            for (var i = 0; i < StarSpots.Length; i++)
            {
                var go = new GameObject("Star" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_starField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(64f, 64f);
                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0f);
                _stars.Add(new Star { Rect = rect, Image = image, Home = StarSpots[i], Phase = i * 1.3f });
            }
        }

        // The little row in the sky where the right cards end up.
        private void BuildPlaced()
        {
            for (var i = 0; i < DayNight.RightPerRound; i++)
            {
                var go = new GameObject("Placed" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_placedField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(PlacedSize, PlacedSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                go.SetActive(false);
                _placed.Add(image);
            }
        }

        private void BuildTray()
        {
            var first = -TrayStep * (DayNight.CardsPerRound - 1) * 0.5f;
            for (var i = 0; i < DayNight.CardsPerRound; i++)
            {
                var card = new Card { Index = i, Home = new Vector2(first + i * TrayStep, TrayY) };
                card.Drag = DragItem.Create(_trayField, "Card" + i, EvaUi.Sprite("icons/dot"), Place(card.Home), CardTapSize);
                card.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = card;
                card.Drag.BeginDrag += _ => OnBeginDrag(captured);
                card.Drag.EndDrag += _ => OnEndDrag(captured);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(card.Drag.transform, false);
                card.Art = (RectTransform)go.transform;
                card.Art.anchorMin = card.Art.anchorMax = card.Art.pivot = new Vector2(0.5f, 0.5f);
                card.Art.anchoredPosition = Vector2.zero;
                card.Art.sizeDelta = new Vector2(CardArt, CardArt);
                card.ArtImage = go.GetComponent<Image>();
                card.ArtImage.preserveAspect = true;
                card.ArtImage.raycastTarget = false;
                _cards.Add(card);
            }
        }

        private void BuildConfetti()
        {
            // An inactive WinCelebration: switching it on plays the fanfare and bursts the confetti.
            _confetti = new GameObject("RoundConfetti", typeof(RectTransform), typeof(WinCelebration));
            _confetti.transform.SetParent(Root, false);
            var rect = (RectTransform)_confetti.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _confetti.SetActive(false);
        }

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(WinCelebration));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));
            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _starField.gameObject.SetActive(!ended);
            _placedField.gameObject.SetActive(!ended);
            _sunny.SetActive(!ended);
            _trayField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The time ---------------------------------------------------------------------------------------------------

        // The meadow changes into this time: at once when the game starts, cross-faded afterwards.
        private void ShowTime(TimeOfDay time, bool snap)
        {
            var sprite = time == TimeOfDay.Day ? _daySprite : _nightSprite;
            if (snap)
            {
                _backFade.sprite = sprite;
                _frontFade.sprite = sprite;
                _frontFade.color = new Color(1f, 1f, 1f, 0f);
                _fade = 1f;
                _night = time == TimeOfDay.Night ? 1f : 0f;
                _life = time == TimeOfDay.Day ? 1f : 0f;
            }
            else if (time != _shown)
            {
                if (_fade >= 0.5f) _backFade.sprite = _shown == TimeOfDay.Day ? _daySprite : _nightSprite;
                _frontFade.sprite = sprite;
                _frontFade.color = new Color(1f, 1f, 1f, 0f);
                _fade = 0f;
            }
            _shown = time;
        }

        // --- The cards --------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Card card)
        {
            if (_roundOver || card.Flying) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a card never waits for the hint hand
            card.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(Card card)
        {
            card.Held = false;
            _idle = 0f;
            if (_roundOver || card.Flying) return;
            if (Scene(card.Drag.Rect.anchoredPosition).y >= MeadowBottom) DropCard(card);
        }

        // The card was let go on the meadow (also what the hint does).
        private void DropCard(Card card)
        {
            var result = _round.Drop(card.Index);
            Haptics.Tap();
            if (result == DayNightDrop.Right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                Say("dn_good_" + card.Activity.Id);
                _runner.StartCoroutine(FlyUp(card, _round.PlacedCount - 1));
                return;
            }
            if (result == DayNightDrop.Wrong)
            {
                card.Wobble = 0.4f;
                _runner.StartCoroutine(AfterWrong(card.Activity));
            }
        }

        // A card of the other time: Eva says when she does it, then asks again (or the hand helps).
        private IEnumerator AfterWrong(DayNightActivity activity)
        {
            Say("dn_bad_" + activity.Id);
            while (_announcing) yield return null;
            if (_round.Done) yield break;
            if (_round.NeedsHint) StartHint();
            else Say("dn_ask_" + DayNight.IdOf(_round.Asked));
        }

        // The right card flies up into its place in the sky and stays there as a little badge.
        private IEnumerator FlyUp(Card card, int slot)
        {
            card.Flying = true;
            _flying++;
            var from = card.Drag.Rect.anchoredPosition;
            var to = Place(PlacedSlots[Mathf.Clamp(slot, 0, PlacedSlots.Length - 1)]);
            for (var t = 0f; t < FlySeconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / FlySeconds);
                card.Drag.Rect.anchoredPosition = Vector2.Lerp(from, to, k);
                card.Art.localScale = Vector3.one * Mathf.Lerp(1f, PlacedSize / CardArt, k);
                yield return null;
            }
            card.Drag.gameObject.SetActive(false);
            var badge = _placed[Mathf.Clamp(slot, 0, _placed.Count - 1)];
            badge.sprite = card.ArtImage.sprite;
            badge.rectTransform.anchoredPosition = to;
            badge.gameObject.SetActive(true);
            _flying--;
        }

        // --- The loop ---------------------------------------------------------------------------------------------------

        private IEnumerator Loop()
        {
            while (true)
            {
                Tick(Time.deltaTime);
                yield return null;
            }
        }

        // One frame. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_round == null || _sessionEnded) return;
            _time += seconds;
            foreach (var card in _cards) UpdateCard(card, seconds);
            UpdateMeadow(seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdateCard(Card card, float seconds)
        {
            if (card.Flying) return;
            if (card.Wobble > 0f)
            {
                card.Wobble = Mathf.Max(0f, card.Wobble - seconds);
                card.Art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(card.Wobble * 50f) * 14f * card.Wobble * 2f);
            }
            else
            {
                card.Art.localRotation = Quaternion.Euler(0f, 0f, card.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
            }
            card.Art.localScale = Vector3.one * (card.Held ? 1.12f : 1f);
            if (!card.Held && !_helpRunning)
            {
                var target = Place(card.Home);
                var position = card.Drag.Rect.anchoredPosition;
                if (position != target) card.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
            }
        }

        private void UpdateMeadow(float seconds)
        {
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + seconds / TimeFadeSeconds);
                _frontFade.color = new Color(1f, 1f, 1f, _fade);
            }
            _night = Mathf.MoveTowards(_night, _shown == TimeOfDay.Night ? 1f : 0f, seconds / 1.4f);
            _life = Mathf.MoveTowards(_life, _shown == TimeOfDay.Day ? 1f : 0f, seconds / 1.5f);
            _sunny.Show(_life, _time);
            foreach (var star in _stars)
            {
                star.Rect.anchoredPosition = Place(star.Home);
                var twinkle = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(_time * 1.6f + star.Phase));
                star.Image.color = new Color(1f, 1f, 1f, _night * twinkle);
                star.Rect.localScale = Vector3.one * (0.7f + 0.3f * twinkle);
            }
            _evaAnchor.anchoredPosition = Place(EvaPoint);
        }

        // --- Voice ------------------------------------------------------------------------------------------------------

        private void Say(string key)
        {
            if (_announceQueue.Count >= 3) return; // never a pile of lines behind the child
            _announceQueue.Enqueue(key);
            if (!_announcing) _runner.StartCoroutine(AnnounceLoop());
        }

        private IEnumerator AnnounceLoop()
        {
            _announcing = true;
            _eva.SetTalking(true);
            while (_announceQueue.Count > 0)
            {
                var key = _announceQueue.Dequeue();
                yield return _game.Voice.SayAndWait(key);
            }
            _eva.SetTalking(false);
            _announcing = false;
        }

        // --- Hint -------------------------------------------------------------------------------------------------------

        private void UpdateIdle(float seconds)
        {
            if (_roundOver || _helpRunning) { _idle = 0f; return; }
            foreach (var card in _cards) if (card.Held) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        private void StartHint()
        {
            if (_helpRunning || _roundOver || _round.NextRight() < 0) return;
            _idle = 0f;
            _round.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint());
        }

        // The child took hold of a card while the hint hand was out: the hint stops and the card goes home.
        private void CancelHint()
        {
            if (_hintRoutine != null) _runner.StopCoroutine(_hintRoutine);
            _hintRoutine = null;
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _game.Voice.Stop();
            _announceQueue.Clear();
            _announcing = false;
            _helpRunning = false;
            _idle = 0f;
        }

        // The hand carries a right card onto the meadow.
        private IEnumerator RunHint()
        {
            var card = _cards[_round.NextRight()];
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("dressforweather_hint");

            var from = card.Drag.Rect.anchoredPosition;
            var to = Place(MeadowCentre);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                card.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            card.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            DropCard(card);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_round.Done || _announcing || _flying > 0) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
            yield return new WaitForSeconds(0.6f); // a moment to look at the badges
            _confetti.SetActive(true); // the fanfare and a burst of confetti
            Haptics.Win();
            _eva.Cheer();
            _rightLineIndex = _rightLineIndex % 3 + 1;
            var payout = _hinted ? CoinPayout.Assisted : CoinPayout.Clean;
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            _runner.StartCoroutine(AnimateCoins(before, before + payout));
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= DayNight.RoundsPerSession)
            {
                yield return _game.Voice.SayAndWait("count_done");
                _eva.Cheer();
                SetSessionEnded(true);
                yield return _game.Voice.SayAndWait("count_again");
            }
            else StartRound();
        }

        private IEnumerator AnimateCoins(int before, int after)
        {
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _evaAnchor.position);
        }

        // --- Small helpers ----------------------------------------------------------------------------------------------

        private RectTransform Container(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void AddPictureBackground()
        {
            _frontFade = BackgroundLayer("BackgroundFade");
            _backFade = BackgroundLayer("Background");
            _backFade.sprite = _daySprite;
            _frontFade.sprite = _daySprite;
            _frontFade.color = new Color(1f, 1f, 1f, 0f);
        }

        // Each new layer goes to the very back, so the one made last is the bottom one.
        private Image BackgroundLayer(string name)
        {
            var background = new GameObject(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }
    }
}
