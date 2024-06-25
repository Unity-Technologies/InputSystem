using System;

namespace UnityEngine.InputSystem.Experimental
{
    interface IForwardReceiver<in T> : IObserver<T>
        where T : struct
    {
        void ForwardOnCompleted();
        void ForwardOnError(Exception error);
        void ForwardOnNext(T value);
    }
    
    internal class ForwardingObserver<T, TReceiver> : IObserver<T> 
        where TReceiver : IForwardReceiver<T>
        where T : struct
    {
        private readonly TReceiver m_Receiver;
        
        public ForwardingObserver(TReceiver receiver)
        {
            m_Receiver = receiver;
        }
        
        public void OnCompleted()
        {
            m_Receiver.ForwardOnCompleted();
        }

        public void OnError(Exception error)
        {
            m_Receiver.ForwardOnError(error);
        }

        public void OnNext(T value)
        {
            m_Receiver.ForwardOnNext(value);
        }
    }
    
    interface IIndexedForwardReceiver<in T> : IObserver<T>
        where T : struct
    {
        void ForwardOnCompleted(int index);
        void ForwardOnError(int index, Exception error);
        void ForwardOnNext(int index, T value);
    }
    
    internal class IndexedForwardingObserver<T, TReceiver> : IObserver<T> 
        where TReceiver : IIndexedForwardReceiver<T>
        where T : struct
    {
        private readonly TReceiver m_Receiver;
        private readonly int m_Index;
        
        public IndexedForwardingObserver(int index, TReceiver receiver)
        {
            m_Receiver = receiver;
            m_Index = index;
        }
        
        public void OnCompleted()
        {
            m_Receiver.ForwardOnCompleted(m_Index);
        }

        public void OnError(Exception error)
        {
            m_Receiver.ForwardOnError(m_Index, error);
        }

        public void OnNext(T value)
        {
            m_Receiver.ForwardOnNext(m_Index, value);
        }
    }
        
}