using System.Collections.Generic;
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

        // --- Levels, parts of other builds, the corner picture, playing with the finished build ---

        private BuildScreen ShowAtLevel(ScreenId id, WorkshopBuildKind kind, int level)
        {
            var levels = new List<int>();
            while (levels.Count <= (int)kind) levels.Add(BuildDifficulty.MinLevel);
            levels[(int)kind] = level;
            _game.Progress.BuildLevels = levels;
            return ShowScreen(id);
        }

        [TestCaseSource(nameof(Games))]
        public void LevelOneIsTheOriginalGameWithEveryOutlineAndNoOtherParts(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowAtLevel(id, kind, 1);
            Assert.That(screen.Level, Is.EqualTo(1));
            for (var s = 0; s < screen.SlotCount; s++) Assert.That(screen.SlotOutlined(s) && screen.DotsShown(s));
            for (var i = 0; i < screen.PieceCount; i++) Assert.That(screen.PieceIsDecoy(i), Is.False);
            Assert.That(screen.BlueprintShown, Is.False);
        }

        [TestCaseSource(nameof(Games))]
        public void LevelTwoKeepsTheBiggerHalfOfTheOutlinesAndMixesInOneOtherPartAndShowsThePicture(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowAtLevel(id, kind, 2);
            var outlined = Enumerable.Range(0, screen.SlotCount).Where(screen.SlotOutlined).ToArray();
            Assert.That(outlined.Length, Is.EqualTo((screen.SlotCount + 1) / 2));
            var smallest = outlined.Min(s => screen.Layout.parts[s].w * screen.Layout.parts[s].h);
            for (var s = 0; s < screen.SlotCount; s++)
            {
                Assert.That(screen.DotsShown(s), Is.EqualTo(screen.SlotOutlined(s)));
                if (!screen.SlotOutlined(s)) Assert.That(screen.Layout.parts[s].w * screen.Layout.parts[s].h, Is.LessThanOrEqualTo(smallest), "the outlines kept are the biggest parts");
            }
            Assert.That(Enumerable.Range(0, screen.PieceCount).Count(screen.PieceIsDecoy), Is.EqualTo(1));
            Assert.That(screen.PieceCount, Is.LessThanOrEqualTo(BuildDifficulty.MaxCards));
            Assert.That(screen.BlueprintShown);
        }

        [TestCaseSource(nameof(Games))]
        public void LevelThreeHasNoOutlinesAndUpToTwoOtherPartsAndEveryCardStillFitsTheScreen(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowAtLevel(id, kind, 3);
            for (var s = 0; s < screen.SlotCount; s++) Assert.That(screen.DotsShown(s), Is.False);
            var decoys = Enumerable.Range(0, screen.PieceCount).Count(screen.PieceIsDecoy);
            Assert.That(decoys, Is.EqualTo(System.Math.Min(2, BuildDifficulty.MaxCards - screen.SlotCount)));
            Assert.That(screen.PieceCount, Is.LessThanOrEqualTo(BuildDifficulty.MaxCards));
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(screen.PieceCount));
            foreach (var drag in drags)
            {
                var rect = WorldRect(drag.Rect);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(Mathf.Abs(drag.Rect.anchoredPosition.x), Is.LessThanOrEqualTo(850f), "inside the screen");
                Assert.That(rect.Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
            }
            for (var i = 0; i < screen.PieceCount; i++)
                Assert.That(Mathf.Max(screen.CardSize(i).x, screen.CardSize(i).y), Is.GreaterThanOrEqualTo(150f).And.LessThanOrEqualTo(BuildScreen.CardBox + 0.5f), "card " + i);
            // the cards sit side by side in a row
            var xs = Enumerable.Range(0, screen.PieceCount).Select(i => screen.PieceHome(i).x).OrderBy(x => x).ToArray();
            for (var k = 1; k < xs.Length; k++) Assert.That(xs[k] - xs[k - 1], Is.GreaterThanOrEqualTo(150f), "cards do not pile up");
        }

        [TestCaseSource(nameof(Games))]
        public void TheCornerPictureSitsClearOfEvaTheBackButtonAndTheBuild(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowAtLevel(id, kind, 2);
            var rect = WorldRect(screen.BlueprintRect);
            Assert.That(rect.Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-960f));
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(-690f));
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(450f));
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(-5f), "above Eva");
        }

        [Test]
        public void APartHiddenOutlineStillTakesTheRightPart()
        {
            var screen = ShowAtLevel(ScreenId.BuildACar, WorkshopBuildKind.Car, 3);
            var slot = 0;
            var card = CardFor(screen, slot);
            Drop(screen, card, screen.SlotPoint(slot) + new Vector2(20f, 10f));
            Assert.That(screen.SlotFilled(slot) && screen.PiecePlaced(card));
            Assert.That(screen.Mistakes, Is.EqualTo(0));
        }

        [Test]
        public void APartOfAnotherBuildLetGoOnTheBuildIsAMissAndGoesBack()
        {
            var screen = ShowAtLevel(ScreenId.BuildACar, WorkshopBuildKind.Car, 2);
            var decoy = Enumerable.Range(0, screen.PieceCount).First(screen.PieceIsDecoy);
            Drop(screen, decoy, screen.SlotPoint(0));
            Assert.That(screen.PiecePlaced(decoy), Is.False);
            Assert.That(screen.SlotFilled(0), Is.False);
            Assert.That(screen.Mistakes, Is.EqualTo(1));
        }

        [Test]
        public void TheOtherPartsDoNotStopTheBuildFromFinishing()
        {
            var screen = ShowAtLevel(ScreenId.BuildARocket, WorkshopBuildKind.Rocket, 3);
            for (var s = 0; s < screen.SlotCount; s++) Drop(screen, CardFor(screen, s), screen.SlotPoint(s));
            Assert.That(screen.RoundOver);
        }

        [Test]
        public void TheHelpShowsAHiddenOutlineAgain()
        {
            var screen = ShowAtLevel(ScreenId.BuildACar, WorkshopBuildKind.Car, 3);
            var decoy = Enumerable.Range(0, screen.PieceCount).First(screen.PieceIsDecoy);
            Drop(screen, decoy, screen.SlotPoint(0));
            Drop(screen, decoy, screen.SlotPoint(0));
            Assert.That(screen.Hinting, "the second miss is the hint");
            screen.Tick(0.02f);
            Assert.That(screen.DotsShown(0), "the outline of the next part shows up");
        }

        [Test]
        public void ANewSessionTakesTheNewLevelFromProgress()
        {
            var screen = ShowAtLevel(ScreenId.BuildABoat, WorkshopBuildKind.Boat, 1);
            Assert.That(screen.BlueprintShown, Is.False);
            BuildDifficulty.Record(_game.Progress, WorkshopBuildKind.Boat, HelpStep.None);
            _game.Navigator.Show(ScreenId.Workshop);
            ShowScreen(ScreenId.BuildABoat);
            Assert.That(screen.Level, Is.EqualTo(2));
            Assert.That(screen.BlueprintShown);
        }

        [Test]
        public void TheLevelGoesUpAfterABuildAndDownAfterOneTheHandHadToFinishAndStaysBetweenOneAndThree()
        {
            var progress = new PlayerProgress();
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Robot), Is.EqualTo(1));
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.None);
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Robot), Is.EqualTo(2));
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.Hint);
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.None);
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Robot), Is.EqualTo(3));
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.Demonstrate);
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Robot), Is.EqualTo(2));
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.Demonstrate);
            BuildDifficulty.Record(progress, WorkshopBuildKind.Robot, HelpStep.Demonstrate);
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Robot), Is.EqualTo(1));
            Assert.That(BuildDifficulty.LevelOf(progress, WorkshopBuildKind.Car), Is.EqualTo(1), "the other games keep their own level");
        }

        [Test]
        public void TheOtherPartsHaveAPictureAndNeverAreAPartOfThisBuild()
        {
            foreach (var kind in new[] { WorkshopBuildKind.Car, WorkshopBuildKind.Rocket, WorkshopBuildKind.House, WorkshopBuildKind.Boat, WorkshopBuildKind.Robot, WorkshopBuildKind.Bridge })
            {
                var own = BuildLayout.Load(kind).parts.Select(p => p.part).ToArray();
                var pool = BuildDifficulty.DecoyPool(kind);
                Assert.That(pool.Length, Is.GreaterThanOrEqualTo(2), kind.ToString());
                foreach (var id in pool)
                {
                    Assert.That(Resources.Load<Sprite>("Art/workshop/build_" + id), Is.Not.Null, id);
                    Assert.That(id.StartsWith(BuildLayout.GameName(kind) + "_"), Is.False, id + " is from another build");
                    Assert.That(own, Does.Not.Contain(id.Substring(id.IndexOf('_') + 1)), id + " must not look like one of the real parts");
                }
            }
        }

        [TestCaseSource(nameof(Games))]
        public void TheFinishedBuildCanBeTappedToPlayWithAndOnlyOnceAtATime(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowScreen(id);
            Assert.That(screen.TapAreaShown, Is.False, "not while building");
            screen.TapBuild();
            Assert.That(screen.Plays, Is.EqualTo(0), "taps do nothing before the build is done");
            screen.BeginPlay();
            // The rocket has its own red button; every other build is tapped itself.
            Assert.That(screen.TapAreaShown, Is.EqualTo(kind != WorkshopBuildKind.Rocket));
            Assert.That(screen.LaunchShown, Is.EqualTo(kind == WorkshopBuildKind.Rocket));
            var rect = WorldRect(kind == WorkshopBuildKind.Rocket ? screen.LaunchRect : screen.BuildTapRect);
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            screen.TapBuild();
            Assert.That(screen.Plays, Is.EqualTo(1));
            Assert.That(screen.PlayBusy, "the show is running");
            screen.TapBuild();
            Assert.That(screen.Plays, Is.EqualTo(1), "a tap in the middle of the show is ignored");
        }

        [Test]
        public void ANewSessionStopsTheFinishedBuildFromBeingTapped()
        {
            var screen = ShowScreen(ScreenId.BuildACar);
            screen.BeginPlay();
            screen.TapBuild();
            _game.Navigator.Show(ScreenId.Workshop);
            ShowScreen(ScreenId.BuildACar);
            Assert.That(screen.Playing, Is.False);
            Assert.That(screen.PlayBusy, Is.False);
            Assert.That(screen.TapAreaShown, Is.False);
            Assert.That(screen.Plays, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(Games))]
        public void TheChildsCharacterAndEvaStandBottomLeftClearOfTheTrayAndTheCornerPicture(ScreenId id, WorkshopBuildKind kind)
        {
            var screen = ShowAtLevel(id, kind, 2);
            Assert.That(screen.Pair, Is.Not.Null);
            Assert.That(screen.Root.GetComponentInChildren<CompanionPairMarker>(true), Is.Not.Null, "the navigator keeps the character's look up to date");
            var player = screen.Pair.PlayerAnchor.anchoredPosition;
            var eva = screen.Pair.EvaAnchor.anchoredPosition;
            Assert.That(player.x, Is.LessThan(eva.x), "the character left of Eva");
            Assert.That(eva.x + CompanionLayout.Bench.EvaHeight * 0.5f, Is.LessThanOrEqualTo(-585f), "left of the first card of the tray");
            Assert.That(player.x - CompanionLayout.Bench.PlayerHeight * 0.25f, Is.GreaterThanOrEqualTo(-960f), "on the screen");
            Assert.That(player.y + CompanionLayout.Bench.PlayerHeight, Is.LessThanOrEqualTo(WorldRect(screen.BlueprintRect).yMin + 40f), "below the corner picture");
        }

        [Test]
        public void TheRocketCountdownRunsFromItsRedButtonAndOnlyOnceAtATime()
        {
            var screen = ShowScreen(ScreenId.BuildARocket);
            screen.BeginPlay();
            screen.TapBuild();
            Assert.That(screen.PlayBusy);
            Assert.That(screen.Plays, Is.EqualTo(1));
            screen.TapBuild();
            Assert.That(screen.Plays, Is.EqualTo(1));
        }

        [Test]
        public void TheLaunchButtonIsClearOfTheEndButtonsAndTheCoinCounter()
        {
            var screen = ShowScreen(ScreenId.BuildARocket);
            screen.BeginPlay();
            var rect = WorldRect(screen.LaunchRect);
            Assert.That(rect.Overlaps(new Rect(-280f, -420f, 560f, 260f)), Is.False, "the replay and home buttons");
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(930f));
            Assert.That(rect.yMax, Is.LessThan(150f));
        }

        [Test]
        public void TheHouseHasARoomBehindItsDoor()
        {
            var screen = ShowScreen(ScreenId.BuildAHouse);
            Assert.That(Resources.Load<Sprite>("Art/workshop/build_house_inside"), Is.Not.Null);
            Assert.That(screen.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true).Any(i => i.name == "Inside"));
        }
    }
}
