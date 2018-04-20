using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
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
        ////REVIEW: replace bool result value with enum?
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
        /// <param name="statePtr">Pointer to the buffer containing the state of the device that is
        /// carried forward.</param>
        /// <returns>True if you have modified the state in <paramref name="statePtr"/>, false if you
        /// have left it as is.</returns>
        bool OnCarryStateForward(IntPtr statePtr);

        /// <summary>
        /// Called when a new state is received for the device but before it is copied over the
        /// device's current state.
        /// </summary>
        /// <param name="oldStatePtr">Pointer to the buffer containing the current state of the device.</param>
        /// <param name="newStatePtr">Pointer to the buffer containing the new state that has been received for the device.</param>
        /// <remarks>
        /// This method can be used to alter the newly received state before it is written into the
        /// device. Pointer delta controls, for example, should accumulate values from multiple consecutive
        /// events that happen in the same frame rather than just overwriting the previously stored delta.
        /// This can be achieved by reading the current delta from <paramref name="oldStatePtr"/> and then
        /// adding it on top of the delta found in <paramref name="newStatePtr"/>.
        ///
        /// Note that this callback is invoked before state change monitors are run to compare the two
        /// states. Thus, if in this method, <paramref name="newStatePtr"/> is changed thus that there is
        /// no difference anymore to the old state, state change monitors will not fire and actions will
        /// not get triggered.
        /// </remarks>
        void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr);

        ////TODO: pass pointer to current state
        /// <summary>
        /// Called when a device receives a chunk of state that is tagged with a different format than the
        /// state of the device itself.
        /// </summary>
        /// <param name="statePtr"></param>
        /// <param name="stateFormat"></param>
        /// <param name="stateSize"></param>
        /// <param name="offsetToStoreAt"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method permits a device to integrate state into its own that is not sent as full-device snapshots
        /// or deltas with specific offsets.
        /// </remarks>
        bool OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize, ref uint offsetToStoreAt);
    }
}
