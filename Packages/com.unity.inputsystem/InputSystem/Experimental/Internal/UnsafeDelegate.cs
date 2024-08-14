using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Do we want a stateless and a stateful delegate?

    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct UnsafeStatelessDelegate
    {
        private readonly delegate*<void> m_Ptr;
        private readonly void* m_UnusedOnlyForPadding;
        
        public UnsafeStatelessDelegate(delegate*<void> callback)
        {
            m_Ptr = callback;
            m_UnusedOnlyForPadding = null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke() => m_Ptr();
        
        public IntPtr ToIntPtr() => (IntPtr)m_Ptr;
        
        internal UnsafeCallback ToCallback() => new (m_Ptr, null);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct UnsafeDelegate
    {
        private readonly delegate*<void*, void> m_Ptr;
        private readonly void* m_Data;
        
        public UnsafeDelegate(delegate*<void*, void> callback, void* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke() => m_Ptr(m_Data);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeCallback ToCallback() => new (m_Ptr, m_Data);
        
        #region Convenience functions to allow for compiler to derive types to reduce code bloat.
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeDelegate Create(delegate*<void*, void> function, void* state)
        {
            return new UnsafeDelegate(function, state);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeDelegate<T> Create<T>(delegate*<T, void*, void> function, void* state) 
        {
            return new UnsafeDelegate<T>(function, state);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeDelegate<T1, T2> Create<T1, T2>(delegate*<T1, T2, void*, void> function, void* state) 
        {
            return new UnsafeDelegate<T1, T2>(function, state);
        }
        
        #endregion
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct UnsafeDelegate<T> // TODO Consider renaming state to target to mimic EventHandler?
    {
        private readonly delegate*<T, void*, void> m_Ptr;
        private readonly void* m_Data;

        public UnsafeDelegate(delegate*<T, void*, void> callback, void* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(T value) => m_Ptr(value, m_Data);
        internal UnsafeCallback ToCallback() => new (m_Ptr, m_Data);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct UnsafeDelegate<T1, T2> 
    {
        private readonly delegate*<T1, T2, void*, void> m_Ptr; // TODO Just use IntPtr?
        private readonly void* m_Data;

        public UnsafeDelegate(delegate*<T1, T2, void*, void> callback, void* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(T1 arg1, T2 arg2) => m_Ptr(arg1, arg2, m_Data);
        
        internal UnsafeCallback ToCallback() => new(m_Ptr, m_Data);
    }

}