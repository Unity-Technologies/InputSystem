namespace InputSystem
{
    // Input devices bundle complex state and usually correlate to ...
    public abstract class InputDevice : InputControl
    {
        public const int kInvalidDeviceId = 0;
        public const int kMaxDeviceId = 256;

	    ////REVIEW: store descriptor on device instead?
        public string product { get; set; }
        public string manufacturer { get; set; }
        public string serial { get; set; }
	    
	    ////REVIEW: move to descriptor?
	    // Systems that support multiple concurrent player inputs on the same system, the available
	    // player inputs are usually numbered. For example, on a console the gamepads slots on the system
	    // will be numbered and associated with gamepads. This number corresponds to the system assigned
	    // player index for the device.
	    public int playerIndex
	    {
		    get { return m_PlayerIndex; }
	    }

	    // Whether the device is currently connected.
	    // If you want to listen for state changes, hook into InputManager.onDeviceChange.
	    public bool connected
	    {
		    get { return m_Connected; }
	    }

	    ////REVIEW: this sort of becomes the index of the root node
        // Every registered device in the system gets a unique numeric ID.
        // For native devices, this is assigned by the underlying runtime.
	    public int deviceId
	    {
		    get { return m_DeviceId; }
	    }

		// Make this the current device of its type.
        // Use this to set static properties that give fast access to the latest device used of a given
	    // type (see Gamepad.current).
	    // This functionality is sort of like a 'pwd' for the semantic paths but one where there can
	    // be multiple current working directories, one for each type.
		public virtual void MakeCurrent()
		{
		}

	    protected InputDevice(string name)
		    : base(name)
	    {
	    }
	    
	    internal bool m_Connected;
	    internal int m_PlayerIndex;
	    internal int m_DeviceId;
    }
}