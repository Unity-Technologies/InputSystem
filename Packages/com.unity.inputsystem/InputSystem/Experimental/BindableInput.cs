using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // Generated from native C enums. May be huge but compiler should strip any unused usages in an optimized build?

    /*public interface IStreamProcessor
    {
        public void 
    }*/

    

    public struct Button
    {
        public readonly int bitIndex;
    }
    
    namespace Device
    {
    }

    public class PressInputBindingSource : IObserver<Button>, IObservable<InputEvent>
    {
        private List<IObserver<InputEvent>> m_Observers;
        private bool m_Actuated;
         
        // TODO We could let context be responsible and use a subscription id to avoid instances
        private class Subscription : IDisposable
        {
            private readonly PressInputBindingSource m_Owner;
            private readonly IObserver<InputEvent> m_Observer;
                
            public Subscription(PressInputBindingSource owner, IObserver<InputEvent> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }
                
            public void Dispose()
            {
                m_Owner.m_Observers.Remove(m_Observer);
            }
        }
        
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(Button value)
        {
            
        }

        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
        {
            m_Observers ??= new List<IObserver<InputEvent>>(1);
            m_Observers.Add(observer);
            return new Subscription(this, observer);
        }
            
        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public InputBindingSource<InputEvent> GetBindingSource()
        {
            return new InputBindingSource<InputEvent>();
        }
    }

    // TODO Implement a stream writer that is an ISequenceObservable
    


    /*public struct InputBindingSource
    {
        private Stream<bool> m_Stream;
        private Stream<bool> stream => m_Stream;
    }*/

    public struct StreamReader<T> : IEnumerable<T> where T : struct
    {
        private readonly Stream<T> m_Stream;
        private readonly int m_Offset;
        
        public StreamReader(Stream<T> stream, int offset)
        {
            m_Stream = stream;
            m_Offset = offset;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly StreamReader<T> m_Reader;
            private int m_Index;
            
            public Enumerator(StreamReader<T> reader, int index = 0)
            {
                m_Reader = reader;
                m_Index = index;
                Current = default;
            }

            public void Dispose()
            {
                // TODO release managed resources here
            }

            public bool MoveNext()
            {
                ++m_Index;
                return false;
            }

            public void Reset()
            {
                m_Index = 0;
            }

            public T Current { get; }

            object IEnumerator.Current => Current;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }



    public struct InputEvent
    {
        
    }

    public struct InputEvent<T> where T : struct
    {
        
    }

    public class BindableOutput<T> : IObserver<T>, IDisposable where T : struct
    {
        public BindableOutput(OutputBindingTarget<T> binding)
        {
            
        }
        
        public void Dispose()
        {
            
        }

        public void Offer(T item)
        {
            // TODO Apply to output bindings
        }
        
        public void Offer(ref T item)
        {
            // TODO Apply to output bindings
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BindableInput<T> : IObserver<T>, IDisposable where T : struct
    {
        public delegate void Callback(T value);
        public event Callback performed;

        private Context m_Context;
        private InputBindingSource<T>[] m_Bindings;
        private int m_BindingCount;
        private IDisposable[] m_Subscriptions;
        private int m_SubscriptionCount;

        public static BindableInput<T> Create(Callback callback, IInputBindingSource<T> source = null, Context context = null)
        {
            return new BindableInput<T>(callback, source, context);
        }
        
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            performed?.Invoke(value);
        }
        
        public BindableInput(Callback callback, IInputBindingSource<T> source = null, Context context = null)
        {
            m_Context = context;
            Bind(source);
            
            performed += callback; // TODO We could dispatch this down to avoid multiple levels of indirection?
        }

        public BindableInput(Callback callback, InputBindingSource<T> binding, Context context = null)
        {
            m_Context = context;
            Bind(binding);

            performed += callback;
        }
        
        public BindableInput(Callback callback, IObservable<T> binding, Context context = null)
        {
            m_Context = context;
            Bind(binding);

            performed += callback;
        }
        
        public void Bind(InputBindingSource<T> source)
        {
            //m_Context.GetOrCreateStreamContext<T>(source.usage).OnStreamDataReceived += OnOnStreamDataReceived;
            ArrayHelpers.AppendWithCapacity(ref m_Bindings, ref m_BindingCount, source);
        }

        private void OnOnStreamDataReceived(IStream stream)
        {
            var typedStream = (Stream<T>)stream;
            var span = typedStream.AsSpan();
            for (var i = 0; i < span.Length; ++i)
            {
                OnNext(span[i]);
            }
        }

        public void Bind(IObservable<T> observable)
        {
            var subscription = observable.Subscribe(this);
            ArrayHelpers.AppendWithCapacity(ref m_Subscriptions, ref m_SubscriptionCount, subscription);
        }

        public void Dispose()
        {
            for (var i = 0; i < m_SubscriptionCount; ++i)
            {
                m_Subscriptions[i].Dispose();
                m_Subscriptions[i] = null;
            }
            m_SubscriptionCount = 0;
        }
    }
}