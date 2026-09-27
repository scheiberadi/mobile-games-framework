using UnityEngine;

namespace EvasLearningWorld.App
{
    // Tint colours multiplied over placeholder/grey-white player art (Eva's cat art is never tinted; she is
    // drawn in her final warm orange). Skin is indexed by CharacterLook.Skin; HairColor/EyeColor by their own
    // fields, all 0..Length-1. Shirt tinting is retired: Top is now an illustrated wardrobe item (Rules/
    // Wardrobe.cs), not a colour - see RigFactory for how an unset/placeholder wardrobe slot renders instead.
    public static class Palette
    {
        public static readonly Color[] Skin =
        {
            new Color(1.00f, 0.93f, 0.82f),
            new Color(0.94f, 0.78f, 0.60f),
            new Color(0.80f, 0.60f, 0.42f),
            new Color(0.60f, 0.42f, 0.28f),
            new Color(0.38f, 0.26f, 0.18f),
        };

        public static readonly Color[] HairColor =
        {
            new Color(0.12f, 0.09f, 0.08f), // near-black
            new Color(0.30f, 0.18f, 0.10f), // dark brown
            new Color(0.55f, 0.36f, 0.18f), // brown
            new Color(0.80f, 0.60f, 0.28f), // blonde
            new Color(0.70f, 0.28f, 0.16f), // red / auburn
            new Color(0.88f, 0.88f, 0.90f), // silvery-grey (playful, not just "old age", for a kids' creator)
        };

        public static readonly Color[] EyeColor =
        {
            new Color(0.32f, 0.21f, 0.12f), // brown
            new Color(0.22f, 0.45f, 0.62f), // blue
            new Color(0.26f, 0.52f, 0.30f), // green
            new Color(0.40f, 0.32f, 0.55f), // grey-violet
            new Color(0.48f, 0.32f, 0.16f), // hazel
            new Color(0.14f, 0.14f, 0.16f), // near-black
        };
    }
}
