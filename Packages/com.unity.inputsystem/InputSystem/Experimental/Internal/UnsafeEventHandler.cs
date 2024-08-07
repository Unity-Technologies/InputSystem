using System;

namespace UnityEngine.InputSystem.Experimental
{
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
        public void Remove(UnsafeDelegateHelper.Callback callback) => UnsafeDelegateHelper.Remove(ref m_Ptr, callback);
        public ReadOnlySpan<UnsafeDelegate> GetInvocationList() => UnsafeDelegateHelper.GetInvocationListAs<UnsafeDelegate>(m_Ptr);
        
        public void Invoke()
        {
            if (m_Ptr == IntPtr.Zero)
                return;

            var header = (UnsafeDelegateHelper.Item*)m_Ptr;
            var handlers = (UnsafeDelegate*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke();
            }
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
        public void Remove(UnsafeDelegateHelper.Callback callback) => UnsafeDelegateHelper.Remove(ref m_Ptr, callback);
        public ReadOnlySpan<UnsafeDelegate<T>> GetInvocationList() => UnsafeDelegateHelper.GetInvocationListAs<UnsafeDelegate<T>>(m_Ptr);
        
        public void Invoke(T arg)
        {
            if (m_Ptr == IntPtr.Zero)
                return;

            var header = (UnsafeDelegateHelper.Item*)m_Ptr;
            var handlers = (UnsafeDelegate<T>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke(arg);
            }
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
            if (m_Ptr == IntPtr.Zero)
                return;
            
            var header = (UnsafeDelegateHelper.Item*)m_Ptr;
            var handlers = (UnsafeDelegate<T1, T2>*)(header + 1);
            for (var end = handlers + header->Length; handlers != end; ++handlers)
            {
                handlers->Invoke(arg1, arg2);
            }
        }
    }
}