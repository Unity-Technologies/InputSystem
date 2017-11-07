using System;

////TODO: the entire control hierarchy should be a linear array; transition to that with InputData

namespace ISX
{
    // Input devices are the roots of control hierarchies.
    // Unlike other controls, usages of InputDevices are allowed to be changed on the fly
    // without requiring a change to the device template.
    public class InputDevice : InputControl
    {
        public const int kInvalidDeviceId = 0;
        public const int kMaxDeviceId = 256;
        internal const int kInvalidDeviceIndex = -1;

        public InputDeviceDescription description => m_Description;

        ////REVIEW: move to descriptor?
        // Systems that support multiple concurrent player inputs on the same system, the available
        // player inputs are usually numbered. For example, on a console the gamepads slots on the system
        // will be numbered and associated with gamepads. This number corresponds to the system assigned
        // player index for the device.
        public int playerId => m_PlayerId;

        // Whether the device is currently connected.
        // If you want to listen for state changes, hook into InputManager.onDeviceChange.
        public bool connected => (m_Flags & Flags.Connected) == Flags.Connected;

        public bool updateBeforeRender => (m_Flags & Flags.UpdateBeforeRender) == Flags.UpdateBeforeRender;

        // Every registered device in the system gets a unique numeric ID.
        // For native devices, this is assigned by the underlying runtime.
        public int id => m_Id;

        // Timestamp of last state event used to update the device.
        public double lastUpdateTime => m_LastUpdateTime;

        // Make this the current device of its type.
        // Use this to set static properties that give fast access to the latest device used of a given
        // type (see Gamepad.current).
        // This functionality is sort of like a 'pwd' for the semantic paths but one where there can
        // be multiple current working directories, one for each type.
        public virtual void MakeCurrent()
        {
        }

        // This has to be public for Activator.CreateInstance() to be happy.
        public InputDevice()
        {
            m_Id = kInvalidDeviceId;
            m_DeviceIndex = kInvalidDeviceIndex;
        }

        [Flags]
        internal enum Flags
        {
            Connected = 1 << 0,
            UpdateBeforeRender = 1 << 1,
            HasAutoResetControls = 1 << 2,
        }

        internal Flags m_Flags;
        internal int m_Id;
        internal int m_PlayerId;////TODO: move to desc
        internal int m_DeviceIndex; // Index in InputManager.m_Devices.
        internal InputDeviceDescription m_Description;

        // Time of last event we received.
        internal double m_LastUpdateTime;

        // The dynamic and fixed update count corresponding to the current
        // front buffers that are active on the device. We use this to know
        // when to flip buffers.
        internal uint m_CurrentDynamicUpdateCount;
        internal uint m_CurrentFixedUpdateCount;

        // List of aliases for all controls. Each control gets a slice of this array.
        // See 'InputControl.aliases'.
        // NOTE: The device's own aliases are part of this array as well.
        internal string[] m_AliasesForEachControl;

        // List of usages for all controls. Each control gets a slice of this array.
        // See 'InputControl.usages'.
        // NOTE: The device's own usages are part of this array as well. They are always
        //       at the *end* of the array.
        internal InternedString[] m_UsagesForEachControl;
        internal InputControl[] m_UsageToControl;

        // List of children for all controls. Each control gets a slice of this array.
        // See 'InputControl.children'.
        // NOTE: The device's own children are part of this array as well.
        internal InputControl[] m_ChildrenForEachControl;

        // List of state blocks in this device that require automatic resetting
        // between frames.
        internal InputStateBlock[] m_AutoResetStateBlocks;

        // NOTE: We don't store processors in an combined array the same way we do for
        //       usages and children as that would require lots of casting from 'object'.

        internal void SetUsage(InternedString usage)
        {
            // Make last entry in m_UsagesForEachControl be our device usage string.
            var numControlUsages = m_UsageToControl?.Length ?? 0;
            Array.Resize(ref m_UsagesForEachControl, numControlUsages + 1);
            m_UsagesForEachControl[numControlUsages] = usage;
            m_UsagesReadOnly = new ReadOnlyArray<InternedString>(m_UsagesForEachControl, numControlUsages, 1);

            // Update controls to all point to new usage array.
            if (m_UsageToControl != null)
            {
                for (var i = 0; i < m_UsageToControl.Length; ++i)
                    m_UsageToControl[i].m_UsagesReadOnly.m_Array = m_UsagesForEachControl;
            }
        }
    }
}
