using UnityEngine;

namespace EvasLearningWorld.App
{
    // Tint colours multiplied over the grey-white player art (Eva's cat art is never tinted; she is drawn
    // in her final warm orange). Indexed by CharacterLook.Skin / CharacterLook.Shirt, both 0..ColorCount-1.
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

        public static readonly Color[] Shirt =
        {
            new Color(1.00f, 0.35f, 0.35f),
            new Color(1.00f, 0.65f, 0.15f),
            new Color(0.30f, 0.75f, 0.35f),
            new Color(0.25f, 0.55f, 1.00f),
            new Color(0.75f, 0.35f, 0.95f),
        };
    }
}
