using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One decoratable room of the doll house. Level 0 = ground, 1 = upper floor, 2 = attic; Column 0..2. The
    // room id doubles as the room type (one room of each type). Neighbour ids are written out explicitly.
    public sealed class HouseRoom
    {
        public string Id;
        public int Level, Column;
        public string Left, Right, Up, Down;

        public HouseRoom(string id, int level, int column, string left, string right, string up, string down)
        {
            Id = id; Level = level; Column = column; Left = left; Right = right; Up = up; Down = down;
        }
    }

    public static class HouseRooms
    {
        // The attic's third bay is a roof terrace (scenery): it is not a room and nothing links to it.
        public static readonly IReadOnlyList<HouseRoom> All = new[]
        {
            new HouseRoom("living",  0, 0, null,      "dining",  "parents", null),
            new HouseRoom("dining",  0, 1, "living",  "kitchen", "kids",    null),
            new HouseRoom("kitchen", 0, 2, "dining",  null,      "bath",    null),
            new HouseRoom("parents", 1, 0, null,      "kids",    "party",   "living"),
            new HouseRoom("kids",    1, 1, "parents", "bath",    "play",    "dining"),
            new HouseRoom("bath",    1, 2, "kids",    null,      null,      "kitchen"),
            new HouseRoom("party",   2, 0, null,      "play",    null,      "parents"),
            new HouseRoom("play",    2, 1, "party",   null,      null,      "kids"),
        };

        public static HouseRoom Find(string id)
        {
            foreach (var room in All)
                if (room.Id == id) return room;
            return null;
        }
    }
}
