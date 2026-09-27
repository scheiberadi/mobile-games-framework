using EvasLearningWorld.Rules;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Pure maths for the doll-house world: a grid of 4 columns and 2 floors, with the attic as one wide room under
    // the roof. A room cell is RoomWidth x RoomHeight (the room art is 3:2); the hallway column (1) is half as wide;
    // the wooden shell frames the cells with WallThickness between them. The camera is the world container's scale
    // and anchored position. In room view the camera is at scale 1 centred on the room, so a room-local point is at
    // the same canvas position (the 900-high canvas shows the middle of the 960-high room art). Two areas are
    // drawn at a different size in the overview than in room view (they are animated between the two): a hallway
    // (half width in the overview) and the attic (the whole roof interior in the overview, 1750 units wide in room
    // view, shifted up so its floor sits above the tray).
    public static class HouseCamera
    {
        public const float RoomWidth = 1440f, RoomHeight = 960f, HallWidth = 720f;
        public const float WallThickness = 72f, RowPitch = 1032f;
        public const int HallColumn = 1;
        // The attic: overview size and centre (world units, origin at the upper floor's middle) from
        // tools/art-import/build-house.js; room view size and the canvas offset of its centre.
        public static readonly Vector2 AtticOverviewSize = new Vector2(5256f, 2210f);
        public static readonly Vector2 AtticViewSize = new Vector2(1750f, 736f);
        public const float AtticCentreY = 1657f, AtticViewOffsetY = 82f;
        // Shell art (Resources/Art/house/shell) size in world units and where its pivot sits: the middle of the
        // upper floor's cell row, so the shell lines up with the room grid. Printed by build-house.js.
        public const float ShellWidth = 6072f, ShellHeight = 4610f;
        public const float ShellPivotX = 0.5f, ShellPivotY = 0.3644f;
        // Provisional: fits the whole shell (roof to base) in the 900-high frame; judged on the phone.
        public const float OverviewScale = 0.191f;
        public static readonly Vector2 OverviewFocus = new Vector2(0f, 625f);

        public static bool IsAttic(HouseRoom room) => room.Id == "party";

        public static float ColumnWidth(int column) => column == HallColumn ? HallWidth : RoomWidth;

        // Size in the overview.
        public static Vector2 RoomSize(HouseRoom room)
        {
            if (IsAttic(room)) return AtticOverviewSize;
            return new Vector2(room.IsHall ? HallWidth : RoomWidth, RoomHeight);
        }

        // Size when zoomed in.
        public static Vector2 RoomViewSize(HouseRoom room) => IsAttic(room) ? AtticViewSize : new Vector2(RoomWidth, RoomHeight);

        // Where the room's centre appears on the canvas in room view.
        public static Vector2 ViewOffset(HouseRoom room) => IsAttic(room) ? new Vector2(0f, AtticViewOffsetY) : Vector2.zero;

        // World units per room-view unit in the overview (furniture drawn on the panel is scaled by this).
        public static float OverviewScaleOf(HouseRoom room) => IsAttic(room) ? AtticOverviewSize.x / AtticViewSize.x : 1f;

        // Centre of a cell: columns are laid left to right with WallThickness between, the whole grid centred on x = 0.
        public static Vector2 RoomCentre(HouseRoom room)
        {
            if (IsAttic(room)) return new Vector2(0f, AtticCentreY);
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
