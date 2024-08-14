using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal interface IUnsafeEventHandler<TDelegate>
    {
        public void Add(UnsafeDelegate callback);
        public void Remove(UnsafeDelegate callback);
        public ReadOnlySpan<TDelegate> GetInvocationList();
    }
    
    /// <summary>
    /// Event handler supporting <see cref="UnsafeDelegate"/> delegates taking only an opaque
    /// contextual state argument.
    /// </summary>
    internal unsafe struct UnsafeEventHandler : IDisposable 
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegateHelper.Dispose(ref m_Ptr);
        public void Add(UnsafeDelegate d) => UnsafeDelegateHelper.Add(ref m_Ptr, d.ToCallback());
        public void Remove(UnsafeDelegate d) => UnsafeDelegateHelper.Remove(ref m_Ptr, d.ToCallback());
        public void Remove(UnsafeCallback callback) => UnsafeDelegateHelper.Remove(ref m_Ptr, callback);
        public ReadOnlySpan<UnsafeDelegate> GetInvocationList() => UnsafeDelegateHelper.GetInvocationListAs<UnsafeDelegate>(m_Ptr);
        
        public void Invoke()
        {
            var ptr = m_Ptr; // Important: Copy to avoid m_Ptr being modified concurrently and keep ref count correct
            if (ptr == IntPtr.Zero)
                return;
            
            UnsafeDelegateHelper.AddRef(ptr);
            var header = (UnsafeDelegateHelper.Item*)ptr;
            var handlers = (UnsafeDelegate*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke();
            }
            UnsafeDelegateHelper.Release(ref ptr);
        }

        internal IntPtr ToPointer() => m_Ptr;
    }
    
    /// <summary>
    /// Event handler supporting <see cref="UnsafeDelegate{T}"/> delegates.
    /// </summary>
    internal unsafe struct UnsafeEventHandler<T> : IDisposable 
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegateHelper.Dispose(ref m_Ptr);
        public void Add(UnsafeDelegate<T> d) => UnsafeDelegateHelper.Add(ref m_Ptr, d.ToCallback());
        public void Remove(UnsafeDelegate<T> d) => UnsafeDelegateHelper.Remove(ref m_Ptr, d.ToCallback());
        public void Remove(UnsafeCallback callback) => UnsafeDelegateHelper.Remove(ref m_Ptr, callback);
        public ReadOnlySpan<UnsafeDelegate<T>> GetInvocationList() => UnsafeDelegateHelper.GetInvocationListAs<UnsafeDelegate<T>>(m_Ptr);
        
        public void Invoke(T arg)
        {
            var ptr = m_Ptr;
            if (ptr == IntPtr.Zero)
                return;
            
            UnsafeDelegateHelper.AddRef(ptr);
            var header = (UnsafeDelegateHelper.Item*)ptr;
            var handlers = (UnsafeDelegate<T>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke(arg);
            }
            UnsafeDelegateHelper.Release(ref ptr);
        }

        internal IntPtr ToPointer() => m_Ptr;
    }
    
    /// <summary>
    /// Event handler supporting <see cref="UnsafeDelegate{T1, T2}"/> delegates.
    /// </summary>
    unsafe struct UnsafeEventHandler<T1, T2> : IDisposable
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegateHelper.Dispose(ref m_Ptr);
        public void Add(UnsafeDelegate<T1, T2> d) => UnsafeDelegateHelper.Add(ref m_Ptr, d.ToCallback());
        public void Remove(UnsafeDelegate<T1, T2> d) => UnsafeDelegateHelper.Remove(ref m_Ptr, d.ToCallback());
        public ReadOnlySpan<UnsafeDelegate<T1, T2>> GetInvocationList() => UnsafeDelegateHelper.GetInvocationListAs<UnsafeDelegate<T1, T2>>(m_Ptr);
        public void Invoke(T1 arg1, T2 arg2)
        {
            var ptr = m_Ptr;
            if (ptr == IntPtr.Zero)
                return;
            
            UnsafeDelegateHelper.AddRef(ptr);
            var header = (UnsafeDelegateHelper.Item*)ptr;
            var handlers = (UnsafeDelegate<T1, T2>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke(arg1, arg2);
            }
            UnsafeDelegateHelper.Release(ref ptr);
        }
    }
}