namespace UnityEngine.InputSystem.DataPipeline.Collections
{
    internal sealed class AlmostManagedSpanDebugView<T> where T : struct
    {
        private AlmostManagedSpan<T> m_Array;

        public AlmostManagedSpanDebugView(AlmostManagedSpan<T> array) => m_Array = array;

        public T[] Items => m_Array.ToArray();
    }
}