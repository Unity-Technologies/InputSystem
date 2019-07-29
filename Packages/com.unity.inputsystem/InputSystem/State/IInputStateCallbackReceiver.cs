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
    /// This interface allows to bypass the built-in logic and instead intercept and manually handle state updates.
    /// </remarks>
    /// <seealso cref="InputDevice"/>
    /// <seealso cref="Pointer"/>
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
    }
}
