using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Put on a game's "EndPanel": every time the panel is shown (the session is won) it plays the win fanfare and bursts confetti.
    // Confetti is plain coloured rectangles drawn as UI images, so it needs no art and sits behind the panel's buttons.
    public sealed class WinCelebration : MonoBehaviour
    {
        private const int Pieces = 90;
        private const float Gravity = -1900f;
        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.30f, 0.30f), new Color(1f, 0.80f, 0.20f), new Color(0.30f, 0.75f, 0.35f),
            new Color(0.30f, 0.55f, 0.95f), new Color(0.95f, 0.45f, 0.75f), new Color(1f, 0.60f, 0.20f),
        };

        private RectTransform _layer;

        // Screens build the panel active and hide it in the same frame; that coroutine dies with the deactivation, so only a
        // panel that is still shown a frame later is a real win.
        private void OnEnable()
        {
            if (Application.isPlaying) StartCoroutine(CelebrateNextFrame());
        }

        private IEnumerator CelebrateNextFrame()
        {
            yield return null;
            if (EvaUi.Sfx != null) EvaUi.Sfx.Win();
            Burst();
        }

        private void Burst()
        {
            if (_layer == null)
            {
                var go = new GameObject("Confetti", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                _layer = (RectTransform)go.transform;
                _layer.anchorMin = _layer.anchorMax = new Vector2(0.5f, 0.5f);
                _layer.sizeDelta = Vector2.zero;
                _layer.SetAsFirstSibling(); // behind the replay / home buttons
            }
            for (var i = _layer.childCount - 1; i >= 0; i--) Destroy(_layer.GetChild(i).gameObject);
            for (var i = 0; i < Pieces; i++) StartCoroutine(Fly(NewPiece(i)));
        }

        private RectTransform NewPiece(int index)
        {
            var go = new GameObject("Piece", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_layer, false);
            var image = go.GetComponent<Image>();
            image.color = Palette[index % Palette.Length];
            image.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            var side = Random.Range(16f, 30f);
            rect.sizeDelta = new Vector2(side, side * Random.Range(0.45f, 0.9f));
            rect.anchoredPosition = new Vector2(Random.Range(-60f, 60f), -250f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            return rect;
        }

        private static IEnumerator Fly(RectTransform piece)
        {
            // An upward fan with a random speed, so the burst opens like a popper.
            var angle = Random.Range(35f, 145f) * Mathf.Deg2Rad;
            var speed = Random.Range(900f, 2000f);
            var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            var spin = Random.Range(-540f, 540f);
            var life = Random.Range(1.8f, 2.8f);
            var image = piece.GetComponent<Image>();
            var color = image.color;
            for (var t = 0f; t < life && piece != null; t += Time.deltaTime)
            {
                var dt = Time.deltaTime;
                velocity.y += Gravity * dt;
                velocity.x *= 1f - 1.4f * dt; // air drag, so the pieces flutter outward instead of flying off screen
                piece.anchoredPosition += velocity * dt;
                piece.Rotate(0f, 0f, spin * dt);
                color.a = Mathf.Clamp01((life - t) / 0.5f);
                image.color = color;
                yield return null;
            }
            if (piece != null) Destroy(piece.gameObject);
        }
    }
}
