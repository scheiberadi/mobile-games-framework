using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // DROP-SORT presenter (answer-variety Prototype B, docs/kids-games/answer-variety-prototypes.md). Only Sorting
    // uses it so far. A round shows 2-3 bins along the top, each a picture of what goes in it, and a belt below
    // that hands over one thing at a time: the current item is large and draggable, the ones still to come wait
    // behind it as small pictures. The child drags the item toward a bin; the bin nearest the finger (within snap
    // range) grows and lights up - a neutral "you are about to drop here" cue that does not say whether it is the
    // right one. Release over the right bin: the item drops in, the bin pops, a small copy of it joins the others
    // inside, the bin's digit goes up by one, and Eva says what it is ("It's a fruit!"). Release over a wrong bin:
    // that bin shakes and the item springs back (a mistake on the help ladder). Release away from every bin: it
    // just springs back, no mistake. A round ends when the belt is empty.
    //
    // Same building blocks as DragToTargetScreen (DragItem, DropGeometry, PointerHand, HelpLadder, CoinPayout,
    // DifficultyLadder, the end panel). The help ladder is per round: 1st mistake a retry, 2nd a hint (the hand
    // carries the current item toward its bin and back), 3rd a demonstration (the hand carries it and drops it for
    // the child); after that further mistakes are just retries. "Clean" means no demonstration fired that round.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): bins (230, decorative, no TapTarget) in a row at y 60, whose top
    // edge (175) clears the Hud's Home button; the current item (240, DragItem = TapTarget) at (-200, -215); the
    // waiting items (90, decorative) in a row at y -385. The companion pair stands bottom-right, clear of all of it.
    //
    // Residents scene (Domestic vs Wild, Land/Sea/Air; a ResidentsLayout and ResidentsScene, each game with its own background
    // picture): the bins are zones of the picture (a pasture, the sky, the water), the child drops the animal anywhere on a zone and
    // the zone under the finger lights up. Only the current animal is shown, waiting at the layout's home. Animals of each kind
    // already stand in the zones, the ones the child sorts join them. A wrong drop makes the animal call out with its real
    // recording and the zone's residents hop, show a "!" and get away, then come back. A finished round: every resident hops
    // for joy and confetti bursts, silently.
    //
    // NOT run on a device: needs a look and the bin/item art (the prototype document lists the 16 images).
    public sealed class DropSortScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxBins = DropSortRoundBuilder.MaxBins;
        private const int MaxItems = DropSortRoundBuilder.MaxItems;
        private const int MaxShownInBin = 3;

        private const float BinSize = 230f;
        private const float ItemSize = EvaUi.MinTap; // 240
        private const float WaitingSize = 90f;
        private const float RowCenterX = -200f;
        private const float BinY = 60f;
        private static readonly Vector2 DefaultItemHome = new Vector2(RowCenterX, -215f);
        private const float WaitingY = -385f;
        private const float WaitingPitch = 110f;
        private static readonly float[] Offsets3 = { -300f, 0f, 300f };
        private static readonly float[] Offsets2 = { -150f, 150f };

        // Inclusive snap range around a bin's centre; under half the 300-unit pitch so ranges never overlap.
        private const float SnapRadius = 150f;
        private const float HoverScale = 1.12f;
        private const float SnapBackSeconds = 0.25f;
        private const float DropSeconds = 0.22f;
        private const float WobbleSeconds = 0.4f;
        private const float NextItemDelaySeconds = 0.3f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<int, System.Random, DropSortRound> _generateRound;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly int _roundsPerSession;
        private readonly string _itemSpritePrefix, _binSpritePrefix;
        private readonly string _promptVoiceKey, _hintVoiceKey, _demoVoiceKey, _itemVoicePrefix;
        private readonly bool _voiceByCategory;
        private readonly ResidentsLayout _layout;

        private Vector2 Home => _layout != null ? _layout.Home : DefaultItemHome;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _binField, _residentField, _waitingField, _itemField;
        private ResidentsScene _scene;
        private bool _reacting;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private RectTransform[] _bins;
        private Image[] _binImages;
        private Image[] _binRings;
        private TextMeshProUGUI[] _binCounts;
        private Image[][] _binFill;
        private int[] _binHolds;
        private WorldPoint[] _binCentres;
        private Image[] _waiting;
        private DragItem _item;
        private Image _itemImage;

        private System.Random _rng;
        private int _roundIndex;
        private DropSortRound _round;
        private int _current;
        private HelpLadder _ladder;
        private bool _roundOver, _helpRunning, _demonstratedThisRound, _placing;
        private int _rightLineIndex;
        private int _hoverBin = -1;
        private Coroutine _hoverRoutine;

        public DropSortScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<int, System.Random, DropSortRound> generateRound,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            int roundsPerSession, string itemSpritePrefix, string binSpritePrefix,
            string promptVoiceKey, string hintVoiceKey, string demoVoiceKey, string itemVoicePrefix, bool voiceByCategory = false, ResidentsLayout layout = null)
        {
            _voiceByCategory = voiceByCategory;
            _layout = layout;
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _generateRound = generateRound;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _roundsPerSession = roundsPerSession;
            _itemSpritePrefix = itemSpritePrefix;
            _binSpritePrefix = binSpritePrefix;
            _promptVoiceKey = promptVoiceKey;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
            _itemVoicePrefix = itemVoicePrefix;
        }

        // The round on screen now (null before the first round) and which item is on the belt. Read-only; exposed
        // so tests can drive the screen.
        public DropSortRound CurrentRound => _round;
        public int CurrentItemIndex => _current;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _binField = CreateFullRectContainer("BinField");
            _residentField = CreateFullRectContainer("ResidentField");
            _waitingField = CreateFullRectContainer("WaitingField");
            _itemField = CreateFullRectContainer("ItemField"); // after the bins, so the dragged item draws above them
            BuildBins();
            BuildWaiting();
            BuildItem();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
            if (_layout != null) _scene = new ResidentsScene(_layout, _runner, _binField, _residentField, Root, _itemSpritePrefix);
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            _scene?.HideConfetti();
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = _generateRound(_getLevel(_game.Progress), _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _helpRunning = false;
            _placing = false;
            _reacting = false;
            _demonstratedThisRound = false;
            _current = 0;
            _scene?.HideConfetti();
            ShowRound(_round);
            ShowCurrentItem();
            _item.enabled = true; // a child moves the first thing at once, never waits for the prompt

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_promptVoiceKey);
            _eva.SetTalking(false);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildBins()
        {
            _bins = new RectTransform[MaxBins];
            _binImages = new Image[MaxBins];
            _binRings = new Image[MaxBins];
            _binCounts = new TextMeshProUGUI[MaxBins];
            _binFill = new Image[MaxBins][];
            _binHolds = new int[MaxBins];
            for (var i = 0; i < MaxBins; i++)
            {
                var go = new GameObject("Bin" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_binField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(BinSize, BinSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
                _bins[i] = rect;
                _binImages[i] = image;

                // A glow ring behind the bin, shown while an item hovers within snap range. It is a child so it
                // moves, scales and shakes with the bin.
                var ringGo = new GameObject("HoverRing", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(go.transform, false);
                ringGo.transform.SetAsFirstSibling();
                var ringRect = (RectTransform)ringGo.transform;
                ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = new Vector2(0.5f, 0.5f);
                ringRect.anchoredPosition = Vector2.zero;
                ringRect.sizeDelta = new Vector2(BinSize * 1.3f, BinSize * 1.3f);
                var ring = ringGo.GetComponent<Image>();
                ring.sprite = EvaUi.Sprite("icons/ring_thin");
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ringGo.SetActive(false);
                _binRings[i] = ring;

                // What the bin holds: up to three small copies just under its bottom edge (so they never hide the bin's
                // own picture), and a digit on a light disc in the top-right corner (dark digits alone vanish on the board).
                _binFill[i] = new Image[MaxShownInBin];
                for (var k = 0; k < MaxShownInBin; k++)
                {
                    var fillGo = new GameObject("Held" + k, typeof(RectTransform), typeof(Image));
                    fillGo.transform.SetParent(go.transform, false);
                    var fillRect = (RectTransform)fillGo.transform;
                    fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = new Vector2(0.5f, 0.5f);
                    fillRect.anchoredPosition = new Vector2((k - (MaxShownInBin - 1) / 2f) * 62f, -BinSize * 0.5f - 20f);
                    fillRect.sizeDelta = new Vector2(70f, 70f);
                    var fill = fillGo.GetComponent<Image>();
                    fill.preserveAspect = true;
                    fill.raycastTarget = false;
                    fillGo.SetActive(false);
                    _binFill[i][k] = fill;
                }

                var discGo = new GameObject("CountDisc", typeof(RectTransform), typeof(Image));
                discGo.transform.SetParent(go.transform, false);
                var discRect = (RectTransform)discGo.transform;
                discRect.anchorMin = discRect.anchorMax = discRect.pivot = new Vector2(0.5f, 0.5f);
                discRect.anchoredPosition = new Vector2(BinSize * 0.5f - 10f, BinSize * 0.5f - 10f);
                discRect.sizeDelta = new Vector2(90f, 90f);
                var disc = discGo.GetComponent<Image>();
                disc.sprite = EvaUi.Sprite("icons/dot");
                disc.color = new Color(1f, 0.97f, 0.88f);
                disc.raycastTarget = false;
                var count = EvaUi.Numeral(discGo.transform, "Count", 64);
                var countRect = (RectTransform)count.transform;
                countRect.anchorMin = countRect.anchorMax = countRect.pivot = new Vector2(0.5f, 0.5f);
                countRect.anchoredPosition = Vector2.zero;
                countRect.sizeDelta = new Vector2(80f, 80f);
                discGo.SetActive(false);
                _binCounts[i] = count;
            }
        }

        private void BuildWaiting()
        {
            _waiting = new Image[MaxItems - 1];
            for (var i = 0; i < _waiting.Length; i++)
            {
                var go = new GameObject("Waiting" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_waitingField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(WaitingSize, WaitingSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0.55f); // greyed: not yet your turn
                _waiting[i] = image;
            }
        }

        private void BuildItem()
        {
            _item = DragItem.Create(_itemField, "sortitem", EvaUi.Sprite("icons/dot"), Home, ItemSize);
            _item.BeginDrag += _ => OnItemBeginDrag();
            _item.EndDrag += _ => OnItemEndDrag();
            _itemImage = _item.GetComponent<Image>();
        }

        private void ShowRound(DropSortRound round)
        {
            StopHoverRoutine();
            var count = round.BinCategories.Length;
            var offsets = count == 3 ? Offsets3 : Offsets2;
            _binCentres = new WorldPoint[count];
            for (var i = 0; i < MaxBins; i++)
            {
                var active = i < count;
                _bins[i].gameObject.SetActive(active);
                _binHolds[i] = 0;
                if (!active) continue;

                var x = _layout != null ? _layout.Gates[i].x : RowCenterX + offsets[i];
                var y = _layout != null ? _layout.Gates[i].y : BinY;
                _bins[i].anchoredPosition = new Vector2(x, y);
                _bins[i].localScale = Vector3.one;
                _bins[i].localRotation = Quaternion.identity;
                _binImages[i].sprite = EvaUi.Sprite(_binSpritePrefix + round.BinCategories[i]);
                _binImages[i].color = Color.white;
                _binImages[i].enabled = _layout == null; // a zone of the picture is the bin: nothing is drawn for it
                _binRings[i].gameObject.SetActive(false);
                _binCounts[i].transform.parent.gameObject.SetActive(false);
                _binCounts[i].text = "0";
                foreach (var held in _binFill[i]) held.gameObject.SetActive(false);
                _binCentres[i] = new WorldPoint(x, y);
            }
            _scene?.BeginRound(round);
        }

        // Puts the next item on the belt (or finishes the round when the belt is empty) and re-lays the waiting row.
        private void ShowCurrentItem()
        {
            var total = _round.ItemIds.Length;
            if (_current >= total)
            {
                _item.gameObject.SetActive(false);
                LayWaiting(total);
                return;
            }
            _item.gameObject.SetActive(true);
            _item.Rect.anchoredPosition = Home;
            _item.Rect.sizeDelta = new Vector2(ItemSize, ItemSize);
            _item.Rect.localScale = Vector3.one;
            _itemImage.sprite = EvaUi.Sprite(_itemSpritePrefix + _round.ItemIds[_current]);
            _itemImage.color = Color.white;
            _itemImage.raycastTarget = true;
            LayWaiting(total);
            _runner.StartCoroutine(PopIn(_item.Rect, 0.2f));
        }

        private void LayWaiting(int total)
        {
            if (_layout != null) total = 0; // only the current animal is shown, not who comes next
            var waiting = Mathf.Max(0, total - _current - 1);
            for (var i = 0; i < _waiting.Length; i++)
            {
                var active = i < waiting;
                _waiting[i].gameObject.SetActive(active);
                if (!active) continue;
                var x = RowCenterX + (i - (waiting - 1) / 2f) * WaitingPitch;
                ((RectTransform)_waiting[i].transform).anchoredPosition = new Vector2(x, WaitingY);
                _waiting[i].sprite = EvaUi.Sprite(_itemSpritePrefix + _round.ItemIds[_current + 1 + i]);
            }
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void OnItemBeginDrag()
        {
            if (_round == null || _roundOver || _helpRunning || _placing || _reacting) return;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop());
        }

        // While the item is held, the nearest bin in snap range grows; every other bin rests.
        private IEnumerator HoverLoop()
        {
            while (true)
            {
                var position = _item.Rect.anchoredPosition;
                var nearest = BinAt(position);
                if (nearest != _hoverBin) SetHover(nearest);
                yield return null;
            }
        }

        // The bin an item at `position` would be dropped in, or -1. The residents scene takes a whole pasture; the other games
        // take the nearest bin within snap range.
        private int BinAt(Vector2 position)
        {
            if (_scene == null) return DropGeometry.NearestWithinRadius(position.x, position.y, _binCentres, SnapRadius);
            var zone = _scene.ZoneAt(position);
            return zone < _binCentres.Length ? zone : -1;
        }

        private void SetHover(int bin)
        {
            _hoverBin = bin;
            if (_scene != null)
            {
                _scene.SetHover(bin);
                return;
            }
            for (var i = 0; i < _binCentres.Length; i++)
            {
                var hovered = i == bin;
                _bins[i].localScale = Vector3.one * (hovered ? HoverScale : 1f);
                _binRings[i].gameObject.SetActive(hovered);
            }
        }

        private void StopHoverRoutine()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hoverBin = -1;
        }

        private void StopHover()
        {
            StopHoverRoutine();
            _scene?.HideGlow();
            if (_bins == null) return;
            for (var i = 0; i < MaxBins; i++)
            {
                if (!_bins[i].gameObject.activeSelf) continue;
                _bins[i].localScale = Vector3.one;
                _binRings[i].gameObject.SetActive(false);
            }
        }

        private void OnItemEndDrag()
        {
            StopHover();
            if (_round == null || _roundOver || _helpRunning || _placing || _reacting || _current >= _round.ItemIds.Length) return;

            var position = _item.Rect.anchoredPosition;
            var bin = BinAt(position);
            if (bin < 0) { SnapBack(); return; } // dropped in empty space: not an attempt, no mistake

            if (bin == _round.BinIndexOf(_current)) _runner.StartCoroutine(PlaceCurrent());
            else
            {
                if (_scene != null) _runner.StartCoroutine(WrongInScene(bin));
                else
                {
                    _runner.StartCoroutine(Wobble(_bins[bin], WobbleSeconds));
                    SnapBack();
                    HandleMistake();
                }
            }
        }

        private void SnapBack() => _runner.StartCoroutine(SlideTo(_item.Rect, Home, SnapBackSeconds));

        // The item drops into its bin: it slides in while shrinking, the bin pops and takes a small copy and a
        // count, Eva says what it is, and the next item arrives (or the round ends).
        private IEnumerator PlaceCurrent()
        {
            _placing = true;
            _item.enabled = false;
            _itemImage.raycastTarget = false;
            var index = _current;
            var bin = _round.BinIndexOf(index);

            var from = _item.Rect.anchoredPosition;
            var to = _scene != null ? _scene.NextSlotPosition(bin) : _bins[bin].anchoredPosition;
            for (var t = 0f; t < DropSeconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / DropSeconds);
                _item.Rect.anchoredPosition = Vector2.Lerp(from, to, k);
                _item.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
                yield return null;
            }
            _item.gameObject.SetActive(false);

            _game.Sfx.Coin();
            AddToBin(bin, _round.ItemIds[index]);
            _runner.StartCoroutine(PopPulse(_bins[bin], 1.15f, 0.25f));
            if (_scene != null)
            {
                // The residents games: Eva stays quiet (her line about the last animal would still be running when the next one
                // arrives) and the animal answers with its own sound.
                _game.Voice.Stop();
                _game.Sfx.PlayAnimal(_round.ItemIds[index]);
            }
            else _game.Voice.Say(_itemVoicePrefix + (_voiceByCategory ? _round.ItemCategories[index] : _round.ItemIds[index]));

            _current++;
            _placing = false;
            if (_current >= _round.ItemIds.Length)
            {
                ShowCurrentItem();
                _runner.StartCoroutine(OnRoundComplete());
                yield break;
            }

            yield return new WaitForSeconds(NextItemDelaySeconds);
            if (_roundOver) yield break;
            ShowCurrentItem();
            if (!_helpRunning) _item.enabled = true;
        }

        private void AddToBin(int bin, string itemId)
        {
            var held = _binHolds[bin]++;
            if (_scene != null)
            {
                _scene.Add(bin, itemId);
                return;
            }
            if (held < MaxShownInBin)
            {
                _binFill[bin][held].sprite = EvaUi.Sprite(_itemSpritePrefix + itemId);
                _binFill[bin][held].gameObject.SetActive(true);
            }
            _binCounts[bin].text = _binHolds[bin].ToString();
            _binCounts[bin].transform.parent.gameObject.SetActive(true);
        }

        // --- Help ladder -------------------------------------------------------------------------------------

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _eva.Angry();
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    if (_demonstratedThisRound) { _game.Sfx.Retry(); _game.Voice.Say("count_retry"); }
                    else _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the hand grabs the current item, carries it to its bin, then back; nothing is placed.
        private IEnumerator RunHint()
        {
            if (_current >= _round.ItemIds.Length) yield break;
            _helpRunning = true;
            _item.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);

            var target = _bins[_round.BinIndexOf(_current)].anchoredPosition;
            yield return Carry(Home, target, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(target, Home, HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            if (!_roundOver && !_placing) _item.enabled = true;
        }

        // 3rd mistake: the hand carries the current item to its bin and drops it there (placed for the child).
        private IEnumerator RunDemonstrate()
        {
            if (_current >= _round.ItemIds.Length) yield break;
            _helpRunning = true;
            _demonstratedThisRound = true;
            _item.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) * 0.3f);

            var target = _bins[_round.BinIndexOf(_current)].anchoredPosition;
            yield return Carry(Home, target, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            yield return PlaceCurrent();
        }

        // The hand glides to the item, "grabs" it, then moves with it to `to`, fingertip on the item's centre.
        private IEnumerator Carry(Vector2 from, Vector2 to, float seconds)
        {
            var rect = _item.Rect;
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from; // any snap-back slide has finished by now; start the carry exactly here
            rect.SetAsLastSibling();
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
        }

        // --- Round and session end ---------------------------------------------------------------------------

        private IEnumerator OnRoundComplete()
        {
            _roundOver = true;
            _item.enabled = false;
            _hand.Hide();

            var clean = !_demonstratedThisRound;

            if (_scene != null) _scene.Celebrate(); // the win fanfare, a silent burst of confetti and every animal hops
            else _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            // The finished scene: every bin that took something pops together.
            for (var i = 0; i < _round.BinCategories.Length; i++)
                _runner.StartCoroutine(PopPulse(_bins[i], 1.2f, 0.35f));

            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _bins[0].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= _roundsPerSession) yield return EndSession();
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
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));

            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _binField.gameObject.SetActive(!ended);
            _residentField.gameObject.SetActive(!ended);
            if (ended) _scene?.HideConfetti();
            _waitingField.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Residents scene ---------------------------------------------------------------------------------

        // A wrong drop: the animal calls out with its own recording, the zone's residents get away (they come back on their own), the
        // item goes back to its place (sliding, falling or sinking), and then the help ladder goes on as usual.
        private IEnumerator WrongInScene(int bin)
        {
            _reacting = true;
            _item.enabled = false;
            var length = _game.Sfx.PlayAnimal(_round.ItemIds[_current]);
            if (length <= 0f) _game.Sfx.Retry();
            _scene.ScareAll(bin);
            yield return _runner.StartCoroutine(_scene.ReturnItem(_item.Rect, _itemImage, bin)); // only the item's trip home holds it, not the animal's sound
            _reacting = false;
            if (_roundOver) yield break;
            HandleMistake();
            if (!_helpRunning && !_placing) _item.enabled = true;
        }

        // --- Eva ---------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens ------------------------------------------------------------------------------------

        private static IEnumerator SlideTo(RectTransform target, Vector2 to, float seconds)
        {
            var from = target.anchoredPosition;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                target.anchoredPosition = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                yield return null;
            }
            target.anchoredPosition = to;
        }

        private static IEnumerator PopIn(RectTransform target, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Scales the target up to `peak` times its current scale and back (its own scale at call time is the base).
        private static IEnumerator PopPulse(RectTransform target, float peak, float duration)
        {
            var baseScale = target.localScale;
            var half = duration * 0.5f;
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

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                var angle = Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay;
                target.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            target.localRotation = Quaternion.identity;
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

        // --- Helpers -----------------------------------------------------------------------------------------

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

        private void AddPictureBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(_backgroundSprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
