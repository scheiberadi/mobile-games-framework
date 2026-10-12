using System;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    public class SolarSystemTests
    {
        [Test]
        public void ASessionIsTheInnerFourThenTheOuterFour()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var session = SolarSystem.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(SolarSystem.RoundsPerSession));
                Assert.That(session.SelectMany(r => r.Planets).Distinct().Count(), Is.EqualTo(8));
                foreach (var round in session) Assert.That(round.Planets.Length, Is.EqualTo(SolarSystem.PlanetsPerRound));
                Assert.That(session[0].Planets.All(p => SolarSystem.RingOf(p) < 4), "the inner planets are the first page");
                Assert.That(session[1].Planets.All(p => SolarSystem.RingOf(p) >= 4), "the outer planets are the second page");
            }
        }

        [Test]
        public void EachPlanetBelongsOnTheRingOfItsPlaceFromTheSun()
        {
            Assert.That(SolarSystem.RingOf(Planet.Mercury), Is.EqualTo(0));
            Assert.That(SolarSystem.RingOf(Planet.Earth), Is.EqualTo(2));
            Assert.That(SolarSystem.RingOf(Planet.Neptune), Is.EqualTo(7));
        }

        [Test]
        public void ARightRingKeepsThePlanetAndAWrongOneDoesNot()
        {
            var round = new SolarRound(new[] { Planet.Mercury, Planet.Venus, Planet.Earth, Planet.Mars }, new System.Random(3));
            Assert.That(round.Drop(Planet.Earth, 1), Is.EqualTo(SolarDrop.Wrong));
            Assert.That(round.IsPlaced(Planet.Earth), Is.False);
            Assert.That(round.Drop(Planet.Earth, 2), Is.EqualTo(SolarDrop.Right));
            Assert.That(round.IsPlaced(Planet.Earth));
            Assert.That(round.Drop(Planet.Earth, 2), Is.EqualTo(SolarDrop.Again));
            Assert.That(round.PlacedCount, Is.EqualTo(1));
        }

        [Test]
        public void AnyPlanetOnItsOwnRingCountsAndTheAskedPlanetMovesOn()
        {
            var round = new SolarRound(new[] { Planet.Mercury, Planet.Venus, Planet.Earth, Planet.Mars }, new System.Random(3));
            var asked = round.Asked.Value;
            var other = round.Planets.First(p => p != asked);
            round.Drop(other, SolarSystem.RingOf(other));
            Assert.That(round.Asked, Is.Not.Null);
            Assert.That(round.IsPlaced(other));
            foreach (var planet in round.Planets) round.Drop(planet, SolarSystem.RingOf(planet));
            Assert.That(round.Done);
            Assert.That(round.Asked, Is.Null);
        }

        [Test]
        public void TwoWrongRingsInARowCallForAHint()
        {
            var round = new SolarRound(new[] { Planet.Mercury, Planet.Venus, Planet.Earth, Planet.Mars }, new System.Random(3));
            round.Drop(Planet.Earth, 5);
            Assert.That(round.NeedsHint, Is.False);
            round.Drop(Planet.Mars, 0);
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }
    }

    // The Space screen: the tray and what a drop on a ring does. Coroutines (voice, hand) do not advance in Edit Mode, but the rules are
    // set synchronously on a drop.
    public class SpaceScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            SpaceScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            SpaceScreen.UseDevicePlacement = true;
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private SpaceScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.Space);
            var screen = _game.Navigator.GetScreen(ScreenId.Space) as SpaceScreen;
            Assert.IsNotNull(screen, "Space is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Drop(SpaceScreen screen, int card, Vector2 point)
        {
            var drag = screen.CardDrags[card];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCard(card, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        private static void OnRing(SpaceScreen screen, int card, int ring) => Drop(screen, card, SpaceScreen.RingPoint(ring, SpaceScreen.RingY));

        [Test]
        public void FourPlanetsAreTheOnlyThingsToDragAndEachIsAFullSizeTapTargetClearOfTheOthers()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(4));
            var rects = drags.Select(d => WorldRect(d.Rect)).ToArray();
            for (var i = 0; i < rects.Length; i++)
            {
                Assert.That(rects[i].width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rects[i].height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, i + " / " + j);
            }
            var back = new Rect(-690f, 175f, 240f, 240f);
            foreach (var rect in rects) Assert.That(rect.Overlaps(back), Is.False, "under the Back button");
        }

        [Test]
        public void TheRingSlotsAreEightAndEachPointIsOnItsOwnRingOnly()
        {
            Assert.That(SpaceScreen.RingX.Length, Is.EqualTo(8));
            for (var ring = 0; ring < 8; ring++)
            {
                Assert.That(SpaceScreen.RingAt(SpaceScreen.RingPoint(ring, SpaceScreen.RingY)), Is.EqualTo(ring));
                Assert.That(SpaceScreen.RingAt(SpaceScreen.RingPoint(ring, SpaceScreen.RingY + 150f)), Is.EqualTo(ring), "the arc bends, a point further up is still on it");
            }
            Assert.That(SpaceScreen.RingAt(new Vector2(0f, -330f)), Is.EqualTo(-1), "the tray is no ring");
        }

        [Test]
        public void ARoundAsksForOnePlanetOfTheFour()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round.Asked, Is.Not.Null);
            Assert.That(screen.Round.PlacedCount, Is.EqualTo(0));
            Assert.That(screen.CardOf(screen.Round.Asked.Value), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void APlanetLetGoOnItsRingStaysAndOneOnAWrongRingIsCounted()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var card = screen.CardOf(round.Asked.Value);
            var planet = screen.CardPlanet(card);
            var wrongRing = (SolarSystem.RingOf(planet) + 3) % 8;
            OnRing(screen, card, wrongRing);
            Assert.That(round.IsPlaced(planet), Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(1));
            Drop(screen, card, new Vector2(0f, -330f));
            Assert.That(round.TotalWrongs, Is.EqualTo(1), "the tray is no ring, nothing counted");
            OnRing(screen, card, SolarSystem.RingOf(planet));
            Assert.That(round.IsPlaced(planet));
        }

        [Test]
        public void AllFourOnTheirRingsFinishTheRound()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            for (var i = 0; i < 4; i++) OnRing(screen, i, SolarSystem.RingOf(screen.CardPlanet(i)));
            Assert.That(round.Done);
        }
    }
}
