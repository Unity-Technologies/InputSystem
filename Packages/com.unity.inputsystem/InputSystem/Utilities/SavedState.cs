namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Provides type erasure and an abstraction of a saved state that
    /// will (must) be restored at a later point.
    /// </summary>
    internal interface ISavedState
    {
        void Restore();
    }

    /// <summary>
    /// Provides functionality to store and support later restoration of a saved
    /// state. The state is expected to be a value-type. If the state is not restored
    /// it must be disposed to not leak resources.
    /// </summary>
    /// <typeparam name="T">The value-type representing the state to be stored.</typeparam>
    internal sealed class SavedStructState<T> : ISavedState where T : struct
    {
        public delegate void TypedRestore(ref T state);

        internal SavedStructState(ref T state, TypedRestore restoreAction)
        {
            Debug.Assert(restoreAction != null);

            m_State = state; // copy
            m_RestoreAction = restoreAction;
        }

        /// <summary>
        /// Restore previous state
        /// </summary>
        public void Restore()
        {
            if (m_RestoreAction != null)
            {
                m_RestoreAction(ref m_State);
                m_RestoreAction = null;
            }
        }

        private T m_State;
        private TypedRestore m_RestoreAction;
    }
}
