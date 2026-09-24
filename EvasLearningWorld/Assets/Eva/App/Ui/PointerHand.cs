using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Eva's pointing hand: guides the child during the Count screen's Hint and Demonstrate help steps by
    // moving to an object or tile, tapping it, and pulsing in place to draw the eye. It never receives taps
    // itself: no TapTarget, raycastTarget off. Hosts its own idle-pulse loop on the caller's Runner (the same
    // MonoBehaviour that hosts every other CountScreen coroutine) so deactivating the screen's Root also stops it.
    public sealed class PointerHand
    {
        public const float Size = 200f;
        private const float TapPeakScale = 1.3f;
        private const float PulsePeriod = 0.6f;
        private const float PulsePeakScale = 1.15f;

        public RectTransform Rect { get; }

        private readonly MonoBehaviour _runner;
        private readonly Image _image;
        private Coroutine _pulseRoutine;

        public PointerHand(Transform parent, MonoBehaviour runner)
        {
            _runner = runner;
            var go = new GameObject("PointerHand", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Rect = (RectTransform)go.transform;
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.sizeDelta = new Vector2(Size, Size);

            _image = go.GetComponent<Image>();
            _image.sprite = EvaUi.Sprite("icons/hand");
            _image.preserveAspect = true;
            _image.raycastTarget = false;

            go.SetActive(false);
        }

        // Fingertip of icons/hand.svg (index-finger tip, art point 204,63 after the group's rotate(-45) scale(.86)
        // is about 92,156 in the 512 viewBox) relative to the hand's centre, in UI px at Size 200 (y up).
        public static readonly Vector2 FingertipOffset = new Vector2(-64f, 39f);

        // Hand rect position that puts the fingertip, not the hand's centre, on `target`.
        public static Vector2 HandPositionFor(Vector2 target) => target - FingertipOffset;

        // Glides from the hand's current position until its fingertip is on `target` over `seconds`, easing in
        // and out. Activates the hand if it was hidden.
        public IEnumerator MoveTo(Vector2 target, float seconds)
        {
            target = HandPositionFor(target);
            Rect.gameObject.SetActive(true);
            var start = Rect.anchoredPosition;
            if (seconds <= 0f)
            {
                Rect.anchoredPosition = target;
                yield break;
            }
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                Rect.anchoredPosition = Vector2.LerpUnclamped(start, target, EaseInOut(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            Rect.anchoredPosition = target;
        }

        // A small press animation in place: scales up then back down.
        public IEnumerator Tap(float seconds)
        {
            Rect.gameObject.SetActive(true);
            var half = Mathf.Max(seconds, 0.02f) * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                Rect.localScale = Vector3.one * Mathf.Lerp(1f, TapPeakScale, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                Rect.localScale = Vector3.one * Mathf.Lerp(TapPeakScale, 1f, t / half);
                yield return null;
            }
            Rect.localScale = Vector3.one;
        }

        // Starts or stops a gentle continuous breathing scale in place, used while the hand is resting on
        // something it wants the child to notice. Safe to call repeatedly; turning it on while already on,
        // or off while already off, does nothing.
        public void Pulse(bool on)
        {
            if (on)
            {
                if (_pulseRoutine != null) return;
                _pulseRoutine = _runner.StartCoroutine(PulseLoop());
            }
            else
            {
                if (_pulseRoutine != null) _runner.StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
                Rect.localScale = Vector3.one;
            }
        }

        // Stops any pulse and deactivates the hand. Idempotent.
        public void Hide()
        {
            Pulse(false);
            Rect.gameObject.SetActive(false);
        }

        private IEnumerator PulseLoop()
        {
            while (true)
            {
                for (var t = 0f; t < PulsePeriod; t += Time.deltaTime)
                {
                    var k = t / PulsePeriod;
                    var scale = k < 0.5f ? Mathf.Lerp(1f, PulsePeakScale, k * 2f) : Mathf.Lerp(PulsePeakScale, 1f, (k - 0.5f) * 2f);
                    Rect.localScale = Vector3.one * scale;
                    yield return null;
                }
            }
        }

        // Pure timing helper (0 to 1 to 0..1 smoothstep), unit-tested directly: 0 -> 0, 1 -> 1, 0.5 -> 0.5, monotone.
        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
