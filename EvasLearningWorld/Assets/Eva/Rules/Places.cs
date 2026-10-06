using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum PlaceId { House, School, Store, Playground, ZooFarm, ScienceLab, Workshop, ArtStudio, BrainGym, FriendsPark, Arcade }

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
    // it is the authoritative gameplay data. A place with its own bespoke road picture (RoadSprite/RoadBox both set)
    // draws that; a place with neither (both null) instead gets a scatter of stepping stones along Road, spaced by
    // MapPath.StonePoints - no bespoke art needed per building.
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
        public const float WorldWidth = 3600f, WorldHeight = 1350f, ViewWidth = 1440f, ViewHeight = 900f;

        // Where every road leaves the hub (in front of the House) and the map's first view (the tutorial) is centred.
        public static readonly WorldPoint Junction = new WorldPoint(0f, 0f);
        public static readonly WorldPoint InitialView = new WorldPoint(0f, -125f);

        // Screen-fixed zones, expressed in the first view (which is centred on the origin): the settings gear at the
        // top-left (exactly the Hud Home button: 30 in, 35 up from the safe area corner, see Hud.HomePosition) and the coin counter at the top-right.
        public static readonly WorldBox SettingsZone = new WorldBox(-570f, 365f, 240f, 240f);
        public static readonly WorldBox CoinZone = new WorldBox(570f, 385f, 240f, 90f);

        // The map is ONE painted picture (art/eva/map/PROMPTS.md "v3"): a hub with the House and ten round clearings on
        // ten separate dirt spokes. The numbers below are measured from that picture (tools/art-import/map-extract.json,
        // pixel -> world: x = px * 3600 / width - 1800, y = 675 - py * 1350 / height): each building stands on its
        // clearing (tap box centred 75 above the clearing centre), the characters stand in front of it, and each Road
        // is the spoke's centre line from the Junction. School and Store sit on the two clearings in the first view.
        private static readonly Place[] Items =
        {
            // The House stands on the hub clearing in the middle of the map; every road starts at the Junction, the front of the hub.
            new Place(PlaceId.House, "House", new WorldBox(0f, 170f, 320f, 280f),
                new[] { Junction }, new WorldPoint(0f, 30f), null,
                "world/place_house", null, "place_house"),
            new Place(PlaceId.School, "School", new WorldBox(-678f, -250f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-181f, -32f),
                    new WorldPoint(-316f, -127f),
                    new WorldPoint(-525f, -207f),
                    new WorldPoint(-508f, -335f),
                    new WorldPoint(-678f, -390f)
                },
                new WorldPoint(-678f, -390f), null,
                "world/place_school", null, "place_school"),
            new Place(PlaceId.Store, "Store", new WorldBox(673f, -250f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(179f, -32f),
                    new WorldPoint(322f, -131f),
                    new WorldPoint(525f, -208f),
                    new WorldPoint(503f, -335f),
                    new WorldPoint(673f, -390f)
                },
                new WorldPoint(673f, -390f), null,
                "world/place_store", null, "place_store"),
            new Place(PlaceId.Playground, "Playground", new WorldBox(657f, 489f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(109f, -13f),
                    new WorldPoint(179f, 29f),
                    new WorldPoint(184f, 82f),
                    new WorldPoint(175f, 150f),
                    new WorldPoint(291f, 222f),
                    new WorldPoint(657f, 349f)
                },
                new WorldPoint(657f, 349f), null,
                "world/place_playground", null, "place_playground"),
            new Place(PlaceId.ZooFarm, "ZooFarm", new WorldBox(-655f, 489f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-109f, -13f),
                    new WorldPoint(-179f, 29f),
                    new WorldPoint(-181f, 150f),
                    new WorldPoint(-299f, 226f),
                    new WorldPoint(-655f, 349f)
                },
                new WorldPoint(-655f, 349f), null,
                "world/place_zoofarm", null, "place_zoofarm"),
            new Place(PlaceId.ScienceLab, "ScienceLab", new WorldBox(1285f, 288f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(109f, -13f),
                    new WorldPoint(240f, 76f),
                    new WorldPoint(590f, 141f),
                    new WorldPoint(1285f, 148f)
                },
                new WorldPoint(1285f, 148f), null,
                "world/place_sciencelab", null, "place_sciencelab"),
            new Place(PlaceId.Workshop, "Workshop", new WorldBox(-1286f, 287f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-109f, -13f),
                    new WorldPoint(-244f, 78f),
                    new WorldPoint(-605f, 142f),
                    new WorldPoint(-971f, 139f),
                    new WorldPoint(-1126f, 174f),
                    new WorldPoint(-1286f, 147f)
                },
                new WorldPoint(-1286f, 147f), null,
                "world/place_workshop", null, "place_workshop"),
            new Place(PlaceId.ArtStudio, "ArtStudio", new WorldBox(0f, -333f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-2f, -75f),
                    new WorldPoint(-170f, -418f),
                    new WorldPoint(0f, -473f)
                },
                new WorldPoint(0f, -473f), null,
                "world/place_artstudio", null, "place_artstudio"),
            new Place(PlaceId.BrainGym, "BrainGym", new WorldBox(-3f, 549f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-109f, -13f),
                    new WorldPoint(-179f, 29f),
                    new WorldPoint(-184f, 82f),
                    new WorldPoint(-122f, 126f),
                    new WorldPoint(0f, 191f),
                    new WorldPoint(-3f, 409f)
                },
                new WorldPoint(-3f, 409f), null,
                "world/place_braingym", null, "place_braingym"),
            new Place(PlaceId.FriendsPark, "FriendsPark", new WorldBox(1293f, -43f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(234f, 17f),
                    new WorldPoint(833f, -92f),
                    new WorldPoint(1128f, -109f),
                    new WorldPoint(1293f, -183f)
                },
                new WorldPoint(1293f, -183f), null,
                "world/place_friendspark", null, "place_friendspark"),
            new Place(PlaceId.Arcade, "Arcade", new WorldBox(-1290f, -43f, 280f, 240f),
                new[]
                {
                    new WorldPoint(0f, 0f),
                    new WorldPoint(-228f, 13f),
                    new WorldPoint(-326f, 2f),
                    new WorldPoint(-719f, -75f),
                    new WorldPoint(-1122f, -119f),
                    new WorldPoint(-1290f, -183f)
                },
                new WorldPoint(-1290f, -183f), null,
                "world/place_arcade", null, "place_arcade"),
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
