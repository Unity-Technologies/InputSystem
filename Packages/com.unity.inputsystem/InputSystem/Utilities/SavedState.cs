using System;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Provides type erasure and an abstraction of a saved state that
    /// will (must) be restored at a later point.
    /// </summary>
    internal interface ISavedState
    {
        void StaticDisposeCurrentState();

        void RestoreSavedState();
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

        /// <summary>
        /// Constructs a SavedStructState.
        /// </summary>
        /// <param name="state">The value-type state to be saved.</param>
        /// <param name="restoreAction">The action to be carried out to restore state.</param>
        /// <param name="staticDisposeCurrentState">The action to be carried out to dispose current state.</param>
        internal SavedStructState(ref T state, TypedRestore restoreAction, Action staticDisposeCurrentState)
        {
            Debug.Assert(restoreAction != null, "Restore action is required");

            m_State = state; // copy
            m_RestoreAction = restoreAction;
            m_StaticDisposeCurrentState = staticDisposeCurrentState;
        }

        /// <summary>
        /// Dispose current state, should be invoked before RestoreSavedState().
        /// </summary>
        public void StaticDisposeCurrentState()
        {
            if (m_StaticDisposeCurrentState != null)
            {
                m_StaticDisposeCurrentState();
                m_StaticDisposeCurrentState = null;
            }
        }

        /// <summary>
        /// Restore previous state, should be invoked after StaticDisposeCurrentState().
        /// </summary>
        public void RestoreSavedState()
        {
            Debug.Assert(m_StaticDisposeCurrentState == null, "Only restore once");
            Debug.Assert(m_RestoreAction != null, "Only restore once");
            m_RestoreAction(ref m_State);
            m_RestoreAction = null;
        }

        private T m_State;
        private TypedRestore m_RestoreAction;
        private Action m_StaticDisposeCurrentState;
    }
}
