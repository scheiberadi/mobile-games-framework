using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class FishingTests
    {
        private const float Step = 0.02f;

        private static SwimmingFish FishIn(int lane, FishSize size, float x, float speed = 100f) => new SwimmingFish
        {
            Lane = lane,
            Y = FishingDirector.LaneY[lane],
            Dir = lane % 2 == 0 ? 1f : -1f,
            Speed = speed,
            Size = size,
            X = x,
        };

        private static void RunFor(FishingDirector director, float seconds, List<SwimmingFish> spawned = null, List<SwimmingFish> hooked = null,
            List<SwimmingFish> landed = null, List<SwimmingFish> gone = null)
        {
            for (var t = 0f; t < seconds; t += Step) director.Tick(Step, spawned, hooked, landed, gone);
        }

        // Where to tap to hit a fish that keeps swimming: ahead of it, by how far it goes while the hook is on its way.
        private static Vector2 AimAt(SwimmingFish fish)
        {
            var aim = new Vector2(fish.X, fish.Y);
            for (var i = 0; i < 5; i++)
            {
                var seconds = Vector2.Distance(new Vector2(FishingDirector.RodTipX, FishingDirector.RodTipY), aim) / FishingDirector.HookSpeed;
                aim = new Vector2(fish.X + fish.Dir * fish.Speed * seconds, fish.Y);
            }
            return aim;
        }

        // A child that always aims at the biggest fish it can reach, one cast at a time.
        private static void PlayToTheEnd(FishingDirector director, int maxSeconds)
        {
            var elapsed = 0f;
            while (!director.LevelDone && elapsed < maxSeconds)
            {
                if (director.Hook.State == HookState.Idle)
                {
                    var target = director.Fish
                        .Where(f => !f.Hooked && Mathf.Abs(AimAt(f).x) < 800f)
                        .OrderByDescending(f => f.Points)
                        .FirstOrDefault();
                    if (target != null)
                    {
                        var aim = AimAt(target);
                        director.Cast(aim.x, aim.y);
                    }
                }
                director.Tick(Step, null, null, null, null);
                elapsed += Step;
            }
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMorePointsAndFasterFish()
        {
            for (var level = FishingDirector.MinLevel + 1; level <= FishingDirector.MaxLevel; level++)
            {
                Assert.Greater(FishingDirector.PointsToPass(level), FishingDirector.PointsToPass(level - 1));
                Assert.Greater(FishingDirector.Speed(level), FishingDirector.Speed(level - 1));
                Assert.Less(FishingDirector.SpawnGap(level), FishingDirector.SpawnGap(level - 1));
                Assert.GreaterOrEqual(FishingDirector.LaneCap(level), FishingDirector.LaneCap(level - 1));
            }
            Assert.Greater(FishingDirector.LaneCap(FishingDirector.MaxLevel), FishingDirector.LaneCap(FishingDirector.MinLevel));
        }

        [Test]
        public void ThereAreThreeSizesOfFishWorthOneTwoAndThreePointsAndTheBiggerOnesAreWider()
        {
            Assert.AreEqual(1, FishingDirector.PointsOf(FishSize.Small));
            Assert.AreEqual(2, FishingDirector.PointsOf(FishSize.Medium));
            Assert.AreEqual(3, FishingDirector.PointsOf(FishSize.Large));
            Assert.Less(FishingDirector.WidthOf(FishSize.Small), FishingDirector.WidthOf(FishSize.Medium));
            Assert.Less(FishingDirector.WidthOf(FishSize.Medium), FishingDirector.WidthOf(FishSize.Large));
        }

        [Test]
        public void FishSwimInTheRowsOfTheLakeAndEveryRowIsOneSizeOrTwoAndAlwaysOneWay()
        {
            var seen = new HashSet<FishSize>();
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(level));
                for (var i = 0; i < 3000; i++)
                {
                    director.Tick(0.05f, null, null, null, null);
                    foreach (var fish in director.Fish)
                    {
                        seen.Add(fish.Size);
                        Assert.AreEqual(FishingDirector.LaneY[fish.Lane], fish.Y, "level " + level);
                        Assert.Less(fish.Y, FishingDirector.SurfaceY, "fish are under the surface");
                        Assert.IsTrue(fish.Look >= 0 && fish.Look < FishingDirector.Looks);
                    }
                }
            }
            Assert.AreEqual(3, seen.Count, "all three sizes show up");
        }

        [Test]
        public void ARowNeverHoldsMoreFishThanTheLevelAllowsAndTwoFishInARowNeverOverlap()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(10 + level));
                for (var i = 0; i < 3000; i++)
                {
                    director.Tick(0.05f, null, null, null, null);
                    for (var lane = 0; lane < FishingDirector.Lanes; lane++)
                    {
                        var inLane = director.Fish.Where(f => f.Lane == lane).OrderBy(f => f.X).ToList();
                        Assert.LessOrEqual(inLane.Count, FishingDirector.LaneCap(level), "level " + level + " row " + lane);
                        for (var j = 1; j < inLane.Count; j++)
                            Assert.GreaterOrEqual(inLane[j].X - inLane[j - 1].X, (inLane[j].Width + inLane[j - 1].Width) * 0.5f, "fish overlap");
                    }
                }
            }
        }

        [Test]
        public void TheLakeStartsWithFishAlreadySwimmingAndFishKeepComingAndLeaveOnTheFarSide()
        {
            var director = new FishingDirector(1, new System.Random(3));
            Assert.GreaterOrEqual(director.Fish.Count, FishingDirector.Lanes, "a fish or two in every row from the start");
            var spawned = new List<SwimmingFish>();
            var gone = new List<SwimmingFish>();
            RunFor(director, 90f, spawned, null, null, gone);
            Assert.GreaterOrEqual(spawned.Count, 5);
            Assert.GreaterOrEqual(gone.Count, 5);
            Assert.AreEqual(0, director.Points, "fish that are not caught are simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(gone.All(f => Mathf.Abs(f.X) > FishingDirector.FieldHalfWidth));
            Assert.IsTrue(gone.All(f => f.X * f.Dir > 0f), "a fish leaves on the side it swims to");
        }

        [Test]
        public void ATapAboveTheWaterSendsTheHookJustUnderTheSurfaceAndOnlyOneHookIsOutAtATime()
        {
            var director = new FishingDirector(1, new System.Random(1), new SwimmingFish[0]);
            Assert.IsTrue(director.Cast(0f, 800f));
            Assert.Less(director.Hook.TargetY, FishingDirector.SurfaceY);
            Assert.AreEqual(HookState.Out, director.Hook.State);
            Assert.IsFalse(director.Cast(300f, -300f), "the hook is out, a second tap does nothing");
            Assert.AreEqual(0f, director.Hook.TargetX);
        }

        [Test]
        public void AHookThatTouchesNothingComesBackEmptyAndTheChildCanCastAgain()
        {
            var director = new FishingDirector(1, new System.Random(1), new SwimmingFish[0]);
            Assert.IsTrue(director.Cast(-500f, -300f));
            var wentOut = false;
            var cameBack = false;
            for (var t = 0f; t < 3f; t += Step)
            {
                director.Tick(Step, null, null, null, null);
                if (director.Hook.State == HookState.Out) wentOut = true;
                if (wentOut && director.Hook.State == HookState.In) cameBack = true;
                if (cameBack && director.Hook.State == HookState.Idle) break;
            }
            Assert.IsTrue(cameBack);
            Assert.AreEqual(HookState.Idle, director.Hook.State);
            Assert.AreEqual(0, director.Points);
            Assert.IsTrue(director.Cast(100f, -100f), "back at the rod, it can be cast again");
        }

        [Test]
        public void ACastAheadOfASwimmingFishHooksItAndEachSizeScoresItsPoints()
        {
            foreach (var size in new[] { FishSize.Small, FishSize.Medium, FishSize.Large })
            {
                var lane = size == FishSize.Small ? 1 : size == FishSize.Medium ? 2 : 4;
                var fish = FishIn(lane, size, -300f, 150f);
                var director = new FishingDirector(1, new System.Random(2), new[] { fish });
                var aim = AimAt(fish);
                Assert.IsTrue(director.Cast(aim.x, aim.y));
                var hooked = new List<SwimmingFish>();
                var landed = new List<SwimmingFish>();
                RunFor(director, 4f, null, hooked, landed);
                CollectionAssert.Contains(hooked, fish, size.ToString());
                CollectionAssert.Contains(landed, fish, size.ToString());
                Assert.AreEqual(FishingDirector.PointsOf(size), director.Points, size.ToString());
                Assert.AreEqual(HookState.Idle, director.Hook.State);
                CollectionAssert.DoesNotContain(director.Fish, fish, "a landed fish is gone from the lake");
            }
        }

        [Test]
        public void ACastBehindASwimmingFishMissesItBecauseFishDoNotStop()
        {
            var fish = FishIn(2, FishSize.Medium, -300f, 400f);
            var director = new FishingDirector(1, new System.Random(2), new[] { fish });
            director.Cast(fish.X, fish.Y); // straight at where it is now, no allowance for its swimming
            RunFor(director, 4f);
            Assert.AreEqual(0, director.Points);
        }

        [Test]
        public void AFishThatSwimsIntoTheLineOnTheWayBackIsHookedToo()
        {
            // The hook goes from the rod tip to (500, -300) and back along the same line; a small fish in the top row is placed so it is
            // away from the line on the way out and exactly on it on the way back.
            var fish = FishIn(0, FishSize.Small, 44.5f, 172.5f);
            var director = new FishingDirector(1, new System.Random(2), new[] { fish });
            director.Cast(500f, -300f);
            var sawIn = false;
            var hookedAfterTurn = false;
            var hooked = new List<SwimmingFish>();
            for (var t = 0f; t < 6f; t += Step)
            {
                hooked.Clear();
                var wasIn = director.Hook.State == HookState.In && director.Hook.Fish == null;
                director.Tick(Step, null, hooked, null, null);
                if (wasIn) sawIn = true;
                if (hooked.Count > 0) hookedAfterTurn = sawIn;
            }
            Assert.IsTrue(hookedAfterTurn, "hooked on the way back");
            Assert.AreEqual(1, director.Points);
        }

        [Test]
        public void ALevelIsDoneAfterItsPointsAndThenNoNewFishComeAndTheHookStays()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(5 + level));
                PlayToTheEnd(director, 1500);
                Assert.IsTrue(director.LevelDone, "level " + level);
                Assert.GreaterOrEqual(director.Points, FishingDirector.PointsToPass(level));
                var spawned = new List<SwimmingFish>();
                RunFor(director, 20f, spawned);
                Assert.AreEqual(0, spawned.Count);
                Assert.IsFalse(director.Cast(0f, -200f));
            }
        }

        [Test]
        public void FishAndHookStillThereAtTheEndOfALevelGoOnIntoTheNextOne()
        {
            var first = new FishingDirector(1, new System.Random(9));
            first.Cast(200f, -200f);
            first.Tick(0.1f, null, null, null, null);
            var carried = first.Fish.ToList();
            var second = new FishingDirector(2, new System.Random(10), carried, first.Hook);
            Assert.AreEqual(carried.Count, second.Fish.Count);
            Assert.AreSame(first.Hook, second.Hook);
            Assert.AreEqual(0, second.Points, "the new level counts its own points");
            var before = carried[0].X;
            second.Tick(0.1f, null, null, null, null);
            Assert.AreNotEqual(before, carried[0].X, "carried fish keep swimming");
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, FishingDirector.SessionCoins);
        }

        [Test]
        public void TheFishBoatBackdropAndThePromptExist()
        {
            foreach (var name in new[] { "a", "b", "c", "d", "e", "f" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/fishing/fish_" + name), "fish_" + name);
            Assert.IsNotNull(Resources.Load<Sprite>("Art/fishing/boat"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/fishing/bg"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_hook"));
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Catch the fish!", lines["fishing_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/fishing_find"));
        }

        [Test]
        public void TheBoatLayoutKeepsTheTwoInTheTopRightApartAndInsideTheFrame()
        {
            var layout = CompanionLayout.Boat;
            Assert.That(layout.PlayerX, Is.LessThan(layout.EvaX));
            Assert.AreEqual(0f, layout.PlayerFootprint.OverlapWith(layout.EvaFootprint));
            Assert.That(layout.Footprint.XMax, Is.LessThanOrEqualTo(720f));
            Assert.That(layout.Footprint.YMax, Is.LessThanOrEqualTo(450f));
            Assert.That(layout.Footprint.YMin, Is.GreaterThan(FishingDirector.SurfaceY - 100f), "they sit at the surface, not under it");
        }

        [Test]
        public void TheScreenHasAFullScreenTouchPadAHookAndALineAndTheBoatIsInFrontOfTheCharacters()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.Fishing);
                var screen = canvasObject.transform.Find("ScreenRoot/FishingScreen");
                var field = screen.Find("Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Hook"));
                Assert.IsNotNull(field.Find("Line"));
                Assert.GreaterOrEqual(field.childCount, 3 + FishingDirector.Lanes * FishingDirector.LaneCap(FishingDirector.MaxLevel));
                Assert.Greater(screen.Find("Boat").GetSiblingIndex(), screen.Find("CompanionPair").GetSiblingIndex(), "the boat is drawn over their legs");
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
