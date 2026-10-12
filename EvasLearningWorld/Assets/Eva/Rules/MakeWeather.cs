using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Weather: "Make the weather". Eva asks for a weather ("Make it rainy!"). The child drags a piece into the sky (the sun,
    // a white cloud, a rain cloud, a storm cloud, a snowflake, the wind) and the village shows what that piece makes. Any piece makes
    // some weather; the round is done when the weather made is the one Eva asked for. A piece that makes another weather is not an
    // error, only "that is windy, try again"; two such tries in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped into the sky" means).
    public enum WeatherKind { Sunny, Cloudy, Rainy, Stormy, Snowy, Windy }

    public enum SkyPiece { Sun, Cloud, RainCloud, StormCloud, Snowflake, Wind }

    public enum WeatherDrop { Right, Wrong }

    public static class MakeWeather
    {
        public const int RoundsPerSession = 3;
        public const int WrongsForHint = 2;

        private static readonly string[] KindIds = { "sunny", "cloudy", "rainy", "stormy", "snowy", "windy" };

        public static string IdOf(WeatherKind kind) => KindIds[(int)kind];

        // What a piece makes: each piece one weather, each weather one piece.
        public static WeatherKind Made(SkyPiece piece) => (WeatherKind)(int)piece;

        public static SkyPiece PieceFor(WeatherKind kind) => (SkyPiece)(int)kind;

        // The weathers Eva asks for in a session: three different ones.
        public static WeatherKind[] CreateSession(Random rng)
        {
            var all = new List<WeatherKind>((WeatherKind[])Enum.GetValues(typeof(WeatherKind)));
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

    // One round: the weather asked for and what the child has made so far.
    public sealed class MakeWeatherRound
    {
        public MakeWeatherRound(WeatherKind asked)
        {
            Asked = asked;
        }

        public WeatherKind Asked { get; }
        public WeatherKind? Made { get; private set; }     // the weather now in the sky, or none yet
        public bool Done { get; private set; }
        public int Wrongs { get; private set; }            // wrong pieces in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool NeedsHint => !Done && Wrongs >= MakeWeather.WrongsForHint;

        public WeatherDrop Drop(SkyPiece piece)
        {
            Made = MakeWeather.Made(piece);
            if (Made == Asked)
            {
                Done = true;
                Wrongs = 0;
                return WeatherDrop.Right;
            }
            Wrongs++;
            TotalWrongs++;
            return WeatherDrop.Wrong;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
