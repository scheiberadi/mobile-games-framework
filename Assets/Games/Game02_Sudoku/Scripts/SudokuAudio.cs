using System.Collections;
using UnityEngine;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    // Lazily-generated, cached procedural SFX shared across every Sudoku screen. Each
    // controller owns its own AudioSource and just asks for a clip to play.
    public static class SudokuAudio
    {
        private static AudioClip _tap;
        private static AudioClip _error;
        private static readonly float[] SuccessChordHz = { 523.25f, 659.25f, 783.99f };

        public static AudioClip Tap => _tap ??= ProceduralAudio.GenerateTone(720f, 0.05f, 0.25f);
        public static AudioClip Error => _error ??= ProceduralAudio.GenerateTone(180f, 0.18f, 0.3f);

        public static void PlaySuccess(MonoBehaviour host, AudioSource source)
        {
            host.StartCoroutine(PlaySuccessChord(source));
        }

        private static IEnumerator PlaySuccessChord(AudioSource source)
        {
            foreach (var hz in SuccessChordHz)
            {
                source.PlayOneShot(ProceduralAudio.GenerateTone(hz, 0.12f, 0.28f));
                yield return new WaitForSeconds(0.09f);
            }
        }
    }
}
