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
        }

        [Test]
        public void PlaygroundHasPatternCompletionFirstAndEveryEntryIsComplete()
        {
            var list = Activities.For(BuildingId.Playground);
            // The building menu scrolls (TileLayout/BuildingScreen), so there is no ceiling on this count any more.
            Assert.That(list.Count, Is.EqualTo(14));
            Assert.That(list[0].Id, Is.EqualTo("pattern_completion"));
            Assert.That(list[1].Id, Is.EqualTo("odd_one_out"));
            Assert.That(list[2].Id, Is.EqualTo("whats_missing"));
            Assert.That(list[3].Id, Is.EqualTo("which_doesnt_make_sense"));
            Assert.That(list[4].Id, Is.EqualTo("item_to_shadow"));
            Assert.That(list[5].Id, Is.EqualTo("finger_maze"));
            Assert.That(list[6].Id, Is.EqualTo("follow_numbers"));
            Assert.That(list[7].Id, Is.EqualTo("follow_letters"));
            Assert.That(list[8].Id, Is.EqualTo("shortest_path"));
            Assert.That(list[9].Id, Is.EqualTo("avoid_obstacles"));
            Assert.That(list[10].Id, Is.EqualTo("collect_everything"));
            Assert.That(list[11].Id, Is.EqualTo("rotate_piece"));
            Assert.That(list[12].Id, Is.EqualTo("jigsaw"));
            Assert.That(list[13].Id, Is.EqualTo("tangram"));
            foreach (var activity in list)
            {
                Assert.That(activity.Building, Is.EqualTo(BuildingId.Playground));
                Assert.IsNotEmpty(activity.ScreenKey);
                Assert.IsNotEmpty(activity.IconSprite);
                Assert.IsNotEmpty(activity.VoiceKey);
            }
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
