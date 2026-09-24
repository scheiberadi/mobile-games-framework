using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The decorating house: a three-level, eight-room doll-house cross-section. The overview shows the whole
    // house as a room chooser (no dragging, no tray); tapping a room pops it and zooms the camera in. In room
    // view the world is at scale 1 centred on the room, so canvas coordinates equal room-local coordinates and
    // owned furniture in the tray (or placed in the room) drags onto the room's slots exactly as before:
    // within SnapDistance of a slot of the right kind it places, dropped in the tray strip it returns to the
    // tray, anywhere else it eases back. Big arrow buttons move between neighbouring rooms; a dollhouse button
    // returns to the overview. Furniture placed in other rooms is drawn as small non-interactive pictures on
    // each room's panel so the overview shows the decorated house.
    public sealed class HouseScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class RoomView
        {
            public HouseRoom Room;
            public RectTransform Rect;
            public RectTransform StaticItems;
            public Button Button;
            public Image Panel;
        }

        private const float SlotSize = 260f;
        private const float TraySize = EvaUi.MinTap; // 240
        private const float PlacedSize = 260f;
        private const float TraySpacing = 260f;
        private const float TrayY = -330f;
        private const float SnapDistance = 260f;
        // Items dropped at or below this y (and within TraySafeHalfWidth) return to the tray. Placed items are
        // drawn before tray items so a tray item's raycast wins where footprints overlap (see the M1 notes).
        private const float TrayZoneMaxY = TrayY + TraySize / 2f + 10f;
        private const float TraySafeHalfWidth = 750f;
        private const float EaseSeconds = 0.2f;
        private const float ZoomSeconds = 0.3f;
        private const float PopSeconds = 0.18f, PopScale = 1.06f;
        private const float NavSideX = 630f, NavSideY = 40f, NavTopY = 330f, NavTopSpacing = 260f;

        // Wall (top) and floor (bottom band) colours that fill the widescreen sides in room view. Keep in sync
        // with ROOMS in art/eva/house/gen.js.
        private static readonly Dictionary<string, (Color wall, Color floor)> RoomColors = new Dictionary<string, (Color, Color)>
        {
            { "living",  (Hex("#ffe8c2"), Hex("#d9a066")) },
            { "dining",  (Hex("#ffd9d0"), Hex("#c98d5a")) },
            { "kitchen", (Hex("#dff3ff"), Hex("#cfd8dc")) },
            { "parents", (Hex("#e8dcff"), Hex("#b98d6a")) },
            { "kids",    (Hex("#d6f5d6"), Hex("#d9a066")) },
            { "bath",    (Hex("#cdeeff"), Hex("#b8d8e8")) },
            { "party",   (Hex("#f3d9ff"), Hex("#e3b478")) },
            { "play",    (Hex("#fff3b0"), Hex("#a5d8a5")) },
        };

        private static readonly Color SlotIdleColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color SlotGlowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private EvaGame _game;
        private Runner _runner;
        private GameObject _outside;
        private GameObject _roomBackdrop;
        private Image _backdropWall, _backdropFloor;
        private RectTransform _world;
        private RectTransform _slotsLayer, _itemsLayer;
        private Button _navLeft, _navRight, _navUp, _navDown, _overviewButton;
        private readonly Dictionary<string, RoomView> _rooms = new Dictionary<string, RoomView>();
        private readonly Dictionary<string, Image> _slotOutlines = new Dictionary<string, Image>();
        private HouseRoom _current; // null = overview
        private bool _moving;
        private Vector2 _dragOrigin;
        private bool _saidPlacedThisSession;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            _outside = AddBackdropImage("Outside", "house/outside");
            BuildRoomBackdrop();

            _world = NewRect("World", Root);
            _world.anchorMin = _world.anchorMax = _world.pivot = new Vector2(0.5f, 0.5f);
            _world.sizeDelta = Vector2.zero;
            foreach (var room in HouseRooms.All) BuildRoom(room);
            BuildShell();

            _slotsLayer = NewRect("Slots", Root);
            SetFullRect(_slotsLayer);
            _itemsLayer = NewRect("Items", Root);
            SetFullRect(_itemsLayer);
            BuildNavigation();
        }

        public override void OnShow()
        {
            if (_game.Progress.Owned.Count == 0)
            {
                _game.Progress.GrantStarter();
                _game.Commit();
            }
            _runner.StopAllCoroutines();
            _moving = false;

            var step = _game.Progress.Tutorial;
            var tutorialRoom = step == TutorialStep.PlaceStarter || step == TutorialStep.PlacePurchase;
            Apply(tutorialRoom ? HouseRooms.Find("living") : null);
            _game.TutorialGuide.Refresh(ScreenId.House);
        }

        public override void OnHide() => _runner.StopAllCoroutines();

        // --- Build ------------------------------------------------------------------------------------------

        private GameObject AddBackdropImage(string name, string sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            go.transform.SetAsFirstSibling();
            SetFullRect((RectTransform)go.transform);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return go;
        }

        private void BuildRoomBackdrop()
        {
            _roomBackdrop = NewRect("RoomBackdrop", Root).gameObject;
            SetFullRect((RectTransform)_roomBackdrop.transform);
            _roomBackdrop.transform.SetSiblingIndex(1);
            var wall = NewImage("Wall", _roomBackdrop.transform);
            wall.rectTransform.anchorMin = Vector2.zero;
            wall.rectTransform.anchorMax = Vector2.one;
            wall.rectTransform.offsetMin = new Vector2(-1500f, -1500f);
            wall.rectTransform.offsetMax = new Vector2(1500f, 1500f);
            var floor = NewImage("Floor", _roomBackdrop.transform);
            floor.rectTransform.anchorMin = new Vector2(0f, 0f);
            floor.rectTransform.anchorMax = new Vector2(1f, 0f);
            floor.rectTransform.offsetMin = new Vector2(-1500f, -1500f);
            floor.rectTransform.offsetMax = new Vector2(1500f, 240f); // the floor band of a 900-high frame
            _backdropWall = wall;
            _backdropFloor = floor;
            _roomBackdrop.SetActive(false);
        }

        private void BuildRoom(HouseRoom room)
        {
            var container = NewRect("Room_" + room.Id, _world);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(HouseCamera.RoomWidth, HouseCamera.RoomHeight);
            container.anchoredPosition = HouseCamera.RoomCentre(room);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget));
            panelGo.transform.SetParent(container, false);
            SetFullRect((RectTransform)panelGo.transform);
            var panel = panelGo.GetComponent<Image>();
            panel.sprite = EvaUi.Sprite("house/room_" + room.Id);
            panel.type = Image.Type.Simple;
            var button = panelGo.GetComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;

            var items = NewRect("StaticItems", container);
            SetFullRect(items);

            var view = new RoomView { Room = room, Rect = container, StaticItems = items, Button = button, Panel = panel };
            button.onClick.AddListener(() => OnRoomTapped(view));
            _rooms[room.Id] = view;
        }

        // Non-interactive shell art (roof, slabs, stairs, balcony, door) above the room panels, in world units.
        private void BuildShell()
        {
            var go = new GameObject("Shell", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_world, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(4800f, 3200f);
            rect.anchoredPosition = new Vector2(0f, 200f); // shell.svg viewBox is x -2400..2400, y -1800..1400 (svg y down)
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("house/shell");
            image.raycastTarget = false;
        }

        private void BuildNavigation()
        {
            _navLeft = Nav("NavLeft", 180f, new Vector2(-NavSideX, NavSideY), () => GoTo(_current?.Left));
            _navRight = Nav("NavRight", 0f, new Vector2(NavSideX, NavSideY), () => GoTo(_current?.Right));
            _navUp = Nav("NavUp", 90f, new Vector2(-NavTopSpacing, NavTopY), () => GoTo(_current?.Up));
            _navDown = Nav("NavDown", 270f, new Vector2(NavTopSpacing, NavTopY), () => GoTo(_current?.Down));
            _overviewButton = EvaUi.IconButton(Root, "OverviewButton", EvaUi.Sprite("icons/dollhouse"), new Vector2(0.5f, 0.5f), new Vector2(0f, NavTopY), 240f, () => GoTo(null, true));
            HideNavigation();
        }

        private Button Nav(string name, float rotation, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var button = EvaUi.IconButton(Root, name, EvaUi.Sprite("icons/arrow"), new Vector2(0.5f, 0.5f), position, 240f, onClick);
            button.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return button;
        }

        // --- Camera and modes -------------------------------------------------------------------------------

        private void SetCamera(float scale, Vector2 focus)
        {
            _world.localScale = Vector3.one * scale;
            _world.anchoredPosition = HouseCamera.ContainerPosition(focus, scale);
        }

        // Jumps straight to the overview (room == null) or a room, with no animation.
        private void Apply(HouseRoom room)
        {
            _current = room;
            if (room == null) SetCamera(HouseCamera.OverviewScale, HouseCamera.OverviewFocus);
            else SetCamera(1f, HouseCamera.RoomCentre(room));
            ShowMode();
        }

        private void ShowMode()
        {
            HideRoomUi();
            RefreshStaticItems();
            var overview = _current == null;
            foreach (var view in _rooms.Values)
            {
                view.Button.interactable = overview;
                view.Panel.raycastTarget = overview;
            }
            _outside.SetActive(overview);
            _roomBackdrop.SetActive(!overview);
            if (overview) return;

            var colors = RoomColors[_current.Id];
            _backdropWall.color = colors.wall;
            _backdropFloor.color = colors.floor;
            BuildSlotOutlines();
            RefreshItems();
            _navLeft.gameObject.SetActive(_current.Left != null);
            _navRight.gameObject.SetActive(_current.Right != null);
            _navUp.gameObject.SetActive(_current.Up != null);
            _navDown.gameObject.SetActive(_current.Down != null);
            _overviewButton.gameObject.SetActive(true);
        }

        private void OnRoomTapped(RoomView view)
        {
            if (_moving || _current != null) return;
            _game.Sfx.Tap();
            GoTo(view.Room.Id, true);
        }

        private void GoTo(string roomId, bool pop = false)
        {
            if (_moving) return;
            var target = roomId == null ? null : HouseRooms.Find(roomId);
            if (roomId != null && target == null) return;
            _runner.StartCoroutine(Transition(target, pop));
        }

        private IEnumerator Transition(HouseRoom target, bool pop)
        {
            _moving = true;
            HideRoomUi();
            foreach (var view in _rooms.Values) { view.Button.interactable = false; view.Panel.raycastTarget = false; }
            _outside.SetActive(true);
            _roomBackdrop.SetActive(false);
            RefreshStaticItems(); // during the move every room shows its furniture, including the one just left

            if (pop && target != null) yield return Pop(_rooms[target.Id].Rect);

            var fromScale = _world.localScale.x;
            var fromPosition = _world.anchoredPosition;
            var toScale = target == null ? HouseCamera.OverviewScale : 1f;
            var toFocus = target == null ? HouseCamera.OverviewFocus : HouseCamera.RoomCentre(target);
            var toPosition = HouseCamera.ContainerPosition(toFocus, toScale);
            for (var t = 0f; t < ZoomSeconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / ZoomSeconds);
                _world.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, k);
                _world.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, k);
                yield return null;
            }
            Apply(target);
            _moving = false;
        }

        private static IEnumerator Pop(RectTransform rect)
        {
            for (var t = 0f; t < PopSeconds; t += Time.deltaTime)
            {
                var k = t / PopSeconds;
                rect.localScale = Vector3.one * Mathf.Lerp(1f, PopScale, k < 0.5f ? k * 2f : (1f - k) * 2f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        private void HideNavigation()
        {
            foreach (var button in new[] { _navLeft, _navRight, _navUp, _navDown, _overviewButton })
                if (button != null) button.gameObject.SetActive(false);
        }

        private void HideRoomUi()
        {
            HideNavigation();
            Clear(_slotsLayer);
            Clear(_itemsLayer);
            _slotOutlines.Clear();
        }

        // --- Slots, tray and items (room view) ----------------------------------------------------------------

        private void BuildSlotOutlines()
        {
            foreach (var slot in HouseSlots.InRoom(_current.Id))
            {
                var go = new GameObject("Slot_" + slot.Id, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_slotsLayer, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(slot.X, slot.Y);
                rect.sizeDelta = new Vector2(SlotSize, SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = RoundedRectSprite.Get();
                image.type = Image.Type.Sliced;
                image.color = SlotIdleColor;
                image.raycastTarget = false;
                _slotOutlines[slot.Id] = image;
            }
        }

        // Small non-interactive pictures of every placed item on its room's panel (world space). The current
        // room's copies are hidden in room view: there the real draggable items are drawn instead.
        private void RefreshStaticItems()
        {
            foreach (var view in _rooms.Values)
            {
                Clear(view.StaticItems);
                view.StaticItems.gameObject.SetActive(_current == null || _current.Id != view.Room.Id || _moving);
            }
            foreach (var placement in _game.Progress.House.Placements)
            {
                var slot = HouseSlots.Find(placement.SlotId);
                if (slot == null) continue;
                var go = new GameObject("Static_" + placement.ItemId, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_rooms[slot.Room].StaticItems, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(slot.X, slot.Y);
                rect.sizeDelta = new Vector2(PlacedSize, PlacedSize);
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("objects/" + placement.ItemId);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        // Rebuilds the draggable items for the current room: placed items of this room first, tray items last
        // (later siblings draw and raycast on top), so every owned tray item stays reachable.
        private void RefreshItems()
        {
            Clear(_itemsLayer);
            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;

            foreach (var id in owned)
            {
                var slot = HouseSlots.Find(house.SlotOf(id));
                if (slot != null && slot.Room == _current.Id) CreateItem(id, new Vector2(slot.X, slot.Y), PlacedSize);
            }

            var unplaced = new List<string>();
            foreach (var id in owned)
                if (house.SlotOf(id) == null) unplaced.Add(id);

            // Spacing shrinks so the whole tray row stays inside +-TraySafeHalfWidth (7 items -> 210).
            var spacing = TraySpacing;
            if (unplaced.Count > 1)
                spacing = Mathf.Min(TraySpacing, 2f * (TraySafeHalfWidth - TraySize / 2f) / (unplaced.Count - 1));
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
            foreach (var slot in HouseSlots.InRoom(_current.Id))
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
                RefreshStaticItems();
                _game.TutorialGuide.Refresh(ScreenId.House);
                return;
            }

            if (dropPosition.y <= TrayZoneMaxY && Mathf.Abs(dropPosition.x) <= TraySafeHalfWidth)
            {
                if (house.SlotOf(item.ItemId) != null)
                {
                    house.Remove(item.ItemId);
                    _game.Commit();
                }
                RefreshItems();
                RefreshStaticItems();
                return;
            }

            _runner.StartCoroutine(EaseTo(item.Rect, dropPosition, _dragOrigin));
        }

        private string NearestValidSlot(string itemId, Vector2 dropPosition, List<string> owned, HouseLayout house)
        {
            string best = null;
            var bestDistance = SnapDistance;
            foreach (var slot in HouseSlots.InRoom(_current.Id))
            {
                if (!house.CanPlace(itemId, slot.Id, owned)) continue;
                var distance = Vector2.Distance(dropPosition, new Vector2(slot.X, slot.Y));
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

        // --- Helpers ----------------------------------------------------------------------------------------

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image NewImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
