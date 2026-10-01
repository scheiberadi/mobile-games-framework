using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Code-driven motion for Eva's layered cat rig (no Animator): idle breathing, head sway, ear twitch, blink,
    // slow tail sway; talk = mouth flap + head bob + livelier tail; cheer/greet = hops; angry = tail lash,
    // ears laid back a little, eyes narrowed, head dropped. Hops move the inner Hop rect, never the rig root,
    // so screens can keep positioning and scaling Root freely.
    public sealed class CatMotion : MonoBehaviour
    {
        private RectTransform _hop, _tail, _body, _legL, _legR, _head, _earL, _earR, _eyes;
        private Image _mouth;

        private bool _talking;
        private float _angry;
        private float _bounceT = -1f, _bounceDur, _bounceHeight, _tilt;
        private int _bounceCount;
        private float _nextTwitch = 2f, _nextBlink = 3f, _twitchT = -1f, _blinkT = -1f, _mouthTimer;
        private bool _twitchLeft, _mouthFlap;
        private Voice _voice;

        internal void Init(RectTransform hop, RectTransform tail, RectTransform body, RectTransform legL, RectTransform legR,
            RectTransform head, RectTransform earL, RectTransform earR, RectTransform eyes, Image mouth)
        {
            _hop = hop; _tail = tail; _body = body; _legL = legL; _legR = legR;
            _head = head; _earL = earL; _earR = earR; _eyes = eyes; _mouth = mouth;
            _mouth.enabled = false;
        }

        public bool IsBouncing => _bounceT >= 0f;

        public void SetTalking(bool talking) => _talking = talking;
        public void Cheer() { if (IsBouncing) return; _bounceT = 0f; _bounceDur = 0.9f; _bounceHeight = 90f; _bounceCount = 2; _tilt = 0f; }
        public void Greet() { if (IsBouncing) return; _bounceT = 0f; _bounceDur = 0.7f; _bounceHeight = 40f; _bounceCount = 2; _tilt = 1f; }
        public void Angry() => _angry = 1.6f;

        private void OnDisable()
        {
            _bounceT = -1f;
            _angry = 0f;
        }

        private void Update()
        {
            var t = Time.time;
            var dt = Time.deltaTime;

            var breath = Mathf.Sin(t * Mathf.PI * 0.5f);
            var sy = 1f + 0.012f * breath;
            var sx = 1f - 0.004f * breath;
            var headRot = Mathf.Sin(t * 0.7f) * 1.2f;
            var headY = Mathf.Sin(t * 0.9f + 1f) * 3f;

            var hop = 0f;
            var perk = 0f;
            var tiltDeg = 0f;
            if (_bounceT >= 0f)
            {
                _bounceT += dt;
                var per = _bounceDur / _bounceCount;
                var idx = Mathf.FloorToInt(_bounceT / per);
                if (idx >= _bounceCount) _bounceT = -1f;
                else
                {
                    var u = (_bounceT - idx * per) / per;
                    var crouch = Mathf.Clamp01(1f - u / 0.2f) * 0.5f + Mathf.Clamp01((u - 0.85f) / 0.15f) * 0.5f;
                    var air = u > 0.2f && u < 0.85f ? Mathf.Sin((u - 0.2f) / 0.65f * Mathf.PI) : 0f;
                    var squash = crouch * 0.08f;
                    hop = air * _bounceHeight;
                    sy += air * 0.06f - squash;
                    sx += squash * 0.5f - air * 0.03f;
                    perk = air;
                    tiltDeg = _tilt * Mathf.Sin(u * Mathf.PI) * 6f * (idx == 0 ? 1f : -1f);
                    headY -= air * 6f;
                }
            }

            _hop.anchoredPosition = new Vector2(0f, hop);

            if (_angry > 0f) _angry -= dt;
            var lash = _angry > 0f ? 1f : 0f;
            var tailSwing = 2f + 5f * perk + (_talking ? 6f : 0f) + 14f * lash;
            var tailRate = 1.3f + (_talking ? 2f : 0f) + 6f * lash;
            _tail.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * tailRate) * tailSwing);
            _body.localScale = new Vector3(sx, sy, 1f);
            _head.localEulerAngles = new Vector3(0f, 0f, headRot + tiltDeg);
            _legL.localScale = _legR.localScale = new Vector3(1f, 1f - 0.22f * perk, 1f);
            _legL.localEulerAngles = new Vector3(0f, 0f, 10f * perk);
            _legR.localEulerAngles = new Vector3(0f, 0f, -10f * perk);
            _earL.localScale = _earR.localScale = new Vector3(1f, lash > 0f ? 0.8f : 1f, 1f);

            var angleL = 0f;
            var angleR = 0f;
            if (_twitchT < 0f && t > _nextTwitch) { _twitchT = 0f; _twitchLeft = Random.value < 0.5f; }
            if (_twitchT >= 0f)
            {
                _twitchT += dt;
                var a = Mathf.Sin(Mathf.Clamp01(_twitchT / 0.22f) * Mathf.PI) * 9f;
                if (_twitchLeft) angleL = a; else angleR = -a;
                if (_twitchT > 0.22f) { _twitchT = -1f; _nextTwitch = t + Random.Range(3f, 6f); }
            }
            _earL.localEulerAngles = new Vector3(0f, 0f, angleL + perk * 5f + lash * 9f);
            _earR.localEulerAngles = new Vector3(0f, 0f, angleR - perk * 5f - lash * 9f);

            var blink = 1f;
            if (_blinkT < 0f && t > _nextBlink) _blinkT = 0f;
            if (_blinkT >= 0f)
            {
                _blinkT += dt;
                blink = 1f - Mathf.Sin(Mathf.Clamp01(_blinkT / 0.16f) * Mathf.PI);
                if (_blinkT > 0.16f) { _blinkT = -1f; _nextBlink = t + Random.Range(3f, 5f); }
            }
            _eyes.localScale = new Vector3(1f, Mathf.Max(0.05f, blink) * (lash > 0f ? 0.75f : 1f), 1f);

            var headOffset = headY - 10f * lash;
            // Mouth moves whenever a voice line is playing, whether or not a screen set the talking flag.
            if (_voice == null) _voice = FindFirstObjectByType<Voice>();
            var open = false;
            if (_voice != null && _voice.IsSpeaking)
            {
                _mouthTimer -= dt;
                if (_mouthTimer <= 0f) { _mouthFlap = !_mouthFlap; _mouthTimer = Random.Range(0.07f, 0.2f); }
                open = _mouthFlap;
            }
            if (_mouth.sprite != null) _mouth.enabled = open;
            if (open) headOffset += Mathf.Abs(Mathf.Sin(t * 9f)) * 3f;
            _head.anchoredPosition = new Vector2(0f, headOffset);
        }
    }
}
