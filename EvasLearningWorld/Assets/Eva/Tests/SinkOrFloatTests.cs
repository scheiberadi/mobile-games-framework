using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Sink or Float's rules: the water surface, the objects in it and the shelf round (the screen itself is in SinkOrFloatScreenTests).
    public class SinkOrFloatTests
    {
        private const float Step = 1f / 60f;
        private const float FloorY = -210f;
        private const float HalfWidth = 315f;

        private static void Run(WaterSurface water, IList<BuoyantBody> bodies, float seconds)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                water.Advance(Step);
                foreach (var body in bodies) body.Step(Step, water, 0f, FloorY, HalfWidth);
                BuoyantBody.Separate(bodies, HalfWidth);
            }
        }

        // --- Water surface ---

        [Test]
        public void StillWaterOnlyMovesWithTheAmbientSwell()
        {
            var water = new WaterSurface(64, 630f);
            water.Advance(5f);
            for (var x = -300f; x <= 300f; x += 25f)
                Assert.That(Math.Abs(water.HeightAt(x)), Is.LessThanOrEqualTo(WaterSurface.AmbientAmplitude + 0.01f), "x " + x);
            Assert.That(water.RippleEnergy, Is.EqualTo(0f));
        }

        [Test]
        public void AnObjectDroppedInSendsRipplesToBothWallsAndTheyFadeOut()
        {
            var water = new WaterSurface(64, 630f);
            water.Disturb(0f, -400f);
            var reachedEdge = false;
            var peak = 0f;
            for (var t = 0f; t < 1.5f; t += Step)
            {
                water.Advance(Step);
                peak = Math.Max(peak, Math.Abs(water.Ripple(0)));
                if (Math.Abs(water.Ripple(0)) > 1f && Math.Abs(water.Ripple(water.Columns - 1)) > 1f) reachedEdge = true;
            }
            Assert.That(reachedEdge, Is.True, "the ripple reaches both glass walls within a second and a half");
            Assert.That(peak, Is.LessThan(120f), "and stays a ripple, not a wave that breaks out of the tank");
            for (var t = 0f; t < 10f; t += Step) water.Advance(Step);
            Assert.That(water.RippleEnergy, Is.LessThan(5f), "ten seconds later the water has calmed");
        }

        [Test]
        public void TheRipplesNeverBlowUpEvenAfterManyPushes()
        {
            var water = new WaterSurface(64, 630f);
            for (var i = 0; i < 40; i++)
            {
                water.Disturb(-250f + i * 12f, i % 2 == 0 ? -900f : 700f);
                water.Advance(0.05f);
            }
            for (var i = 0; i < water.Columns; i++) Assert.That(Math.Abs(water.Ripple(i)), Is.LessThan(400f), "column " + i);
        }

        [Test]
        public void HeightIsSmoothBetweenColumnsAndClampedAtTheWalls()
        {
            var water = new WaterSurface(64, 630f);
            water.Disturb(0f, -300f);
            water.Advance(0.2f);
            var a = water.HeightAt(0f);
            var b = water.HeightAt(2f);
            Assert.That(Math.Abs(a - b), Is.LessThan(8f));
            Assert.That(water.HeightAt(-5000f), Is.EqualTo(water.HeightAt(-315f)).Within(WaterSurface.AmbientAmplitude * 2f));
        }

        // --- Objects in the water ---

        [Test]
        public void AFloaterDroppedFromAboveEndsRidingTheSurface()
        {
            var water = new WaterSurface(64, 630f);
            var body = new BuoyantBody(true, 150f, 0.4f, 0f, 0f, 0f, 220f, 1);
            var entered = 0;
            for (var t = 0f; t < 6f; t += Step)
            {
                water.Advance(Step);
                if (body.Step(Step, water, 0f, FloorY, HalfWidth).EnteredWater) entered++;
            }
            Assert.That(entered, Is.EqualTo(1), "it touches the water once");
            Assert.That(body.Settled, Is.True);
            var rest = water.HeightAt(body.X) + 150f * (0.5f - 0.4f);
            Assert.That(body.Y, Is.EqualTo(rest).Within(8f), "its centre rides just above the surface, the rest of it under");
        }

        [Test]
        public void AFloaterLetGoUnderwaterRisesToTheSurface()
        {
            var water = new WaterSurface(64, 630f);
            var body = new BuoyantBody(true, 150f, 0.5f, 0f, 0f, 0f, -150f, 2);
            Run(water, new List<BuoyantBody> { body }, 6f);
            Assert.That(body.Y, Is.GreaterThan(-20f), "it did not stay down");
            Assert.That(body.Settled, Is.True);
        }

        [Test]
        public void ASinkerFallsToTheFloorOnceAndLiesThere()
        {
            var water = new WaterSurface(64, 630f);
            var body = new BuoyantBody(false, 150f, 0f, 380f, 8f, 20f, 200f, 3);
            var floorHits = 0;
            for (var t = 0f; t < 6f; t += Step)
            {
                water.Advance(Step);
                if (body.Step(Step, water, 0f, FloorY, HalfWidth).HitFloor) floorHits++;
            }
            Assert.That(floorHits, Is.EqualTo(1));
            Assert.That(body.Settled, Is.True);
            Assert.That(body.Y, Is.EqualTo(FloorY + 150f * 0.42f).Within(0.5f));
            Assert.That(body.VY, Is.EqualTo(0f));
        }

        [Test]
        public void AHeavySinkerReachesTheFloorBeforeALightOne()
        {
            var water = new WaterSurface(64, 630f);
            var hammer = new BuoyantBody(false, 150f, 0f, 430f, 8f, -100f, 120f, 4);
            var coin = new BuoyantBody(false, 150f, 0f, 200f, 75f, 100f, 120f, 5);
            var hammerSettledAt = -1f;
            var coinSettledAt = -1f;
            for (var t = 0f; t < 8f; t += Step)
            {
                water.Advance(Step);
                hammer.Step(Step, water, 0f, FloorY, HalfWidth);
                coin.Step(Step, water, 0f, FloorY, HalfWidth);
                if (hammerSettledAt < 0f && hammer.Settled) hammerSettledAt = t;
                if (coinSettledAt < 0f && coin.Settled) coinSettledAt = t;
            }
            Assert.That(hammerSettledAt, Is.GreaterThan(0f));
            Assert.That(coinSettledAt, Is.GreaterThan(hammerSettledAt), "the coin flutters down slowly");
        }

        [Test]
        public void ASinkerDoesNotLeaveTheTankSideways()
        {
            var water = new WaterSurface(64, 630f);
            var body = new BuoyantBody(false, 150f, 0f, 200f, 75f, 310f, 100f, 6);
            Run(water, new List<BuoyantBody> { body }, 6f);
            Assert.That(Math.Abs(body.X), Is.LessThanOrEqualTo(HalfWidth));
        }

        [Test]
        public void FloatersDroppedOnTheSameSpotSlideApartAndStayInTheTank()
        {
            var water = new WaterSurface(64, 630f);
            var bodies = new List<BuoyantBody>();
            for (var i = 0; i < 3; i++) bodies.Add(new BuoyantBody(true, 150f, 0.4f, 0f, 0f, 0f, 150f + i * 5f, i));
            Run(water, bodies, 8f);
            for (var i = 0; i < bodies.Count; i++)
            {
                Assert.That(Math.Abs(bodies[i].X), Is.LessThanOrEqualTo(HalfWidth));
                for (var j = i + 1; j < bodies.Count; j++)
                    Assert.That(Math.Abs(bodies[i].X - bodies[j].X), Is.GreaterThan(150f * 0.8f * 0.85f), "floaters " + i + " and " + j);
            }
        }

        [Test]
        public void AFloaterTiltsAndRidesARipple()
        {
            var water = new WaterSurface(64, 630f);
            var body = new BuoyantBody(true, 150f, 0.4f, 0f, 0f, 0f, 150f, 7);
            Run(water, new List<BuoyantBody> { body }, 5f);
            water.Disturb(120f, -500f, 40f);
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            var maxTilt = 0f;
            for (var t = 0f; t < 2f; t += Step)
            {
                water.Advance(Step);
                body.Step(Step, water, 0f, FloorY, HalfWidth);
                minY = Math.Min(minY, body.Y);
                maxY = Math.Max(maxY, body.Y);
                maxTilt = Math.Max(maxTilt, Math.Abs(body.Angle));
            }
            Assert.That(maxY - minY, Is.GreaterThan(3f), "the ripple lifts and drops it");
            Assert.That(maxTilt, Is.GreaterThan(1f), "and tips it");
        }

        // --- The shelf ---

        [Test]
        public void ThereAreSixThingsThatFloatAndSixThatSink()
        {
            Assert.That(SinkOrFloat.Items.Count, Is.EqualTo(12));
            Assert.That(SinkOrFloat.Items.Count(i => i.Floats), Is.EqualTo(6));
            Assert.That(SinkOrFloat.Items.Select(i => i.Id).Distinct().Count(), Is.EqualTo(12));
            foreach (var item in SinkOrFloat.Items.Where(i => i.Floats)) Assert.That(item.Submerge, Is.InRange(0.05f, 0.6f), item.Id);
            foreach (var item in SinkOrFloat.Items.Where(i => !i.Floats)) Assert.That(item.SinkSpeed, Is.InRange(150f, 500f), item.Id);
        }

        [Test]
        public void ASessionShowsEveryThingOnceThreeAndThreeToARound()
        {
            for (var seed = 0; seed < 30; seed++)
            {
                var session = SinkOrFloat.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(SinkOrFloat.RoundsPerSession));
                foreach (var round in session)
                {
                    Assert.That(round.Length, Is.EqualTo(SinkOrFloat.ObjectsPerRound));
                    Assert.That(round.Count(id => SinkOrFloat.Find(id).Floats), Is.EqualTo(SinkOrFloat.ObjectsPerRound / 2));
                }
                Assert.That(session.SelectMany(r => r).Distinct().Count(), Is.EqualTo(12), "every thing once, none twice (seed " + seed + ")");
            }
        }

        [Test]
        public void TheShelvesAreShuffledNotSortedByKind()
        {
            var orders = new HashSet<string>();
            for (var seed = 0; seed < 20; seed++) orders.Add(string.Join(",", SinkOrFloat.CreateSession(new System.Random(seed))[0]));
            Assert.That(orders.Count, Is.GreaterThan(10));
        }

        [Test]
        public void EveryThingHasItsPictureAndItsVoiceLine()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var item in SinkOrFloat.Items)
            {
                Assert.That(Resources.Load<Sprite>("Art/sciencelab/object_" + item.Id), Is.Not.Null, "picture " + item.Id);
                var key = "sinkorfloat_say_" + item.Id;
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
                Assert.That(lines[key], Does.Contain(item.Floats ? "floats" : "sinks"), key + " says what it does");
            }
            foreach (var key in new[] { "sinkorfloat_prompt", "sinkorfloat_hint" })
            {
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
            }
        }
    }
}
