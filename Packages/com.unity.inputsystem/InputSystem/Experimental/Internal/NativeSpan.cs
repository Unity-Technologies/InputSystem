namespace UnityEngine.InputSystem.Experimental
{
    internal unsafe struct NativeSpan
    {
        private void* m_Ptr;
        private int m_Length;

        public NativeSpan(void* pointer, int length)
        {
            m_Ptr = pointer;
            m_Length = length;
        }

        public int length => m_Length;
        public void* pointer => m_Ptr;
    }

    internal readonly unsafe struct NativeReadOnlySpan
    {
        private readonly void* m_Ptr;
        private readonly int m_Length;
        
        public NativeReadOnlySpan(void* pointer, int length)
        {
            m_Ptr = pointer;
            m_Length = length;
        }

        public int length => m_Length;
        public void* pointer => m_Ptr;

        public NativeReadOnlySpan<T> As<T>() where T : unmanaged
        {
            return new NativeReadOnlySpan<T>((T*)m_Ptr, m_Length);
        }
    }

    internal readonly unsafe struct NativeSpan<T> where T : unmanaged
    {
        private readonly T* m_Ptr;
        private readonly int m_Length;
        
        public NativeSpan(T* pointer, int length)
        {
            m_Ptr = pointer;
            m_Length = length;
        }

        public int length => m_Length;
        public T* pointer => m_Ptr;
    }
    
    internal readonly unsafe struct NativeReadOnlySpan<T>
    {
        private readonly void* m_Ptr;
        private readonly int m_Length;
        
        public NativeReadOnlySpan(void* pointer, int length)
        {
            m_Ptr = pointer;
            m_Length = length;
        }

        public int length => m_Length;
        public void* pointer => m_Ptr;
    }
}