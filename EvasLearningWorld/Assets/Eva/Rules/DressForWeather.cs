using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Weather: "Dress for the weather". The village shows a weather (sunny, rainy, snowy or windy) and Eva stands in it. The
    // child drags an outfit onto her: the sun outfit, the rain outfit, the snow outfit or the wind outfit. The right outfit stays on and the
    // round is done; a wrong one is only tried on, she does not like it in this weather (too hot, wet, cold, blown about), it comes off
    // again and the child tries another. Two wrong outfits in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped onto Eva" means).
    public enum Outfit { Sun, Rain, Snow, Wind }

    public enum DressDrop { Right, Wrong }

    public static class DressForWeather
    {
        public const int RoundsPerSession = 3;
        public const int WrongsForHint = 2;

        // The weathers an outfit exists for, in the order of Outfit.
        public static readonly WeatherKind[] Weathers = { WeatherKind.Sunny, WeatherKind.Rainy, WeatherKind.Snowy, WeatherKind.Windy };

        private static readonly string[] OutfitIds = { "sun", "rain", "snow", "wind" };

        public static string IdOf(Outfit outfit) => OutfitIds[(int)outfit];

        public static WeatherKind WeatherFor(Outfit outfit) => Weathers[(int)outfit];

        public static Outfit OutfitFor(WeatherKind weather)
        {
            var index = Array.IndexOf(Weathers, weather);
            if (index < 0) throw new ArgumentException("No outfit for " + weather);
            return (Outfit)index;
        }

        // The weathers asked in a session: three different ones.
        public static WeatherKind[] CreateSession(Random rng)
        {
            var all = new List<WeatherKind>(Weathers);
            for (var i = all.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = all[i];
                all[i] = all[j];
                all[j] = swap;
            }
            return all.GetRange(0, RoundsPerSession).ToArray();
        }
    }

    // One round: the weather in the village and what Eva is wearing.
    public sealed class DressRound
    {
        public DressRound(WeatherKind weather)
        {
            Weather = weather;
            DressForWeather.OutfitFor(weather); // a weather without an outfit is a mistake
        }

        public WeatherKind Weather { get; }
        public Outfit Right => DressForWeather.OutfitFor(Weather);
        public Outfit? Worn { get; private set; }     // the outfit on her now, or none
        public bool Done { get; private set; }
        public int Wrongs { get; private set; }       // wrong outfits in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool NeedsHint => !Done && Wrongs >= DressForWeather.WrongsForHint;

        public DressDrop Drop(Outfit outfit)
        {
            Worn = outfit;
            if (outfit == Right)
            {
                Done = true;
                Wrongs = 0;
                return DressDrop.Right;
            }
            Wrongs++;
            TotalWrongs++;
            return DressDrop.Wrong;
        }

        // A wrong outfit comes off again.
        public void TakeOff()
        {
            if (!Done) Worn = null;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
