using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Something buried in the sand: a treasure (the thing to find) or junk (a decoy that gives a lower beep and digs up in two taps).
    public sealed class HiddenItem
    {
        public float X;
        public float Y;
        public bool Junk;
        public int Kind; // treasure 0-5 (chest, crown, gem, coins, ring, key) or junk 0-2 (can, boot, bottle)
        public bool Found;
    }

    public enum HuntPhase { Searching, Marked }

    // What the metal detector says right now.
    public enum DetectorSignal { Silent, Beeping, Continuous }

    [Flags]
    public enum HuntEvents
    {
        None = 0,
        Beep = 1, // one beep of the detector (Beeping only)
        Marked = 2, // the beep went continuous and stayed so: the spot is marked with an X
        Dug = 4, // one more tap of digging
        Revealed = 8, // the last tap: the buried thing comes out (see LastRevealed)
    }

    // The rules of Treasure Hunt, Arcade's metal detector game (docs/kids-games/arcade-redesign.md): the child drags the detector over the
    // sand, it beeps faster the closer it gets to something buried and goes continuous right over it; the spot is then marked and every
    // tap digs until the buried thing comes out. Nothing is ever lost: junk is just a funny find, and the child can always keep searching.
    // The game is levels 1-6 in a row: a smaller hot zone, more treasures, more taps to dig, decoys, and the glow on the detector goes
    // away. Pure logic with no clock of its own: the screen feeds in the frame time, the detector position and the taps.
    public sealed class TreasureHuntDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        public const int TreasureKinds = 6;
        public const int JunkKinds = 3;
        public const int JunkDigs = 2;
        public const float HoldSeconds = 0.5f; // continuous this long before the spot is marked

        // The part of the sand where things are buried (canvas units from the middle). The sea is above, the progress bar above that.
        public const float MinX = -760f;
        public const float MaxX = 760f;
        public const float MinY = -330f;
        public const float MaxY = 150f;
        public const float MinSeparation = 380f;

        private const float FastBeepSeconds = 0.14f; // just outside the hot zone
        private const float SlowBeepSeconds = 0.95f; // at the edge of hearing

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] TreasuresByLevel = { 1, 1, 2, 2, 3, 3 };
        private static readonly int[] JunkByLevel = { 0, 0, 1, 1, 2, 2 };
        private static readonly float[] HotByLevel = { 130f, 115f, 100f, 90f, 80f, 70f };
        private static readonly float[] FarByLevel = { 500f, 470f, 430f, 390f, 350f, 320f };
        private static readonly int[] DigsByLevel = { 3, 4, 4, 5, 6, 8 };

        public static int TreasuresToFind(int level) => TreasuresByLevel[Index(level)];
        public static int JunkCount(int level) => JunkByLevel[Index(level)];
        public static float HotRadius(int level) => HotByLevel[Index(level)];
        public static float FarRadius(int level) => FarByLevel[Index(level)];
        public static int DigsNeeded(int level) => DigsByLevel[Index(level)];
        public static bool ShowsGlow(int level) => level <= 2; // from level 3 only the sound tells how close it is

        private static int Index(int level) => Math.Max(MinLevel, Math.Min(MaxLevel, level)) - 1;

        private readonly Random _rng;
        private readonly List<HiddenItem> _items = new List<HiddenItem>();
        private float _hold;
        private float _beepClock;
        private int _treasureCounter;

        public TreasureHuntDirector(Random rng)
        {
            _rng = rng ?? new Random();
            StartLevel(MinLevel);
        }

        public int Level { get; private set; }
        public IReadOnlyList<HiddenItem> Items => _items;
        public float DetectorX { get; private set; }
        public float DetectorY { get; private set; }
        public HuntPhase Phase { get; private set; }
        public HiddenItem Marked { get; private set; }
        public HiddenItem LastRevealed { get; private set; }
        public int Digs { get; private set; } // taps so far on the marked spot
        public int Found { get; private set; } // treasures found in this level
        public int TotalFound { get; private set; } // treasures found in the whole game
        public DetectorSignal Signal { get; private set; }
        public float BeepInterval { get; private set; } // seconds between beeps while Beeping
        public float Closeness { get; private set; } // 0 out of hearing .. 1 in the hot zone
        public bool NearestIsJunk { get; private set; }
        public bool LevelDone => Found >= TreasuresToFind(Level);

        public void StartLevel(int level)
        {
            Level = Math.Max(MinLevel, Math.Min(MaxLevel, level));
            _items.Clear();
            Marked = null;
            LastRevealed = null;
            Digs = 0;
            Found = 0;
            _hold = 0f;
            _beepClock = 0f;
            Phase = HuntPhase.Searching;
            Signal = DetectorSignal.Silent;
            Closeness = 0f;
            for (var i = 0; i < TreasuresToFind(Level); i++) Place(false, _treasureCounter++ % TreasureKinds);
            for (var i = 0; i < JunkCount(Level); i++) Place(true, _rng.Next(JunkKinds));
        }

        private void Place(bool junk, int kind)
        {
            var separation = MinSeparation;
            for (var attempt = 0; ; attempt++)
            {
                var x = MinX + (float)_rng.NextDouble() * (MaxX - MinX);
                var y = MinY + (float)_rng.NextDouble() * (MaxY - MinY);
                var free = true;
                foreach (var other in _items)
                    if (Distance(x, y, other.X, other.Y) < separation) { free = false; break; }
                if (!free && attempt < 200) continue;
                if (!free) { separation *= 0.9f; attempt = 0; continue; } // crowded: relax a little rather than fail
                _items.Add(new HiddenItem { X = x, Y = y, Junk = junk, Kind = kind });
                return;
            }
        }

        public void MoveDetector(float x, float y)
        {
            DetectorX = x;
            DetectorY = y;
        }

        public HuntEvents Tick(float dt, bool tap)
        {
            var events = HuntEvents.None;
            if (Phase == HuntPhase.Marked)
            {
                Signal = DetectorSignal.Silent;
                if (tap)
                {
                    Digs++;
                    events |= HuntEvents.Dug;
                    var needed = Marked.Junk ? JunkDigs : DigsNeeded(Level);
                    if (Digs >= needed)
                    {
                        Marked.Found = true;
                        LastRevealed = Marked;
                        if (!Marked.Junk)
                        {
                            Found++;
                            TotalFound++;
                        }
                        Marked = null;
                        Digs = 0;
                        _hold = 0f;
                        Phase = HuntPhase.Searching;
                        events |= HuntEvents.Revealed;
                    }
                }
                return events;
            }

            HiddenItem nearest = null;
            var best = float.MaxValue;
            foreach (var item in _items)
            {
                if (item.Found) continue;
                var d = Distance(DetectorX, DetectorY, item.X, item.Y);
                if (d < best) { best = d; nearest = item; }
            }

            var hot = HotRadius(Level);
            var far = FarRadius(Level);
            if (nearest == null || best >= far)
            {
                Signal = DetectorSignal.Silent;
                Closeness = 0f;
                _hold = 0f;
                _beepClock = 0f;
                return events;
            }

            NearestIsJunk = nearest.Junk;
            if (best <= hot)
            {
                Signal = DetectorSignal.Continuous;
                Closeness = 1f;
                _beepClock = 0f;
                _hold += dt;
                if (_hold >= HoldSeconds)
                {
                    Marked = nearest;
                    Digs = 0;
                    Phase = HuntPhase.Marked;
                    events |= HuntEvents.Marked;
                }
                return events;
            }

            _hold = 0f;
            var k = (best - hot) / (far - hot); // 0 at the hot zone, 1 at the edge of hearing
            Closeness = 1f - k;
            BeepInterval = FastBeepSeconds + (SlowBeepSeconds - FastBeepSeconds) * k;
            Signal = DetectorSignal.Beeping;
            _beepClock += dt;
            if (_beepClock >= BeepInterval)
            {
                _beepClock = 0f;
                events |= HuntEvents.Beep;
            }
            return events;
        }

        private static float Distance(float ax, float ay, float bx, float by)
        {
            var dx = ax - bx;
            var dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
