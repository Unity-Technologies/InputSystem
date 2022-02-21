// In retrospect, allowing Touchscreen to do what it does the way it does it was a mistake. It came out of thinking that
// we need Touchscreen to have a large pool of TouchStates from which to dynamically allocate -- as this was what the old
// input system does. This made it unfeasible/unwise to put the burden of touch allocation on platform backends and thus
// led to the current setup where backends are sending TouchState events which Touchscreen dynamically incorporates.
//
// This shouldn't have happened.
//
// Ultimately, this led to IInputStateCallbackReceiver in its current form. While quite flexible in what it allows you to
// do, it introduces a lot of additional complication and deviation from an otherwise very simple model based on trivially
// understood chunks of input state.

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface for devices that implement their own state update handling.
    /// </summary>
    /// <remarks>
    /// The input system has built-in logic to automatically handle the state buffers that store input values for devices. This
    /// means that if an input event containing input state is processed, its data will be copied automatically into the state
    /// memory for the device.
    ///
    /// However, some devices need to apply custom logic whenever new input is received. An example of this is <see cref="Pointer.delta"/>
    /// which needs to accumulate deltas as they are received within a frame and then reset the delta at the beginning of a new frame.
    ///
    /// Also, devices like <see cref="Touchscreen"/> extensively customize event handling in order to implement features such as
    /// tap detection and primary touch handling. This is what allows the device to receive state events in <see cref="TouchState"/>
    /// format even though that is not the format of the device itself (which is mainly a composite of several TouchStates).
    ///
    /// This interface allows to bypass the built-in logic and instead intercept and manually handle state updates.
    /// </remarks>
    /// <seealso cref="InputDevice"/>
    /// <seealso cref="Pointer"/>
    /// <seealso cref="Touchscreen"/>
    public interface IInputStateCallbackReceiver
    {
        /// <summary>
        /// A new input update begins. This means that the current state of the device is being carried over into the next
        /// frame.
        /// </summary>
        /// <remarks>
        /// This is called without the front and back buffer for the device having been flipped. You can use <see cref="InputState.Change"/>
        /// to write values into the device's state (e.g. to reset a given control to its default state) which will implicitly perform
        /// the buffer flip.
        /// </remarks>
        void OnNextUpdate();

        /// <summary>
        /// A new state event has been received and is being processed.
        /// </summary>
        /// <param name="eventPtr">The state event. This will be either a <see cref="StateEvent"/> or a <see cref="DeltaStateEvent"/>.</param>
        /// <remarks>
        /// Use <see cref="InputState.Change"/> to write state updates into the device state buffers. While nothing will prevent a device
        /// from writing directly into the memory buffers retrieved with <see cref="InputControl.currentStatePtr"/>, doing so will bypass
        /// the buffer flipping logic as well as change detection from change monitors (<see cref="IInputStateChangeMonitor"/>; this will
        /// cause <see cref="InputAction"/> to not work with the device) and thus lead to incorrect behavior.
        /// </remarks>
        /// <seealso cref="StateEvent"/>
        /// <seealso cref="DeltaStateEvent"/>
        void OnStateEvent(InputEventPtr eventPtr);

        /// <summary>
        /// Compute an offset that correlates <paramref name="control"/> with the state in <paramref name="eventPtr"/>.
        /// </summary>
        /// <param name="control">Control the state of which we want to access within <paramref name="eventPtr"/>.</param>
        /// <param name="eventPtr">An input event. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/></param>
        /// <param name="offset"></param>
        /// <returns>False if the correlation failed or true if <paramref name="offset"/> has been set and should be used
        /// as the offset for the state of <paramref name="control"/>.</returns>
        /// <remarks>
        /// This method will only be called if the given state event has a state format different than that of the device. In that case,
        /// the memory of the input state captured in the given state event cannot be trivially correlated with the control.
        ///
        /// The input system calls the method to know which offset (if any) in the device's state block to consider the state
        /// in <paramref name="eventPtr"/> relative to when accessing the state for <paramref name="control"/> as found in
        /// the event.
        ///
        /// An example of when this is called is for touch events. These are normally sent in <see cref="TouchState"/> format
        /// which, however, is not the state format of <see cref="Touchscreen"/> (which uses a composite of several TouchStates).
        /// When trying to access the state in <paramref name="eventPtr"/> to, for example, read out the touch position,
        /// </remarks>
        /// <seealso cref="InputControlExtensions.GetStatePtrFromStateEvent"/>
        bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset);
    }
}
