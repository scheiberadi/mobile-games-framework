using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // How the animals of a zone get away when a wrong animal is dropped there (and how a wrong item goes back to its place).
    public enum RefugeKind
    {
        Run,  // hop, run into the distance (a barn, the trees), getting smaller and fainter; a wrong item just slides back
        Fly,  // hop, fly up and off the top; a wrong item falls back spinning
        Dive, // hop, sink into the water, fading; a wrong item sinks and comes back
    }

    public struct ResidentSlot
    {
        public Vector2 Position;
        public float Size;

        public ResidentSlot(float x, float y, float size)
        {
            Position = new Vector2(x, y);
            Size = size;
        }
    }

    // Everything one residents scene needs to know about its background picture, in canvas units (y up). Zone i belongs to
    // Categories[i]; the round's bins are put in this order (DropSortRound.WithBinOrder).
    public sealed class ResidentsLayout
    {
        public string[] Categories;
        public Vector2 Home;                 // where the current animal waits
        public float NearHomeRadius;         // a drop this close to Home is no drop at all (the animal was just let go)
        public Vector2[] Gates;              // where the hand points to in a hint, per zone
        public ResidentSlot[][] Slots;       // per zone: the near slots (in fill order), then FarCount far ones
        public int FarCount;                 // the animals already there fill the far slots first, then the first near ones
        public RefugeKind[] Refuge;
        public Vector2[] GlowCentre, GlowSize;
        public Func<Vector2, int> RawZoneAt; // the zone a point on the picture belongs to, -1 for none
        public Func<int, int, Vector2, Vector2> RunTarget; // zone, slot, slot position -> where a running animal goes

        public int ZoneCount => Categories.Length;
        public int NearCount(int zone) => Slots[zone].Length - FarCount;

        // --- Domestic vs Wild: a farm pasture left and a forest pasture right behind a fence with a gate each ---------------

        // Measured on world/domestic_wild_bg (1920x900 frame): the gates in the front fence, the start of the road, two rows of
        // animals just behind the fence, and three small ones far back.
        public static readonly ResidentsLayout DomesticVsWild = BuildDomesticVsWild();

        private static ResidentsLayout BuildDomesticVsWild()
        {
            var startX = new[] { -640f, 110f };
            var rowY = new[] { 70f, 150f };
            var farX = new[] { new[] { -380f, -290f, -200f }, new[] { 120f, 230f, 340f } };
            var slots = new ResidentSlot[2][];
            for (var zone = 0; zone < 2; zone++)
            {
                var list = new List<ResidentSlot>();
                for (var k = 0; k < 10; k++)
                    list.Add(new ResidentSlot(startX[zone] + k / 2 * 105f + (k % 2 == 1 ? 52.5f : 0f), rowY[k % 2], 90f));
                foreach (var x in farX[zone]) list.Add(new ResidentSlot(x, 205f, 60f));
                slots[zone] = list.ToArray();
            }
            return new ResidentsLayout
            {
                Categories = new[] { "domestic", "wild" },
                Home = new Vector2(-120f, -300f),
                NearHomeRadius = 0f,
                Gates = new[] { new Vector2(-483f, -43f), new Vector2(298f, -43f) },
                Slots = slots,
                FarCount = 3,
                Refuge = new[] { RefugeKind.Run, RefugeKind.Run },
                GlowCentre = new[] { new Vector2(-555f, 92.5f), new Vector2(555f, 92.5f) },
                GlowSize = new[] { new Vector2(1090f, 335f), new Vector2(1090f, 335f) },
                // Drop anywhere on a pasture: left of the hedge is the farm, right of it the forest; the fence and everything
                // behind it counts, the meadow in front of the fence does not.
                RawZoneAt = p => p.y < -75f ? -1 : p.x < 0f ? 0 : 1,
                // The farm animals run into the barn (its door), the forest animals into the trees along the back edge.
                RunTarget = (zone, slot, _) => zone == 0 ? new Vector2(-660f, 215f) : new Vector2(380f + slot % 5 * 60f, 225f),
            };
        }

        // --- Land / Sea / Air: sky on top, a meadow in the middle, a bay below, a stump at the left where the waiting animal stands -----

        // Measured on world/land_sea_air_bg (1920x900 frame): horizon at y 105 (top of the grass), shore at y -90, the bay's
        // outline (the beach fills the bottom-right corner, where the companion pair stands); the stump is at (-562, 36) and the animal waits in front of it.
        public static readonly ResidentsLayout LandSeaAir = BuildLandSeaAir();

        private static readonly Vector2[] BayOutline =
        {
            new Vector2(-1100f, -95f), new Vector2(120f, -95f), new Vector2(320f, -120f), new Vector2(500f, -160f),
            new Vector2(610f, -225f), new Vector2(590f, -300f), new Vector2(500f, -370f), new Vector2(420f, -470f), new Vector2(-1100f, -470f),
        };

        private static ResidentsLayout BuildLandSeaAir()
        {
            // Ten slots per zone, listed in the order they fill, scattered so the first five already look spread out.
            var air = new List<ResidentSlot>();
            var land = new List<ResidentSlot>();
            var sea = new List<ResidentSlot>();
            var seaY = new[] { -190f, -290f, -380f };
            for (var i = 0; i < 10; i++)
            {
                var j = i * 3 % 10;
                air.Add(new ResidentSlot(-330f + 100f * j, j % 2 == 0 ? 250f : 335f, 90f));
                land.Add(new ResidentSlot(-250f + j / 2 * 130f + (j % 2 == 1 ? 65f : 0f), j % 2 == 0 ? 55f : -35f, 90f));
                sea.Add(new ResidentSlot(-600f + 95f * j, seaY[j % 3], 90f));
            }
            return new ResidentsLayout
            {
                Categories = new[] { "air", "land", "sea" },
                Home = new Vector2(-470f, 55f), // beside the stump, clear of the Back button above it
                NearHomeRadius = 150f,
                Gates = new[] { new Vector2(150f, 280f), new Vector2(100f, 10f), new Vector2(-150f, -260f) },
                Slots = new[] { air.ToArray(), land.ToArray(), sea.ToArray() },
                FarCount = 0,
                Refuge = new[] { RefugeKind.Fly, RefugeKind.Run, RefugeKind.Dive },
                GlowCentre = new[] { new Vector2(0f, 280f), new Vector2(0f, 10f), new Vector2(-340f, -280f) },
                GlowSize = new[] { new Vector2(2200f, 350f), new Vector2(2200f, 190f), new Vector2(1520f, 370f) },
                RawZoneAt = p => InBay(p) ? 2 : p.y >= 105f ? 0 : p.y >= -85f ? 1 : -1,
                // The land animals run off to the trees at the nearest edge.
                RunTarget = (zone, slot, position) => new Vector2(position.x < 0f ? -850f : 850f, position.y + 40f),
            };
        }

        private static bool InBay(Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = BayOutline.Length - 1; i < BayOutline.Length; j = i++)
            {
                var a = BayOutline[i];
                var b = BayOutline[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }

    // The animals standing in a scene's zones (DropSortScreen's "residents scene", e.g. Domestic vs Wild's two pastures): a few are
    // already there when a round starts, the ones the child sorts join them. A wrong drop makes the zone's animals hop, show a "!"
    // and get away (run, fly or dive, then come back); a won round makes them all hop for joy under a burst of confetti.
    // Pictures only; the game logic stays in DropSortScreen.
    public sealed class ResidentsScene
    {
        private const float DistantScale = 0.35f;

        private readonly ResidentsLayout _layout;
        private readonly MonoBehaviour _runner;
        private readonly string _spritePrefix;
        private readonly Image[][] _images, _alerts;
        private readonly int[][] _motion;
        private readonly int[] _nearUsed, _farUsed;
        private readonly Image[] _glow;
        private readonly GameObject _confetti;

        public ResidentsScene(ResidentsLayout layout, MonoBehaviour runner, RectTransform glowParent, RectTransform residentParent,
            Transform confettiParent, string spritePrefix)
        {
            _layout = layout;
            _runner = runner;
            _spritePrefix = spritePrefix;
            var zones = layout.ZoneCount;
            _images = new Image[zones][];
            _alerts = new Image[zones][];
            _motion = new int[zones][];
            _nearUsed = new int[zones];
            _farUsed = new int[zones];
            _glow = new Image[zones];

            for (var zone = 0; zone < zones; zone++)
            {
                var glowGo = new GameObject("ZoneGlow" + zone, typeof(RectTransform), typeof(Image));
                glowGo.transform.SetParent(glowParent, false);
                glowGo.transform.SetAsFirstSibling();
                var glowRect = (RectTransform)glowGo.transform;
                glowRect.anchorMin = glowRect.anchorMax = glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.anchoredPosition = layout.GlowCentre[zone];
                glowRect.sizeDelta = layout.GlowSize[zone];
                var glow = glowGo.GetComponent<Image>();
                glow.color = new Color(1f, 1f, 0.75f, 0.22f);
                glow.raycastTarget = false;
                glowGo.SetActive(false);
                _glow[zone] = glow;
            }

            for (var zone = 0; zone < zones; zone++)
            {
                var slots = layout.Slots[zone];
                _images[zone] = new Image[slots.Length];
                _alerts[zone] = new Image[slots.Length];
                _motion[zone] = new int[slots.Length];
                for (var n = 0; n < slots.Length; n++)
                {
                    var k = (layout.NearCount(zone) + n) % slots.Length; // the far ones first, so they are drawn behind the near ones
                    var size = slots[k].Size;
                    var go = new GameObject("Resident" + zone + "_" + k, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(residentParent, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(size, size);
                    var image = go.GetComponent<Image>();
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    _images[zone][k] = image;

                    var alertGo = new GameObject("Alert", typeof(RectTransform), typeof(Image));
                    alertGo.transform.SetParent(go.transform, false);
                    var alertRect = (RectTransform)alertGo.transform;
                    alertRect.anchorMin = alertRect.anchorMax = alertRect.pivot = new Vector2(0.5f, 0.5f);
                    alertRect.anchoredPosition = new Vector2(size * 0.35f, size * 0.55f);
                    alertRect.sizeDelta = new Vector2(size * 0.67f, size * 0.67f);
                    var alert = alertGo.GetComponent<Image>();
                    alert.sprite = EvaUi.Sprite("icons/exclaim");
                    alert.preserveAspect = true;
                    alert.raycastTarget = false;
                    _alerts[zone][k] = alert;
                    alertGo.SetActive(false);

                    go.SetActive(false);
                }
            }

            // An inactive WinCelebration: switching it on plays the fanfare and bursts the confetti.
            _confetti = new GameObject("RoundConfetti", typeof(RectTransform), typeof(WinCelebration));
            _confetti.transform.SetParent(confettiParent, false);
            var confettiRect = (RectTransform)_confetti.transform;
            confettiRect.anchorMin = Vector2.zero;
            confettiRect.anchorMax = Vector2.one;
            confettiRect.offsetMin = Vector2.zero;
            confettiRect.offsetMax = Vector2.zero;
            _confetti.SetActive(false);
        }

        public ResidentsLayout Layout => _layout;

        // The zone an item let go at `position` is dropped in, or -1 (not on any zone, or just released next to its home).
        public int ZoneAt(Vector2 position)
        {
            if (_layout.NearHomeRadius > 0f && Vector2.Distance(position, _layout.Home) < _layout.NearHomeRadius) return -1;
            return _layout.RawZoneAt(position);
        }

        // --- Rounds ---------------------------------------------------------------------------------------------------

        // Clears the last round and stands the animals that are already there (round.Residents, in zone order).
        public void BeginRound(DropSortRound round)
        {
            HideConfetti();
            HideGlow();
            for (var zone = 0; zone < _layout.ZoneCount; zone++)
            {
                _nearUsed[zone] = 0;
                _farUsed[zone] = 0;
                for (var k = 0; k < _images[zone].Length; k++)
                {
                    _motion[zone][k]++; // stops any hop or run still going from the last round
                    _alerts[zone][k].gameObject.SetActive(false);
                    _images[zone][k].gameObject.SetActive(false);
                }
            }
            for (var zone = 0; zone < round.BinCategories.Length; zone++)
                for (var j = 0; j < round.Residents[zone].Length; j++) Place(zone, round.Residents[zone][j], j < _layout.FarCount);
        }

        // An animal the child sorted joins the zone.
        public void Add(int zone, string animalId) => Place(zone, animalId, false);

        // Where the next animal sorted into `zone` will stand (the dropped item flies there).
        public Vector2 NextSlotPosition(int zone)
        {
            var next = Math.Min(_nearUsed[zone], _layout.NearCount(zone) - 1);
            return _layout.Slots[zone][next].Position;
        }

        private void Place(int zone, string animalId, bool far)
        {
            var nearCount = _layout.NearCount(zone);
            if (far ? _farUsed[zone] >= _layout.FarCount : _nearUsed[zone] >= nearCount) return;
            var k = far ? nearCount + _farUsed[zone]++ : _nearUsed[zone]++;
            var image = _images[zone][k];
            _motion[zone][k]++;
            image.sprite = EvaUi.Sprite(_spritePrefix + animalId);
            image.rectTransform.anchoredPosition = _layout.Slots[zone][k].Position;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            image.color = Color.white;
            _alerts[zone][k].gameObject.SetActive(false);
            image.gameObject.SetActive(true);
            _runner.StartCoroutine(PopIn(image.rectTransform, 0.25f));
        }

        private IEnumerable<int> Active(int zone)
        {
            for (var k = 0; k < _images[zone].Length; k++)
                if (_images[zone][k].gameObject.activeSelf) yield return k;
        }

        // --- Hover and celebration --------------------------------------------------------------------------------------

        public void SetHover(int zone)
        {
            for (var i = 0; i < _glow.Length; i++) _glow[i].gameObject.SetActive(i == zone); // the zone under the finger lights up
        }

        public void HideGlow()
        {
            foreach (var glow in _glow) glow.gameObject.SetActive(false);
        }

        public void HideConfetti() => _confetti.SetActive(false);

        // Confetti (and the win fanfare), and every animal there hops for joy.
        public void Celebrate()
        {
            _confetti.SetActive(true);
            for (var zone = 0; zone < _layout.ZoneCount; zone++)
                foreach (var k in Active(zone)) _runner.StartCoroutine(HappyHop(zone, k));
        }

        private IEnumerator HappyHop(int zone, int k)
        {
            var id = ++_motion[zone][k];
            var image = _images[zone][k];
            var rect = image.rectTransform;
            var slot = _layout.Slots[zone][k].Position;
            _alerts[zone][k].gameObject.SetActive(false);
            image.color = Color.white;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = slot;
            yield return new WaitForSeconds(0.3f + 0.06f * (k % 8) + 0.04f * (zone % 2));
            const float hopSeconds = 0.4f;
            for (var hop = 0; hop < 3; hop++)
                for (var t = 0f; t < hopSeconds; t += Time.deltaTime)
                {
                    if (_motion[zone][k] != id) yield break;
                    var lift = Mathf.Sin(Mathf.PI * t / hopSeconds);
                    rect.anchoredPosition = slot + new Vector2(0f, 60f * lift);
                    rect.localScale = new Vector3(1f - 0.08f * lift, 1f + 0.12f * lift, 1f);
                    yield return null;
                }
            rect.anchoredPosition = slot;
            rect.localScale = Vector3.one;
        }

        // --- A wrong drop -----------------------------------------------------------------------------------------------

        public void ScareAll(int zone)
        {
            foreach (var k in Active(zone)) _runner.StartCoroutine(Scare(zone, k));
        }

        // Hop with a "!", get away (run into the distance, fly off the top, dive), wait, come back to the slot.
        private IEnumerator Scare(int zone, int k)
        {
            var id = ++_motion[zone][k];
            var image = _images[zone][k];
            var rect = image.rectTransform;
            var alert = _alerts[zone][k].gameObject;
            var slot = _layout.Slots[zone][k].Position;
            var kind = _layout.Refuge[zone];
            Vector2 refuge;
            float endScale, bounce;
            switch (kind)
            {
                case RefugeKind.Fly: refuge = new Vector2(slot.x + 250f, 560f); endScale = 0.6f; bounce = 14f; break;
                case RefugeKind.Dive: refuge = slot + new Vector2(0f, -110f); endScale = 0.5f; bounce = 0f; break;
                default: refuge = _layout.RunTarget(zone, k, slot); endScale = DistantScale; bounce = 18f; break;
            }

            alert.SetActive(true);
            const float hopSeconds = 0.3f, runSeconds = 0.7f, returnSeconds = 0.8f;
            for (var t = 0f; t < hopSeconds; t += Time.deltaTime)
            {
                if (_motion[zone][k] != id) yield break;
                rect.anchoredPosition = slot + new Vector2(0f, 45f * Mathf.Sin(Mathf.PI * t / hopSeconds));
                yield return null;
            }
            for (var t = 0f; t < runSeconds; t += Time.deltaTime)
            {
                if (_motion[zone][k] != id) yield break;
                var p = t / runSeconds;
                var ease = p * p;
                rect.anchoredPosition = Vector2.Lerp(slot, refuge, ease) + new Vector2(0f, Mathf.Abs(Mathf.Sin(p * Mathf.PI * 5f)) * bounce * (1f - p));
                rect.localScale = Vector3.one * Mathf.Lerp(1f, endScale, ease);
                image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - 0.5f) * 2f));
                yield return null;
            }
            alert.SetActive(false);
            image.color = new Color(1f, 1f, 1f, 0f);
            rect.anchoredPosition = refuge;

            yield return new WaitForSeconds(0.5f + 0.08f * (k % 6));
            for (var t = 0f; t < returnSeconds; t += Time.deltaTime)
            {
                if (_motion[zone][k] != id) yield break;
                var p = t / returnSeconds;
                var ease = 1f - (1f - p) * (1f - p);
                rect.anchoredPosition = Vector2.Lerp(refuge, slot, ease) + new Vector2(0f, Mathf.Abs(Mathf.Sin(p * Mathf.PI * 4f)) * bounce * p);
                rect.localScale = Vector3.one * Mathf.Lerp(endScale, 1f, ease);
                image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p * 3f));
                yield return null;
            }
            rect.anchoredPosition = slot;
            rect.localScale = Vector3.one;
            image.color = Color.white;
        }

        // The wrong item goes back to its place: slides (land), falls spinning (sky) or sinks and reappears (water).
        public IEnumerator ReturnItem(RectTransform item, Image itemImage, int zone)
        {
            var home = _layout.Home;
            var from = item.anchoredPosition;
            var kind = _layout.Refuge[zone];
            if (kind == RefugeKind.Fly)
            {
                const float seconds = 0.6f;
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    var p = t / seconds;
                    item.anchoredPosition = Vector2.Lerp(from, home, p * p);
                    item.localRotation = Quaternion.Euler(0f, 0f, 720f * p);
                    yield return null;
                }
                item.localRotation = Quaternion.identity;
            }
            else if (kind == RefugeKind.Dive)
            {
                const float seconds = 0.45f;
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    var p = t / seconds;
                    item.anchoredPosition = from + new Vector2(0f, -80f * p);
                    item.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, p);
                    itemImage.color = new Color(1f, 1f, 1f, 1f - p);
                    yield return null;
                }
                item.anchoredPosition = home;
                item.localScale = Vector3.one;
                for (var t = 0f; t < 0.25f; t += Time.deltaTime)
                {
                    itemImage.color = new Color(1f, 1f, 1f, t / 0.25f);
                    yield return null;
                }
                itemImage.color = Color.white;
            }
            else
            {
                const float seconds = 0.25f;
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    item.anchoredPosition = Vector2.Lerp(from, home, PointerHand.EaseInOut(t / seconds));
                    yield return null;
                }
            }
            item.anchoredPosition = home;
            item.localScale = Vector3.one;
            item.localRotation = Quaternion.identity;
            itemImage.color = Color.white;
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
    }
}
