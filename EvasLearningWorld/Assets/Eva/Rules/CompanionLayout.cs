namespace EvasLearningWorld.Rules
{
    // A box in canvas units (the 1440 x 900 design frame, origin at centre, y up).
    public readonly struct FootprintBox
    {
        public FootprintBox(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin; YMin = yMin; XMax = xMax; YMax = yMax;
        }

        public float XMin { get; }
        public float YMin { get; }
        public float XMax { get; }
        public float YMax { get; }

        // How far the two boxes overlap along their shallower axis; 0 or less means they do not overlap.
        public float OverlapWith(FootprintBox other)
        {
            var x = System.Math.Min(XMax, other.XMax) - System.Math.Max(XMin, other.XMin);
            var y = System.Math.Min(YMax, other.YMax) - System.Math.Max(YMin, other.YMin);
            return x <= 0f || y <= 0f ? 0f : System.Math.Min(x, y);
        }
    }

    // One standard size/position for the player-character + Eva pairing shown on a gameplay screen (M5 Task 6,
    // docs/superpowers/spikes/character-everywhere.md). Player on the left, Eva on the right, both standing on
    // FeetY. A screen picks one of the standard layouts below; it never supplies its own numbers.
    //
    // Footprints are estimates, not measured art: width is taken as 0.5 x height for the player (shoulder span
    // plus arms, see RigFactory) and 1.0 x height for Eva (a deliberately generous box around the cat's body and
    // tail), so overlap checks err towards reporting a conflict. Corner and Side were approved at Gate 2.
    public sealed class CompanionLayout
    {
        public const float PlayerWidthPerHeight = 0.5f;
        public const float EvaWidthPerHeight = 1.0f;

        public string Name { get; }
        public float PlayerHeight { get; }
        public float EvaHeight { get; }
        public float PlayerX { get; }
        public float EvaX { get; }
        public float FeetY { get; }

        private CompanionLayout(string name, float playerHeight, float evaHeight, float playerX, float evaX, float feetY)
        {
            Name = name; PlayerHeight = playerHeight; EvaHeight = evaHeight; PlayerX = playerX; EvaX = evaX; FeetY = feetY;
        }

        // The default: small, tucked into the bottom-right corner, for the large majority of screens.
        public static readonly CompanionLayout Corner = new CompanionLayout("Corner", 200f, 200f, 450f, 610f, -440f);

        // For screens whose bottom row runs the full width (Free Drawing's stamps, the Dress for Occasion shelf): in
        // the right-hand column, where the screen's own big Eva used to stand, with her feet above that bottom row.
        // Approved at Gate 2 at feet y -190; raised to -90 so the pair clears the five-item shelf's last tile.
        public static readonly CompanionLayout Side = new CompanionLayout("Side", 260f, 260f, 500f, 700f, -90f);

        // Fishing only (not in All): the two sit in the boat on the lake surface at the top right, their lower legs hidden behind the boat's front rim, the boat drawn
        // in front of their legs.
        public static readonly CompanionLayout Boat = new CompanionLayout("Boat", 210f, 140f, 285f, 430f, 176f);

        public static readonly CompanionLayout[] All = { Corner, Side };

        public FootprintBox PlayerFootprint => Box(PlayerX, PlayerHeight, PlayerWidthPerHeight);
        public FootprintBox EvaFootprint => Box(EvaX, EvaHeight, EvaWidthPerHeight);

        public FootprintBox Footprint
        {
            get
            {
                var p = PlayerFootprint;
                var e = EvaFootprint;
                return new FootprintBox(System.Math.Min(p.XMin, e.XMin), FeetY,
                    System.Math.Max(p.XMax, e.XMax), FeetY + System.Math.Max(PlayerHeight, EvaHeight));
            }
        }

        private FootprintBox Box(float x, float height, float widthPerHeight) =>
            new FootprintBox(x - height * widthPerHeight * 0.5f, FeetY, x + height * widthPerHeight * 0.5f, FeetY + height);
    }
}
