using System;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The child's world: a painted landscape twice the screen wide, the House in the middle and the other places on
    // roads around it (Places). The screen is a 1440 x 900 window on it: dragging pans, and the view follows the two
    // small characters when they walk to a tapped place. World units are the canvas units of the World container, whose
    // centre is the world origin, so a child's anchoredPosition inside World is its world position.
    public sealed class MapScreen : ScreenBase
    {
        private const float CharacterHeight = 160f;
        private const float PairOffsetX = 60f, FeetDrop = 80f;
        private const float HopHeight = 18f, HopRate = 9f;

        private sealed class Ticker : MonoBehaviour
        {
            public Action<float> OnTick;
            private void Update() => OnTick?.Invoke(Time.deltaTime);
        }

        private EvaGame _game;
        private RectTransform _view, _world, _waveZone;
        private CharacterRig _eva, _player;
        private PlaceId _at = PlaceId.House;
        private Vector2 _camera;

        private WorldPoint[] _route;
        private PlaceId _target;
        private float _duration, _elapsed;
        private Vector2 _walkStartCamera;

        // The camera eases from where it was to the characters over this long at the start of a walk, so it never jumps.
        private const float CameraEaseSeconds = 0.3f;

        // Task 12: after Done, Eva greets the child once per app run (a plain instance flag: the screen instance lives
        // for the whole session).
        private bool _saidWelcomeThisSession;

        public bool IsWalking { get; private set; }
        public PlaceId At => _at;
        public Vector2 CameraCentre => _camera;

        public override void Build(EvaGame game)
        {
            _game = game;

            // The view covers the whole canvas (under a camera cutout too); only the gear below stays in the safe area.
            var viewObject = new GameObject("WorldView", typeof(RectTransform));
            _view = (RectTransform)viewObject.transform;
            _view.SetParent(Root, false);
            FullBleed.Attach(_view);
            var worldObject = new GameObject("World", typeof(RectTransform), typeof(MapDrag), typeof(TapTarget));
            _world = (RectTransform)worldObject.transform;
            _world.SetParent(_view, false);
            _world.anchorMin = _world.anchorMax = _world.pivot = new Vector2(0.5f, 0.5f);
            _world.sizeDelta = new Vector2(Places.WorldWidth, Places.WorldHeight);
            worldObject.GetComponent<MapDrag>().Init(_view, Pan, OnPanEnd);

            // Backdrop halves take the drag (raycast on); roads and buildings are drawn over them.
            AddPicture("BackdropLeft", "world/map_world_left", new Vector2(-Places.WorldWidth / 4f, 0f), new Vector2(Places.WorldWidth / 2f, Places.WorldHeight), true);
            AddPicture("BackdropRight", "world/map_world_right", new Vector2(Places.WorldWidth / 4f, 0f), new Vector2(Places.WorldWidth / 2f, Places.WorldHeight), true);
            foreach (var place in Places.All)
                if (place.RoadSprite != null)
                {
                    var box = place.RoadBox.Value;
                    AddPicture("Road_" + place.Id, place.RoadSprite, new Vector2(box.X, box.Y), new Vector2(box.Width, box.Height), false);
                }
                else AddStonePath(place);
            AddWaveZone(); // below the place buttons: where it overlaps a building, the building's button wins
            foreach (var place in Places.All) AddPlaceButton(place);

            _player = RigFactory.CreatePlayer(_world, game.Progress.Look, CharacterHeight);
            _eva = RigFactory.CreateEva(_world, CharacterHeight);

            var gear = EvaUi.IconButton(Root, "SettingsButton", EvaUi.Sprite("icons/gear"), Hud.HomeAnchor, Hud.HomePosition, Hud.HomeSize,
                () => _game.Navigator.Show(ScreenId.ParentGate));
            EvaUi.ShrinkIcon(gear, Hud.HomeIconInset);

            Root.gameObject.AddComponent<Ticker>().OnTick = Advance;
        }

        public override void OnShow()
        {
            CancelWalk();
            _at = Places.ParseOrHouse(_game.Progress.LastPlace);
            var spot = Places.Find(_at).StandingSpot;
            PlaceCharacters(new Vector2(spot.X, spot.Y), 0f, 0f);
            // While the first-run tutorial runs the view starts on the first view, so the hand finds School and Store (a pan re-targets it).
            var focus = _game.Progress.Tutorial == TutorialStep.Done ? spot : Places.InitialView;
            SetCamera(new Vector2(focus.X, focus.Y));

            _game.TutorialGuide.Refresh(ScreenId.Map);
            if (_game.Progress.Tutorial == TutorialStep.Done && !_saidWelcomeThisSession)
            {
                _saidWelcomeThisSession = true;
                _game.Voice.Say("map_welcome");
            }
        }

        public override void OnHide() => CancelWalk();

        // Where a place's tap box centre is in Root canvas units at the current camera (the tutorial hand points there).
        public Vector2 ScreenPositionOf(PlaceId id)
        {
            var box = Places.Find(id).TapBox;
            // World centre is the canvas centre; Root (where the guide hand lives) is offset from it by any cutout.
            return new Vector2(box.X, box.Y) - _camera - FullBleed.CentreShift(FullBleed.CurrentInsets(_view));
        }

        // A drag of `delta` canvas units moves the world with the finger, so the view moves the opposite way.
        public void Pan(Vector2 delta)
        {
            if (IsWalking) return;
            SetCamera(_camera - delta);
        }

        // The tutorial hand points at a live building position, so it re-targets once the finger lifts.
        private void OnPanEnd()
        {
            if (_game.Progress.Tutorial != TutorialStep.Done) _game.TutorialGuide.Refresh(ScreenId.Map);
        }

        // One step of the walk (called every frame by the ticker, and directly by tests).
        public void Advance(float seconds)
        {
            if (!IsWalking) return;
            _elapsed += seconds;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var point = MapPath.PositionAt(_route, t);
            var spot = new Vector2(point.X, point.Y);
            // A temporary prototype hop (front-facing rigs); real side-view walking arrives with step 2.
            var hopPlayer = Mathf.Abs(Mathf.Sin(_elapsed * HopRate)) * HopHeight;
            var hopEva = Mathf.Abs(Mathf.Sin(_elapsed * HopRate + 1f)) * HopHeight;
            PlaceCharacters(spot, hopPlayer, hopEva);
            SetCamera(Vector2.Lerp(_walkStartCamera, ClampedCamera(spot), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / CameraEaseSeconds))));
            if (t >= 1f) Arrive();
        }

        private void OnPlaceTapped(Place place)
        {
            if (IsWalking) return;
            _game.Voice.Say(place.VoiceKey);
            _target = place.Id;
            _route = MapPath.Route(_at, place.Id);
            if (_route.Length <= 1)
            {
                Arrive();
                return;
            }
            _duration = MapPath.Duration(_route);
            _elapsed = 0f;
            _walkStartCamera = _camera;
            IsWalking = true;
        }

        private void Arrive()
        {
            IsWalking = false;
            _at = _target;
            _game.Progress.LastPlace = _at.ToString();
            _game.Commit();
            var place = Places.Find(_at);
            _game.Navigator.Show((ScreenId)Enum.Parse(typeof(ScreenId), place.ScreenKey));
        }

        private void CancelWalk() => IsWalking = false;

        private Vector2 ClampedCamera(Vector2 centre)
        {
            // The visible size in world units is the whole canvas (a phone is wider than 1440 x 900), so the world
            // edge meets the physical screen edge, cutout included.
            var size = _view.rect.size;
            var visible = size.x > 1f && size.y > 1f ? size : new Vector2(Places.ViewWidth, Places.ViewHeight);
            var clamped = MapCamera.Clamp(new WorldPoint(centre.x, centre.y), visible.x, visible.y);
            return new Vector2(clamped.X, clamped.Y);
        }

        private void SetCamera(Vector2 centre)
        {
            _camera = ClampedCamera(centre);
            _world.anchoredPosition = -_camera;
        }

        private void PlaceCharacters(Vector2 spot, float hopPlayer, float hopEva)
        {
            _waveZone.anchoredPosition = spot;
            SetFeet(_player.Root, spot + new Vector2(-PairOffsetX, -FeetDrop), hopPlayer);
            SetFeet(_eva.Root, spot + new Vector2(PairOffsetX, -FeetDrop), hopEva);
        }

        private static void SetFeet(RectTransform root, Vector2 feet, float hop)
        {
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = feet + new Vector2(0f, hop);
        }

        private void AddPicture(string name, string sprite, Vector2 position, Vector2 size, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_world, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = raycast;
        }

        // Stepping stones along a road with no bespoke art: a handful of interchangeable stone sprites scattered at
        // MapPath.StonePoints, each picking a variant and a small perpendicular jitter from a seed derived from the
        // place and its index along the road, so the scatter is fixed (same every load) without needing art per place.
        private const float StoneSpacing = 130f, StoneSize = 90f, StoneJitter = 22f;
        private const int StoneVariants = 3;

        private void AddStonePath(Place place)
        {
            var points = MapPath.StonePoints(place.Road, StoneSpacing);
            for (var i = 0; i < points.Length; i++)
            {
                var seed = (int)place.Id * 7919 + i * 104729;
                var variant = ((seed % StoneVariants) + StoneVariants) % StoneVariants;
                var jitterX = (Hash01(seed) - 0.5f) * 2f * StoneJitter;
                var jitterY = (Hash01(seed + 1) - 0.5f) * 2f * StoneJitter;
                var position = new Vector2(points[i].X + jitterX, points[i].Y + jitterY);
                AddPicture("Stone_" + place.Id + "_" + i, "world/stone_" + variant, position, new Vector2(StoneSize, StoneSize), false);
            }
        }

        // A stable pseudo-random value in [0, 1) from an int seed (no System.Random instance needed for a one-off).
        private static float Hash01(int seed)
        {
            unchecked
            {
                var x = (uint)seed;
                x = (x ^ 61) ^ (x >> 16);
                x *= 9;
                x ^= x >> 4;
                x *= 0x27d4eb2d;
                x ^= x >> 15;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private void AddPlaceButton(Place place)
        {
            var box = place.TapBox;
            EvaUi.IconButton(_world, "Place_" + place.Id, EvaUi.Sprite(place.BuildingSprite), new Vector2(0.5f, 0.5f),
                new Vector2(box.X, box.Y), new Vector2(box.Width, box.Height), () => OnPlaceTapped(place));
        }

        // The characters' tap area: Eva waves when it is tapped. It is a 240 unit square centred on the pair. It may
        // overlap its own building, but it sits below the place buttons in sibling order, so the building buttons win.
        private void AddWaveZone()
        {
            var zone = new GameObject("WaveZone", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
            zone.transform.SetParent(_world, false);
            _waveZone = (RectTransform)zone.transform;
            _waveZone.anchorMin = _waveZone.anchorMax = _waveZone.pivot = new Vector2(0.5f, 0.5f);
            _waveZone.sizeDelta = new Vector2(Place.StandingAreaSize, Place.StandingAreaSize);

            var image = zone.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = zone.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (EvaUi.Sfx != null) EvaUi.Sfx.Tap();
                _eva.Wave();
            });
        }
    }
}
