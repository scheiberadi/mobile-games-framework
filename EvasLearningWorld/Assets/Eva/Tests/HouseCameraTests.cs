using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class HouseCameraTests
    {
        [Test]
        public void RoomCentresFollowTheThreeByThreeGrid()
        {
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("living")), Is.EqualTo(new Vector2(-2268f, -1032f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("hall_upper")), Is.EqualTo(new Vector2(-756f, 0f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("play")), Is.EqualTo(new Vector2(756f, 1032f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("bath")), Is.EqualTo(new Vector2(2268f, 0f)));
        }

        [Test]
        public void ZoomedOnARoomWorldPointsLandAtTheirRoomLocalCanvasPosition()
        {
            foreach (var room in HouseRooms.All)
            {
                var centre = HouseCamera.RoomCentre(room);
                var container = HouseCamera.ContainerPosition(centre, 1f);
                foreach (var slot in HouseSlots.InRoom(room.Id))
                {
                    var world = centre + new Vector2(slot.X, slot.Y);
                    Assert.That(world * 1f + container, Is.EqualTo(new Vector2(slot.X, slot.Y)), slot.Id);
                }
            }
        }

        [Test]
        public void OverviewFitsTheWholeHouseInsideTheFrame()
        {
            var s = HouseCamera.OverviewScale;
            var focus = HouseCamera.OverviewFocus;
            // shell extents in world units: roof top, base slab bottom, half width (see HouseCamera.Shell*)
            var top = (1f - HouseCamera.ShellPivotY) * HouseCamera.ShellHeight;
            var bottom = -HouseCamera.ShellPivotY * HouseCamera.ShellHeight;
            Assert.That((top - focus.y) * s, Is.LessThanOrEqualTo(450f));
            Assert.That((bottom - focus.y) * s, Is.GreaterThanOrEqualTo(-450f));
            Assert.That(HouseCamera.ShellWidth / 2f * s, Is.LessThanOrEqualTo(720f));
        }
    }
}
