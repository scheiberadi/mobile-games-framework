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

        // Where every road leaves the House (just below its front) and the map's first view is centred.
        public static readonly WorldPoint Junction = new WorldPoint(60f, -190f);
        public static readonly WorldPoint InitialView = new WorldPoint(0f, 0f);

        // Screen-fixed zones, expressed in the first view (which is centred on the origin): the settings gear at the
        // top-left (exactly the Hud Home button: 30 in, 35 up from the safe area corner, see Hud.HomePosition) and the coin counter at the top-right.
        public static readonly WorldBox SettingsZone = new WorldBox(-570f, 365f, 240f, 240f);
        public static readonly WorldBox CoinZone = new WorldBox(570f, 385f, 240f, 90f);

        // M4.10 placement pass (docs/kids-games/m4-handover.md "Building placement"): every POI added this
        // milestone was first stood up at a placeholder coordinate (a plain grid position, no regard for the
        // painted map art) and the roads below were re-derived from scratch against the actual background -
        // scouted by rendering each candidate TapBox over the real map_world_left/right.png art (see
        // tools/art-import/mockup-map.js) so every building sits clear of the painted trees/bushes/rocks, and
        // re-checked by tools/art-import/verify-roads.js (an exact segment-vs-rectangle test) so no place's Road
        // polyline crosses another place's TapBox. Re-run that script after moving anything below.
        private static readonly Place[] Items =
        {
            new Place(PlaceId.House, "House", new WorldBox(60f, 20f, 320f, 280f),
                new[] { Junction }, new WorldPoint(-120f, -250f), null,
                "world/place_house", null, "place_house"),
            new Place(PlaceId.School, "School", new WorldBox(-470f, 40f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -155f), new WorldPoint(-200f, -150f),
                    new WorldPoint(-350f, -135f), new WorldPoint(-470f, -120f)
                },
                new WorldPoint(-470f, -120f), null,
                "world/place_school", null, "place_school"),
            new Place(PlaceId.Store, "Store", new WorldBox(510f, -225f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(190f, -260f), new WorldPoint(320f, -345f),
                    new WorldPoint(440f, -405f), new WorldPoint(555f, -420f)
                },
                new WorldPoint(555f, -420f), null,
                "world/place_store", null, "place_store"),
            // Top row, left-to-right: ZooFarm, BrainGym, Playground, ScienceLab, all approached from below (their
            // Road climbs to a standing spot just south of their own TapBox) and all clear of the House/School
            // TapBoxes (y <= 160) along the way.
            // Standing spot sits off to the south-east of the TapBox's own centre (345,215 instead of 300,430's
            // own footprint) so its 240-unit StandingArea clears both the House TapBox (to its south-west) and
            // the screen-fixed CoinZone (to its north-east) instead of grazing one of them.
            new Place(PlaceId.Playground, "Playground", new WorldBox(300f, 430f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 130f), new WorldPoint(345f, 215f)
                },
                new WorldPoint(345f, 215f), null,
                "world/place_playground", null, "place_playground"),
            // Travels west at y=-150 - clear of House/School (both bottom out at y >= -120) and of Arcade
            // (whose TapBox tops out at y=-280) - all the way to its own column before climbing north. Standing
            // spot sits just short of the TapBox's own bottom edge (310) rather than the usual 40 further out,
            // so its StandingArea still clears Workshop's TapBox to its south without landing exactly on its own
            // TapBox's edge (which WorldBox.Contains treats as inside).
            new Place(PlaceId.ZooFarm, "ZooFarm", new WorldBox(-950f, 430f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -150f), new WorldPoint(-950f, -150f), new WorldPoint(-950f, 300f)
                },
                new WorldPoint(-950f, 300f), null,
                "world/place_zoofarm", null, "place_zoofarm"),
            new Place(PlaceId.ScienceLab, "ScienceLab", new WorldBox(950f, 380f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 150f),
                    new WorldPoint(700f, 150f), new WorldPoint(950f, 150f), new WorldPoint(950f, 220f)
                },
                new WorldPoint(950f, 220f), null,
                "world/place_sciencelab", null, "place_sciencelab"),
            // Bottom row, left-to-right: Workshop, Arcade, ArtStudio - all approached from the north/Junction
            // side (their standing spot sits just outside their own TapBox's top edge).
            // Travels west at y=-130 (clear of House/School, which bottom out at y >= -120, and of Arcade,
            // which tops out at y=-280) before climbing the short remaining stretch into its own column.
            new Place(PlaceId.Workshop, "Workshop", new WorldBox(-1150f, 50f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-40f, -160f), new WorldPoint(-1150f, -130f), new WorldPoint(-1150f, -110f)
                },
                new WorldPoint(-1150f, -110f), null,
                "world/place_workshop", null, "place_workshop"),
            // Dips to y=-400 (south of Store's TapBox, which bottoms out at -345, and of Arcade's, which bottoms
            // out at -520 - clear since this leg's x never reaches Arcade's column) before its eastbound run
            // to x=1000 - short of its own TapBox's column (x >= 1010), not x=1150, so this leg doesn't clip
            // back into its own TapBox - then the final climb straight up that column. x=1000 (not 1150) also
            // means the standing spot's 240-unit StandingArea clears FriendsPark's TapBox (x >= 1160) by a
            // comfortable margin instead of grazing it. Sits at y=-350 rather than closer to the Junction so
            // its own TapBox keeps a 100-unit gap from FriendsPark's TapBox too.
            new Place(PlaceId.ArtStudio, "ArtStudio", new WorldBox(1150f, -350f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(150f, -260f), new WorldPoint(150f, -400f),
                    new WorldPoint(1000f, -400f), new WorldPoint(1000f, -190f)
                },
                new WorldPoint(1000f, -190f), null,
                "world/place_artstudio", null, "place_artstudio"),
            // Sits at x=-300 (not -350) so its TapBox clears the screen-fixed SettingsZone to its west (whose
            // own TapBox reaches to x=-450) - -300 is already clear of School's TapBox in x (x <= -330) too, so
            // the road's jog through the gap between House's and School's TapBoxes (x in [-330,-100], neither
            // box reaches into it) only needs to clear School on the way up, not land inside its x-range.
            // Standing spot sits just short of the TapBox's own bottom edge (310) rather than the usual 40
            // further out, so its StandingArea still clears School's TapBox to its south without landing
            // exactly on its own TapBox's edge (which WorldBox.Contains treats as inside).
            new Place(PlaceId.BrainGym, "BrainGym", new WorldBox(-300f, 430f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(-200f, -190f), new WorldPoint(-200f, 200f),
                    new WorldPoint(-300f, 200f), new WorldPoint(-300f, 300f)
                },
                new WorldPoint(-300f, 300f), null,
                "world/place_braingym", null, "place_braingym"),
            // Due east at the world's edge, at the House's own height rather than in either row above - Science
            // Lab's TapBox (y in [260,500]) leaves no room to clear it with a comfortable gap this far east
            // within the world bounds, so Friends' Park instead sits at y=0, clear of Science Lab/Art Studio
            // (whose TapBoxes don't reach y=0) and of House/Store (whose TapBoxes it clears in x once the road
            // passes x=250). Untouched by the M4.10 placement pass - already clear.
            new Place(PlaceId.FriendsPark, "FriendsPark", new WorldBox(1300f, 0f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(250f, -190f), new WorldPoint(250f, 0f), new WorldPoint(1120f, 0f)
                },
                new WorldPoint(1120f, 0f), null,
                "world/place_friendspark", null, "place_friendspark"),
            // Sits at y=-400 (not the usual 100 further north) so its standing spot's 240-unit StandingArea
            // clears School's TapBox to its north-east instead of grazing it. Dips straight south of the
            // Junction (clear of every other TapBox at this x) before its westbound leg at y=-250 - north of
            // its own TapBox's top edge (-280) - then the short final climb into its own column.
            new Place(PlaceId.Arcade, "Arcade", new WorldBox(-550f, -400f, 280f, 240f),
                new[]
                {
                    Junction, new WorldPoint(60f, -250f), new WorldPoint(-550f, -250f), new WorldPoint(-550f, -240f)
                },
                new WorldPoint(-550f, -240f), null,
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
