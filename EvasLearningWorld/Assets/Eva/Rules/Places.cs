using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum PlaceId { House, School, Store, Playground, ZooFarm, ScienceLab }

    // A point in world units (centre origin, x right, y up).
    public readonly struct WorldPoint
    {
        public WorldPoint(float x, float y) { X = x; Y = y; }
        public float X { get; }
        public float Y { get; }
    }

    // A rectangle given by its centre and size, in world units.
    public readonly struct WorldBox
    {
        public WorldBox(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float XMin => X - Width / 2f;
        public float XMax => X + Width / 2f;
        public float YMin => Y - Height / 2f;
        public float YMax => Y + Height / 2f;

        public bool Overlaps(WorldBox other) => XMin < other.XMax && XMax > other.XMin && YMin < other.YMax && YMax > other.YMin;
        public bool Contains(WorldPoint point) => point.X >= XMin && point.X <= XMax && point.Y >= YMin && point.Y <= YMax;
    }

    // One place on the map. ScreenKey is the name of the App layer's ScreenId (a string so this assembly stays
    // engine- and App-free). Road is the explicit walking path from the shared junction outward to the standing spot;
    // it is the authoritative gameplay data, the road picture is drawn to follow it.
    public sealed class Place
    {
        public const float StandingAreaSize = 240f;

        public Place(PlaceId id, string screenKey, WorldBox tapBox, WorldPoint[] road, WorldPoint standingSpot,
            WorldBox? roadBox, string buildingSprite, string roadSprite, string voiceKey)
        {
            Id = id;
            ScreenKey = screenKey;
            TapBox = tapBox;
            Road = road;
            StandingSpot = standingSpot;
            RoadBox = roadBox;
            BuildingSprite = buildingSprite;
            RoadSprite = roadSprite;
            VoiceKey = voiceKey;
        }

        public PlaceId Id { get; }
        public string ScreenKey { get; }
        public WorldBox TapBox { get; }
        public IReadOnlyList<WorldPoint> Road { get; }
        public WorldPoint StandingSpot { get; }
        public WorldBox? RoadBox { get; }
        public string BuildingSprite { get; }
        public string RoadSprite { get; }
        public string VoiceKey { get; }

        // The tap area of the characters standing here (Eva waves when it is tapped).
        public WorldBox StandingArea => new WorldBox(StandingSpot.X, StandingSpot.Y, StandingAreaSize, StandingAreaSize);
    }

    // The map's places and its initial composition (spec "Initial composition"). World units, centre origin, the House
    // near the middle. Only places that exist are listed: no locks, no "coming soon".
    public static class Places
    {
        public const float WorldWidth = 2880f, WorldHeight = 1350f, ViewWidth = 1440f, ViewHeight = 900f;

        // Where every road leaves the House (just below its front) and the map's first view is centred.
        public static readonly WorldPoint Junction = new WorldPoint(60f, -190f);
        public static readonly WorldPoint InitialView = new WorldPoint(0f, 0f);

        // Screen-fixed zones, expressed in the first view (which is centred on the origin): the settings gear at the
        // top-left (exactly the Hud Home button: 30 in, 35 up from the safe area corner, see Hud.HomePosition) and the coin counter at the top-right.
        public static readonly WorldBox SettingsZone = new WorldBox(-570f, 365f, 240f, 240f);
        public static readonly WorldBox CoinZone = new WorldBox(570f, 385f, 240f, 90f);

        private static readonly Place[] Items =
        {
            new Place(PlaceId.House, "House", new WorldBox(60f, -30f, 320f, 280f),
                new[] { Junction }, new WorldPoint(-120f, -300f), null,
                "world/place_house", null, "place_house"),
            new Place(PlaceId.School, "School", new WorldBox(-470f, -10f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -205f), new WorldPoint(-200f, -200f),
                    new WorldPoint(-350f, -185f), new WorldPoint(-470f, -170f)
                },
                new WorldPoint(-470f, -170f), new WorldBox(-205f, -188f, 650f, 200f),
                "world/place_school", "world/road_school", "place_school"),
            new Place(PlaceId.Store, "Store", new WorldBox(490f, -225f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(170f, -260f), new WorldPoint(300f, -345f),
                    new WorldPoint(420f, -405f), new WorldPoint(535f, -420f)
                },
                new WorldPoint(535f, -420f), new WorldBox(300f, -305f, 600f, 350f),
                "world/place_store", "world/road_store", "place_store"),
            // M4.1: first of 8 new POIs the full-content plan adds beyond the initial House/School/Store composition
            // (docs/superpowers/plans/2026-09-26-m4-full-content-plan.md). Sits north of the House, outside the
            // first view (PlacesTests only requires the original three inside it) - the world is 2880x1350 for
            // exactly this, and the road detours right around the House's tap box on its way up. Coordinates and
            // sprites are a placeholder composition, same as every "stand up the place" step here: real map art and
            // final placement are a design pass, flagged for the user same as Number Hunt's tile layout was.
            new Place(PlaceId.Playground, "Playground", new WorldBox(300f, 530f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 130f), new WorldPoint(300f, 370f)
                },
                new WorldPoint(300f, 370f), new WorldBox(180f, 90f, 300f, 600f),
                "world/place_playground", "world/road_playground", "place_playground"),
            // M4.4: Zoo & Farm, the fifth new POI beyond the initial three (docs/superpowers/plans/2026-09-26-
            // m4-full-content-plan.md "4.4 Zoo & Farm"). Its road dips south (like School's own first leg) to
            // clear the House, then runs west well past School's tap box before turning north to the building -
            // in the world's left half the first view never shows. Same placeholder-composition caveat as
            // Playground: real map art and final placement are a design pass.
            new Place(PlaceId.ZooFarm, "ZooFarm", new WorldBox(-950f, 380f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -220f), new WorldPoint(-650f, -260f),
                    new WorldPoint(-950f, -50f), new WorldPoint(-950f, 220f)
                },
                new WorldPoint(-950f, 220f), new WorldBox(-445f, -20f, 1050f, 520f),
                "world/place_zoofarm", "world/road_zoofarm", "place_zoofarm"),
            // M4.5: Science Lab, the sixth new POI (docs/superpowers/plans/2026-09-26-m4-full-content-plan.md
            // "4.5 Science Lab"). Mirrors Zoo & Farm's road shape on the opposite (east) side: dips north first
            // to clear the Store's tap box and road corridor (both hug the y range just below the first view)
            // before running east, then turns north to the building. Same placeholder-composition caveat as
            // every POI added this milestone: real map art and final placement are a design pass.
            new Place(PlaceId.ScienceLab, "ScienceLab", new WorldBox(950f, 380f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 150f),
                    new WorldPoint(700f, 150f), new WorldPoint(950f, 150f), new WorldPoint(950f, 220f)
                },
                new WorldPoint(950f, 220f), new WorldBox(505f, 15f, 890f, 410f),
                "world/place_sciencelab", "world/road_sciencelab", "place_sciencelab"),
        };

        public static IReadOnlyList<Place> All => Items;

        public static Place Find(PlaceId id)
        {
            foreach (var place in Items)
                if (place.Id == id) return place;
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        // The saved place name back to an id; anything unknown (an old save, a damaged one) means the House.
        public static PlaceId ParseOrHouse(string name)
        {
            foreach (var place in Items)
                if (place.Id.ToString() == name) return place.Id;
            return PlaceId.House;
        }
    }
}
