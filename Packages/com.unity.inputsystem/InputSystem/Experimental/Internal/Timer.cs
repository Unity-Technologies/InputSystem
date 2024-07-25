using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    public readonly struct MonotonicClock
    {
        private static uint _ticks;
        private static uint _autoTick;
        
        public static TimePoint now
        {
            get => new (_ticks += _autoTick);
            internal set => _ticks = value.ticks;
        }
        
        public static uint autoTick
        {
            get => _autoTick;
            set => _autoTick = value;
        }
    }
    
    public readonly struct TimePoint
    {
        public TimePoint(uint ticks)
        {
            this.ticks = ticks;
        }

        public static TimePoint FromTimeSpan(TimeSpan timeSpan)
        {
            var value = timeSpan.TotalMilliseconds * 1000.0;
            return new TimePoint((uint)(value));
        }

        public static TimePoint FromMicroseconds(uint microseconds)
        {
            return new TimePoint(microseconds);
        }

        public uint ticks { get; }

        public static TimePoint operator +(TimePoint a, TimePoint b)
        {
            return new TimePoint(a.ticks + b.ticks);
        }
        
        public static TimePoint operator -(TimePoint a, TimePoint b)
        {
            return new TimePoint(a.ticks - b.ticks);
        }
        
        public static TimePoint operator +(TimePoint a, TimeSpan b)
        {
            return new TimePoint(a.ticks + ToTicks(b));
        }
        
        public static TimePoint operator -(TimePoint a, TimeSpan b)
        {
            return new TimePoint(a.ticks + ToTicks(b));
        }

        private static uint ToTicks(TimeSpan timeSpan)
        {
            return (uint)(timeSpan.TotalSeconds * 1_000_000.0);
        }

        public override string ToString()
        {
            return ticks + " microseconds";
        }
    }
    
    public delegate void TimerCallback(Timer timer); 
    
    public sealed class Timer : IDisposable
    {
        public static readonly TimeSpan IndefinatePeriod = TimeSpan.MaxValue;
        
        private readonly TimerManager m_Owner;
        private readonly TimerCallback m_Callback;
        private readonly object m_UserData;
        private bool m_Scheduled;

        internal Timer([NotNull] TimerCallback callback, object userData = null)
            : this(Context.instance.timerManager, callback, userData)
        { }
        
        internal Timer([NotNull] TimerManager manager, [NotNull] TimerCallback callback, object userData = null)
        {
            m_Owner = manager;
            m_Callback = callback;
            m_UserData = userData;
            m_Scheduled = false;
            expireTime = 0;
        }
        
        public void Dispose()
        {
            Cancel();
            
            // TODO Wait for concurrent callback
        }

        internal uint expireTime { get; set; }

        internal uint period { get; private set; }

        internal bool isScheduled => m_Scheduled;

        /// <summary>
        /// Schedules the timer to expire once at an absolute time given by <paramref name="expireTime"/>.
        /// </summary>
        /// <param name="expireTime">The absolute time at which the timer should expire.</param>
        /// <param name="period">Optional periodic expiry interval. If set to TimeSpan.Zero the timer will
        /// only expire once.,</param>
        /// <exception cref="ArgumentOutOfRangeException">If period is negative.</exception>
        public void ExpireAt(TimePoint expireTime, TimeSpan period)
        {
            if (period.Ticks < 0)
                throw new ArgumentOutOfRangeException($"{nameof(period)} must be zero or positive");
            // TODO Verify that period <= int.MaxValue / 2
            
            this.expireTime = expireTime.ticks;
            this.period = (uint)period.Ticks;
            
            if (!m_Scheduled)
                m_Owner.RemoveTimer(this);
            m_Owner.AddTimer(this);
            m_Scheduled = true;
        }

        /// <summary>
        /// Schedules the timer to expire once at an absolute time relative to the current time
        /// and then periodically according to the given periodicity with relation to the initial <paramref name="dueTime"/>.
        /// </summary>
        /// <param name="dueTime">The delay before the timer should first expire.</param>
        /// <param name="period">Optional periodic expiry interval. If set to TimeSpan.Zero the timer will only
        /// expire once.</param>
        public void ExpireFromNow(TimeSpan dueTime, TimeSpan period)
        {
            ExpireAt(MonotonicClock.now + dueTime, period);
        }

        /// <summary>
        /// Schedules the timer to expire once at an absolute time relative to the current time.
        /// </summary>
        /// <param name="dueTime">The delay before the timer should expire.</param>
        public void ExpireFromNow(TimeSpan dueTime)
        {
            ExpireAt(MonotonicClock.now + dueTime, TimeSpan.Zero);
        }

        /// <summary>
        /// Cancels the timer.
        /// </summary>
        public void Cancel()
        {
            if (!m_Scheduled) 
                return;
            
            m_Owner.RemoveTimer(this);
            m_Scheduled = false;
        }

        public bool isPeriodic => period != TimeSpan.MaxValue.Ticks && period != 0;
        
        internal void Invoke()
        {
            m_Callback(this);
        }
    }

    internal sealed class TimerManager : IDisposable
    {
        // Note: C# doesn't have a priority queue implementation until .NET 6
        // See: https://github.com/dotnet/runtime/commit/826aa4f7844fd3d48784025ec6d47010867baab4
        private readonly PriorityQueue<Timer, Timer> m_PriorityQueue;

        private class Comparer : IComparer<Timer>
        {
            public uint ReferenceTime;

            public int Compare( Timer x, Timer y)
            {
                var dx = x!.expireTime - ReferenceTime;
                var dy = y!.expireTime - ReferenceTime;
                return (int)(dy - dx);
            }
        }

        private readonly Comparer m_Comparer;

        public TimerManager()
        {
            m_Comparer = new Comparer();
            m_PriorityQueue = new PriorityQueue<Timer, Timer>(m_Comparer);
        }
        
        public void Dispose()
        {
            m_PriorityQueue.Clear();
        }

        internal void AddTimer(Timer timer)
        {
            m_PriorityQueue.Enqueue(timer, timer);
        }

        internal void RemoveTimer(Timer timer)
        {
            if (m_PriorityQueue.Count == 0) 
                return;
            
            for (;;)
            {
                var t = m_PriorityQueue.Dequeue();
                if (t == timer)
                    break;
                    
                // Reinsert timer if not timer of interest
                m_PriorityQueue.Enqueue(t, t);
            }
        }
        
        public int PollOne()
        {
            if (m_PriorityQueue.Count == 0)
                return 0;

            var t = m_PriorityQueue.Peek();
            var now = MonotonicClock.now;
            if (t.expireTime > now.ticks)
                return 0;

            m_PriorityQueue.Dequeue();
            
            // Only reschedule if timer is periodic
            if (t.isPeriodic)
            {
                t.expireTime += t.period; // TODO Incorrect, should align with now
                m_PriorityQueue.Enqueue(t, t);
            }
            
            t.Invoke();

            return 1;
        }

        public void Poll()
        {
            
        }
    }
}