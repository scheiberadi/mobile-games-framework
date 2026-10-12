using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // What a sunny day brings to the weather pictures (Make the weather, Dress for weather): sunbeams, flowers growing out of the meadow
    // one after another, butterflies and a bee flying about, a ladybird crawling and sparkles on the pond and grass. Everything is hidden
    // until Show(life > 0); life 0..1 is how much of it shows (the screen eases it in and out). Positions are in picture space and
    // `place` turns them into the screen's own space.
    public sealed class SunnyLife
    {
        private sealed class Item
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home;        // flowers and sparkles: where it is; critters: the middle of its flight
            public float Phase, A, B, W; // critters: size of the flight (A across, B up) and its speed W
        }

        private static readonly Vector2[] FlowerSpots =
        {
            new Vector2(-390f, -130f), new Vector2(-260f, -195f), new Vector2(-120f, -110f), new Vector2(-10f, -200f),
            new Vector2(110f, -125f), new Vector2(220f, -205f), new Vector2(250f, -60f),
        };
        private static readonly string[] FlowerIds = { "tulip", "sunflower", "poppy", "bell", "daisy" };
        private const float FlowerHeight = 105f;
        private static readonly Vector2[] SparkleSpots =
        {
            new Vector2(520f, -90f), new Vector2(640f, -112f), new Vector2(760f, -95f), new Vector2(590f, -140f),
            new Vector2(700f, -68f), new Vector2(-200f, -150f), new Vector2(60f, -225f),
        };

        private readonly Func<Vector2, Vector2> _place;
        private readonly Image _rays;
        private readonly RectTransform _field;
        private readonly List<Item> _flowers = new List<Item>();
        private readonly List<Item> _butterflies = new List<Item>();
        private readonly List<Item> _sparkles = new List<Item>();
        private Item _bee, _ladybug;

        // `parent` is the screen's root; the rays and the field are added on top of what is there. Flowers further left than
        // `flowersFromX` (picture space) are left out, so a screen can keep them clear of a character standing in the meadow; `flowerSpots`
        // replaces the default places of the flowers (where each stands, picture space).
        public SunnyLife(Transform parent, Func<Vector2, Vector2> place, float flowersFromX = float.NegativeInfinity, Vector2[] flowerSpots = null)
        {
            _place = place;
            _rays = FullImage(parent, "Rays");
            _rays.sprite = EvaUi.Sprite("sciencelab/sunrays");
            var fieldObject = new GameObject("SunnyLife", typeof(RectTransform));
            fieldObject.transform.SetParent(parent, false);
            _field = (RectTransform)fieldObject.transform;
            _field.anchorMin = Vector2.zero;
            _field.anchorMax = Vector2.one;
            _field.offsetMin = _field.offsetMax = Vector2.zero;
            Build(flowersFromX, flowerSpots ?? FlowerSpots);
        }

        public void SetActive(bool active)
        {
            _rays.gameObject.SetActive(active);
            _field.gameObject.SetActive(active);
        }

        private static Image FullImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            return image;
        }

        private Item Picture(string name, string sprite, float size, bool byHeight, Vector2 pivot)
        {
            var art = EvaUi.Sprite(sprite);
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_field, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            var aspect = art.rect.width / art.rect.height;
            rect.sizeDelta = byHeight ? new Vector2(size * aspect, size) : new Vector2(size, size / aspect);
            rect.localScale = Vector3.zero;
            var image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return new Item { Rect = rect, Image = image };
        }

        private void Build(float flowersFromX, Vector2[] flowerSpots)
        {
            for (var i = 0; i < flowerSpots.Length; i++)
            {
                if (flowerSpots[i].x < flowersFromX) continue;
                var flower = Picture("Flower" + i, "sciencelab/sunnyflower_" + FlowerIds[i % FlowerIds.Length], FlowerHeight, true, new Vector2(0.5f, 0f));
                flower.Home = flowerSpots[i];
                flower.Phase = 0.05f + _flowers.Count * 0.09f;
                _flowers.Add(flower);
            }
            var kinds = new[] { "orange", "blue", "yellow" };
            var centres = new[] { new Vector2(-250f, 130f), new Vector2(150f, 40f), new Vector2(-30f, 210f) };
            var across = new[] { 380f, 450f, 320f };
            var up = new[] { 110f, 90f, 70f };
            var speed = new[] { 0.45f, 0.38f, 0.5f };
            for (var i = 0; i < 3; i++)
            {
                var butterfly = Picture("Butterfly" + i, "sciencelab/sunnycritter_butterfly_" + kinds[i], 120f, false, new Vector2(0.5f, 0.5f));
                butterfly.Home = centres[i]; butterfly.A = across[i]; butterfly.B = up[i]; butterfly.W = speed[i]; butterfly.Phase = i * 2.1f;
                _butterflies.Add(butterfly);
            }
            _bee = Picture("Bee", "sciencelab/sunnycritter_bee", 100f, false, new Vector2(0.5f, 0.5f));
            _bee.Home = new Vector2(100f, 120f); _bee.A = 470f; _bee.B = 60f; _bee.W = 0.6f; _bee.Phase = 1f;
            _ladybug = Picture("Ladybug", "sciencelab/sunnycritter_ladybug", 58f, true, new Vector2(0.5f, 0.5f));
            _ladybug.Home = new Vector2(-100f, -150f); _ladybug.A = 260f; _ladybug.W = 0.16f;
            for (var i = 0; i < SparkleSpots.Length; i++)
            {
                var sparkle = Picture("Sparkle" + i, "sciencelab/senseeffect_sparkle", 46f, false, new Vector2(0.5f, 0.5f));
                sparkle.Home = SparkleSpots[i];
                sparkle.Phase = i * 0.9f;
                _sparkles.Add(sparkle);
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            var u = x - 1f;
            return 1f + (c1 + 1f) * u * u * u + c1 * u * u;
        }

        // One frame at `life` (0..1) and clock `time` seconds.
        public void Show(float life, float time)
        {
            _rays.color = new Color(1f, 1f, 1f, 0.55f * life * (0.85f + 0.15f * Mathf.Sin(time * 1.2f)));
            foreach (var flower in _flowers)
            {
                var grow = Mathf.Clamp01((life - flower.Phase) / 0.4f);
                flower.Rect.gameObject.SetActive(grow > 0f);
                flower.Rect.localScale = Vector3.one * EaseOutBack(grow);
                flower.Rect.anchoredPosition = _place(flower.Home);
            }
            var alpha = new Color(1f, 1f, 1f, Mathf.Clamp01(life * 2f));
            var live = life > 0.01f;
            foreach (var butterfly in _butterflies)
            {
                butterfly.Rect.gameObject.SetActive(live);
                if (!live) continue;
                var t = time * butterfly.W + butterfly.Phase;
                var point = butterfly.Home + new Vector2(butterfly.A * Mathf.Sin(t), butterfly.B * Mathf.Sin(t * 1.7f));
                butterfly.Rect.anchoredPosition = _place(point);
                var flap = 0.55f + 0.45f * Mathf.Abs(Mathf.Cos(time * 12f + butterfly.Phase));
                butterfly.Rect.localScale = new Vector3(flap, 1f, 1f) * life;
                butterfly.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-butterfly.A * butterfly.W * Mathf.Cos(t) * 0.04f, -18f, 18f));
                butterfly.Image.color = alpha;
            }
            _bee.Rect.gameObject.SetActive(live);
            if (live)
            {
                var t = time * _bee.W + _bee.Phase;
                var point = _bee.Home + new Vector2(_bee.A * Mathf.Sin(t), _bee.B * Mathf.Sin(t * 2.3f) + Mathf.Sin(time * 18f) * 6f);
                _bee.Rect.anchoredPosition = _place(point);
                _bee.Rect.localScale = Vector3.one * life * (1f + 0.04f * Mathf.Sin(time * 22f));
                _bee.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 20f) * 4f - Mathf.Cos(t) * 6f);
                _bee.Image.color = alpha;
            }
            _ladybug.Rect.gameObject.SetActive(live);
            if (live)
            {
                var t = time * _ladybug.W;
                _ladybug.Rect.anchoredPosition = _place(_ladybug.Home + new Vector2(_ladybug.A * Mathf.Sin(t), Mathf.Sin(time * 0.7f) * 18f));
                _ladybug.Rect.localScale = Vector3.one * life;
                // The head points the way it walks: right is -90 degrees, left +90.
                _ladybug.Rect.localRotation = Quaternion.Euler(0f, 0f, -90f * Mathf.Clamp(Mathf.Cos(t) * 8f, -1f, 1f) + Mathf.Sin(time * 14f) * 5f);
                _ladybug.Image.color = alpha;
            }
            foreach (var sparkle in _sparkles)
            {
                var pulse = Mathf.Max(0f, Mathf.Sin(time * 1.7f + sparkle.Phase * 3f));
                sparkle.Rect.gameObject.SetActive(live && pulse > 0f);
                if (!live || pulse <= 0f) continue;
                sparkle.Rect.anchoredPosition = _place(sparkle.Home);
                sparkle.Rect.localScale = Vector3.one * pulse * 1.1f * life;
                sparkle.Rect.localRotation = Quaternion.Euler(0f, 0f, time * 40f + sparkle.Phase * 50f);
                sparkle.Image.color = alpha;
            }
        }
    }
}
