using System;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // Multiplexes samples as sub-sample data in an ordered sequence
    /*internal class Composite<TIn, TOut> : IInputBindingSource<TOut>, ISourceObserver<TIn>
        where TIn : struct
        where TOut : struct
    {
        
    }*/

    internal interface IUnsubscribable
    {
        public void Unsubscribe();
    }
    
    // Subscription object could be simplified if we just keep it has a struct of e.g. subscription indicies
    
    // An observer that forwards observations to a target and associates it with an associated index.
    // Note that TTarget is an interface constraint type that allows us to avoid virtual indirect calls.
    internal readonly struct IndexedObserver<T, TTarget> : IObserver<T>
        where TTarget : IMuxObserver<T> where T : struct
    {
        private readonly TTarget m_Target;
        private readonly int m_Index;

        public IndexedObserver(TTarget target, int index)
        {
            m_Target = target;
            m_Index = index;
        }

        public void OnCompleted() => m_Target.OnCompleted(m_Index);
        public void OnError(Exception error) => m_Target.OnError(m_Index, error);
        public void OnNext(T value) => m_Target.OnNext(m_Index, value);   
    }

    /*internal struct Filter<T>
    {
        private unsafe delegate*<T, bool> m_Accept;

        public Filter(delegate<)
        
        public unsafe bool Apply(T value)
        {
            return m_Accept == null || m_Accept(value);
        }
    }*/

    internal interface IOperator<T>
    {
        void Apply();
    }
    
    internal class Multiplexer2<T, TSource, TOperation> : ObserverList2<T>, IObservableInput<T>
        where T : struct
        where TSource : IObservableInput<T>
        where TOperation : IOperator<T>
    {
        public Multiplexer2(TSource source1, TSource source2)
        {
                   
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
    
    // TODO Mux do not need 2 indexed observers for uniform type !
    internal struct Mux<T, TSource, TTarget> : IObservableInput<T>, IObserver<T>, IMuxObserver<T> 
        where T : struct
        where TSource : IObservableInput<T>
        where TTarget : IMuxObserver<T>
    {
        private ObserverList<T> m_ObserverList;
        private bool m_Completed1, m_Completed2;
        private readonly TSource m_Source1, m_Source2;
        //private readonly Target m_Target;
        private readonly IndexedObserver<T, TTarget> m_Observer1, m_Observer2;
        
        public Mux(TTarget target, TSource source1, TSource source2)
        {
            m_Source1 = source1;
            m_Source2 = source2;
            m_Observer1 = new IndexedObserver<T, TTarget>(target, 0);
            m_Observer2 = new IndexedObserver<T, TTarget>(target, 1);
            m_ObserverList = new ObserverList<T>();
            m_Completed1 = false;
            m_Completed2 = false;
        }

        private void Subscribe(Context context)
        {
            // Subscribe on dependencies
            var subscription1 = m_Source1.Subscribe(context, m_Observer1); // TODO Eliminate boxing since this TSource can be a concrete type here, we can forward concerete receiver implementing IObservable§<T> to allow for reduction
            var subscription2 = m_Source2.Subscribe(context, m_Observer2); // TODO Eliminate boxing
            
            // TODO When we actually subscribe we need to construct the actual work as part of a class to allow it to be observable
            // Provide shared disposable to observer list to unsubscribe when all subscribers have been removed
            m_ObserverList = new ObserverList<T>(() => // TODO Get rid of allocation (Action)
            {
                subscription1.Dispose();
                subscription2.Dispose();
            });
        }
        
        // TODO All of the below is just extensions upon observer list, only difference is underlying subscribe.
        //      If we would delegate this may we do with a derivate of ObserverList instead?
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            if (m_ObserverList.empty)
                Subscribe(context);
            return m_ObserverList.Add(observer);
        }
        
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted()
        {
            m_ObserverList.OnCompleted();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnError(Exception error)
        {
            m_ObserverList.OnError(error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public void OnNext(T value)
        {
            m_ObserverList.OnNext(value);
        }

        private void Complete()
        {
            m_ObserverList.OnCompleted();
            
            m_Completed1 = false;
            m_Completed2 = false;
        }
        
        public void OnCompleted(int sourceIndex)
        {
            switch (sourceIndex)
            {
                case 0: 
                    m_Completed1 = true;
                    if (m_Completed2)
                        Complete();            
                    break;
                case 1:
                    m_Completed2 = true;
                    if (m_Completed1)
                        Complete();
                    break;
                default:
                    break;
            }
        }

        public void OnError(int sourceIndex, Exception error)
        {
            m_ObserverList.OnError(error);
        }

        public void OnNext(int sourceIndex, T value)
        {
            m_ObserverList.OnNext(value);
        }
    }

    /*internal struct NoOperation<T> : IOperator<T>
    {
        public void Apply()
        {
            
        }
    }*/

    internal class MergeOp<T> : IObservableInput<T> where T : struct
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
    
    internal class Multiplexer<T> : IObservableInput<T>, IMuxObserver<T>
        where T : struct
    {
        private Mux<T, ObservableInput<T>, Multiplexer<T>> m_Mux;
        
        public Multiplexer(ObservableInput<T> first, ObservableInput<T> second)
        {
            m_Mux = new Mux<T, ObservableInput<T>, Multiplexer<T>>(this, first, second);
        }

        public IDisposable Subscribe(IObserver<T> observer) => m_Mux.Subscribe(observer);
        public IDisposable Subscribe(Context context, IObserver<T> observer) => m_Mux.Subscribe(context, observer);

        // TODO Not great to have these here, calls for a class since implementing interface
        
        public void OnCompleted(int sourceIndex)
        {
            m_Mux.OnCompleted();
        }

        public void OnError(int sourceIndex, Exception error)
        {
            m_Mux.OnError(error);
        }

        public void OnNext(int sourceIndex, T value)
        {
            m_Mux.OnNext(value);
        }
    }

    public class MergeOperation<T, TSource>
    {
        public MergeOperation(TSource first, TSource second)
        {
            
        }
    }
    
    // Multiplexes samples in an ordered sequence
    // internal class Multiplexer<T> : IInputBindingSource<T>, ISourceObserver<T>
    //     where T : struct, IComparable<T>
    // {
    //     private readonly InputBindingSource<T> m_First;
    //     private readonly InputBindingSource<T> m_Second;
    //     private ObserverList<T> m_ObserverList;
    //     private Receiver m_FirstObserver;
    //     private Receiver m_SecondObserver;
    //     private IDisposable m_FirstSubscription; // TODO Should not really be here
    //     private IDisposable m_SecondSubscription; // TODO Should not really be here
    //     private T m_Value;
    //
    //     public Multiplexer(InputBindingSource<T> first, InputBindingSource<T> second)
    //     {
    //         m_First = first;
    //         m_Second = second;
    //         m_FirstObserver = null;
    //         m_SecondObserver = null;
    //         m_ObserverList = new ObserverList<T>();
    //         m_FirstSubscription = null;
    //         m_SecondSubscription = null;
    //         m_Value = default;
    //     }
    //
    //     private void Unsubscribe()
    //     {
    //         m_FirstSubscription.Dispose();
    //         m_FirstSubscription = null;
    //
    //         m_SecondSubscription.Dispose();
    //         m_SecondSubscription = null;
    //     }
    //
    //     private void UpdateSubscription(Context context)
    //     {
    //         if (m_FirstSubscription != null)
    //             return;
    //
    //         // Note that we need separated observers here to distinguish source
    //         // TODO This is state, consider moving it all out, not needed until subscribed, should not be in proxy
    //         m_ObserverList = new ObserverList<T>(Unsubscribe);
    //         m_FirstObserver = new Receiver(this, 0);
    //         m_FirstSubscription = m_First.Subscribe(context, m_FirstObserver);
    //         m_SecondObserver = new Receiver(this, 1);
    //         m_SecondSubscription = m_Second.Subscribe(context, m_SecondObserver);
    //     }
    //
    //     public IDisposable Subscribe(IObserver<T> observer)
    //     {
    //         UpdateSubscription(Context.instance);
    //         return m_ObserverList.Add(observer);
    //     }
    //
    //     public IDisposable Subscribe(Context context, IObserver<T> observer)
    //     {
    //         UpdateSubscription(context);
    //         return m_ObserverList.Add(observer);
    //     }
    //
    //     // TODO This would require less work if operating on stream directly since having access to source streams?!
    //     private class Receiver : IObserver<T>
    //     {
    //         private readonly Multiplexer<T> m_Multiplexer;
    //         private readonly int m_Index;
    //
    //         public Receiver(Multiplexer<T> multiplexer, int index)
    //         {
    //             m_Multiplexer = multiplexer;
    //             m_Index = index;
    //         }
    //
    //         public void OnCompleted() => m_Multiplexer.OnCompleted(m_Index);
    //         public void OnError(Exception error) => m_Multiplexer.OnError(m_Index, error);
    //         public void OnNext(T value) => m_Multiplexer.OnNext(m_Index, value);
    //     }
    //
    //     public void OnCompleted(int source)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void OnError(int source, Exception error)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void OnNext(int source, T value)
    //     {
    //         // TODO Implement other logic or abstract out
    //
    //         var result = value.CompareTo(value);
    //         if (result > 0)
    //             m_Value = value;
    //
    //         m_ObserverList.OnNext(m_Value);
    //     }
    // }
}
