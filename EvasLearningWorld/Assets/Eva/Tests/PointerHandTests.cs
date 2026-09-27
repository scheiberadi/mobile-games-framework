using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // PointerHand.EaseInOut is the one pure, easily-unit-tested piece of the hand's behaviour (everything
    // else is coroutine choreography, covered by the Count screen's own behaviour tests instead).
    public class PointerHandTests
    {
        [Test]
        public void EaseInOutMapsTheEndpointsAndTheMidpoint()
        {
            Assert.That(PointerHand.EaseInOut(0f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(PointerHand.EaseInOut(1f), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(PointerHand.EaseInOut(0.5f), Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void EaseInOutIsMonotoneIncreasing()
        {
            var previous = PointerHand.EaseInOut(0f);
            for (var i = 1; i <= 20; i++)
            {
                var t = i / 20f;
                var value = PointerHand.EaseInOut(t);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous), "t=" + t);
                previous = value;
            }
        }

        [Test]
        public void EaseInOutClampsInputOutsideZeroToOne()
        {
            Assert.That(PointerHand.EaseInOut(-0.5f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(PointerHand.EaseInOut(1.5f), Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void HandPositionPutsFingertipOnTargetCentre()
        {
            var target = new Vector2(120f, -75f);
            var handPosition = PointerHand.HandPositionFor(target);
            Assert.That(handPosition + PointerHand.FingertipOffset, Is.EqualTo(target));
        }
    }
}
