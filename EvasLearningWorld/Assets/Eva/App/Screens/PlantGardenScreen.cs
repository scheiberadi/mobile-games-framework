using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Plant Growth: a garden. One to three plants (of different kinds: a daisy, a tomato, ...) stand in the flower bed (more
    // in the later rounds), each a seed that grows through its own number of stages. A thought cloud beside the top of each plant (it moves
    // up as the plant grows) shows what it wants next: a water drop, a sun, a bag of fertilizer, one or more of them, each with dots for how
    // many doses (the child sees the dots turn green). The child drags the watering can, the sun, or the fertilizer bag onto a plant and
    // keeps it there a moment: that is a dose; drag it away and back for the next.
    // When the plant has everything it asked for it grows, with a squash-and-stretch and sparkles, and asks again. A thing it did not ask for
    // earns a kind word from Eva. A round ends when every plant is in flower; a session is three rounds, each harder (more plants, more
    // doses; Rules/PlantGarden.cs). Nothing is a mistake.
    //
    // Layout in "picture space", the 1920 x 900 frame of the background: x = px - 960, y = 450 - py. The plants stand in a row in the middle;
    // the sun and the fertilizer wait on the left, the watering can on the right. Every item is put back where it lives when let go.
    public sealed class PlantGardenScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class ItemView
        {
            public string Id;
            public GardenSupply Supply;
            public Vector2 Home;
            public Vector2 Tip;            // from the item's middle to the point that has to be over the plant (picture space)
            public float Tilt;             // degrees the item leans while it is held (it pours)
            public DragItem Drag;
            public RectTransform Art;
            public bool Held;              // a finger (or the hint hand) has it
            public float Lean;             // 0..1: how far it has leaned
            public float PourTimer;
        }

        private sealed class PlantView
        {
            public RectTransform Plant;
            public Image PlantImage;
            public RectTransform Cloud;
            public Image[] Icons = new Image[GardenPlant.SupplyCount];
            public Image[][] Dots = new Image[GardenPlant.SupplyCount][];
            public bool Growing;
            public float Sway;             // seconds since a dose / a word, for the little reaction
            public float Shake;            // "no" shake left
            public float BloomTime;
            public int ShownStage = -1;
            public string ShownKind;
        }

        private sealed class Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;       // picture space
            public Vector2 Velocity;
            public float Life;
            public float Total;
            public float Tall;           // 0: a round thing (sparkle, crumb); else its height as a multiple of its width (a drop)
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        private const float PlantBase = -235f;      // where the plants' feet stand, in the flower bed
        private const float UnitsPerPixel = 0.7f;   // a plant picture is drawn this many canvas units per pixel (the last stage is 512 px tall)
        public const float PlantHalfWidth = 120f;   // the widest a plant is, half of it, for keeping clear of the things at the sides
        public const float OverRange = 170f;
        private const float CloudBase = 20f;        // the thought cloud's tail ends this far above the plant's top
        private const float CloudPerIcon = 82f, CloudPadding = 110f;
        private const float CloudAspect = 286f / 742f;
        private const float CloudTail = 0.07f;      // the bubble's little puffs are this far (of its width) right of its middle
        private const float ItemTapSize = 240f;
        private static readonly float[][] PlantXs = { new[] { 0f }, new[] { -190f, 190f }, new[] { -350f, 0f, 350f } };

        private const float GrowSeconds = 0.9f;
        private const float ReturnSpeed = 2600f;
        private const int ParticlePool = 40;
        private const float IdleHintSeconds = 12f;
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintRestSeconds = 0.4f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _plantField, _itemField, _particleField;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;
        private Sprite _dropSprite, _sunSprite, _bagSprite;
        private readonly Sprite[][] _stageSprites = new Sprite[PlantGarden.Kinds.Length][];

        private readonly List<ItemView> _items = new List<ItemView>();
        private readonly PlantView[] _plantViews = new PlantView[3];
        private readonly List<Particle> _particles = new List<Particle>();
        private int _nextParticle;

        private PlantGarden _garden;
        private System.Random _rng;
        private int _roundIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _anyHeld;
        private float _idle, _time;
        private bool _announcing;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private int _rightLineIndex;
        private Coroutine _hintRoutine;

        public PlantGardenScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state and hooks for tests.
        public PlantGarden Garden => _garden;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public IReadOnlyList<string> ItemIds { get { var ids = new List<string>(); foreach (var i in _items) ids.Add(i.Id); return ids; } }
        public DragItem Item(string id) => Find(id).Drag;

        // The middle of plant index as it is now (picture space): the thing the child holds has to be over this.
        public Vector2 PlantPoint(int index)
        {
            var view = _plantViews[index];
            return new Vector2(PlantXs[_garden.Plants.Count - 1][index], PlantBase + Mathf.Min(view.Plant.sizeDelta.y * 0.5f, 130f));
        }

        // The top of plant index as it is now, with the squash of a growing plant (picture space).
        private float PlantTop(int index)
        {
            var view = _plantViews[index];
            return PlantBase + view.Plant.sizeDelta.y * view.Plant.localScale.y;
        }

        // Puts the item so that its working point is at `point` (picture space).
        public void PlaceItem(string id, Vector2 point)
        {
            var item = Find(id);
            item.Drag.Rect.anchoredPosition = Place(point - item.Tip);
        }

        public void Grab(string id) => OnBeginDrag(Find(id));

        public void Release(string id) => OnEndDrag(Find(id));

        private ItemView Find(string id)
        {
            foreach (var item in _items) if (item.Id == id) return item;
            throw new KeyNotFoundException(id);
        }

        // All the layout above is in "picture space": the 1920x900 frame of the background. The picture is stretched over the whole screen
        // (full-bleed) while the screen's own space is centred on the safe area, so on a phone the two differ: Place() turns a picture-space
        // point into this screen's space (and Scene() back). Tests turn it off (identity).
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
            _dropSprite = EvaUi.Sprite("sciencelab/drop");
            _sunSprite = EvaUi.Sprite("world/sun");
            _bagSprite = EvaUi.Sprite("sciencelab/fertilizer_bag");
            for (var k = 0; k < PlantGarden.Kinds.Length; k++)
            {
                _stageSprites[k] = new Sprite[PlantGarden.Kinds[k].Stages];
                for (var stage = 0; stage < _stageSprites[k].Length; stage++) _stageSprites[k][stage] = EvaUi.Sprite("sciencelab/plant_" + PlantGarden.Kinds[k].Id + "_" + stage);
            }
            AddPictureBackground();
            BuildEva();
            _plantField = Container("Plants");
            BuildPlants();
            _particleField = Container("Particles");
            BuildParticles();
            _itemField = Container("Items");
            BuildItems();
            _hand = new PointerHand(Root, _runner);
            BuildConfetti();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            UpdatePlacement();
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
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
            _garden = new PlantGarden(_roundIndex, _rng);
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            foreach (var particle in _particles) particle.Rect.gameObject.SetActive(false);

            var count = _garden.Plants.Count;
            for (var i = 0; i < _plantViews.Length; i++)
            {
                var view = _plantViews[i];
                var active = i < count;
                view.Plant.gameObject.SetActive(active);
                view.Cloud.gameObject.SetActive(active);
                view.Growing = false;
                view.Shake = 0f;
                view.BloomTime = 0f;
                view.ShownStage = -1;
                view.ShownKind = null;
                if (!active) continue;
                view.Plant.anchoredPosition = Place(new Vector2(PlantXs[count - 1][i], PlantBase));
                view.Plant.localScale = Vector3.one;
                view.Plant.localRotation = Quaternion.identity;
                ShowStage(i);
                _runner.StartCoroutine(PopIn(view.Plant, 0.25f, 0.1f * i));
            }
            foreach (var item in _items)
            {
                item.Held = false;
                item.Lean = 0f;
                item.Drag.Rect.anchoredPosition = Place(item.Home);
                item.Art.localRotation = Quaternion.identity;
                item.Art.localScale = Vector3.one;
            }
            _runner.StartCoroutine(SayPrompt());
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("garden_prompt");
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildPlants()
        {
            for (var i = 0; i < _plantViews.Length; i++)
            {
                var view = new PlantView();
                var plant = new GameObject("Plant" + i, typeof(RectTransform), typeof(Image));
                plant.transform.SetParent(_plantField, false);
                view.Plant = (RectTransform)plant.transform;
                view.Plant.anchorMin = view.Plant.anchorMax = new Vector2(0.5f, 0.5f);
                view.Plant.pivot = new Vector2(0.5f, 0f); // the plant grows up from the soil
                view.Plant.sizeDelta = new Vector2(PlantHalfWidth * 2f, PlantHalfWidth * 2f);
                view.PlantImage = plant.GetComponent<Image>();
                view.PlantImage.preserveAspect = true;
                view.PlantImage.raycastTarget = false;

                // The thought cloud (the one the animals have in Feeding), with a slot for each of the three things.
                var cloud = new GameObject("Thought" + i, typeof(RectTransform), typeof(Image));
                cloud.transform.SetParent(_plantField, false);
                view.Cloud = (RectTransform)cloud.transform;
                view.Cloud.anchorMin = view.Cloud.anchorMax = view.Cloud.pivot = new Vector2(0.5f, 0.5f);
                var cloudImage = cloud.GetComponent<Image>();
                cloudImage.sprite = EvaUi.Sprite("zoofarm/bubble");
                cloudImage.raycastTarget = false;
                for (var s = 0; s < GardenPlant.SupplyCount; s++)
                {
                    var icon = new GameObject("Icon" + s, typeof(RectTransform), typeof(Image));
                    icon.transform.SetParent(view.Cloud, false);
                    var iconRect = (RectTransform)icon.transform;
                    iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.sizeDelta = new Vector2(64f, 64f);
                    view.Icons[s] = icon.GetComponent<Image>();
                    view.Icons[s].sprite = s == 0 ? _dropSprite : s == 1 ? _sunSprite : _bagSprite;
                    view.Icons[s].preserveAspect = true;
                    view.Icons[s].raycastTarget = false;
                    view.Dots[s] = new Image[3];
                    for (var d = 0; d < 3; d++)
                    {
                        var dot = new GameObject("Dot" + d, typeof(RectTransform), typeof(Image));
                        dot.transform.SetParent(view.Cloud, false);
                        var dotRect = (RectTransform)dot.transform;
                        dotRect.anchorMin = dotRect.anchorMax = dotRect.pivot = new Vector2(0.5f, 0.5f);
                        dotRect.sizeDelta = new Vector2(20f, 20f);
                        view.Dots[s][d] = dot.GetComponent<Image>();
                        view.Dots[s][d].sprite = EvaUi.Sprite("icons/dot");
                        view.Dots[s][d].raycastTarget = false;
                    }
                }
                _plantViews[i] = view;
            }
        }

        private void BuildParticles()
        {
            for (var i = 0; i < ParticlePool; i++)
            {
                var go = new GameObject("Particle" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_particleField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                go.SetActive(false);
                _particles.Add(new Particle { Rect = rect, Image = image });
            }
        }

        private void BuildItems()
        {
            AddItem("can", GardenSupply.Water, "sciencelab/watering_can", new Vector2(650f, 20f), 230f, new Vector2(-90f, -25f), 24f);
            AddItem("sun", GardenSupply.Sun, "world/sun", new Vector2(-650f, 40f), 180f, Vector2.zero, 0f);
            AddItem("fertilizer", GardenSupply.Fertilizer, "sciencelab/fertilizer_bag", new Vector2(-650f, -200f), 190f, new Vector2(-75f, -10f), 35f);
        }

        private void AddItem(string id, GardenSupply supply, string sprite, Vector2 home, float width, Vector2 tip, float tilt)
        {
            var art = EvaUi.Sprite(sprite);
            var item = new ItemView { Id = id, Supply = supply, Home = home, Tip = tip, Tilt = tilt };
            item.Drag = DragItem.Create(_itemField, id, EvaUi.Sprite("icons/dot"), Place(home), ItemTapSize);
            item.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
            item.Drag.BeginDrag += _ => OnBeginDrag(item);
            item.Drag.EndDrag += _ => OnEndDrag(item);
            var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(item.Drag.transform, false);
            item.Art = (RectTransform)go.transform;
            item.Art.anchorMin = item.Art.anchorMax = item.Art.pivot = new Vector2(0.5f, 0.5f);
            item.Art.anchoredPosition = Vector2.zero;
            item.Art.sizeDelta = new Vector2(width, width * art.rect.height / art.rect.width);
            var image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            _items.Add(item);
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
            _plantField.gameObject.SetActive(!ended);
            _particleField.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- The things the child holds ---------------------------------------------------------------------------------

        private void OnBeginDrag(ItemView item)
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for something never waits for the hint hand
            item.Held = true;
            item.PourTimer = 0f;
            _idle = 0f;
        }

        private void OnEndDrag(ItemView item)
        {
            item.Held = false;
            _idle = 0f;
        }

        private Vector2 WorkingPoint(ItemView item) => Scene(item.Drag.Rect.anchoredPosition) + item.Tip;

        // The plant the item is over (nearest, within OverRange), or -1.
        private int PlantUnder(ItemView item)
        {
            var point = WorkingPoint(item);
            var count = _garden.Plants.Count;
            var best = -1;
            var bestDistance = OverRange;
            for (var i = 0; i < count; i++)
            {
                var distance = Vector2.Distance(point, PlantPoint(i));
                if (distance < bestDistance) { best = i; bestDistance = distance; }
            }
            return best;
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

        // One frame of the garden. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_garden == null || _sessionEnded) return;
            _time += seconds;
            _anyHeld = false;
            var over = new bool[3, GardenPlant.SupplyCount];
            foreach (var item in _items)
            {
                if (item.Held) _anyHeld = true;
                if (item.Held && !_roundOver)
                {
                    var plant = PlantUnder(item);
                    if (plant >= 0) over[plant, (int)item.Supply] = true;
                }
            }
            if (!_roundOver)
            {
                for (var i = 0; i < _garden.Plants.Count; i++)
                {
                    var view = _plantViews[i];
                    var plant = _garden.Plants[i];
                    if (view.Growing || plant.Done) continue;
                    plant.Step(seconds, over[i, 0], over[i, 1], over[i, 2]);
                    foreach (var supply in plant.JustDosed) OnDosed(i, supply);
                    foreach (var supply in plant.JustUnasked) OnUnasked(i, supply);
                    if (plant.JustGrew) _runner.StartCoroutine(Grow(i));
                }
            }
            UpdateItems(seconds);
            UpdateParticles(seconds);
            for (var i = 0; i < _garden.Plants.Count; i++) UpdatePlant(i, seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void OnDosed(int plantIndex, GardenSupply supply)
        {
            Haptics.Tap();
            _plantViews[plantIndex].Sway = 0.6f;
            if (_game != null && _game.Sfx != null) _game.Sfx.Place();
        }

        private void OnUnasked(int plantIndex, GardenSupply supply)
        {
            _plantViews[plantIndex].Shake = 0.6f;
            Say(supply == GardenSupply.Water ? "garden_no_water" : supply == GardenSupply.Sun ? "garden_no_sun" : "garden_no_fertilizer");
        }

        // --- Drawing the plants and their thoughts ----------------------------------------------------------------------

        private void ShowStage(int index)
        {
            var view = _plantViews[index];
            var plant = _garden.Plants[index];
            if (view.ShownStage == plant.Stage && view.ShownKind == plant.Kind.Id) return;
            view.ShownStage = plant.Stage;
            view.ShownKind = plant.Kind.Id;
            var kind = System.Array.FindIndex(PlantGarden.Kinds, k => k.Id == plant.Kind.Id);
            var sprite = _stageSprites[kind][plant.Stage];
            view.PlantImage.sprite = sprite;
            view.Plant.sizeDelta = sprite.rect.size * UnitsPerPixel;
        }

        private void UpdatePlant(int index, float seconds)
        {
            var view = _plantViews[index];
            var plant = _garden.Plants[index];
            var count = _garden.Plants.Count;
            var phase = index * 1.3f;
            if (!view.Growing)
            {
                ShowStage(index);
                var wobble = Mathf.Sin(_time * 2.2f + phase) * 1.5f;
                if (view.Sway > 0f) { view.Sway -= seconds; wobble += Mathf.Sin(_time * 18f) * 4f * Mathf.Clamp01(view.Sway / 0.6f); }
                if (view.Shake > 0f) { view.Shake -= seconds; wobble += Mathf.Sin(_time * 30f) * 7f * Mathf.Clamp01(view.Shake / 0.6f); }
                view.Plant.localRotation = Quaternion.Euler(0f, 0f, wobble);
                if (plant.Done)
                {
                    view.BloomTime += seconds;
                    var bounce = Mathf.Max(0f, Mathf.Sin(view.BloomTime * 3f)) * 0.04f;
                    view.Plant.localScale = new Vector3(1f - bounce * 0.5f, 1f + bounce, 1f);
                }
                else view.Plant.localScale = Vector3.one;
            }
            UpdateThought(index, count);
        }

        private void UpdateThought(int index, int count)
        {
            var view = _plantViews[index];
            var plant = _garden.Plants[index];
            view.Cloud.gameObject.SetActive(!plant.Done);
            if (plant.Done) return;
            var asked = 0;
            for (var s = 0; s < GardenPlant.SupplyCount; s++) if (plant.Asks((GardenSupply)s)) asked++;
            var width = CloudPadding + CloudPerIcon * asked;
            var height = width * CloudAspect;
            view.Cloud.sizeDelta = new Vector2(width, height);
            // Beside the top of the plant: the bubble's puffs lead down to it, so the cloud rides up as the plant grows.
            view.Cloud.anchoredPosition = Place(new Vector2(PlantXs[count - 1][index] - CloudTail * width, PlantTop(index) + CloudBase + height * 0.5f));
            var slot = 0;
            for (var s = 0; s < GardenPlant.SupplyCount; s++)
            {
                var supply = (GardenSupply)s;
                var icon = view.Icons[s];
                var wanted = plant.Asks(supply);
                icon.gameObject.SetActive(wanted);
                for (var d = 0; d < 3; d++) view.Dots[s][d].gameObject.SetActive(wanted && d < plant.Required(supply));
                if (!wanted) continue;
                var x = (slot - (asked - 1) * 0.5f) * CloudPerIcon;
                slot++;
                var complete = plant.Complete(supply);
                icon.rectTransform.anchoredPosition = new Vector2(x, height * 0.1f);
                icon.color = complete ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                var required = plant.Required(supply);
                for (var d = 0; d < required; d++)
                {
                    var dot = view.Dots[s][d];
                    dot.rectTransform.anchoredPosition = new Vector2(x + (d - (required - 1) * 0.5f) * 24f, -height * 0.24f);
                    var given = d < plant.Given(supply);
                    var filling = d == plant.Given(supply) ? plant.DoseProgress(supply) : 0f;
                    dot.color = given ? new Color(0.25f, 0.75f, 0.3f) : Color.Lerp(new Color(0.45f, 0.4f, 0.38f), new Color(0.25f, 0.75f, 0.3f), filling);
                    dot.rectTransform.localScale = Vector3.one * (1f + 0.35f * filling);
                }
            }
        }

        // A plant grows: it squashes down, turns into the next stage, springs up, and sparkles fly.
        private IEnumerator Grow(int index)
        {
            var view = _plantViews[index];
            var plant = _garden.Plants[index];
            view.Growing = true;
            Haptics.Tap();
            if (_game != null && _game.Sfx != null) _game.Sfx.Pop();
            var swapped = false;
            for (var t = 0f; t < GrowSeconds; t += Time.deltaTime)
            {
                var k = t / GrowSeconds;
                float sx, sy;
                if (k < 0.3f) { var q = k / 0.3f; sx = 1f + 0.15f * q; sy = 1f - 0.3f * q; }
                else if (k < 0.65f) { var q = (k - 0.3f) / 0.35f; sx = 1.15f - 0.3f * q; sy = 0.7f + 0.6f * q; }
                else { var q = (k - 0.65f) / 0.35f; sx = 0.85f + 0.15f * q; sy = 1.3f - 0.3f * q; }
                if (!swapped && k >= 0.3f)
                {
                    swapped = true;
                    ShowStage(index);
                    Sparkle(new Vector2(PlantXs[_garden.Plants.Count - 1][index], PlantBase + view.Plant.sizeDelta.y * 0.6f));
                }
                view.Plant.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }
            view.Plant.localScale = Vector3.one;
            view.Growing = false;
            if (plant.Done)
            {
                Haptics.Win();
                Say("garden_bloom");
            }
        }

        private void Sparkle(Vector2 centre)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                Spawn(centre, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 260f + new Vector2(0f, 60f), 0.8f, EvaUi.Sprite("sciencelab/space_star"), Color.white, 44f, 0f);
            }
        }

        // --- Items and particles ----------------------------------------------------------------------------------------

        private void UpdateItems(float seconds)
        {
            foreach (var item in _items)
            {
                var held = item.Held || _helpRunning && item.Held;
                item.Lean = Mathf.MoveTowards(item.Lean, held ? 1f : 0f, seconds * 6f);
                var sway = held ? Mathf.Sin(_time * 14f) * 3f : 0f;
                var spin = item.Id == "sun" ? _time * 25f : 0f;
                item.Art.localRotation = Quaternion.Euler(0f, 0f, item.Tilt * item.Lean + sway + spin);
                item.Art.localScale = Vector3.one * (1f + 0.1f * item.Lean);
                if (!item.Held && !_helpRunning)
                {
                    var target = Place(item.Home);
                    var position = item.Drag.Rect.anchoredPosition;
                    if (position != target) item.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
                }
                if (item.Held && item.Id != "sun") Pour(item, seconds);
            }
        }

        // The can and the bag pour from their spout while they are held.
        private void Pour(ItemView item, float seconds)
        {
            item.PourTimer -= seconds;
            if (item.PourTimer > 0f) return;
            item.PourTimer = item.Id == "fertilizer" ? 0.05f : 0.045f;
            var from = WorkingPoint(item);
            from += new Vector2(Random.Range(-8f, 8f), 0f);
            if (item.Id == "fertilizer")
                Spawn(from, new Vector2(Random.Range(-40f, 20f), -300f), 0.5f, EvaUi.Sprite("icons/dot"), new Color(0.42f, 0.28f, 0.16f), 16f, 0f);
            else
                Spawn(from, new Vector2(Random.Range(-30f, 10f), -420f), 0.55f, _dropSprite, Color.white, 40f, 1.5f);
        }

        private void Spawn(Vector2 position, Vector2 velocity, float life, Sprite sprite, Color color, float size, float tall)
        {
            var particle = _particles[_nextParticle];
            _nextParticle = (_nextParticle + 1) % _particles.Count;
            particle.Position = position;
            particle.Velocity = velocity;
            particle.Life = particle.Total = life;
            particle.Tall = tall;
            particle.Image.sprite = sprite;
            particle.Image.color = color;
            particle.Rect.sizeDelta = new Vector2(size, size * (tall > 0f ? tall : 1f));
            particle.Rect.gameObject.SetActive(true);
            particle.Rect.anchoredPosition = Place(position);
        }

        private void UpdateParticles(float seconds)
        {
            foreach (var particle in _particles)
            {
                if (!particle.Rect.gameObject.activeSelf) continue;
                particle.Life -= seconds;
                if (particle.Life <= 0f) { particle.Rect.gameObject.SetActive(false); continue; }
                particle.Position += particle.Velocity * seconds;
                if (particle.Tall == 0f) particle.Velocity += new Vector2(0f, -200f) * seconds; // sparkles drift down a little
                particle.Rect.anchoredPosition = Place(particle.Position);
                var fade = Mathf.Clamp01(particle.Life / (particle.Total * 0.4f));
                var color = particle.Image.color;
                color.a = fade;
                particle.Image.color = color;
            }
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
            if (_roundOver || _helpRunning || _anyHeld) { _idle = 0f; return; }
            foreach (var view in _plantViews) if (view.Growing) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds)
            {
                _idle = 0f;
                _hintRoutine = _runner.StartCoroutine(RunHint());
            }
        }

        // The child took hold of something while the hint hand was out: the hint stops and the thing goes home.
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
            foreach (var item in _items) item.Held = false;
            _idle = 0f;
        }

        // After a long quiet the hand carries what a plant wants to that plant, for one dose.
        private IEnumerator RunHint()
        {
            var count = _garden.Plants.Count;
            var target = -1;
            ItemView chosen = null;
            for (var i = 0; i < count && chosen == null; i++)
            {
                var plant = _garden.Plants[i];
                if (plant.Done || _plantViews[i].Growing) continue;
                for (var s = 0; s < GardenPlant.SupplyCount && chosen == null; s++)
                {
                    if (!plant.Asks((GardenSupply)s) || plant.Complete((GardenSupply)s)) continue;
                    foreach (var item in _items) if (item.Supply == (GardenSupply)s) { chosen = item; target = i; break; }
                }
            }
            if (chosen == null) yield break;

            _helpRunning = true;
            _hinted = true;
            chosen.Held = true;
            _eva.SetTalking(true);
            _game.Voice.Say("garden_hint");

            var from = chosen.Drag.Rect.anchoredPosition;
            var to = Place(PlantPoint(target) - chosen.Tip);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                chosen.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            chosen.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds + GardenPlant.DoseSeconds);
            _hand.Pulse(false);
            chosen.Held = false;

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            _idle = 0f;
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_garden.Done || _announcing) return;
            foreach (var view in _plantViews) if (view.Growing) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
            foreach (var item in _items) item.Held = false;
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
            yield return new WaitForSeconds(1.2f); // time to look at the flowers

            _roundIndex++;
            if (_roundIndex >= PlantGarden.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _plantViews[0].Plant.position);
        }

        // --- Small helpers ----------------------------------------------------------------------------------------------

        private static IEnumerator PopIn(RectTransform target, float duration, float delay)
        {
            target.localScale = Vector3.one * 0.3f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

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
