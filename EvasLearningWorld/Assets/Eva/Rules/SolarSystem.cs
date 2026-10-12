using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Space: "Build the solar system". The sun glows at the left edge and eight orbit rings curve away from it. A tray holds
    // four planets (the four inner ones, then on a second page the four outer ones) and Eva asks where one of them goes ("Where does Earth go?"). The child
    // drags a planet onto its ring: the right ring keeps the planet (it starts to travel along its ring), a wrong ring sends it back and
    // Eva says where it really goes. Any planet put on its own ring counts, asked or not. Two wrong rings in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides which ring a drop is on).
    public enum Planet { Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune }

    public enum SolarDrop { Right, Wrong, Again }

    public static class SolarSystem
    {
        public const int RoundsPerSession = 2;
        public const int PlanetsPerRound = 4;
        public const int WrongsForHint = 2;

        private static readonly string[] Ids = { "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune" };

        public static string IdOf(Planet planet) => Ids[(int)planet];

        // The ring a planet belongs on: 0 is the one nearest the sun.
        public static int RingOf(Planet planet) => (int)planet;

        // Two pages: the four inner planets first, then the four outer ones (the inner ones stay on their rings while the outer ones are asked),
    // so the child always places all eight.
        public static SolarRound[] CreateSession(Random rng)
        {
            var inner = new[] { Planet.Mercury, Planet.Venus, Planet.Earth, Planet.Mars };
            var outer = new[] { Planet.Jupiter, Planet.Saturn, Planet.Uranus, Planet.Neptune };
            return new[] { inner, outer }.Select(set => new SolarRound(set, rng)).ToArray();
        }
    }

    // One round: four planets, asked one after another in a random order.
    public sealed class SolarRound
    {
        private readonly List<Planet> _order;
        private readonly HashSet<Planet> _placed = new HashSet<Planet>();

        public SolarRound(Planet[] planets, Random rng)
        {
            Planets = planets;
            _order = planets.OrderBy(_ => rng.Next()).ToList();
        }

        public Planet[] Planets { get; }
        public int Wrongs { get; private set; }       // wrong rings in a row since the last hint
        public int TotalWrongs { get; private set; }
        public int PlacedCount => _placed.Count;
        public bool Done => _placed.Count == Planets.Length;
        public bool NeedsHint => !Done && Wrongs >= SolarSystem.WrongsForHint;

        // The planet Eva asks for now: the first in the asked order that is not placed yet.
        public Planet? Asked
        {
            get
            {
                foreach (var planet in _order) if (!_placed.Contains(planet)) return planet;
                return null;
            }
        }

        public bool IsPlaced(Planet planet) => _placed.Contains(planet);

        // This planet was let go on this ring (0 = nearest the sun).
        public SolarDrop Drop(Planet planet, int ring)
        {
            if (_placed.Contains(planet)) return SolarDrop.Again;
            if (ring == SolarSystem.RingOf(planet))
            {
                _placed.Add(planet);
                Wrongs = 0;
                return SolarDrop.Right;
            }
            Wrongs++;
            TotalWrongs++;
            return SolarDrop.Wrong;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
