using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Pure maths for the doll-house world: a grid of 4 columns and 3 levels. A room cell is RoomWidth x RoomHeight
    // (the room art is 3:2); the hallway column (1) is half as wide. The wooden shell frames the cells with
    // WallThickness between them. The camera is the world container's scale and anchored position. In room view
    // the camera is at scale 1 centred on the room, so a room-local point is at the same canvas position (the
    // 900-high canvas shows the middle of the 960-high room art; a hallway is shown at RoomWidth in room view,
    // squeezed to HallWidth only in the overview).
    public static class HouseCamera
    {
        public const float RoomWidth = 1440f, RoomHeight = 960f, HallWidth = 720f;
        public const float WallThickness = 72f, RowPitch = 1032f;
        public const int HallColumn = 1;
        // Shell art (Resources/Art/house/shell) size in world units and where its pivot sits: the middle of the
        // upper floor's cell row, so the shell lines up with the room grid. Printed by tools/art-import/build-house.js.
        public const float ShellWidth = 6072f, ShellHeight = 4056f;
        public const float ShellPivotX = 0.5f, ShellPivotY = 0.4142f;
        // Provisional: fits the whole shell (roof to base) in the 900-high frame; judged on the phone.
        public const float OverviewScale = 0.217f;
        public static readonly Vector2 OverviewFocus = new Vector2(0f, 348f);

        public static float ColumnWidth(int column) => column == HallColumn ? HallWidth : RoomWidth;

        public static Vector2 RoomSize(HouseRoom room) => new Vector2(room.IsHall ? HallWidth : RoomWidth, RoomHeight);

        // Centre of a cell: columns are laid left to right with WallThickness between, the whole grid centred on x = 0.
        public static Vector2 RoomCentre(HouseRoom room)
        {
            var total = WallThickness * 3f;
            for (var c = 0; c < 4; c++) total += ColumnWidth(c);
            var x = -total / 2f;
            for (var c = 0; c < room.Column; c++) x += ColumnWidth(c) + WallThickness;
            return new Vector2(x + ColumnWidth(room.Column) / 2f, (room.Level - 1) * RowPitch);
        }

        // Container position that puts world point `focus` at the canvas centre at `scale`.
        public static Vector2 ContainerPosition(Vector2 focus, float scale) => -focus * scale;
    }
}
