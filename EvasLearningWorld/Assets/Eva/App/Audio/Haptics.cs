using UnityEngine;

namespace EvasLearningWorld.App
{
    // Short, soft vibrations of the phone for the moments a child can feel: something sticks, a thing hits the water, a wrong answer. Plain
    // Handheld.Vibrate() buzzes for a full second, so on Android this asks the vibrator for a single pulse with its own strength
    // (VibrationEffect, Android 8+), or a short pattern of pulses. Off the phone (the editor, tests) everything is a no-op. It follows the
    // Sound effects switch in Settings (EvaGame.ApplyAudioSettings).
    //
    // Tick   a barely there touch (a detector beep, a jetpack lighting)
    // Tap    a clear short pulse (a hit, a catch, a right drop, a stick)
    // Thud   a firmer pulse, 0..1 for how hard (a splash, a bump, a dig)
    // Buzz   a very light pulse whose strength follows something (the magnet pulling), 0..1
    // Wrong  two short pulses
    // Win    a short rising double pulse (a level or a round is done)
    public static class Haptics
    {
        public static bool Enabled = true;

        private const float MinGapSeconds = 0.03f;   // pulses closer than this are dropped, so a burst of events is not one long buzz
        private const float WrongGapSeconds = 0.4f;  // one "wrong" per mistake even when two things report it
        private static float _last = -10f, _lastWrong = -10f;

        public static void Tick() => Pulse(12, 0.3f);

        public static void Tap() => Pulse(25, 0.6f);

        public static void Thud(float strength = 1f) => Pulse(45, Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(strength)));

        public static void Buzz(float strength) => Pulse(16, Mathf.Lerp(0.12f, 0.55f, Mathf.Clamp01(strength)));

        public static void Wrong()
        {
            if (Time.realtimeSinceStartup - _lastWrong < WrongGapSeconds) return;
            _lastWrong = Time.realtimeSinceStartup;
            Pattern(new long[] { 0, 30, 60, 30 }, new[] { 0, Amplitude(0.7f), 0, Amplitude(0.7f) });
        }

        public static void Win() => Pattern(new long[] { 0, 20, 50, 30 }, new[] { 0, Amplitude(0.4f), 0, Amplitude(0.8f) });

        private static int Amplitude(float strength) => Mathf.Clamp(Mathf.RoundToInt(strength * 255f), 1, 255);

        private static void Pulse(int milliseconds, float strength)
        {
            if (!Enabled || Time.realtimeSinceStartup - _last < MinGapSeconds) return;
            _last = Time.realtimeSinceStartup;
            Play(milliseconds, Amplitude(strength));
        }

        private static void Pattern(long[] timings, int[] amplitudes)
        {
            if (!Enabled) return;
            _last = Time.realtimeSinceStartup;
            PlayPattern(timings, amplitudes);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _effect;
        private static int _sdk;
        private static bool _failed;

        private static bool Ready()
        {
            if (_vibrator != null) return true;
            if (_failed) return false;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                _effect = new AndroidJavaClass("android.os.VibrationEffect");
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = version.GetStatic<int>("SDK_INT");
                if (_vibrator == null) _failed = true;
            }
            catch (System.Exception)
            {
                _failed = true;
            }
            return !_failed;
        }

        private static void Play(int milliseconds, int amplitude)
        {
            if (!Ready()) return;
            try
            {
                if (_sdk >= 26)
                    using (var effect = _effect.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude)) _vibrator.Call("vibrate", effect);
                else _vibrator.Call("vibrate", (long)milliseconds);
            }
            catch (System.Exception)
            {
                _failed = true;
            }
        }

        private static void PlayPattern(long[] timings, int[] amplitudes)
        {
            if (!Ready()) return;
            try
            {
                if (_sdk >= 26)
                    using (var effect = _effect.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1)) _vibrator.Call("vibrate", effect);
                else _vibrator.Call("vibrate", timings, -1);
            }
            catch (System.Exception)
            {
                _failed = true;
            }
        }

        // Never runs: naming Handheld.Vibrate makes Unity put the VIBRATE permission into the app's manifest, which the calls above need.
        private static void KeepPermission()
        {
            if (_sdk < 0) Handheld.Vibrate();
        }
#else
        private static void Play(int milliseconds, int amplitude) { }

        private static void PlayPattern(long[] timings, int[] amplitudes) { }
#endif
    }
}
