using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Pure maths for the doll-house world: each room is a RoomWidth x RoomHeight panel on a 3x3 grid, the
    // camera is the world container's scale and anchored position. In room view the camera is at scale 1
    // centred on the room, so a room-local point is at the same canvas position.
    public static class HouseCamera
    {
        public const float RoomWidth = 1440f, RoomHeight = 900f;
        public const float OverviewScale = 0.25f;
        public static readonly Vector2 OverviewFocus = new Vector2(0f, 290f);

        public static Vector2 RoomCentre(HouseRoom room) => new Vector2((room.Column - 1) * RoomWidth, (room.Level - 1) * RoomHeight);

        // Container position that puts world point `focus` at the canvas centre at `scale`.
        public static Vector2 ContainerPosition(Vector2 focus, float scale) => -focus * scale;
    }
}
