using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    public struct TimePoint
    {
        public uint ticks;
    }
    
    public delegate void TimerCallback(object userData); 
    
    public class Timer : IDisposable
    {
        private readonly TimerManager m_Owner;
        private readonly TimerCallback m_Callback;
        private readonly object m_UserData;
        private int m_Handle;
        
        internal Timer([NotNull] TimerCallback callback, object userData)
            : this(Context.instance.timerManager, callback, userData)
        { }
        
        internal Timer([NotNull] TimerManager manager, TimerCallback callback, object userData)
        {
            m_Owner = manager;
            m_Callback = callback;
            m_UserData = userData;
            m_Handle = -1;
        }
        
        public void Dispose()
        {
            Cancel();
            
            // TODO Wait for concurrent callback
        }
        
        public void ExpireAt(TimePoint expireTimeTicks, TimeSpan duration)
        {
            if (duration.Ticks < 0)
                throw new ArgumentOutOfRangeException($"{nameof(duration)} must be zero or positive");
            
            m_Owner.AddTimer(this);
        }

        public void ExpireFromNow(TimeSpan duration)
        {
            ExpireAt(TimerManager.currentTime, duration);
        }

        public void Cancel()
        {
            ExpireAt(new TimePoint(){ ticks = 0 }, TimeSpan.FromTicks(0));
        }

        internal void Invoke()
        {
            m_Callback(m_UserData);
        }
    }

    internal class TimerManager : IDisposable
    {
        // Note: C# doesn't have a priority queue implementation until .NET 6
        // See: https://github.com/dotnet/runtime/commit/826aa4f7844fd3d48784025ec6d47010867baab4
        //private PriorityQueue<Timer> m_PriorityQueue;
        
        internal struct TimerContext
        {
            
        }

        private List<TimerContext> m_Timers;
        
        public void Dispose()
        {
        }

        public static TimePoint currentTime => new TimePoint();

        public void AddTimer(Timer timer)
        {
            
        }

        public void RemoveTimer(Timer timer)
        {
            
        }

        private void UpdateTimer(Timer timer)
        {
            
        }
    }
}