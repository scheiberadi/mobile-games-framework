using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Workshop build games: the finished build is dotted outlines in the middle, the parts wait in a mixed-up tray, a part dropped on an outline it
    // fits snaps in, one dropped on an outline it does not fit is a miss, and the last part finishes the build. Coroutines (voice, hand, the build's
    // little show) do not advance in Edit Mode, but placement and the rules are set synchronously.
    public class BuildScreenTests
    {
        private static readonly object[][] Games =
        {
            new object[] { ScreenId.BuildACar, WorkshopBuildKind.Car },
            new object[] { ScreenId.BuildARocket, WorkshopBuildKind.Rocket },
            new object[] { ScreenId.BuildAHouse, WorkshopBuildKind.House },
            new object[] { ScreenId.BuildABoat, WorkshopBuildKind.Boat },
            new object[] { ScreenId.BuildARobot, WorkshopBuildKind.Robot },
            new object[] { ScreenId.BridgeBuilding, WorkshopBuildKind.Bridge },
        };

        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            BuildScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            BuildScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private BuildScreen ShowScreen(ScreenId id)
        {
            _game.Navigator.Show(id);
            var screen = _game.Navigator.GetScreen(id) as BuildScreen;
            Assert.IsNotNull(screen, id + " is a build screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // Picks the part up, carries it to a point (the screen's own coordinates) and lets go.
        private static void Drop(BuildScreen screen, int piece, Vector2 point)
        {
            var drag = screen.PieceDrag(piece);
            drag.OnBeginDrag(new PointerEventData(null));
            drag.Rect.anchoredPosition = point;
            drag.OnEndDrag(new PointerEventData(null));
        }

        private static int CardFor(BuildScreen screen, int slot)
        {
            for (var i = 0; i < screen.PieceCount; i++)
                if (!screen.PiecePlaced(i) && screen.PieceId(i) == screen.SlotPartId(slot)) return i;
            return -1;
        }

        [TestCaseSource(nameof(Games))]
        public void EveryGameHasFiveToSevenPartsAndAllTheirPictures(ScreenId id, WorkshopBuildKind kind)
        {
            var layout = BuildLayout.Load(kind);
            Assert.IsNotNull(layout, "the importer wrote the layout");
            Assert.That(layout.parts.Length, Is.InRange(5, 7));
            foreach (var part in layout.parts)
            {
                Assert.That(Resources.Load<Sprite>("Art/workshop/build_" + layout.game + "_" + part.part), Is.Not.Null, part.part + " picture");
                Assert.That(Resources.Load<Sprite>("Art/workshop/build_" + layout.game + "_" + part.part + "_slot"), Is.Not.Null, part.part + " dotted outline");
                Assert.That(part.x, Is.GreaterThanOrEqualTo(-20), part.part + " inside the picture");
                Assert.That(part.y, Is.GreaterThanOrEqualTo(-20), part.part + " inside the picture");
                Assert.That(part.x + part.w, Is.LessThanOrEqualTo(layout.width + 20), part.part + " inside the picture");
                Assert.That(part.y + part.h, Is.LessThanOrEqualTo(layout.height + 20), part.part + " inside the picture");
            }
        }

        [TestCaseSource(nameof(Games))]
        public void ThePartsStartInTheTrayAndTheBuildIsOnlyDottedOutlines(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowScreen(id);
            Assert.That(screen.SlotCount, Is.EqualTo(screen.Layout.parts.Length));
            Assert.That(screen.PieceCount, Is.EqualTo(screen.SlotCount), "a card for every part");
            for (var s = 0; s < screen.SlotCount; s++)
            {
                Assert.That(screen.DotsShown(s), "outline " + s);
                Assert.That(screen.SolidShown(s), Is.False);
                Assert.That(screen.SlotFilled(s), Is.False);
            }
            for (var i = 0; i < screen.PieceCount; i++) Assert.That(screen.PiecePlaced(i), Is.False);
            Assert.That(screen.RoundOver, Is.False);
        }

        [TestCaseSource(nameof(Games))]
        public void EveryCardIsAFullSizeTapTargetInsideTheScreenAndClearOfTheBackButton(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowScreen(id);
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(screen.PieceCount));
            foreach (var drag in drags)
            {
                var rect = WorldRect(drag.Rect);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
                Assert.That(Mathf.Abs(drag.Rect.anchoredPosition.x), Is.LessThanOrEqualTo(850f), "inside the screen");
            }
        }

        [TestCaseSource(nameof(Games))]
        public void TheBuildFitsAboveTheTrayAndClearOfTheCornerControls(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowScreen(id);
            for (var s = 0; s < screen.SlotCount; s++)
            {
                var part = screen.Layout.parts[s];
                var half = new Vector2(part.w, part.h) * screen.Scale * 0.5f;
                var middle = screen.SlotPoint(s);
                Assert.That(middle.y - half.y, Is.GreaterThan(BuildScreen.TrayY + 125f), "above the tray, part " + part.part);
                Assert.That(middle.y + half.y, Is.LessThan(450f), "on the screen, part " + part.part);
                Assert.That(middle.x - half.x, Is.GreaterThan(-640f), "right of Eva and the Back button, part " + part.part);
                Assert.That(middle.x + half.x, Is.LessThan(740f), "left of the coin counter, part " + part.part);
            }
        }

        [TestCaseSource(nameof(Games))]
        public void ThePartsAreMixedUpInTheTray(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowScreen(id);
            var inTrayOrder = Enumerable.Range(0, screen.PieceCount).OrderBy(i => screen.PieceHome(i).x).ToArray();
            Assert.That(inTrayOrder, Is.Not.EqualTo(Enumerable.Range(0, screen.PieceCount).ToArray()), "not in the order of the build");
        }

        [Test]
        public void APartLetGoOverItsOwnOutlineSnapsIn()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            var slot = 0;
            var card = CardFor(screen, slot);
            Drop(screen, card, screen.SlotPoint(slot) + new Vector2(30f, -20f));
            Assert.That(screen.PiecePlaced(card));
            Assert.That(screen.SlotFilled(slot));
            Assert.That(screen.SolidShown(slot));
            Assert.That(screen.DotsShown(slot), Is.False);
            Assert.That(screen.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void ThePairOfWheelsFitsEitherOutline()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            var wheels = Enumerable.Range(0, screen.SlotCount).Where(s => screen.SlotPartId(s) == "wheel").ToArray();
            Assert.That(wheels.Length, Is.EqualTo(2));
            var cards = Enumerable.Range(0, screen.PieceCount).Where(i => screen.PieceId(i) == "wheel").ToArray();
            Drop(screen, cards[0], screen.SlotPoint(wheels[1]));
            Drop(screen, cards[1], screen.SlotPoint(wheels[0]));
            Assert.That(screen.SlotFilled(wheels[0]) && screen.SlotFilled(wheels[1]));
            Assert.That(screen.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void APartLetGoOverAnOutlineItDoesNotFitIsAMissAndGoesBack()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            var door = Enumerable.Range(0, screen.SlotCount).First(s => screen.SlotPartId(s) == "door");
            var card = Enumerable.Range(0, screen.PieceCount).First(i => screen.PieceId(i) == "headlight");
            Drop(screen, card, screen.SlotPoint(door));
            Assert.That(screen.PiecePlaced(card), Is.False);
            Assert.That(screen.SlotFilled(door), Is.False);
            Assert.That(screen.Mistakes, Is.EqualTo(1));
        }

        [Test]
        public void APartLetGoInTheMiddleOfNowhereIsNotAMiss()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            Drop(screen, 0, new Vector2(700f, -100f));
            Assert.That(screen.PiecePlaced(0), Is.False);
            Assert.That(screen.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void ThePartsOfAFinishedBuildEndTheRound()
        {
            var screen = ShowScreen(ScreenId.BuildARocket);
            for (var s = 0; s < screen.SlotCount; s++)
            {
                Assert.That(screen.RoundOver, Is.False, "not before the last part");
                Drop(screen, CardFor(screen, s), screen.SlotPoint(s));
            }
            for (var s = 0; s < screen.SlotCount; s++) Assert.That(screen.SlotFilled(s));
            Assert.That(screen.RoundOver);
        }

        [Test]
        public void AnyOrderWorksAndTheBigPartsAreNotNeededFirst()
        {
            var screen = ShowScreen(ScreenId.BuildAHouse);
            for (var s = screen.SlotCount - 1; s >= 0; s--) Drop(screen, CardFor(screen, s), screen.SlotPoint(s));
            Assert.That(screen.RoundOver);
            Assert.That(screen.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void ANewSessionPutsEveryPartBackInTheTray()
        {
            var screen = ShowScreen(ScreenId.BuildABoat);
            for (var s = 0; s < screen.SlotCount; s++) Drop(screen, CardFor(screen, s), screen.SlotPoint(s));
            Assert.That(screen.RoundOver);
            _game.Navigator.Show(ScreenId.Workshop);
            ShowScreen(ScreenId.BuildABoat);
            Assert.That(screen.RoundOver, Is.False);
            for (var s = 0; s < screen.SlotCount; s++)
            {
                Assert.That(screen.SlotFilled(s), Is.False);
                Assert.That(screen.DotsShown(s));
            }
            for (var i = 0; i < screen.PieceCount; i++) Assert.That(screen.PiecePlaced(i), Is.False);
        }

        [Test]
        public void ThreeMissesInARowMakeTheHandPutThePartIn()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            var door = Enumerable.Range(0, screen.SlotCount).First(s => screen.SlotPartId(s) == "door");
            var card = Enumerable.Range(0, screen.PieceCount).First(i => screen.PieceId(i) == "headlight");
            for (var n = 0; n < 3; n++) Drop(screen, card, screen.SlotPoint(door));
            Assert.That(screen.Mistakes, Is.EqualTo(3));
            Assert.That(screen.Hinting, "the hand starts helping");
        }
    }
}
