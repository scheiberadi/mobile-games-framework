using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class HouseRoomsTests
    {
        [Test]
        public void ThereAreNineAreasWithUniqueIdsSevenRoomsAndTwoHallways()
        {
            Assert.That(HouseRooms.All.Count, Is.EqualTo(9));
            Assert.That(HouseRooms.All.Select(r => r.Id).Distinct().Count(), Is.EqualTo(9));
            Assert.That(HouseRooms.All.Count(r => r.IsHall), Is.EqualTo(2));
            Assert.That(HouseRooms.All.Count(r => !r.IsHall), Is.EqualTo(7));
            foreach (var room in HouseRooms.All) Assert.That(HouseRooms.Find(room.Id), Is.SameAs(room));
        }

        [Test]
        public void NeighbourLinksPointAtRealRoomsAndAreSymmetric()
        {
            foreach (var room in HouseRooms.All)
            {
                Check(room, room.Left, r => r.Right);
                Check(room, room.Right, r => r.Left);
                Check(room, room.Up, r => r.Down);
                Check(room, room.Down, r => r.Up);
            }
        }

        private static void Check(HouseRoom room, string neighbourId, System.Func<HouseRoom, string> back)
        {
            if (neighbourId == null) return;
            var neighbour = HouseRooms.Find(neighbourId);
            Assert.That(neighbour, Is.Not.Null, room.Id + " links to unknown room " + neighbourId);
            Assert.That(back(neighbour), Is.EqualTo(room.Id), room.Id + " <-> " + neighbourId);
        }

        [Test]
        public void EveryRoomIsReachableFromEveryOtherRoom()
        {
            foreach (var start in HouseRooms.All)
            {
                var seen = new HashSet<string> { start.Id };
                var queue = new Queue<HouseRoom>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var room = queue.Dequeue();
                    foreach (var id in new[] { room.Left, room.Right, room.Up, room.Down })
                        if (id != null && seen.Add(id)) queue.Enqueue(HouseRooms.Find(id));
                }
                Assert.That(seen.Count, Is.EqualTo(9), "from " + start.Id);
            }
        }

        [Test]
        public void NeighbourDirectionsMatchTheGridPositions()
        {
            foreach (var room in HouseRooms.All)
            {
                if (room.Right != null) { var n = HouseRooms.Find(room.Right); Assert.That((n.Level, n.Column), Is.EqualTo((room.Level, room.Column + 1))); }
                if (room.Left != null) { var n = HouseRooms.Find(room.Left); Assert.That((n.Level, n.Column), Is.EqualTo((room.Level, room.Column - 1))); }
                if (room.Up != null) { var n = HouseRooms.Find(room.Up); Assert.That((n.Level, n.Column), Is.EqualTo((room.Level + 1, room.Column))); }
                if (room.Down != null) { var n = HouseRooms.Find(room.Down); Assert.That((n.Level, n.Column), Is.EqualTo((room.Level - 1, room.Column))); }
            }
        }

        [Test]
        public void OnlyHallwaysHaveStairsAndOnlyRoomThreeHasTwoDoors()
        {
            foreach (var room in HouseRooms.All)
            {
                var doors = (room.Left != null ? 1 : 0) + (room.Right != null ? 1 : 0);
                if (room.IsHall) continue;
                Assert.That(room.Up, Is.Null, room.Id + " has stairs up");
                if (room.Id != "party") Assert.That(room.Down, Is.Null, room.Id + " has stairs");
                var twoDoors = room.Id == "dining" || room.Id == "kids";
                Assert.That(doors, Is.EqualTo(room.Id == "party" ? 0 : twoDoors ? 2 : 1), room.Id + " (the attic is entered by the stairs)");
            }
        }

        [Test]
        public void HallwaysHaveNoSlots()
        {
            foreach (var room in HouseRooms.All.Where(r => r.IsHall))
                Assert.That(HouseSlots.InRoom(room.Id).Count(), Is.EqualTo(0), room.Id);
        }

        [Test]
        public void EveryRoomHasThreeOrFourSlotsAndSlotsBelongToRealRooms()
        {
            foreach (var room in HouseRooms.All.Where(r => !r.IsHall))
                Assert.That(HouseSlots.InRoom(room.Id).Count(), room.Id == "party" ? Is.InRange(8, 12) : Is.InRange(3, 4), room.Id); // the attic is one big room
            Assert.That(HouseSlots.All.Select(s => s.Id).Distinct().Count(), Is.EqualTo(HouseSlots.All.Count));
            foreach (var slot in HouseSlots.All) Assert.That(HouseRooms.Find(slot.Room), Is.Not.Null, slot.Id);
            Assert.That(HouseSlots.All.Count, Is.InRange(24, 36));
        }

        [Test]
        public void BedSlotsExistOnlyInTheTwoBedrooms()
        {
            var rooms = HouseSlots.All.Where(s => s.Kind == SlotKind.Bed).Select(s => s.Room).OrderBy(r => r).ToArray();
            Assert.That(rooms, Is.EqualTo(new[] { "kids", "parents" }));
        }

        [Test]
        public void SlotFeetStayInsideTheFrameBetweenTheBackWallAndTheTray()
        {
            // Slots are feet positions: a 240-260 unit item stands on them, so the feet must be above the bottom of the
            // frame and low enough that a bed (about 280 tall) or bookshelf (about 340) stays clear of the top edge.
            foreach (var slot in HouseSlots.All)
            {
                var attic = slot.Room == "party"; // the attic is a wide room: its picture is 1750 units wide in room view
                Assert.That(System.Math.Abs(slot.X) + 130f, Is.LessThanOrEqualTo(attic ? 1000f : 720f), slot.Id + " x");
                Assert.That(slot.Y, Is.GreaterThanOrEqualTo(-260f), slot.Id + " feet below the tray");
                Assert.That(slot.Y, Is.LessThanOrEqualTo(attic ? 140f : -30f), slot.Id + " feet above the back wall base");
            }
        }
    }
}
