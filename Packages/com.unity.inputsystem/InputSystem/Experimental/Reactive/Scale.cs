using System;

namespace UnityEngine.InputSystem.Experimental
{
    public static class ScaleExtensions
    {
        // TODO How to solve overloads on inner types T? E.g. float, Vector2, Vector3 etc
        //
        // Option 1, make this agnostic by letting source implement e.g. IScale and handle within node
        // Option 2, overload on specific types (bad since not scaling)
        
        // TODO This is not type safe, should we have different controls for absolute and relative? For dynamic device we cannot now unless we generate the controls at run-time, which we of course could.
        /// <summary>
        /// Scales the stream values by delta-time to give units per second, if and only if the underlying control is
        /// an absolute control. If the underlying control is relative this is a no-op.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <typeparam name="TSource">The source type, must be an observable numeric type.</typeparam>
        /// <returns><paramref name="source"/></returns>
        public static TSource ScaleWithDeltaTime<TSource>(this TSource source)
            where TSource : IObservableInput<Vector2>
        {
            // TODO Apply scale to source or construct a consuming node that applies the scale, potentially just a bit setting
            return source;
        }
        
        public static TSource Scale<TSource>(this TSource source, float scale)
            where TSource : IObservableInput<float>
        {
            // TODO Apply scale to source or construct a consuming node that applies the scale
            return source;
        }
        
        public static TSource Scale<TSource, TScaleSource>(this TSource source, TScaleSource scale)
            where TSource : IObservableInput<float>
            where TScaleSource : IObservable<float>
        {
            // TODO Apply scale to source or construct a consuming node that applies the scale
            return source;
        }
    }
}