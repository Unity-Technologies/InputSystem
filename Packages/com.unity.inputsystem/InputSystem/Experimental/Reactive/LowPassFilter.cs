using System;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IFilterFunc<T>
    {
        bool Filter(ref T value);
    }
    
    public struct FloatOneEuroFilter : IFilterFunc<float>
    {
        private readonly float m_Alpha;
        private float m_Last;
        
        public FloatOneEuroFilter(float alpha, float initialValue)
        {
            if (alpha <= 0.0f || alpha > 1.0f)
                throw new ArgumentOutOfRangeException($"{nameof(alpha)} should be in range (0.0., 1.0]");
            
            m_Last = initialValue;
            m_Alpha = alpha;
        }

        public bool Filter(ref float value)
        {
            value = m_Alpha * value + (1.0f - m_Alpha) * m_Last;
            m_Last = value;
            return true;
        }

        public static void Filter(ref float value, ref float lastValue, float alpha)
        {
            value = alpha * value + (1.0f - alpha) * lastValue;
            lastValue = value;
        }
    }
    
    public struct Vector2OneEuroFilter : IFilterFunc<Vector2>
    {
        public bool Filter(ref Vector2 value)
        {
            throw new NotImplementedException();
        }
    }
    
    public struct LowPassFilter<T, TSource, TFilter> : IObservableInput<T>
        where T : struct
        where TSource : IObservableInput<T>
        where TFilter : IFilterFunc<T>
    {
        private class Impl : IObserver<T>
        {
            private readonly ObserverList2<T> m_Observers;
            private readonly TFilter m_Filter;

            public Impl(Context context, TSource source, TFilter filter)
            {
                m_Observers = new ObserverList2<T>(source.Subscribe(context, this));
                m_Filter = filter;
            }
            
            public IDisposable Subscribe(IObserver<T> observer) => 
               Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<T> observer) =>
                m_Observers.Subscribe(context, observer);
            
            private void Process(T value)
            {
                if (m_Filter.Filter(ref value))
                    m_Observers.OnNext(value);
            }
            
            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);
            public void OnNext(T value) => Process(value);
        }
        
        
        private readonly TSource m_Source;
        private Impl m_Impl;
        private readonly TFilter m_Filter;
        
        public LowPassFilter(TSource source, TFilter filter)
        {
            m_Source = source;
            m_Filter = filter;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<T> observer) =>
            (m_Impl ??= new Impl(context, m_Source, m_Filter)).Subscribe(context, observer);

        public bool Equals(IDependencyGraphNode other)
        {
            throw new NotImplementedException();
        }
        
        public string displayName => "Low-Pass Filter";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
    
    /// <summary>
    /// Allows applying Low-pass filtering processing to an observable input source.
    /// </summary>
    public static class LowPassFilterExtensionMethods
    {
        /*public static LowPassFilter<float, IObservableInput<float>, FloatOneEuroFilter> LowPassFilter<T>(
            this IObservableInput<float> source) 
            where T : struct 
        {
            return new LowPassFilter<float, IObservableInput<float>, FloatOneEuroFilter>(source, new FloatOneEuroFilter());
        }*/
        
        public static LowPassFilter<float, TSource, FloatOneEuroFilter> LowPassFilter<TSource>(this TSource source) 
            where TSource : IObservableInput<float>
        {
            return new LowPassFilter<float, TSource, FloatOneEuroFilter>(source, new FloatOneEuroFilter());
        }
        
        // TODO Figure out how to fix these overloads
        /*public static LowPassFilter<Vector2, TSource, Vector2OneEuroFilter> LowPassFilter<TSource>(this TSource source) 
            where TSource : IObservableInput<Vector2>
        {
            return new LowPassFilter<Vector2, TSource, Vector2OneEuroFilter>(source, new Vector2OneEuroFilter());
        }*/
    }
}