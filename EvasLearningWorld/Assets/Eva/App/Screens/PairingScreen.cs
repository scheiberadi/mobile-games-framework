using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // What a pairing game needs from its game: the pairs, the pictures, Eva's lines and how a wrong drop reacts.
    public sealed class PairingConfig
    {
        public IReadOnlyList<(string Id, string Key)> Pairs;
        public string ItemSpritePrefix;      // + the item's id
        public string TargetSpritePrefix;    // + the target's key
        public Func<string, string> TargetSprite;  // the target's picture by its key, instead of TargetSpritePrefix (optional)
        public int Slots = PairingGame.MaxSlots;  // how many things stand on screen at once (3 or 4)
        public int PairsPerGame;             // how many of the pairs one game uses (0 = all)
        public string PromptKey, HintKey, DemoKey;
        public Func<string, string, bool> Confusable;                  // wrong but believable pairs, kept apart (optional)
        public Func<string, string, PairReaction> WrongReaction;       // (item id, target key) -> what the item does there
    }

    // PAIRING presenter: "take each thing to its partner", used by Zoo & Farm's Mother (baby to its mother) and Habitat
    // (animal to its habitat). Four targets stand in a row at the top, four draggable items in a row below. The child drags
    // an item with a finger to a target; the target nearest the finger grows and lights up (a neutral cue that does not
    // say whether it is right). On a match the item walks into the target, the two hop for joy, the animal's own sound
    // plays, the item (and the target, when nothing else belongs to it) disappears and new ones take their places. There
    // are no levels: the game runs until every pair has been made (PairingGame), then pays 1 coin and ends. On a wrong drop
    // the item reacts (see PairReaction), returns to the child, and the mistake climbs the help ladder: 2nd in a row a hint
    // (the hand carries a matching item to its target and back), 3rd a demonstration (the hand makes the match). Release
    // away from every target: the item just springs back, no mistake.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): targets (230, decorative) at y 60 clear of the Hud's Home button;
    // items (240, DragItem = TapTarget) at y -230; same x positions as DragToTargetScreen; Eva bottom-right.
    public sealed class PairingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private int Slots => _config.Slots;
        private const float TargetSize = 230f;
        private const float ItemSize = EvaUi.MinTap; // 240
        private const float RowCenterX = -200f;
        private const float TargetY = 60f;
        private const float ItemY = -230f;
        private static readonly float[] FourOffsets = { -390f, -130f, 130f, 390f };
        private static readonly float[] ThreeOffsets = { -300f, 0f, 300f };
        private float[] Offsets => Slots == 3 ? ThreeOffsets : FourOffsets;
        private static readonly Vector2 VisitOffset = new Vector2(0f, -35f);
        private const float VisitScale = 0.62f;

        private const float SnapRadius = 130f;
        private const float HoverScale = 1.1f;
        private const float SnapBackSeconds = 0.25f;
        private const float ArriveSeconds = 0.3f;
        private const float HopSeconds = 0.3f;
        private const float ReactionSeconds = 1.1f;
        private const float MaxSoundWait = 1.8f;
        private const float VanishSeconds = 0.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;
        private const int BubbleCount = 5;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly PairingConfig _config;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _targetField, _itemField;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private RectTransform[] _targets;
        private Image[] _targetImages;
        private Image[] _targetRings;
        private DragItem[] _items;
        private Image[] _itemImages;
        private Image[] _bubbles;
        private WorldPoint[] _targetCentres;

        // What the slots show right now, to see what changed after the game refilled them.
        private string[] _shownItem;
        private string[] _shownTarget;

        private System.Random _rng;
        private PairingGame _pairs;
        private int _mistakes;
        private bool _busy, _over;
        private int _dragging = -1;
        private int _hoverTarget = -1;
        private Coroutine _hoverRoutine;

        public PairingScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite, PairingConfig config)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _config = config;
        }

        // Read-only state, exposed so tests can drive the screen.
        public PairingGame Game => _pairs;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _targetField = CreateFullRectContainer("TargetField");
            _itemField = CreateFullRectContainer("ItemField"); // after the targets, so dragged items draw above them
            BuildTargets();
            BuildBubbles();
            BuildItems();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewGame();
        }

        private void StartNewGame()
        {
            _runner.StopAllCoroutines();
            _rng = new System.Random();
            _pairs = new PairingGame(PairingSession.Pick(_config.Pairs, _config.PairsPerGame, _rng), _rng, _config.Confusable, _config.Slots);
            _mistakes = 0;
            _busy = false;
            _over = false;
            _dragging = -1;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);
            _shownItem = new string[Slots];
            _shownTarget = new string[Slots];
            Sync();
            _runner.StartCoroutine(Intro());
        }

        private IEnumerator Intro()
        {
            SetItemsEnabled(false);
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_config.PromptKey);
            _eva.SetTalking(false);
            if (!_busy && !_over) SetItemsEnabled(true);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildTargets()
        {
            _targets = new RectTransform[Slots];
            _targetImages = new Image[Slots];
            _targetRings = new Image[Slots];
            _targetCentres = new WorldPoint[Slots];
            for (var i = 0; i < Slots; i++)
            {
                var x = RowCenterX + Offsets[i];
                var go = new GameObject("Target" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_targetField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, TargetY);
                rect.sizeDelta = new Vector2(TargetSize, TargetSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
                _targets[i] = rect;
                _targetImages[i] = image;
                _targetCentres[i] = new WorldPoint(x, TargetY);

                var ringGo = new GameObject("HoverRing", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(go.transform, false);
                ringGo.transform.SetAsFirstSibling();
                var ringRect = (RectTransform)ringGo.transform;
                ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = new Vector2(0.5f, 0.5f);
                ringRect.anchoredPosition = Vector2.zero;
                ringRect.sizeDelta = new Vector2(TargetSize * 1.3f, TargetSize * 1.3f);
                var ring = ringGo.GetComponent<Image>();
                ring.sprite = EvaUi.Sprite("icons/ring_thin");
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ringGo.SetActive(false);
                _targetRings[i] = ring;
                go.SetActive(false);
            }
        }

        // Small light-blue dots that rise off a sinking animal.
        private void BuildBubbles()
        {
            _bubbles = new Image[BubbleCount];
            for (var i = 0; i < BubbleCount; i++)
            {
                var go = new GameObject("Bubble" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_itemField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(34f, 34f);
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/dot");
                image.color = new Color(0.7f, 0.9f, 1f, 0.85f);
                image.raycastTarget = false;
                go.SetActive(false);
                _bubbles[i] = image;
            }
        }

        private Vector2 ItemHome(int slot) => new Vector2(RowCenterX + Offsets[slot], ItemY);

        private void BuildItems()
        {
            _items = new DragItem[Slots];
            _itemImages = new Image[Slots];
            for (var i = 0; i < Slots; i++)
            {
                var slot = i;
                var item = DragItem.Create(_itemField, "pairitem" + i, EvaUi.Sprite("icons/dot"), ItemHome(i), ItemSize);
                item.BeginDrag += _ => OnBeginDrag(slot);
                item.EndDrag += _ => OnEndDrag(slot);
                _items[i] = item;
                _itemImages[i] = item.GetComponent<Image>();
                item.gameObject.SetActive(false);
            }
        }

        // Shows what the game holds in every slot; slots that changed pop in (or disappear).
        private void Sync()
        {
            for (var i = 0; i < Slots; i++)
            {
                var itemId = _pairs.Items[i]?.Id;
                if (itemId != _shownItem[i])
                {
                    _shownItem[i] = itemId;
                    var item = _items[i];
                    item.gameObject.SetActive(itemId != null);
                    if (itemId != null)
                    {
                        item.Rect.anchoredPosition = ItemHome(i);
                        item.Rect.sizeDelta = new Vector2(ItemSize, ItemSize);
                        item.Rect.localRotation = Quaternion.identity;
                        _itemImages[i].sprite = EvaUi.Sprite(_config.ItemSpritePrefix + itemId);
                        _itemImages[i].color = Color.white;
                        _itemImages[i].raycastTarget = true;
                        _runner.StartCoroutine(PopIn(item.Rect, 0.25f));
                    }
                }

                var key = _pairs.Targets[i];
                if (key != _shownTarget[i])
                {
                    _shownTarget[i] = key;
                    _targets[i].gameObject.SetActive(key != null);
                    if (key != null)
                    {
                        _targets[i].localRotation = Quaternion.identity;
                        _targets[i].anchoredPosition = new Vector2(_targetCentres[i].X, _targetCentres[i].Y);
                        _targetImages[i].sprite = EvaUi.Sprite(_config.TargetSprite != null ? _config.TargetSprite(key) : _config.TargetSpritePrefix + key);
                        _targetRings[i].gameObject.SetActive(false);
                        _runner.StartCoroutine(PopIn(_targets[i], 0.25f));
                    }
                }
            }
            foreach (var bubble in _bubbles) bubble.gameObject.SetActive(false);
        }

        private void SetItemsEnabled(bool enabled)
        {
            for (var i = 0; i < Slots; i++)
                if (_items[i] != null) _items[i].enabled = enabled && _shownItem != null && _shownItem[i] != null;
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void OnBeginDrag(int slot)
        {
            if (_busy || _over) return;
            _dragging = slot;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop(slot));
        }

        // While an item is held, the nearest target in snap range grows; every other target rests.
        private IEnumerator HoverLoop(int slot)
        {
            while (true)
            {
                var position = _items[slot].Rect.anchoredPosition;
                var nearest = NearestTarget(position);
                if (nearest != _hoverTarget) SetHover(nearest);
                yield return null;
            }
        }

        private int NearestTarget(Vector2 position)
        {
            // Only targets that are showing count: an empty slot sits far away from every drop.
            var centres = new WorldPoint[Slots];
            for (var i = 0; i < Slots; i++)
                centres[i] = _shownTarget[i] != null ? _targetCentres[i] : new WorldPoint(100000f, 100000f);
            return DropGeometry.NearestWithinRadius(position.x, position.y, centres, SnapRadius);
        }

        private void SetHover(int target)
        {
            _hoverTarget = target;
            for (var i = 0; i < Slots; i++)
            {
                if (_shownTarget[i] == null) continue;
                var hovered = i == target;
                _targets[i].localScale = Vector3.one * (hovered ? HoverScale : 1f);
                _targetRings[i].gameObject.SetActive(hovered);
            }
        }

        private void StopHover()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hoverTarget = -1;
            if (_targets == null) return;
            for (var i = 0; i < Slots; i++)
            {
                _targets[i].localScale = Vector3.one;
                _targetRings[i].gameObject.SetActive(false);
            }
        }

        private void OnEndDrag(int slot)
        {
            StopHover();
            _dragging = -1;
            if (_busy || _over || _pairs == null || _shownItem[slot] == null) return;

            var target = NearestTarget(_items[slot].Rect.anchoredPosition);
            if (target < 0) { _runner.StartCoroutine(SlideTo(_items[slot].Rect, ItemHome(slot), SnapBackSeconds)); return; } // empty space: no attempt
            _runner.StartCoroutine(_pairs.Matches(slot, target) ? Match(slot, target) : Wrong(slot, target));
        }

        // --- A match -----------------------------------------------------------------------------------------

        // The item walks into the target, the two hop, the animal's sound plays, the item (and the target, if used up)
        // disappears and the game refills the screen.
        private IEnumerator Match(int slot, int target)
        {
            _busy = true;
            SetItemsEnabled(false);
            var item = _items[slot];
            var itemId = _pairs.Items[slot].Id;
            _mistakes = 0;

            var spot = _targets[target].anchoredPosition + VisitOffset;
            yield return SlideScale(item.Rect, spot, VisitScale, ArriveSeconds);

            _game.Sfx.Coin();
            var heard = _game.Sfx.PlayAnimal(itemId);
            if (heard <= 0f && ZooFarmAnimals.All.Any(a => a.Id == itemId && a.RealmOf == Realm.Sea)) { _game.Sfx.Splash(); heard = 0.8f; } // a swimmer with no call of its own
            var wait = Mathf.Clamp(heard, HopSeconds * 2f, MaxSoundWait);
            var hops = Mathf.Clamp(Mathf.RoundToInt(wait / HopSeconds), 2, 6);
            _runner.StartCoroutine(Hops(item.Rect, spot, hops));
            _runner.StartCoroutine(Hops(_targets[target], _targets[target].anchoredPosition, hops));
            yield return new WaitForSeconds(wait);

            // Gone: the item always, the target when nothing is left that belongs to it.
            _pairs.Resolve(slot);
            var targetGone = _pairs.Targets[target] != _shownTarget[target];
            _runner.StartCoroutine(Vanish(item.Rect));
            if (targetGone) _runner.StartCoroutine(Vanish(_targets[target]));
            yield return new WaitForSeconds(VanishSeconds);
            Sync();

            if (_pairs.Finished) { yield return Finish(); yield break; }
            _busy = false;
            SetItemsEnabled(true);
        }

        private IEnumerator Hops(RectTransform rect, Vector2 home, int hops)
        {
            for (var h = 0; h < hops; h++)
                for (var t = 0f; t < HopSeconds; t += Time.deltaTime)
                {
                    rect.anchoredPosition = home + new Vector2(0f, Mathf.Sin(t / HopSeconds * Mathf.PI) * 45f);
                    yield return null;
                }
            rect.anchoredPosition = home;
        }

        private IEnumerator Vanish(RectTransform rect)
        {
            var from = rect.localScale.x;
            for (var t = 0f; t < VanishSeconds; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(from, 0f, t / VanishSeconds);
                yield return null;
            }
            rect.localScale = Vector3.zero;
        }

        // --- A wrong drop ------------------------------------------------------------------------------------

        // The item reacts at the target it was dropped on, then returns to the child; the mistake climbs the help ladder.
        private IEnumerator Wrong(int slot, int target)
        {
            _busy = true;
            SetItemsEnabled(false);
            var item = _items[slot];
            var itemId = _pairs.Items[slot].Id;
            var reaction = _config.WrongReaction(itemId, _pairs.Targets[target]);

            var spot = _targets[target].anchoredPosition + VisitOffset;
            yield return SlideScale(item.Rect, spot, VisitScale, ArriveSeconds);

            switch (reaction)
            {
                case PairReaction.Sink: _game.Sfx.Splash(); yield return SinkReaction(item.Rect, _itemImages[slot], spot); break;
                case PairReaction.Flop: _game.Sfx.Flop(); yield return FlopReaction(item.Rect, spot); break;
                case PairReaction.Shiver: _game.Sfx.Shiver(); yield return ShiverReaction(item.Rect, spot); break;
                default: _game.Sfx.Retry(); yield return Wobble(_targets[target], 0.5f); break;
            }
            _itemImages[slot].color = Color.white;
            item.Rect.localRotation = Quaternion.identity;
            foreach (var bubble in _bubbles) bubble.gameObject.SetActive(false);
            yield return SlideScale(item.Rect, ItemHome(slot), 1f, SnapBackSeconds * 1.5f);

            _busy = false;
            SetItemsEnabled(true);
            HandleMistake();
        }

        // A land animal in the water: it drops down and fades into the blue while bubbles rise, then bobs back up.
        private IEnumerator SinkReaction(RectTransform rect, Image image, Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var down = Mathf.Sin(Mathf.Min(k * 1.4f, 1f) * Mathf.PI * 0.5f); // sinks quickly, then rests
                var back = Mathf.Clamp01((k - 0.7f) / 0.3f);                       // bobs back up at the end
                var depth = down * (1f - back);
                rect.anchoredPosition = spot + new Vector2(Mathf.Sin(k * 18f) * 6f * (1f - back), -depth * 70f);
                rect.localScale = Vector3.one * (VisitScale * (1f - depth * 0.35f));
                image.color = Color.Lerp(Color.white, new Color(0.55f, 0.75f, 1f, 0.55f), depth);
                for (var b = 0; b < BubbleCount; b++)
                {
                    var rise = Mathf.Repeat(k * 1.6f - b * 0.12f, 1f);
                    var bubble = _bubbles[b];
                    bubble.gameObject.SetActive(k > b * 0.08f && k < 0.85f);
                    ((RectTransform)bubble.transform).anchoredPosition =
                        spot + new Vector2((b - 2) * 22f + Mathf.Sin(rise * 9f + b) * 8f, 10f + rise * 110f);
                    bubble.color = new Color(0.7f, 0.9f, 1f, 0.85f * (1f - rise));
                }
                yield return null;
            }
        }

        // A water animal out of the water: it flips from side to side and hops, gasping.
        private IEnumerator FlopReaction(RectTransform rect, Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var fade = 1f - k;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 7f * Mathf.PI * 2f) * 35f * fade + 10f * k);
                rect.anchoredPosition = spot + new Vector2(0f, Mathf.Abs(Mathf.Sin(k * 5f * Mathf.PI)) * 40f * fade);
                yield return null;
            }
        }

        // Anywhere else: a small shiver and a shake ("this is not my home").
        private IEnumerator ShiverReaction(RectTransform rect, Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var fade = 1f - k * 0.5f;
                rect.anchoredPosition = spot + new Vector2(Mathf.Sin(k * 26f) * 9f * fade, 0f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 9f) * 8f);
                yield return null;
            }
        }

        // --- Help ladder -------------------------------------------------------------------------------------

        // Mistakes in a row since the last match: the 1st is only the reaction, the 2nd a hint, the 3rd on a demonstration.
        private void HandleMistake()
        {
            _mistakes++;
            if (_mistakes == 1) return; // the reaction was the feedback
            if (!_pairs.TryFindMatch(out var slot, out var target)) return;
            if (_mistakes == 2)
            {
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                _runner.StartCoroutine(RunHint(slot, target));
            }
            else
            {
                _mistakes = 0;
                _runner.StartCoroutine(RunDemonstrate(slot, target));
            }
        }

        // The hand grabs a matching item, carries it to its target, then back; nothing is matched.
        private IEnumerator RunHint(int slot, int target)
        {
            _busy = true;
            SetItemsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_config.HintKey);

            var to = _targets[target].anchoredPosition;
            yield return Carry(slot, ItemHome(slot), to, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(slot, to, ItemHome(slot), HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _busy = false;
            if (!_over) SetItemsEnabled(true);
        }

        // The hand carries a matching item to its target and the match is made for the child.
        private IEnumerator RunDemonstrate(int slot, int target)
        {
            _busy = true;
            SetItemsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_config.DemoKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_config.DemoKey) * 0.3f);

            yield return Carry(slot, ItemHome(slot), _targets[target].anchoredPosition, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _busy = false;
            yield return Match(slot, target);
        }

        // The hand glides to the item, "grabs" it, then moves with it to `to`, fingertip on its centre.
        private IEnumerator Carry(int slot, Vector2 from, Vector2 to, float seconds)
        {
            var rect = _items[slot].Rect;
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from;
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

        // --- The end -----------------------------------------------------------------------------------------

        private IEnumerator Finish()
        {
            _over = true;
            _hand.Hide();
            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));

            // One coin for the whole game, like the Arcade games.
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(1);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + 1, _game.Sfx, _targets[0].position);

            yield return _game.Voice.SayAndWait("count_done");
            SetGameEnded(true);
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
                EndButtonPositions[0], EndButtonSize, StartNewGame);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));

            _endPanel.SetActive(false);
        }

        private void SetGameEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _targetField.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
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

        private static IEnumerator SlideScale(RectTransform target, Vector2 to, float scale, float seconds)
        {
            var from = target.anchoredPosition;
            var fromScale = target.localScale.x;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / seconds);
                target.anchoredPosition = Vector2.Lerp(from, to, k);
                target.localScale = Vector3.one * Mathf.Lerp(fromScale, scale, k);
                yield return null;
            }
            target.anchoredPosition = to;
            target.localScale = Vector3.one * scale;
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

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay);
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
