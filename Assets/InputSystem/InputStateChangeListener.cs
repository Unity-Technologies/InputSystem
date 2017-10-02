namespace ISX
{
	// Listener for device state changes.
	// State can change in one of two ways:
	// 1) State change events fed into the system.
	// 2) Input controls mutating the state directly.
	// The latter usually only happens in the case of output controls.
	// If there are state change events, listeners will see notifications *before* a dynamic/fixed/render starts.
	// If there are state changes made *during* an update, listeners will see notifications *after* a dynamic/fixed/render
	// update has completed.
	// NOTE: For outputs, this is the way to send a changed output state block back to the underlying platform device.
	// NOTE: The system does not track for you which part of a state has changed. Code has to either establish its
	//       own logic for monitoring specific value changes or has to treat state in bulk (e.g. for output it usually
	//       makes sense just to send the entire output state to the device rather than only a single value change).
	// NOTE: The system does not track individual control value changes. It will only notify about state changes
	//       of the topmost control against which a state change event is sent.
	public delegate void InputStateChangeListener(InputControl control);
	////REVIEW: register these for specific state FourCC codes?
}