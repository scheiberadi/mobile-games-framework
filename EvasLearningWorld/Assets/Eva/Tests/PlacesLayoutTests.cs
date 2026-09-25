using System;
using System.IO;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // tools/art-import/places-layout.json is what the art scripts (placeholders and the layout guide for ChatGPT) read.
    // Places is the authority; this test fails if the two ever differ.
    public class PlacesLayoutTests
    {
        [Serializable] private class Point { public float x, y; }
        [Serializable] private class Box { public float x, y, w, h; }
        [Serializable] private class PlaceData { public string id; public Box tapBox; public Point[] road; public Point standing; public Box roadBox; }
        [Serializable] private class LayoutData { public Point junction; public PlaceData[] places; }

        [Test]
        public void TheLayoutFileTheArtScriptsReadMatchesPlaces()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../tools/art-import/places-layout.json"));
            Assert.IsTrue(File.Exists(path), path);
            var layout = JsonUtility.FromJson<LayoutData>(File.ReadAllText(path));
            Assert.That(layout.junction.x, Is.EqualTo(Places.Junction.X));
            Assert.That(layout.junction.y, Is.EqualTo(Places.Junction.Y));
            Assert.That(layout.places.Length, Is.EqualTo(Places.All.Count));
            for (var i = 0; i < layout.places.Length; i++)
            {
                var data = layout.places[i];
                var place = Places.All[i];
                Assert.That(data.id, Is.EqualTo(place.Id.ToString()));
                AssertBox(data.tapBox, place.TapBox, place.Id + " tapBox");
                Assert.That(data.standing.x, Is.EqualTo(place.StandingSpot.X), place.Id + " standing x");
                Assert.That(data.standing.y, Is.EqualTo(place.StandingSpot.Y), place.Id + " standing y");
                Assert.That(data.road.Length, Is.EqualTo(place.Road.Count), place.Id + " road length");
                for (var p = 0; p < data.road.Length; p++)
                {
                    Assert.That(data.road[p].x, Is.EqualTo(place.Road[p].X), place.Id + " road " + p + " x");
                    Assert.That(data.road[p].y, Is.EqualTo(place.Road[p].Y), place.Id + " road " + p + " y");
                }
                if (place.RoadBox.HasValue) AssertBox(data.roadBox, place.RoadBox.Value, place.Id + " roadBox");
                else Assert.That(data.roadBox.w, Is.EqualTo(0f), place.Id + " has no road box");
            }
        }

        private static void AssertBox(Box data, WorldBox box, string label)
        {
            Assert.That(new[] { data.x, data.y, data.w, data.h }, Is.EqualTo(new[] { box.X, box.Y, box.Width, box.Height }), label);
        }
    }
}
