using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO This would be so much better with .NET 6
    
    internal interface INumericPolicy<T>
    {
        T Zero();
        T Add(T a, T b);
        T Subtract(T a, T b);
        T Multiply(T a, T b);
        T Divide(T a, T b);
    }

    internal interface IVectorMath<in T>
    {
        float Magnitude(T v);
    }

    internal interface INumeric<TSelf> 
        where TSelf : unmanaged, IComparable<TSelf>, IEquatable<TSelf>
    { }
    
    /// <summary>
    /// This provides a temporary workaround for .NET 8 generic math support.
    /// </summary>
    internal struct NumericPolicies : 
        INumericPolicy<int>, 
        INumericPolicy<float>, 
        IVectorMath<float>,
        IVectorMath<Vector2>,
        IVectorMath<Vector3>
    {
        public static NumericPolicies Instance = new NumericPolicies();
        
        #region INumericPolicy<int>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] int INumericPolicy<int>.Zero() => 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] int INumericPolicy<int>.Add(int a, int b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] int INumericPolicy<int>.Subtract(int a, int b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] int INumericPolicy<int>.Multiply(int a, int b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] int INumericPolicy<int>.Divide(int a, int b) => a / b;
        #endregion
        
        #region INumericPolicy<float>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float INumericPolicy<float>.Zero() => 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float INumericPolicy<float>.Add(float a, float b) => a + b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float INumericPolicy<float>.Subtract(float a, float b) => a - b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float INumericPolicy<float>.Multiply(float a, float b) => a * b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float INumericPolicy<float>.Divide(float a, float b) => a / b;
        #endregion
        
        #region IVectorMath<float>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] float IVectorMath<float>.Zero() => 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float IVectorMath<float>.Magnitude(float x) => x;
        #endregion
        
        #region IVectorMath<Vector2>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] Vector2 IVectorMath<Vector2>.Zero() => Vector2.zero;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float IVectorMath<Vector2>.Magnitude(Vector2 v) => v.magnitude;
        #endregion
        
        #region IVectorMath<Vector2>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] Vector3 IVectorMath<Vector3>.Zero() => Vector3.zero;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] float IVectorMath<Vector3>.Magnitude(Vector3 v) => v.magnitude;
        #endregion
    }

    internal static class NumericMath
    {
        public static float Magnitude<TNumericPolicy, T>(this TNumericPolicy p, T value)
            where TNumericPolicy : IVectorMath<T>, INumericPolicy<T>
        {
            return p.Magnitude(value);
        }
        
        public static T Sum<TNumericPolicy, T>(this TNumericPolicy p, params T[] a)
            where TNumericPolicy: INumericPolicy<T>
        {
            var r = p.Zero();
            foreach(var i in a)
            {
                r = p.Add(r, i);
            }
            return r;
        }

    }
}