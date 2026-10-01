using System.Collections;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // M5 Task 5: the player-character reactions on dress-up picks, shared by CreatorScreen and Dress the
    // Character. Two sizes, both scale bounces on the rig root relative to its own baseline (so they never
    // disturb the rig's configured height), restarted cleanly if another fires mid-bounce - rapid tapping
    // resets to the baseline instead of stacking coroutines that fight over the scale:
    //  - Pleased: the light per-pick reaction (small, quick bounce). Chosen over CharacterRig.Cheer() for
    //    every tap because Cheer is a full Animator clip, and the Animator's IsBusy guard would swallow most
    //    of a quick run of taps (feels dead) while the rest would feel repetitive.
    //  - Big: Cheer() plus a larger, slower bounce, for the one "delighted with myself" moment (randomize).
    // Which one reads well per tap is a judgement call to confirm on-device (plan Task 5); the constants are
    // the knobs.
    public sealed class JoyReactions
    {
        private const float PleasedPeak = 1.12f, PleasedDuration = 0.2f;
        private const float BigPeak = 1.22f, BigDuration = 0.45f;

        private readonly MonoBehaviour _runner;
        private readonly CharacterRig _rig;
        private readonly Vector3 _baseScale;
        private Coroutine _running;

        public JoyReactions(MonoBehaviour runner, CharacterRig rig)
        {
            _runner = runner;
            _rig = rig;
            _baseScale = rig.Root.localScale;
        }

        public void Pleased() => Play(PleasedPeak, PleasedDuration);

        public void Big()
        {
            _rig.Cheer();
            Play(BigPeak, BigDuration);
        }

        private void Play(float peak, float duration)
        {
            if (_running != null) _runner.StopCoroutine(_running);
            _running = _runner.StartCoroutine(Bounce(peak, duration));
        }

        private IEnumerator Bounce(float peak, float duration)
        {
            var target = _rig.Root;
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = _baseScale * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = _baseScale * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = _baseScale;
            _running = null;
        }
    }
}
