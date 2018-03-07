using System;

namespace ISX.LowLevel
{
    /// <summary>
    /// Allows a device to intercept operations performed on its state.
    /// </summary>
    /// <remarks>
    /// This is an expensive interface that incurs costly extra processing. Only put this on
    /// a device if it is really needed.
    /// </remarks>
    public interface IInputStateCallbackReceiver
    {
        /// <summary>
        /// Called when a new input update is started and existing state of a device is carried forward.
        /// </summary>
        /// <remarks>
        /// This method is mainly useful to implement auto-resetting of state. For example, pointer
        /// position deltas should go back to their default state at the beginning of a frame. Using
        /// this state callback, it's possible to zero out delta control state at the beginning of
        /// an update.
        ///
        /// Note that the system will still run change monitors over the old and new version of the
        /// state after calling this method. This means that actions are able to observe changes applied
        /// to state in this method.
        /// </remarks>
        void OnCarryStateForward(IntPtr statePtr);

        /// <summary>
        /// Called when the state of the device is updated.
        /// </summary>
        void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr);
    }
}
