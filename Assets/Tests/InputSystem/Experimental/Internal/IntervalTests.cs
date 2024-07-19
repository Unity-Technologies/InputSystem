using Mono.Collections.Generic;
using NUnit.Framework;
using UnityEngine.InputSystem.Utilities;

namespace Tests.InputSystem.Experimental
{
    // Key properties:
    // - The interval set contains no overlapping intervals.

    [Category("Experimental")]
    internal sealed class IntervalTests
    {
        [Test]
        [TestCase(0, 0, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, true)]
        [TestCase(-1, -1, true)]
        [TestCase(int.MinValue, int.MaxValue, false)]
        [TestCase(int.MaxValue, int.MinValue, true)]
        public void isEmpty_ShouldReturnTrue_OnlyIfIntervalHasPositiveLength(int lower, int upper, bool expected)
        {
            Assert.That(new Interval(lower, upper).isEmpty, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(-1, -1, 0)]
        //[TestCase(int.MinValue, int.MaxValue, 0)]
        //[TestCase(int.MaxValue, int.MinValue, 0)]
        public void length_ShouldReturnRange(int lower, int upper, int expected)
        {
            Assert.That(new Interval(lower, upper).length, Is.EqualTo(expected));
        }

        [TestCase(0, 0, 0, false)]
        [TestCase(0, 1, 0, true)]
        [TestCase(0, 1, 1, false)]
        [TestCase(1, 1, 1, false)]
        [TestCase(-1, -1, -1, false)]
        [TestCase(-1, -1, 0, false)]
        public void contains_ShouldReturnTrue_IfIntervalContainsPoint(int lower, int upper, int p, bool expected)
        {
            Assert.That(new Interval(lower, upper).Contains(p), Is.EqualTo(expected));
        }
    }
}