using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO This is basically AND, redesign as such?
    public struct Shortcut<TSource> : IObservableInput<bool>, IDependencyGraphNode
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<ValueTuple<bool, bool>>
        {
            private ValueTuple<bool, bool> m_Previous;
            private bool m_PreviousResult;
            private readonly ObserverList2<bool> m_Observers;
            
            public Impl(Context context, TSource source0, TSource source1)
            {
                // TODO This is one way to implement this, but it might be better to use same strategy as merge to avoid copying? (But this is simpler)
                var combineLatest = new CombineLatest<bool, bool, TSource, TSource>(source0, source1);
                m_Observers = new ObserverList2<bool>(combineLatest.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<bool> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<bool> observer) =>
                m_Observers.Subscribe(context, observer);

            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(ValueTuple<bool, bool> value) // TODO This would be way easier if we could have a source reference
            {
                if (value == m_Previous)
                    return;
                var result = m_Previous is { Item1: true, Item2: false } && value.Item2;
                m_Previous = value;
                if (m_PreviousResult == result) 
                    return;
                m_PreviousResult = result;
                m_Observers.OnNext(result);
            }
        }
        
        private readonly TSource m_Modifier;
        private readonly TSource m_Trigger;
        private Impl m_Impl;
        
        public Shortcut([InputPort] [NotNull] TSource modifier, [InputPort] [NotNull] TSource trigger)
        {
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            if (modifier.Equals(trigger))
                throw new ArgumentException($"{nameof(modifier)} may not be the same as {nameof(trigger)}");
                
            m_Modifier = modifier;
            m_Trigger = trigger;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<bool> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<bool> observer) =>
            (m_Impl ??= new Impl(context, m_Modifier, m_Trigger)).Subscribe(context, observer);
        
        // TODO Reader end-point
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Shortcut";
        public int childCount => 1;

        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Modifier;
                case 1: return m_Trigger;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    // Shortcut is a combining function and hence has a static factory function
    public static partial class Combine
    {
        public static Shortcut<TSource> Shortcut<TSource>(TSource source1, TSource source2)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Shortcut<TSource>(source1, source2);
        }
    }
}