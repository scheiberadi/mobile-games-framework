using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum PlaceId { House, School, Store, Playground, ZooFarm, ScienceLab, Workshop, ArtStudio, BrainGym, FriendsPark }

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
            // M4.6: Workshop, the seventh new POI (docs/superpowers/plans/2026-09-26-m4-full-content-plan.md
            // "4.9 Workshop" / tracker doc "8. Workshop"). Sits well west of Zoo & Farm (whose own tap box and
            // road corridor hug x in [-1090, 80]) rather than directly north of it, since a due-north site would
            // put ZooFarm's own building between the Junction and Workshop on the same column - the road instead
            // continues ZooFarm's own westbound leg further out, staying south of every tap box (y <= -100) until
            // its final northbound run into Workshop's own column, clear of every other building's tap box. Same
            // placeholder-composition caveat as every POI added this milestone: real map art and final placement
            // are a design pass.
            new Place(PlaceId.Workshop, "Workshop", new WorldBox(-1650f, 380f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -220f), new WorldPoint(-650f, -260f),
                    new WorldPoint(-1090f, -260f), new WorldPoint(-1650f, -100f), new WorldPoint(-1650f, 220f)
                },
                new WorldPoint(-1650f, 220f), new WorldBox(-795f, -20f, 1710f, 480f),
                "world/place_workshop", "world/road_workshop", "place_workshop"),
            // M4.7: Art Studio, the eighth new POI (docs/superpowers/plans/2026-09-26-m4-full-content-plan.md
            // "4.8 Art Studio" / tracker doc "9. Art Studio"). Sits south-east, well clear of Store's tapbox/
            // roadbox (x<=630) and ScienceLab's (y>=260) - the road dips to y=-400 (below Store's own tapbox
            // range) right after the junction, then runs east at that depth before its final northbound run up
            // into Art Studio's own column, clear of every other building's tap box. Same placeholder-
            // composition caveat as every POI added this milestone: real map art and final placement are a
            // design pass.
            new Place(PlaceId.ArtStudio, "ArtStudio", new WorldBox(1200f, -450f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(150f, -260f), new WorldPoint(150f, -400f),
                    new WorldPoint(1200f, -400f), new WorldPoint(1200f, -290f)
                },
                new WorldPoint(1200f, -290f), new WorldBox(630f, -295f, 1140f, 210f),
                "world/place_artstudio", "world/road_artstudio", "place_artstudio"),
            // M4.8: Brain Gym, the ninth new POI (docs/kids-games/full-catalogue-plan.md "10. Brain Gym").
            // Sits south-west, well clear of School's tap box (y <= 110, our column stays south of -430) and
            // House's (x <= -100, our column starts west of -160) - the road runs due south from the junction to
            // y=-390 (below every other building's tap box) before its final westbound run into Brain Gym's own
            // column. Same placeholder-composition caveat as every POI added this milestone: real map art and
            // final placement are a design pass.
            new Place(PlaceId.BrainGym, "BrainGym", new WorldBox(-300f, -550f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(60f, -390f), new WorldPoint(-300f, -390f)
                },
                new WorldPoint(-300f, -390f), new WorldBox(-120f, -290f, 460f, 300f),
                "world/place_braingym", "world/road_braingym", "place_braingym"),
            // M4.9: Friends' Park, the tenth new POI (docs/kids-games/full-catalogue-plan.md "11. Friends'
            // Park"). Sits due east at the world's edge, at the House's own height rather than north or south
            // like every other POI - Science Lab's tap box (y in [260,500]) leaves no room to clear it with a
            // 100-unit gap this far east within the world bounds, so Friends' Park instead sits at y=0, clear
            // of Science Lab/Art Studio (whose tap boxes don't reach y=0) and of House/Store (whose tap boxes
            // it clears in x once the road passes x=250). The road climbs from the junction's y to y=0 while
            // still west of Store's tap box (x < 350), then runs due east at that height, clear of every
            // building along the way, before arriving at the door. Same placeholder-composition caveat as
            // every POI added this milestone: real map art and final placement are a design pass.
            new Place(PlaceId.FriendsPark, "FriendsPark", new WorldBox(1300f, 0f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 0f), new WorldPoint(1120f, 0f)
                },
                new WorldPoint(1120f, 0f), new WorldBox(590f, -95f, 1100f, 220f),
                "world/place_friendspark", "world/road_friendspark", "place_friendspark"),
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
