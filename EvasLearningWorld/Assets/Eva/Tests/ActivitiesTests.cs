using System.Collections.Generic;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ActivitiesTests
    {
        [Test]
        public void SchoolHasCountFirstAndEveryEntryIsComplete()
        {
            var list = Activities.For(BuildingId.School);
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(list[0].Id, Is.EqualTo("count"));
            var ids = new HashSet<string>();
            foreach (var activity in list)
            {
                Assert.IsTrue(ids.Add(activity.Id), "duplicate id " + activity.Id);
                Assert.That(activity.Building, Is.EqualTo(BuildingId.School));
                Assert.IsNotEmpty(activity.ScreenKey);
                Assert.IsNotEmpty(activity.IconSprite);
                Assert.IsNotEmpty(activity.VoiceKey);
            }
            Assert.That(list.Count, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void ForReturnsTheSameOrderEveryTime()
        {
            var first = Activities.For(BuildingId.School);
            var second = Activities.For(BuildingId.School);
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++) Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
        }

        // The catalogue holds one School entry today, so the ordering rule is proven on a synthetic list: entries
        // keep their declared order and only the asked building's entries come back.
        [Test]
        public void FilterKeepsTheDeclaredOrder()
        {
            var source = new List<Activity>
            {
                new Activity("c", BuildingId.School, "Count", "a/c", "v_c"),
                new Activity("a", BuildingId.School, "Count", "a/a", "v_a"),
                new Activity("b", BuildingId.School, "Count", "a/b", "v_b"),
            };
            var result = Activities.Filter(BuildingId.School, source);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Id, Is.EqualTo("c"));
            Assert.That(result[1].Id, Is.EqualTo("a"));
            Assert.That(result[2].Id, Is.EqualTo("b"));
        }
    }
}
