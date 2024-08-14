using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A stateful event handler based on C# 9.0+ unsafe function pointers taking zero invocation arguments.
    /// </summary>
    /// <typeparam name="TState">The state type passed by reference.</typeparam>
    internal unsafe struct UnsafeStatefulEventHandler<TState> : IDisposable 
        where TState : unmanaged
    {
        private IntPtr m_Ptr;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => UnsafeDelegateHelper.Dispose(ref m_Ptr);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(UnsafeStatefulDelegate<TState> d) => UnsafeDelegateHelper.Add(ref m_Ptr, d.ToCallback());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Add(delegate*<ref TState, void> d, TState* state) =>
            Add(new UnsafeStatefulDelegate<TState>(d, state));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(UnsafeStatefulDelegate<TState> d) => UnsafeDelegateHelper.Remove(ref m_Ptr, d.ToCallback());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke()
        {
            if (m_Ptr == IntPtr.Zero)
                return;
            
            var header = (UnsafeDelegateHelper.Item*)m_Ptr;
            var handlers = (UnsafeStatefulDelegate<TState>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke();
            }
        }
    }
    
    /// <summary>
    /// A stateful event handler based on C# 9.0+ unsafe function pointers taking zero invocation arguments.
    /// </summary>
    /// <typeparam name="TState">The state type passed by reference.</typeparam>
    /// <typeparam name="T1">The invocation argument type.</typeparam>
    internal unsafe struct UnsafeStatefulEventHandler<TState, T1> : IDisposable 
        where TState : unmanaged
    {
        private IntPtr m_Ptr;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => UnsafeDelegateHelper.Dispose(ref m_Ptr);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(UnsafeStatefulDelegate<TState, T1> d) => UnsafeDelegateHelper.Add(ref m_Ptr, d.ToCallback());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(delegate*<ref TState, T1, void> d, TState* state = null) =>
            UnsafeDelegateHelper.Add(ref m_Ptr, new UnsafeCallback(d, state));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(UnsafeStatefulDelegate<TState, T1> d) => UnsafeDelegateHelper.Remove(ref m_Ptr, d.ToCallback());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(T1 arg1)
        {
            var ptr = m_Ptr;
            if (ptr == IntPtr.Zero)
                return;
            
            UnsafeDelegateHelper.AddRef(ptr);
            var header = (UnsafeDelegateHelper.Item*)ptr;
            var handlers = (UnsafeStatefulDelegate<TState, T1>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke(arg1);
            }
            UnsafeDelegateHelper.Release(ref ptr);
        }
    }
}