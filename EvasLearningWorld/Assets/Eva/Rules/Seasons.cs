using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Seasons: "Year tree". One tree stands in a meadow and Eva asks for a season ("Make it spring!"). The child drops a
    // season card on the meadow and the whole picture changes into that season, whichever card it is: a card never gives an error, it
    // only shows its season. The round is done when the meadow shows the asked season. Two wrong seasons in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped on the meadow" means).
    public enum Season { Spring, Summer, Autumn, Winter }

    public static class Seasons
    {
        public const int RoundsPerSession = 3;
        public const int WrongsForHint = 2;

        // The meadow starts a session in summer.
        public const Season Start = Season.Summer;

        private static readonly string[] Ids = { "spring", "summer", "autumn", "winter" };

        public static string IdOf(Season season) => Ids[(int)season];

        // The seasons asked in a session: three different ones, the first not the one the meadow already shows.
        public static Season[] CreateSession(Random rng)
        {
            var all = new List<Season>((Season[])Enum.GetValues(typeof(Season)));
            do
            {
                for (var i = all.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    var swap = all[i];
                    all[i] = all[j];
                    all[j] = swap;
                }
            }
            while (all[0] == Start);
            return all.GetRange(0, RoundsPerSession).ToArray();
        }
    }

    // One round: the season asked for and the season the meadow shows now.
    public sealed class SeasonRound
    {
        public SeasonRound(Season asked, Season shown)
        {
            Asked = asked;
            Shown = shown;
        }

        public Season Asked { get; }
        public Season Shown { get; private set; }
        public bool Done { get; private set; }
        public int Wrongs { get; private set; }       // wrong seasons in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool NeedsHint => !Done && Wrongs >= Seasons.WrongsForHint;

        // A card of this season was dropped: true when it is the asked season (the round is done). Dropping the season the meadow
        // already shows changes nothing.
        public bool Drop(Season season)
        {
            if (Done) return false;
            if (season == Shown) return false;
            Shown = season;
            if (season == Asked)
            {
                Done = true;
                Wrongs = 0;
                return true;
            }
            Wrongs++;
            TotalWrongs++;
            return false;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
