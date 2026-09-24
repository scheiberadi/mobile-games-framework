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
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("living")), Is.EqualTo(new Vector2(-1440f, -900f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("kids")), Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("play")), Is.EqualTo(new Vector2(0f, 900f)));
            Assert.That(HouseCamera.RoomCentre(HouseRooms.Find("bath")), Is.EqualTo(new Vector2(1440f, 0f)));
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
            // house extents in world units incl. roof (top 1800), ground (bottom -1400), balcony/door sides (+-2400)
            Assert.That((1800f - focus.y) * s, Is.LessThanOrEqualTo(450f));
            Assert.That((-1400f - focus.y) * s, Is.GreaterThanOrEqualTo(-450f));
            Assert.That(2400f * s, Is.LessThanOrEqualTo(720f));
        }
    }
}
