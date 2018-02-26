using System;
using ISX.LowLevel;
using ISX.Utilities;

// per device functions:
//  - update/poll
//  - IOCTL
//  - text input
//  - configuration change
//  - make current
//  - on remove (also resets current)
//
// Ideally, these would *not* be virtual methods on InputDevice but use a different process (which?)
// for associating responses with devices

namespace ISX
{
    /// <summary>
    /// The root of a control hierarchy.
    /// </summary>
    /// <remarks>
    /// Input devices act as the container for control hierarchies. Every hierarchy has to have
    /// a device at the root. Devices cannot occur inside of hierarchies.
    ///
    /// Unlike other controls, usages of InputDevices are allowed to be changed on the fly
    /// without requiring a change to the device template (<see cref="InputSystem.SetUsage"/>).
    /// </remarks>
    /// \todo The entire control hierarchy should be a linear array; transition to that with InputData.
    public class InputDevice : InputControl
    {
        public const int kInvalidDeviceId = 0;
        internal const int kInvalidDeviceIndex = -1;

        /// <summary>
        /// Generic failure code for <see cref="IOCTL"/> calls.
        /// </summary>
        /// <remarks>
        /// Any negative return value for an <see cref="IOCTL"/> call should be considered failure.
        /// </remarks>
        public const long kCommandResultFailure = -1;

        /// <summary>
        /// Metadata describing the device (product name etc.).
        /// </summary>
        /// <remarks>
        /// The description of a device is unchanging over its lifetime and does not
        /// comprise data about a device's configuration (which is considered mutable).
        /// </remarks>
        public InputDeviceDescription description
        {
            get { return m_Description; }
        }

        ////REVIEW: turn this into an object of some kind?
        ////REVIEW: on Xbox, a device can have multiple player IDs assigned to it
        /// <summary>
        /// The user currently associated with the input device or null if no user is.
        /// </summary>
        public string userId
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_UserId;
            }
            protected set { m_UserId = value; }
        }

        /// <summary>
        /// Whether the device is mirrored from a remote input system and not actually present
        /// as a "real" device in the local system.
        /// </summary>
        public bool remote
        {
            get { return (m_Flags & Flags.Remote) == Flags.Remote; }
        }

        /// <summary>
        /// Whether the device comes from the native Unity runtime.
        /// </summary>
        public bool native
        {
            get { return (m_Flags & Flags.Native) == Flags.Native; }
        }

        public bool updateBeforeRender
        {
            get { return (m_Flags & Flags.UpdateBeforeRender) == Flags.UpdateBeforeRender; }
        }

        /// <summary>
        /// Unique numeric ID for the device.
        /// </summary>
        /// <remarks>
        /// This is only assigned once a device has been added to the system. Not two devices will receive the same
        /// ID and no device will receive an ID that another device used before even if the device was removed.
        ///
        /// IDs are assigned by the input runtime.
        /// </remarks>
        /// <seealso cref="IInputRuntime.AllocateDeviceId"/>
        public int id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// Timestamp of last state event used to update the device.
        /// </summary>
        /// <remarks>
        /// Events other than <see cref="LowLevel.StateEvent"/> and <see cref="LowLevel.DeltaStateEvent"/> will
        /// not cause lastUpdateTime to be changed.
        /// </remarks>
        public double lastUpdateTime
        {
            get { return m_LastUpdateTime; }
        }

        // This has to be public for Activator.CreateInstance() to be happy.
        public InputDevice()
        {
            m_Id = kInvalidDeviceId;
            m_DeviceIndex = kInvalidDeviceIndex;
        }

        /// <summary>
        /// Make this the current device of its type.
        /// </summary>
        /// <remarks>
        /// Use this to set static properties that give fast access to the latest device used of a given
        /// type (<see cref="Gamepad.current"/> or <see cref="XRController.leftHand"/> and <see cref="XRController.rightHand"/>).
        ///
        /// This functionality is somewhat like a 'pwd' for the semantic paths but one where there can
        /// be multiple current working directories, one for each type.
        ///
        /// A device will be made current by the system initially when it is created and subsequently whenever
        /// it receives an event.
        /// </remarks>
        public virtual void MakeCurrent()
        {
        }

        ////REVIEW: should this receive a timestamp, too?
        /// <summary>
        /// Invoked when the device receive a <see cref="LowLevel.TextEvent">text input event</see>.
        /// </summary>
        /// <param name="character"></param>
        public virtual void OnTextInput(char character)
        {
        }

        /// <summary>
        /// Called by the system when the configuration of the device has changed.
        /// </summary>
        /// <seealso cref="DeviceConfigurationEvent"/>
        public virtual void OnConfigurationChanged()
        {
            // Mark all controls in the hierarchy as having their config out of date.
            // We don't want to update configuration right away but rather wait until
            // someone actually depends on it.
            m_ConfigUpToDate = false;
            for (var i = 0; i < m_ChildrenForEachControl.Length; ++i)
                m_ChildrenForEachControl[i].m_ConfigUpToDate = false;
        }

        ////REVIEW: return just bool instead of long and require everything else to go in the command?
        /// <summary>
        /// Perform a device-specific command.
        /// </summary>
        /// <param name="command">Data for the command to be performed.</param>
        /// <returns>A transfer-specific return code. Negative values are considered failure codes.</returns>
        /// <remarks>
        /// Commands allow devices to set up custom protocols without having to extend
        /// the device API. This is most useful for devices implemented in the native Unity runtime
        /// which, through the command interface, may provide custom, device-specific functions.
        ///
        /// This is a low-level API. It works in a similar way to <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa363216%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396"
        /// target="_blank">DeviceIoControl</a> on Windows and <a href="https://developer.apple.com/legacy/library/documentation/Darwin/Reference/ManPages/man2/ioctl.2.html"
        /// target="_blank">ioctl</a> on UNIX-like systems.
        /// </remarks>
        public virtual long OnDeviceCommand<TCommand>(ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            return InputRuntime.s_Runtime.DeviceCommand(id, ref command);
        }

        protected void RefreshUserId()
        {
            m_UserId = null;
            var command = QueryUserIdCommand.Create();
            if (OnDeviceCommand(ref command) > 0)
                m_UserId = command.ReadId();
        }

        [Flags]
        internal enum Flags
        {
            UpdateBeforeRender = 1 << 0,
            HasAutoResetControls = 1 << 1,////TODO: remove
            Remote = 1 << 2, // It's a local mirror of a device from a remote player connection.
            Native = 1 << 3, // It's a device created from data surfaced by NativeInputSystem.
        }

        internal Flags m_Flags;
        internal int m_Id;
        internal string m_UserId;
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
        internal InternedString[] m_AliasesForEachControl;

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

        ////TODO: output is still in the works
        // Buffer that will receive state events for output generated from this device.
        // May be shared with other devices.
        internal InputEventBuffer m_OutputBuffer;

        // NOTE: We don't store processors in a combined array the same way we do for
        //       usages and children as that would require lots of casting from 'object'.

        internal void SetUsage(InternedString usage)
        {
            // Make last entry in m_UsagesForEachControl be our device usage string.
            var numControlUsages = m_UsageToControl != null ? m_UsageToControl.Length : 0;
            Array.Resize(ref m_UsagesForEachControl, numControlUsages + 1);
            m_UsagesForEachControl[numControlUsages] = usage;
            m_UsagesReadOnly = new ReadOnlyArray<InternedString>(m_UsagesForEachControl, numControlUsages, 1);

            // Update controls to all point to new usage array.
            UpdateUsageArraysOnControls();
        }

        internal void UpdateUsageArraysOnControls()
        {
            if (m_UsageToControl == null)
                return;

            for (var i = 0; i < m_UsageToControl.Length; ++i)
                m_UsageToControl[i].m_UsagesReadOnly.m_Array = m_UsagesForEachControl;
        }
    }
}
