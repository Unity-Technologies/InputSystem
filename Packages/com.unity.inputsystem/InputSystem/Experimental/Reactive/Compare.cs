using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    public enum Compare
    {
        EqualTo,
        NotEqualTo,
        GreaterThan,
        LessThan,
        GreaterOrEqualTo,
        LessOrEqualTo
    }
    
    public struct Compare<TSource, T> : IObservableInputNode<bool>, IUnsafeObservable<bool>
        where TSource : IObservableInputNode<T>
        where T : struct, IComparable<T>
    {
        private TSource m_Source;
        private T m_Value;
        private Compare m_Compare;
        
        public Compare(TSource source, T value, Compare compare)
        { 
            m_Source = source;
            m_Value = value;
            m_Compare = compare;
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public bool Equals(IDependencyGraphNode other)
        {
            throw new NotImplementedException();
        }

        public string displayName => "Compare";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source;
                default: throw new ArgumentOutOfRangeException($"nameof(index)");
            }
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<bool>
        {
            var node = new CompareObserver<T>();
            node.Initialize(context, m_Source.Subscribe(context, node), m_Value, m_Compare);
            return Subscribe(context, observer);
        }

        public UnsafeSubscription Subscribe(Context context, UnsafeDelegate<bool> observer)
        {
            throw new NotImplementedException();
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InputNodeAttribute : Attribute
    {
        public string name { get; set; }
    }
    
    [InputNode(name="Compare")]
    internal sealed class CompareObserver<T> : ObserverBase<bool>, IObserver<T>, IUnsubscribe<bool> 
        where T : struct, IComparable<T>
    {
        private IDisposable m_SourceSubscription; // TODO This means we have already boxed it if its a struct, bad
        private Compare m_Compare;
        private T m_Comparand;
        
        public void Initialize([NotNull] Context ctx, [NotNull] IDisposable sourceSubscription, T comparand, Compare compare)
        {
            context = ctx;
            
            m_SourceSubscription = sourceSubscription;
            m_Compare = compare;
            m_Comparand = comparand;
        }

        public void Unsubscribe([NotNull] IObserver<bool> observer)
        {
            if (RemoveObserver(observer))
            {
                m_SourceSubscription.Dispose();
                m_SourceSubscription = null;
            }
        }

        public void OnNext(T value)
        {
            switch (m_Compare)
            {
                case Compare.EqualTo:
                    ForwardOnNext(value.CompareTo(m_Comparand) == 0);
                    break;
                case Compare.NotEqualTo:
                    ForwardOnNext(value.CompareTo(m_Comparand) != 0);
                    break;
                case Compare.GreaterThan:
                    ForwardOnNext(value.CompareTo(m_Comparand) > 0);
                    break;
                case Compare.LessThan:
                    ForwardOnNext(value.CompareTo(m_Comparand) < 0);
                    break;
                case Compare.GreaterOrEqualTo:
                    ForwardOnNext(value.CompareTo(m_Comparand) >= 0);
                    break;
                case Compare.LessOrEqualTo:
                    ForwardOnNext(value.CompareTo(m_Comparand) <= 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }        
    }
    
    public static class CompareExtensions
    {
        public static Compare<TSource, T> GreaterThan<TSource, T>(this TSource source, T value)
            where TSource : IObservableInputNode<T> 
            where T : struct, IComparable<T>
        {
            return new Compare<TSource, T>(source, value, Compare.GreaterThan);
        }
        
        public static Compare<TSource, float> AsButton<TSource>(this TSource source, float value = 0.5f)
            where TSource : IObservableInputNode<float> 
        {
            return new Compare<TSource, float>(source, value, Compare.GreaterThan);
        }
    }
}