using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One area of the doll house: a decoratable room or a hallway. Level 0 = ground, 1 = upper floor, 2 = attic;
    // Column 0..3 (the hallways are column 1 and half as wide as a room; the attic is one wide room filling the
    // whole roof, drawn at column 1 for the stairs link). The room id doubles as the room type (one room of each
    // type). Hallways (ids "hall_*") hold the stairs and have no furniture slots. Neighbour ids are the doors
    // (Left/Right) and stairs (Up/Down), written out explicitly: on the ground and upper floors room 1 opens onto
    // the hallway, room 3 opens onto the hallway and room 4, room 4 opens onto room 3. The upper hallway's stairs
    // lead up into the party room, the attic.
    public sealed class HouseRoom
    {
        public string Id;
        public int Level, Column;
        public string Left, Right, Up, Down;

        public bool IsHall => Id.StartsWith("hall_");

        public HouseRoom(string id, int level, int column, string left, string right, string up, string down)
        {
            Id = id; Level = level; Column = column; Left = left; Right = right; Up = up; Down = down;
        }
    }

    public static class HouseRooms
    {
        public static readonly IReadOnlyList<HouseRoom> All = new[]
        {
            new HouseRoom("living",      0, 0, null,          "hall_ground", null,          null),
            new HouseRoom("hall_ground", 0, 1, "living",      "dining",      "hall_upper",  null),
            new HouseRoom("dining",      0, 2, "hall_ground", "kitchen",     null,          null),
            new HouseRoom("kitchen",     0, 3, "dining",      null,          null,          null),
            new HouseRoom("parents",     1, 0, null,          "hall_upper",  null,          null),
            new HouseRoom("hall_upper",  1, 1, "parents",     "kids",        "party",       "hall_ground"),
            new HouseRoom("kids",        1, 2, "hall_upper",  "bath",        null,          null),
            new HouseRoom("bath",        1, 3, "kids",        null,          null,          null),
            new HouseRoom("party",       2, 1, null,          null,          null,          "hall_upper"),
        };

        public static HouseRoom Find(string id)
        {
            foreach (var room in All)
                if (room.Id == id) return room;
            return null;
        }
    }
}
