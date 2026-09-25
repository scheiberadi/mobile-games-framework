using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The decorating house: a three-level doll-house cross-section of eight rooms and three hallways (stairs). The
    // overview shows the whole house as an area chooser (no dragging, no tray); tapping an area pops it and zooms
    // the camera in. In room view the world is at scale 1 centred on the area, so canvas coordinates equal
    // room-local coordinates and owned furniture in the tray (or placed in the room) drags onto the room's slots
    // exactly as before: within SnapDistance of a slot of the right kind it places, dropped in the tray strip it
    // returns to the tray, anywhere else it eases back. Hallways have no slots and no tray. Big arrow buttons sit
    // on the doors (left/right) and stairs (hallways: up/down) and a dollhouse button returns to the overview.
    // Furniture placed in other rooms is drawn as small non-interactive pictures on each room's panel so the
    // overview shows the decorated house. Room view hides the shell and the other areas: only the current room
    // is drawn, and the widescreen sides repeat the room art's outermost pixel column.
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
        // The item sprites are cut with their content sitting on the bottom edge (a 4% margin), so an item's feet
        // are FeetMargin of its size above its rect bottom. Slot coordinates are feet positions on the room art.
        private const float FeetMargin = 0.04f;

        // Placed size per item: the sprites are square, and the sizes make the furniture fit the room art
        // (a sofa about as wide as the TV wall it faces). Nothing below MinTap: placed items stay draggable.
        private static readonly Dictionary<string, float> PlacedSizes = new Dictionary<string, float>
        {
            { "sofa", 380f }, { "rug", 356f }, { "table", 260f }, { "plant", 240f },
            { "chest", 240f }, { "bed", 300f }, { "bookshelf", 368f },
        };

        private static float PlacedSizeOf(string itemId) => PlacedSizes.TryGetValue(itemId, out var size) ? size : PlacedSize;
        private static Vector2 CentreFor(HouseSlot slot, float size) => new Vector2(slot.X, slot.Y + size * (0.5f - FeetMargin));
        private static Vector2 FeetOf(RectTransform rect) => rect.anchoredPosition - new Vector2(0f, rect.sizeDelta.y * (0.5f - FeetMargin));
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
        private const float NavSideX = 630f, NavSideY = 60f, NavTopY = 330f;

        // Arrow positions over the doors and stairs of the hallway and attic art (room-local units); rooms use NavSideX/Y.
        // Order: left, right, up, down. Provisional, tuned by eye against the art.
        private static readonly Dictionary<string, Vector2[]> HallNav = new Dictionary<string, Vector2[]>
        {
            { "hall_ground", new[] { new Vector2(-585f, 30f), new Vector2(585f, 30f), new Vector2(280f, 80f),  Vector2.zero } },
            { "hall_upper",  new[] { new Vector2(-585f, 40f), new Vector2(585f, 60f), new Vector2(230f, 120f), new Vector2(450f, -200f) } },
            // The attic (a room, not a hallway): only the stairs down, over the stairwell in the picture.
            { "party",       new[] { Vector2.zero,            Vector2.zero,           Vector2.zero,            new Vector2(-270f, 150f) } },
        };

        // Furniture is drawn back to front so tall things behind do not hide the low things in front of them.
        private static int DrawOrder(SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Wall: return 0;
                case SlotKind.Corner: return 1;
                case SlotKind.Bed: return 2;
                case SlotKind.Floor: return 3;
                case SlotKind.Table: return 4;
                default: return 5; // seat
            }
        }

        private static readonly Color SlotIdleColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color SlotGlowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private EvaGame _game;
        private Runner _runner;
        private GameObject _outside;
        private GameObject _roomBackdrop, _shell, _shellBack;
        private RawImage _backdropLeft, _backdropRight;
        private Image _atticFill;
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
            _shellBack = BuildShell("ShellBack", "house/shell_back"); // the inside of the roof, behind the rooms
            foreach (var room in HouseRooms.All) BuildRoom(room);
            _shell = BuildShell("Shell", "house/shell");

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
            // Cover any canvas up to 2400x1600 without stretching the 3:2 picture, shifted down to show the hills.
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2400f, 1600f);
            rect.anchoredPosition = new Vector2(0f, 350f);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return go;
        }

        // Widescreen sides in room view: the room art is 1440 wide, phones are wider, so the outermost pixel column
        // of the room art is stretched sideways to fill the rest.
        private void BuildRoomBackdrop()
        {
            _roomBackdrop = NewRect("RoomBackdrop", Root).gameObject;
            SetFullRect((RectTransform)_roomBackdrop.transform);
            _roomBackdrop.transform.SetSiblingIndex(1);
            _backdropLeft = NewEdge("Left", -1120f, new Rect(0f, 0f, 0.003f, 1f));
            _backdropRight = NewEdge("Right", 1120f, new Rect(0.997f, 0f, 0.003f, 1f));
            // The attic picture is a triangle with transparent corners: behind it, the inside of the roof.
            _atticFill = NewImage("AtticFill", _roomBackdrop.transform);
            SetFullRect(_atticFill.rectTransform);
            _atticFill.color = Hex("#f1d9a6");
            _roomBackdrop.SetActive(false);
        }

        private RawImage NewEdge(string name, float x, Rect uv)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(_roomBackdrop.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, HouseCamera.RoomHeight);
            rect.anchoredPosition = new Vector2(x, 0f);
            var image = go.GetComponent<RawImage>();
            image.uvRect = uv;
            image.raycastTarget = false;
            return image;
        }

        private void BuildRoom(HouseRoom room)
        {
            var container = NewRect("Room_" + room.Id, _world);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = HouseCamera.RoomSize(room);
            container.anchoredPosition = HouseCamera.RoomCentre(room);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget));
            panelGo.transform.SetParent(container, false);
            SetFullRect((RectTransform)panelGo.transform);
            var panel = panelGo.GetComponent<Image>();
            panel.sprite = EvaUi.Sprite(room.IsHall ? "house/" + room.Id : "house/room_" + room.Id);
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

        // Non-interactive shell art (roof, walls, slabs) above the room panels, in world units; the overview only.
        private GameObject BuildShell(string name, string sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_world, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(HouseCamera.ShellPivotX, HouseCamera.ShellPivotY);
            rect.sizeDelta = new Vector2(HouseCamera.ShellWidth, HouseCamera.ShellHeight);
            rect.anchoredPosition = Vector2.zero; // the pivot is the middle of the room grid
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.raycastTarget = false;
            return go;
        }

        private void BuildNavigation()
        {
            _navLeft = Nav("NavLeft", 180f, new Vector2(-NavSideX, NavSideY), () => GoTo(_current?.Left));
            _navRight = Nav("NavRight", 0f, new Vector2(NavSideX, NavSideY), () => GoTo(_current?.Right));
            _navUp = Nav("NavUp", 90f, Vector2.zero, () => GoTo(_current?.Up));
            _navDown = Nav("NavDown", 270f, Vector2.zero, () => GoTo(_current?.Down));
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
            else SetCamera(1f, HouseCamera.RoomCentre(room) - HouseCamera.ViewOffset(room));
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
            _shell.SetActive(overview);
            _shellBack.SetActive(overview);
            foreach (var view in _rooms.Values)
            {
                view.Rect.gameObject.SetActive(overview || view.Room == _current);
                ApplySize(view, view.Room == _current && !overview ? 1f : 0f);
            }
            if (overview) return;

            var attic = HouseCamera.IsAttic(_current);
            _atticFill.gameObject.SetActive(attic);
            _backdropLeft.gameObject.SetActive(!attic);
            _backdropRight.gameObject.SetActive(!attic);
            var texture = _rooms[_current.Id].Panel.sprite.texture;
            _backdropLeft.texture = texture;
            _backdropRight.texture = texture;
            if (!_current.IsHall)
            {
                BuildSlotOutlines();
                RefreshItems();
            }
            var hall = HallNav.TryGetValue(_current.Id, out var spots) ? spots : null;
            PlaceNav(_navLeft, _current.Left != null, hall != null ? hall[0] : new Vector2(-NavSideX, NavSideY));
            PlaceNav(_navRight, _current.Right != null, hall != null ? hall[1] : new Vector2(NavSideX, NavSideY));
            PlaceNav(_navUp, _current.Up != null && hall != null, hall != null ? hall[2] : Vector2.zero);
            PlaceNav(_navDown, _current.Down != null && hall != null, hall != null ? hall[3] : Vector2.zero);
            _overviewButton.gameObject.SetActive(true);
        }

        // A hallway is squeezed to half a room's width in the overview and the attic is the whole roof interior; in
        // room view both are shown at their own picture size. fraction 0 = overview size, 1 = room view size. The
        // furniture drawn on the panel keeps its place on the picture as the panel is resized.
        private static void ApplySize(RoomView view, float fraction)
        {
            var overview = HouseCamera.RoomSize(view.Room);
            var zoomed = HouseCamera.RoomViewSize(view.Room);
            var size = new Vector2(Mathf.Lerp(overview.x, zoomed.x, fraction), Mathf.Lerp(overview.y, zoomed.y, fraction));
            view.Rect.sizeDelta = size;
            view.StaticItems.localScale = Vector3.one * (size.x / overview.x);
        }

        private static void PlaceNav(Button button, bool visible, Vector2 position)
        {
            button.gameObject.SetActive(visible);
            ((RectTransform)button.transform).anchoredPosition = position;
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
            foreach (var view in _rooms.Values) { view.Button.interactable = false; view.Panel.raycastTarget = false; view.Rect.gameObject.SetActive(true); }
            _outside.SetActive(true);
            _roomBackdrop.SetActive(false);
            _shell.SetActive(true);
            _shellBack.SetActive(true);
            RefreshStaticItems(); // during the move every room shows its furniture, including the one just left

            if (pop && target != null) yield return Pop(_rooms[target.Id].Rect);

            var fromScale = _world.localScale.x;
            var fromPosition = _world.anchoredPosition;
            var toScale = target == null ? HouseCamera.OverviewScale : 1f;
            var toFocus = target == null ? HouseCamera.OverviewFocus : HouseCamera.RoomCentre(target) - HouseCamera.ViewOffset(target);
            var toPosition = HouseCamera.ContainerPosition(toFocus, toScale);
            var fromView = _current != null ? _rooms[_current.Id] : null;
            var toView = target != null ? _rooms[target.Id] : null;
            for (var t = 0f; t < ZoomSeconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / ZoomSeconds);
                if (fromView != null) ApplySize(fromView, 1f - k);
                if (toView != null) ApplySize(toView, k);
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
                rect.anchoredPosition = CentreFor(slot, SlotSize);
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
            foreach (var placement in PlacedBackToFront())
            {
                var slot = HouseSlots.Find(placement.SlotId);
                var go = new GameObject("Static_" + placement.ItemId, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_rooms[slot.Room].StaticItems, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                // Slot coordinates are room-view canvas units; on the overview panel they are scaled to its size
                // (the attic's panel is three times its room-view size).
                var room = HouseRooms.Find(slot.Room);
                var scale = HouseCamera.OverviewScaleOf(room);
                var size = PlacedSizeOf(placement.ItemId);
                rect.anchoredPosition = (CentreFor(slot, size) - HouseCamera.ViewOffset(room)) * scale;
                rect.sizeDelta = new Vector2(size, size) * scale;
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("objects/" + placement.ItemId);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        // Every placement with a known slot, ordered so that things further back are drawn first.
        private List<Placement> PlacedBackToFront()
        {
            var placed = new List<Placement>();
            foreach (var placement in _game.Progress.House.Placements)
                if (HouseSlots.Find(placement.SlotId) != null) placed.Add(placement);
            placed.Sort((a, b) => DrawOrder(HouseSlots.Find(a.SlotId).Kind).CompareTo(DrawOrder(HouseSlots.Find(b.SlotId).Kind)));
            return placed;
        }

        // Rebuilds the draggable items for the current room: placed items of this room first, tray items last
        // (later siblings draw and raycast on top), so every owned tray item stays reachable.
        private void RefreshItems()
        {
            Clear(_itemsLayer);
            var owned = _game.Progress.Owned;
            var house = _game.Progress.House;

            foreach (var placement in PlacedBackToFront())
            {
                var slot = HouseSlots.Find(placement.SlotId);
                if (slot.Room == _current.Id) CreateItem(placement.ItemId, CentreFor(slot, PlacedSizeOf(placement.ItemId)), PlacedSizeOf(placement.ItemId));
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

            var bestSlot = NearestValidSlot(item.ItemId, FeetOf(item.Rect), owned, house);
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

        private string NearestValidSlot(string itemId, Vector2 dropFeet, List<string> owned, HouseLayout house)
        {
            string best = null;
            var bestDistance = SnapDistance;
            foreach (var slot in HouseSlots.InRoom(_current.Id))
            {
                if (!house.CanPlace(itemId, slot.Id, owned)) continue;
                var distance = Vector2.Distance(dropFeet, new Vector2(slot.X, slot.Y));
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
