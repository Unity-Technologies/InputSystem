using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal class Multiplexer<T> : IInputBindingSource<T>, ISourceObserver<T>
        where T : struct, IComparable<T>
    {
        private readonly InputBindingSource<T> m_First;
        private readonly InputBindingSource<T> m_Second;
        private ObserverList<T> m_ObserverList;
        private Receiver m_FirstObserver;
        private Receiver m_SecondObserver;
        private IDisposable m_FirstSubscription;
        private IDisposable m_SecondSubscription;
        private T m_Value;

        public Multiplexer(InputBindingSource<T> first, InputBindingSource<T> second)
        {
            m_First = first;
            m_Second = second;
            m_FirstObserver = null;
            m_SecondObserver = null;
            m_ObserverList = new ObserverList<T>();
            m_FirstSubscription = null;
            m_SecondSubscription = null;
            m_Value = default;
        }

        private void Unsubscribe()
        {
            m_FirstSubscription.Dispose();
            m_FirstSubscription = null;

            m_SecondSubscription.Dispose();
            m_SecondSubscription = null;
        }

        private void UpdateSubscription(Context context)
        {
            if (m_FirstSubscription != null)
                return;

            // Note that we need separated observers here to distinguish source
            // TODO This is state, consider moving it all out, not needed until subscribed, should not be in proxy
            m_ObserverList = new ObserverList<T>(Unsubscribe);
            m_FirstObserver = new Receiver(this, 0);
            m_FirstSubscription = m_First.Subscribe(context, m_FirstObserver);
            m_SecondObserver = new Receiver(this, 1);
            m_SecondSubscription = m_Second.Subscribe(context, m_SecondObserver);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            UpdateSubscription(Context.instance);
            return m_ObserverList.Add(observer);
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            UpdateSubscription(context);
            return m_ObserverList.Add(observer);
        }

        // TODO This would require less work if operating on stream directly since having access to source streams?!
        private class Receiver : IObserver<T>
        {
            private readonly Multiplexer<T> m_Multiplexer;
            private readonly int m_Index;

            public Receiver(Multiplexer<T> multiplexer, int index)
            {
                m_Multiplexer = multiplexer;
                m_Index = index;
            }

            public void OnCompleted() => m_Multiplexer.OnCompleted(m_Index);
            public void OnError(Exception error) => m_Multiplexer.OnError(m_Index, error);
            public void OnNext(T value) => m_Multiplexer.OnNext(m_Index, value);
        }

        public void OnCompleted(int source)
        {
            throw new NotImplementedException();
        }

        public void OnError(int source, Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(int source, T value)
        {
            // TODO Implement other logic or abstract out

            var result = value.CompareTo(value);
            if (result > 0)
                m_Value = value;

            m_ObserverList.OnNext(m_Value);
        }
    }
}
