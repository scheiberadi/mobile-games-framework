using UnityEngine;

namespace EvasLearningWorld.App
{
    // Code-driven motion for the player's one-piece body (no Animator): idle breathing and a slight sway, talk = a livelier bob,
    // wave = a happy rock side to side with a small hop, cheer = two jumps with a squash on landing. Everything moves the inner
    // Hop rect (pivot at the feet), never the rig root, so screens can keep positioning and scaling Root freely.
    public sealed class PlayerMotion : MonoBehaviour
    {
        private RectTransform _hop;
        private bool _talking;
        private float _bounceT = -1f, _bounceDur, _bounceHeight, _rock;
        private int _bounceCount;

        internal void Init(RectTransform hop) => _hop = hop;

        public bool IsBouncing => _bounceT >= 0f;

        public void SetTalking(bool talking) => _talking = talking;
        public void Wave() { if (IsBouncing) return; _bounceT = 0f; _bounceDur = 0.8f; _bounceHeight = 14f; _bounceCount = 2; _rock = 6f; }
        public void Cheer() { if (IsBouncing) return; _bounceT = 0f; _bounceDur = 0.9f; _bounceHeight = 46f; _bounceCount = 2; _rock = 0f; }

        private void OnDisable() => _bounceT = -1f;

        private void Update()
        {
            if (_hop == null) return;
            var t = Time.time;

            var breath = Mathf.Sin(t * Mathf.PI * 0.8f);
            var sy = 1f + 0.014f * breath;
            var sx = 1f - 0.006f * breath;
            var rot = Mathf.Sin(t * 0.7f) * 0.6f;
            var y = 0f;

            if (_talking)
            {
                var talk = Mathf.Sin(t * 9f);
                sy += 0.012f * talk;
                y += Mathf.Max(0f, talk) * 2.5f;
            }

            if (_bounceT >= 0f)
            {
                _bounceT += Time.deltaTime;
                var k = _bounceT / _bounceDur;
                if (k >= 1f) _bounceT = -1f;
                else
                {
                    var phase = k * _bounceCount;
                    var local = phase - Mathf.Floor(phase);
                    var arc = Mathf.Sin(local * Mathf.PI);
                    y += arc * _bounceHeight;
                    // Stretch in the air, squash just before and after touching down.
                    sy += arc * 0.06f - (1f - arc) * 0.05f;
                    sx -= arc * 0.03f - (1f - arc) * 0.03f;
                    rot += Mathf.Sin(k * Mathf.PI * 4f) * _rock;
                }
            }

            _hop.anchoredPosition = new Vector2(0f, y);
            _hop.localScale = new Vector3(sx, sy, 1f);
            _hop.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}
