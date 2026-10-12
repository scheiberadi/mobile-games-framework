using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Human Senses' "What do I use?" screen: the table, the organs, what a drop does. Coroutines (voice, hand, the reaction) do not
    // advance in Edit Mode, but the rules run synchronously on a drop, which these tests do by hand.
    public class SensesScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            SensesScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            SensesScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private SensesScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.HumanSenses);
            var screen = _game.Navigator.GetScreen(ScreenId.HumanSenses) as SensesScreen;
            Assert.IsNotNull(screen, "Human Senses is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // The organ for `sense` is dragged onto thing `index` and let go.
        private static void Drop(SensesScreen screen, Sense sense, int index)
        {
            var drag = screen.OrganDrags[(int)sense];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceOrgan((int)sense, screen.ThingPoint(index));
            drag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void FiveOrgansAreTheOnlyThingsToDragAndEachIsAFullSizeTapTarget()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(5));
            foreach (var drag in drags)
            {
                Assert.That(WorldRect(drag.Rect).width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(WorldRect(drag.Rect).height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            }
        }

        [Test]
        public void ThreeThingsStandOnTheTableClearOfOneAnotherAndTheBackButton()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round.Count, Is.EqualTo(Senses.ThingsPerRound));
            var boxes = Enumerable.Range(0, 3).Select(i => WorldRect((RectTransform)screen.Root.Find("Things/Thing" + i))).ToArray();
            for (var i = 0; i < 3; i++)
                for (var j = i + 1; j < 3; j++) Assert.That(boxes[i].Overlaps(boxes[j]), Is.False, i + " / " + j);
            var back = new Rect(-690f, 175f, 240f, 240f);
            foreach (var box in boxes) Assert.That(box.Overlaps(back), Is.False, "under the Back button");
        }

        [Test]
        public void TheOrgansDoNotTouchEachOtherNorTheCompanions()
        {
            var screen = ShowScreen();
            var rects = screen.OrganDrags.Select(d => WorldRect(d.Rect)).ToArray();
            for (var i = 0; i < rects.Length; i++)
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, i + " / " + j);
            var corner = new Rect(450f, -440f, 250f, 200f); // where Eva and the child stand
            foreach (var rect in rects) Assert.That(rect.Overlaps(corner), Is.False);
        }

        [Test]
        public void TheRightOrganUsesTheThing()
        {
            var screen = ShowScreen();
            Drop(screen, screen.Round.SenseOf(0), 0);
            Assert.That(screen.Round.IsUsed(0));
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(0));
        }

        [Test]
        public void AWrongOrganChangesNothingAndIsNotAMistakeAgainstTheThing()
        {
            var screen = ShowScreen();
            var wrong = (Sense)(((int)screen.Round.SenseOf(0) + 1) % 5);
            Drop(screen, wrong, 0);
            Assert.That(screen.Round.IsUsed(0), Is.False);
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(1));
        }

        [Test]
        public void AnOrganLetGoFarFromEveryThingIsNotTried()
        {
            var screen = ShowScreen();
            var drag = screen.OrganDrags[0];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceOrgan(0, new Vector2(-600f, -330f));
            drag.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(0));
            Assert.That(screen.Round.Wrongs, Is.EqualTo(0));
        }

        [Test]
        public void TwoWrongOrgansInARowStartTheHintWhichStartsTheCountAgain()
        {
            var screen = ShowScreen();
            var wrong = (Sense)(((int)screen.Round.SenseOf(0) + 1) % 5);
            Drop(screen, wrong, 0);
            Drop(screen, wrong, 0);
            Assert.That(screen.Round.Wrongs, Is.EqualTo(0), "the hint was started and the count began again");
        }

        [Test]
        public void UsingAllThreeThingsFinishesTheRound()
        {
            var screen = ShowScreen();
            for (var i = 0; i < 3; i++) Drop(screen, screen.Round.SenseOf(i), i);
            Assert.That(screen.Round.Done);
        }
    }
}
