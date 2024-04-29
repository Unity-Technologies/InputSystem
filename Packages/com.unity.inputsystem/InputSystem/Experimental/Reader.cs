namespace UnityEngine.InputSystem.Experimental
{
    public struct Reader<T>
    {
        internal unsafe Reader(Context context, Stream* stream = null)
        {
            m_Context = context;
            m_Stream = stream;
        }

        internal readonly bool TryRead(ref T state)
        {
            state = default;
            return true;
        }

        private Context m_Context;
        private unsafe Stream* m_Stream;
    }
}