using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class TimerTests
    {
        [Test]
        public void Timer_ShouldExpire_IfConfiguredToExpireOnce()
        {
            var timestamps = new List<TimePoint>();
            var mgr = new TimerManager();
            using var t = new Timer(mgr, (userData) => timestamps.Add(MonotonicClock.now), timestamps);
            t.ExpireFromNow(dueTime: TimeSpan.FromMilliseconds(1), Timer.IndefinatePeriod);
            
            Assert.That(mgr.PollOne(), Is.EqualTo(0));
            Assert.That(timestamps.Count, Is.EqualTo(0));

            MonotonicClock.now = TimePoint.FromMicroseconds(900);
            Assert.That(mgr.PollOne(), Is.EqualTo(0));
            Assert.That(timestamps.Count, Is.EqualTo(0));
            
            MonotonicClock.now = TimePoint.FromMicroseconds(1000);
            Assert.That(mgr.PollOne(), Is.EqualTo(1));
            Assert.That(timestamps.Count, Is.EqualTo(1));
        }
    }
}