using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Pure maths for the doll-house world: each room is a RoomWidth x RoomHeight cell on a grid of 4 columns and
    // 3 levels (the room art is 3:2, the wooden shell frames the cells with ColumnPitch/RowPitch spacing). The
    // camera is the world container's scale and anchored position. In room view the camera is at scale 1
    // centred on the room, so a room-local point is at the same canvas position (the 900-high canvas shows the
    // middle of the 960-high room art).
    public static class HouseCamera
    {
        public const float RoomWidth = 1440f, RoomHeight = 960f;
        public const float ColumnPitch = 1512f, RowPitch = 1032f;
        // Shell art (Resources/Art/house/shell) size in world units and where its pivot sits: the middle of the
        // 4x3 cell grid, so the shell's own centre lines up with the room grid. Printed by tools/art-import/build-house.js.
        public const float ShellWidth = 6696f, ShellHeight = 4092f;
        public const float ShellPivotX = 0.5f, ShellPivotY = 0.4106f;
        // Provisional: fits the whole shell (roof to base) in the 900-high frame; judged on the phone.
        public const float OverviewScale = 0.215f;
        public static readonly Vector2 OverviewFocus = new Vector2(0f, 366f);

        public static Vector2 RoomCentre(HouseRoom room) => new Vector2((room.Column - 1.5f) * ColumnPitch, (room.Level - 1) * RowPitch);

        // Container position that puts world point `focus` at the canvas centre at `scale`.
        public static Vector2 ContainerPosition(Vector2 focus, float scale) => -focus * scale;
    }
}
