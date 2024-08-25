using System;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    public class ObservableCondition<T> : IObservable<bool>
    {
        private ObserverList2<bool> m_Observers;

        public IDisposable Subscribe(IObserver<bool> observer) => Subscribe(Context.instance, observer);
        
        public IDisposable Subscribe(Context context, IObserver<bool> observer)
        {
            if (m_Observers == null)
                m_Observers = new ObserverList2<bool>();
            return m_Observers.Subscribe(context, observer);
        }
    }

    // gameState.IsEqualTo()
    
    public class Observable
    {
        /*public static ObservableCondition Condition()
        {
            
        }*/
    }

    internal interface IDisposeSubscription
    {
        public void DisposeSubscription();
    }
    
    internal class ObservablePredicateNode<T> : IDisposable, IObserver<T>, IObservable<bool>, IDisposeSubscription
    {
        private IDisposable m_SourceSubscription;
        private ObserverList3<bool> m_Observers;
        private Predicate<T> m_Predicate;
        private bool m_Previous;
        
        public void Initialize(Context context, IDisposable sourceSubscription, Predicate<T> predicate)
        {
            m_SourceSubscription = sourceSubscription;
            m_Predicate = predicate;
        }

        public void DisposeSubscription()
        {
            
        }

        public void Dispose()
        {
            // Unsubscribe from underlying dependencies
            
            // Reset node state
            m_Predicate = null;
            m_Observers = default;

            // TODO Return instance to context
        }

        private void UnsubscribeFromDependencies()
        {
            m_SourceSubscription.Dispose();
            m_SourceSubscription = null;
        }

        public void OnCompleted()
        {
            m_Observers.OnCompleted();
            UnsubscribeFromDependencies();
        }

        public void OnError(Exception error)
        {
            m_Observers.OnError(error);
        }

        public void OnNext(T value)
        {
            var current = m_Predicate(value);
            if (current == m_Previous)
                return;
            m_Previous = current;
            m_Observers.OnNext(current);
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            return m_Observers.Add(observer);
        }
    }
    
    public struct ObservablePredicate<TObservable, T> : IObservable<bool>
        where TObservable : IObservable<T>
        where T : struct
    {
        public ObservablePredicate(TObservable source, Predicate<T> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public TObservable source { get; set; }
        public Predicate<T> predicate { get; set; }
        
        public IDisposable Subscribe(IObserver<bool> observer)
        {
            var node = Context.instance.RentNode<ObservablePredicateNode<T>>() ?? new ObservablePredicateNode<T>();
            node.Initialize(Context.instance, source.Subscribe(node), predicate); // TODO Need to setup so that it recycle on dispose()
            return node.Subscribe(observer);
        }
    }

    // TODO Its annoying we can never know if there would be another subscription to our object...
    //      But, we can intentionally avoid caching if we think its undesirable.
    //      Consider this chain:
    //
    //      Gamepad.leftStick.Invert().Scale(0.5f).LessThan(0.1f).Subscribe(x => Debug.Log(x));
    //
    // First node is lookup instance via context
    //    - We need to subscribe to source stream
    //    - There is an opporunity to combine invert/scale/less-than into a single node since proxy can carry dependencies in its type.
    //    - Hence, 
    //      In subscribe, if observer is constrained generic, 
    //
    // public IDisposable Subscribe<TObserver>(TObserver observer) where TObserver : IObserver<bool>
    // {
    //     observer.OnNext( Invert(  ) );
    //    node.Initialize(source.Subscribe(node),predicate);
    //    return node.Subscribe(observer);
    // }
    
    public static class ObservableConditionExtensions
    {
        public static ObservablePredicate<TObservable, T> Predicate<TObservable, T>(this TObservable observable, Predicate<T> predicate)
            where TObservable : IObservable<T>
            where T : struct
        {
            return new ObservablePredicate<TObservable, T>(observable, predicate);
        }
        
        public static ObservablePredicate<TObservable, int> Predicate<TObservable>(this TObservable observable, Predicate<int> predicate)
            where TObservable : IObservable<int>
        {
            return new ObservablePredicate<TObservable, int>(observable, predicate);
        }
        
        // TODO EqualTo
        // TODO LessThan
        // TODO LessOrEqualTo
        // TODO GreaterThan
        // TODO GreaterOrEqualTo
        // TODO NotEqualTo
    }
}