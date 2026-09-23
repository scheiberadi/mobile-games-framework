using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The decorating room: a two-room cutaway (living room left, bedroom right) with seven fixed furniture
    // slots. Owned furniture that is not yet placed sits in a tray along the bottom edge; dragging a tray item
    // (or a placed one) onto a slot of the right kind, within SnapDistance of its centre, places it there.
    // Dropping a placed item back down in the tray strip sends it back to the tray; dropping anywhere else
    // (not a valid slot, not the tray) eases the item smoothly back to wherever the drag started, so a slot is
    // never left half-filled and the child can never lose an item mid-drag. All layout is in canvas units,
    // origin centre, matching the brief's fixed slot positions - the same on every device (see the class notes
    // on ScreenBase / CreatorScreen for why: the production canvas is height-matched, so y in [-450, 450] is
    // the real on-device frame, and every element's full rect must stay inside it; every slot and tray position
    // below was checked against that bound, not just guessed).
    public sealed class HouseScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float SlotSize = 260f; // hit and snap area per the brief
        private const float TraySize = EvaUi.MinTap; // 240: unplaced tray items
        private const float PlacedSize = 260f; // within the brief's 240-300 range
        private const float TraySpacing = 260f; // default spacing; RefreshItems tightens this when needed (see below)
        private const float TrayY = -330f; // as low as TraySize (half 120) allows before its own bottom edge
                                            // (-450) leaves the -450..450 on-device frame; see the class comment
        private const float SnapDistance = 260f;

        // Review fix (Critical): the tray's own top edge sits at TrayY + TraySize/2 = -210. Checked against
        // every slot's placed-item bottom edge (centre y - half 130), not just living_floor's: living_floor
        // (-420) overlaps by 210 units, living_table (-250) by 40, bedroom_bed (-230) by 20; the other four
        // slots (living_seat -190, living_corner -10, bedroom_corner -30, bedroom_wall 20) clear the tray
        // entirely. The brief's fixed slot coordinates and the -450 canvas floor leave no legal TrayY that
        // clears all seven (see the fix report for the full per-slot arithmetic). RefreshItems below resolves
        // this by drawing placed items before tray items, so a tray item's raycast always wins over a placed
        // item wherever their rects overlap, for every one of these slots equally - the specific harm the
        // review flagged (a child unable to tap an owned tray item) is eliminated regardless of the residual
        // visual overlap.
        private const float TrayZoneMaxY = TrayY + TraySize / 2f + 10f; // 10 units of slack above the tray's own top edge (-200)

        // Review fix (Important): the safe on-device x half-range, matching the brief's own outermost slot
        // edges (bedroom_wall 620 + half 130 = 750; living_corner -620 - half 130 = -750). Used both to bound
        // the "back to tray" drop zone below (so it no longer fires at any x) and to cap the tray row's own
        // spacing so it can never lay an item outside this range (see RefreshItems).
        private const float TraySafeHalfWidth = 750f;

        private const float EaseSeconds = 0.2f;

        private static readonly Dictionary<string, Vector2> SlotPositions = new Dictionary<string, Vector2>
        {
            { "living_seat", new Vector2(-480f, -60f) },
            { "living_floor", new Vector2(-400f, -290f) },
            { "living_table", new Vector2(-170f, -120f) },
            { "living_corner", new Vector2(-620f, 120f) },
            { "bedroom_bed", new Vector2(440f, -100f) },
            { "bedroom_corner", new Vector2(200f, 100f) },
            { "bedroom_wall", new Vector2(620f, 150f) },
        };

        private static readonly Color SlotIdleColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color SlotGlowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _itemsLayer;
        private readonly Dictionary<string, Image> _slotOutlines = new Dictionary<string, Image>();
        private Vector2 _dragOrigin;

        // Eva's house_placed line only plays once per session (app run), not once per placement - the screen
        // instance itself lives for the whole session (Navigator builds it once and only shows/hides it), so a
        // plain instance flag is exactly "first time this session" without needing to persist anything.
        private bool _saidPlacedThisSession;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddHouseBackground();
            BuildSlotOutlines();

            _itemsLayer = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
            _itemsLayer.SetParent(Root, false);
            SetFullRect(_itemsLayer);
        }

        public override void OnShow()
        {
            if (_game.Progress.Owned.Count == 0)
            {
                _game.Progress.GrantStarter();
                _game.Commit();
            }
            RefreshItems();
            _game.TutorialGuide.Refresh(ScreenId.House);
        }

        // --- Background and slots -------------------------------------------------------------------------

        private void AddHouseBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            SetFullRect((RectTransform)background.transform);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/house_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }

        private void BuildSlotOutlines()
        {
            var layer = new GameObject("Slots", typeof(RectTransform));
            layer.transform.SetParent(Root, false);
            SetFullRect((RectTransform)layer.transform);

            foreach (var slot in HouseSlots.All)
            {
                var go = new GameObject("Slot_" + slot.Id, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(layer.transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = SlotPositions[slot.Id];
                rect.sizeDelta = new Vector2(SlotSize, SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = RoundedRectSprite.Get();
                image.type = Image.Type.Sliced;
                image.color = SlotIdleColor;
                image.raycastTarget = false; // decorative only: never a drop target by itself, never needs TapTarget

                _slotOutlines[slot.Id] = image;
            }
        }

        // --- Tray and placed items --------------------------------------------------------------------------

        // Rebuilds every draggable item from scratch against the current save state. Simpler than tracking
        // incremental moves, and cheap enough to call on every placement/removal and every OnShow (which also
        // covers items bought in the Store between visits, once that screen exists).
        private void RefreshItems()
        {
            for (var i = _itemsLayer.childCount - 1; i >= 0; i--)
            {
                var child = _itemsLayer.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }

            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;

            // Placed items first, tray items last: later siblings draw and raycast on top in uGUI, so whatever
            // is created last here is what a tap actually hits wherever a tray item's footprint overlaps a
            // placed item's (living_floor, living_table, bedroom_bed; see the TrayY/TrayZoneMaxY comments
            // above). This keeps every owned tray item reachable regardless of the residual geometric overlap.
            foreach (var id in owned)
            {
                var slotId = house.SlotOf(id);
                if (slotId != null) CreateItem(id, SlotPositions[slotId], PlacedSize);
            }

            var unplaced = new List<string>();
            foreach (var id in owned)
                if (house.SlotOf(id) == null) unplaced.Add(id);

            // Review fix (Important): spacing shrinks - never below what keeps the whole row inside
            // TraySafeHalfWidth - once enough items are in the tray that the default 260-unit spacing would
            // push the outermost item's centre, plus its own half-width, past the safe on-device x range. At
            // the maximum possible 7 owned items (one per slot kind), spacing = 2*(750-120)/(7-1) = 210, giving
            // outer edges at +-(3*210 + 120) = +-750 - exactly the safe bound, never past it.
            var spacing = TraySpacing;
            if (unplaced.Count > 1)
            {
                var maxCenterOffset = TraySafeHalfWidth - TraySize / 2f;
                spacing = Mathf.Min(TraySpacing, 2f * maxCenterOffset / (unplaced.Count - 1));
            }

            var startX = -Mathf.Max(0, unplaced.Count - 1) * spacing / 2f;
            for (var i = 0; i < unplaced.Count; i++)
                CreateItem(unplaced[i], new Vector2(startX + i * spacing, TrayY), TraySize);
        }

        private void CreateItem(string itemId, Vector2 position, float size)
        {
            var item = DragItem.Create(_itemsLayer, itemId, EvaUi.Sprite("objects/" + itemId), position, size);
            item.BeginDrag += OnItemBeginDrag;
            item.EndDrag += OnItemEndDrag;
        }

        private void OnItemBeginDrag(DragItem item)
        {
            _dragOrigin = item.Rect.anchoredPosition;
            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;
            foreach (var slot in HouseSlots.All)
                if (house.CanPlace(item.ItemId, slot.Id, owned))
                    _slotOutlines[slot.Id].color = SlotGlowColor;
        }

        private void OnItemEndDrag(DragItem item)
        {
            foreach (var outline in _slotOutlines.Values) outline.color = SlotIdleColor;

            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;
            var dropPosition = item.Rect.anchoredPosition;

            var bestSlot = NearestValidSlot(item.ItemId, dropPosition, owned, house);
            if (bestSlot != null)
            {
                house.TryPlace(item.ItemId, bestSlot, owned);
                _game.Sfx.Place();
                _game.Progress.Advance(TutorialEvent.ItemPlaced);
                _game.Commit();
                if (!_saidPlacedThisSession)
                {
                    _saidPlacedThisSession = true;
                    _game.Voice.Say("house_placed");
                }
                RefreshItems();
                _game.TutorialGuide.Refresh(ScreenId.House);
                return;
            }

            // Review fix (Important): bounded by x as well as y, so dropping in the bottom band anywhere off to
            // either side of the actual tray strip (past TraySafeHalfWidth) eases back to the drag origin
            // instead of silently un-placing the item, matching the brief's "dropping elsewhere ... eases back".
            if (dropPosition.y <= TrayZoneMaxY && Mathf.Abs(dropPosition.x) <= TraySafeHalfWidth)
            {
                if (house.SlotOf(item.ItemId) != null)
                {
                    house.Remove(item.ItemId);
                    _game.Commit();
                }
                RefreshItems();
                return;
            }

            _runner.StartCoroutine(EaseTo(item.Rect, dropPosition, _dragOrigin));
        }

        private static string NearestValidSlot(string itemId, Vector2 dropPosition, List<string> owned, HouseLayout house)
        {
            string best = null;
            var bestDistance = SnapDistance;
            foreach (var slot in HouseSlots.All)
            {
                if (!house.CanPlace(itemId, slot.Id, owned)) continue;
                var distance = Vector2.Distance(dropPosition, SlotPositions[slot.Id]);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = slot.Id;
                }
            }
            return best;
        }

        private static IEnumerator EaseTo(RectTransform rect, Vector2 from, Vector2 to)
        {
            for (var t = 0f; t < EaseSeconds; t += Time.deltaTime)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, PointerHand.EaseInOut(t / EaseSeconds));
                yield return null;
            }
            rect.anchoredPosition = to;
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
