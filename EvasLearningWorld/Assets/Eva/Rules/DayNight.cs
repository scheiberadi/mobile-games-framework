using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Day/Night: "Eva's day". The meadow is in the day or in the night and Eva asks what she does then. Four activity cards lie
    // in a tray: two belong to the asked time and two to the other one. The child drops the right ones onto the meadow; a wrong one goes back
    // to the tray and Eva says when she really does it. The round is done when both right cards are placed. Two wrong cards in a row call for a
    // hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped on the meadow" means).
    public enum TimeOfDay { Day, Night }

    public sealed class DayNightActivity
    {
        public DayNightActivity(string id, TimeOfDay time)
        {
            Id = id;
            Time = time;
        }

        public string Id { get; }
        public TimeOfDay Time { get; }
    }

    public enum DayNightDrop { Right, Wrong, Again }

    public static class DayNight
    {
        public const int RoundsPerSession = 3;
        public const int CardsPerRound = 4;
        public const int RightPerRound = 2;
        public const int WrongsForHint = 2;

        public static readonly DayNightActivity[] Activities =
        {
            new DayNightActivity("day_breakfast", TimeOfDay.Day), new DayNightActivity("day_ball", TimeOfDay.Day),
            new DayNightActivity("day_school", TimeOfDay.Day), new DayNightActivity("day_kite", TimeOfDay.Day),
            new DayNightActivity("night_sleep", TimeOfDay.Night), new DayNightActivity("night_story", TimeOfDay.Night),
            new DayNightActivity("night_stars", TimeOfDay.Night), new DayNightActivity("night_pyjamas", TimeOfDay.Night),
        };

        public static TimeOfDay Other(TimeOfDay time) => time == TimeOfDay.Day ? TimeOfDay.Night : TimeOfDay.Day;

        public static string IdOf(TimeOfDay time) => time == TimeOfDay.Day ? "day" : "night";

        // Three rounds: the first time is any, the second the other one, the third any (a session always has both).
        public static DayNightRound[] CreateSession(Random rng)
        {
            var first = rng.Next(2) == 0 ? TimeOfDay.Day : TimeOfDay.Night;
            var times = new[] { first, Other(first), rng.Next(2) == 0 ? TimeOfDay.Day : TimeOfDay.Night };
            return times.Select(time => CreateRound(time, rng)).ToArray();
        }

        // Two cards of the asked time and two of the other, in a random order.
        public static DayNightRound CreateRound(TimeOfDay asked, Random rng)
        {
            var right = Shuffle(Activities.Where(a => a.Time == asked), rng).Take(RightPerRound);
            var wrong = Shuffle(Activities.Where(a => a.Time != asked), rng).Take(CardsPerRound - RightPerRound);
            return new DayNightRound(asked, Shuffle(right.Concat(wrong), rng).ToArray());
        }

        private static List<DayNightActivity> Shuffle(IEnumerable<DayNightActivity> source, Random rng)
        {
            var list = source.ToList();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
            return list;
        }
    }

    // One round: the time asked and the four cards in the tray (by index).
    public sealed class DayNightRound
    {
        private readonly bool[] _placed;

        public DayNightRound(TimeOfDay asked, DayNightActivity[] cards)
        {
            Asked = asked;
            Cards = cards;
            _placed = new bool[cards.Length];
        }

        public TimeOfDay Asked { get; }
        public DayNightActivity[] Cards { get; }
        public int Wrongs { get; private set; }       // wrong cards in a row since the last hint
        public int TotalWrongs { get; private set; }
        public int PlacedCount => _placed.Count(p => p);
        public bool Done => Cards.Select((c, i) => c.Time != Asked || _placed[i]).All(ok => ok);
        public bool NeedsHint => !Done && Wrongs >= DayNight.WrongsForHint;

        public bool IsPlaced(int card) => _placed[card];

        // The card that is placed next by the hint: the first right card not placed yet, or -1.
        public int NextRight()
        {
            for (var i = 0; i < Cards.Length; i++) if (Cards[i].Time == Asked && !_placed[i]) return i;
            return -1;
        }

        public DayNightDrop Drop(int card)
        {
            if (_placed[card]) return DayNightDrop.Again;
            if (Cards[card].Time == Asked)
            {
                _placed[card] = true;
                Wrongs = 0;
                return DayNightDrop.Right;
            }
            Wrongs++;
            TotalWrongs++;
            return DayNightDrop.Wrong;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
