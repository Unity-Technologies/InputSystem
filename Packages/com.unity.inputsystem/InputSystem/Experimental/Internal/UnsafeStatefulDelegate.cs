using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    public static class UnsafeStatefulDelegate
    {
        public static unsafe UnsafeStatefulDelegate<TState> Create<TState>(
            delegate*<ref TState, void> function, TState* state) 
            where TState : unmanaged
        {
            return new UnsafeStatefulDelegate<TState>(function, state);
        }
        
        public static unsafe UnsafeStatefulDelegate<TState, T1> Create<TState, T1>(
            delegate*<ref TState, T1, void> function, TState* state) 
            where TState : unmanaged
        {
            return new UnsafeStatefulDelegate<TState, T1>(function, state);
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeStatefulDelegate<TState> 
        where TState : unmanaged
    {
        private delegate*<ref TState, void> m_Ptr;
        private TState* m_Data;
        
        public UnsafeStatefulDelegate(delegate*<ref TState, void> callback, TState* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }
        
        public void Invoke() => m_Ptr(ref *m_Data);

        internal UnsafeCallback ToCallback() => new (m_Ptr, m_Data);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeStatefulDelegate<TState, T1>
        where TState : unmanaged
    {
        private delegate*<ref TState, T1, void> m_Ptr;
        private TState* m_Data;

        public UnsafeStatefulDelegate(delegate*<ref TState, T1, void> callback, TState* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }

        public void Invoke(T1 value) => m_Ptr(ref *m_Data, value);

        internal UnsafeCallback ToCallback() => new(m_Ptr, m_Data);
    }
}