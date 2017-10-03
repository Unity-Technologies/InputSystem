namespace ISX
{
    // Input devices bundle complex state and usually correlate to ...
    public class InputDevice : InputControl
    {
        public const int kInvalidDeviceId = 0;
        public const int kMaxDeviceId = 256;

        public InputDeviceDescriptor descriptor
        {
            get { return m_Descriptor; }
        }

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
        public int id
        {
            get { return m_Id; }
        }

        public InputDevice()
        {
            m_StateBlock.byteOffset = 0;
        }

        // Make this the current device of its type.
        // Use this to set static properties that give fast access to the latest device used of a given
        // type (see Gamepad.current).
        // This functionality is sort of like a 'pwd' for the semantic paths but one where there can
        // be multiple current working directories, one for each type.
        public virtual void MakeCurrent()
        {
        }

        internal bool m_Connected;
        internal int m_PlayerIndex;
        internal int m_Id;
        internal InputDeviceDescriptor m_Descriptor;

        // Where our state data starts in the global state buffers.
        // NOTE: This is baked into the InputStateBlock of each control of the device. We remember
        //       it here for when we need to re-allocate state buffers.
        internal uint m_StateBufferOffset;

        // List of usages for all controls. Each control gets a slice of this array.
        // See 'InputControl.usages'.
        // NOTE: The device's own usages are part of this array as well.
        internal InputUsage[] m_UsagesForEachControl;

        // List of children for all controls. Each control gets a slice of this array.
        // See 'InputControl.children'.
        // NOTE: The device's own children are part of this array as well.
        internal InputControl[] m_ChildrenForEachControl;

        // NOTE: We don't store processors in an combined array the same way we do for
        //       usages and children as that would require lots of casting from 'object'.

        ////REVIEW: should this have dictionaries for paths and usages?
    }
}
