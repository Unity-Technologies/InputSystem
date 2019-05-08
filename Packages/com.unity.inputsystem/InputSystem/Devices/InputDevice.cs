using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Plugins.XR;

////TODO: runtime remapping of usages on a per-device basis

////TODO: finer-grained control over what devices deliver input while running in background
////      (e.g. get gamepad input but do *not* get mouse and keyboard input)

////REVIEW: should be possible to completely hijack the input stream of a device such that its original input is suppressed

////REVIEW: can we construct the control tree of devices on demand so that the user never has to pay for
////        the heap objects of devices that aren't used?

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

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// The root of a control hierarchy.
    /// </summary>
    /// <remarks>
    /// Input devices act as the container for control hierarchies. Every hierarchy has to have
    /// a device at the root. Devices cannot occur inside of hierarchies.
    ///
    /// Unlike other controls, usages of InputDevices are allowed to be changed on the fly
    /// without requiring a change to the device layout (<see cref="InputSystem.SetDeviceUsage(InputDevice,string)"/>).
    /// </remarks>
    public class InputDevice : InputControl
    {
        public const int InvalidDeviceId = 0;
        public const int LocalParticipantId = 0;
        internal const int kInvalidDeviceIndex = -1;

        /// <summary>
        /// Metadata describing the device (product name etc.).
        /// </summary>
        /// <remarks>
        /// The description of a device is unchanging over its lifetime and does not
        /// comprise data about a device's configuration (which is considered mutable).
        /// </remarks>
        public InputDeviceDescription description => m_Description;

        ////REVIEW: this might be useful even at the control level
        /// <summary>
        /// Whether the device is currently enabled (i.e. sends and receives events).
        /// </summary>
        /// <remarks>
        /// A device that is disabled will not receive events. I.e. events that are being sent to the device
        /// will be ignored.
        ///
        /// When disabling a <see cref="native"/> device, a <see cref="DisableDeviceCommand">disable command</see> will
        /// also be sent to the <see cref="IInputRuntime">runtime</see>. It depends on the specific runtime whether the
        /// device command is supported but if it is, the device will be disabled in the runtime and no longer send
        /// events. This is especially important for devices such as <see cref="Sensor">sensors</see> that incur both
        /// computation and battery consumption overhead while enabled.
        ///
        /// Specific types of devices can choose to start out in disabled state by default. This is generally the
        /// case for <see cref="Sensor">sensors</see> to ensure that their overhead is only incurred when actually
        /// being used by the application.
        /// </remarks>
        /// <seealso cref="InputSystem.EnableDevice"/>
        /// <seealso cref="InputSystem.DisableDevice"/>
        public bool enabled
        {
            get
            {
                // Fetch state from runtime, if necessary.
                if ((m_DeviceFlags & DeviceFlags.DisabledStateHasBeenQueried) != DeviceFlags.DisabledStateHasBeenQueried)
                {
                    var command = QueryEnabledStateCommand.Create();
                    if (ExecuteCommand(ref command) >= 0)
                    {
                        if (command.isEnabled)
                            m_DeviceFlags &= ~DeviceFlags.Disabled;
                        else
                            m_DeviceFlags |= DeviceFlags.Disabled;
                    }
                    else
                    {
                        // We got no response on the enable/disable state. Assume device is enabled.
                        m_DeviceFlags &= ~DeviceFlags.Disabled;
                    }

                    // Only fetch enable/disable state again if we get a configuration change event.
                    m_DeviceFlags |= DeviceFlags.DisabledStateHasBeenQueried;
                }

                return (m_DeviceFlags & DeviceFlags.Disabled) != DeviceFlags.Disabled;
            }
        }

        /// <summary>
        /// If true, the device is capable of delivering input while the application is running in the background.
        /// </summary>
        /// <remarks>
        /// Note that by default, even if capable of doing so, devices will not generate input while the application is not
        /// focused. To enable the behavior, use <see cref="InputSettings.runInBackground"/>.
        /// </remarks>
        /// <seealso cref="InputSettings.runInBackground"/>
        public bool canRunInBackground
        {
            get
            {
#if UNITY_2019_1_OR_NEWER
                var command = QueryCanRunInBackground.Create();
                if (ExecuteCommand(ref command) >= 0)
                {
                    return command.canRunInBackground;
                }
#endif
                return false;
            }
        }

        /// <summary>
        /// Whether the device has been added to the system.
        /// </summary>
        /// <remarks>
        /// Input devices can be constructed manually through <see cref="InputDeviceBuilder"/>. Also,
        /// they can be removed through <see cref="InputSystem.RemoveDevice"/>. This property reflects
        /// whether the device is currently added to the system.
        ///
        /// Note that devices in <see cref="InputSystem.disconnectedDevices"/> will all have this
        /// property return false.
        /// </remarks>
        /// <seealso cref="InputSystem.AddDevice(InputDevice)"/>
        /// <seealso cref="InputSystem.devices"/>
        public bool added => m_DeviceIndex != kInvalidDeviceIndex;

        /// <summary>
        /// Whether the device is mirrored from a remote input system and not actually present
        /// as a "real" device in the local system.
        /// </summary>
        /// <seealso cref="InputSystem.remoting"/>
        /// <seealso cref="InputRemoting"/>
        public bool remote => (m_DeviceFlags & DeviceFlags.Remote) == DeviceFlags.Remote;

        /// <summary>
        /// Whether the device comes from the <see cref="IInputRuntime">runtime</see>
        /// </summary>
        /// <remarks>
        /// Devices can be discovered when <see cref="IInputRuntime.onDeviceDiscovered">reported</see>
        /// by the runtime or they can be added manually through the various <see cref="InputSystem.AddDevice(InputDevice)">
        /// AddDevice</see> APIs. Devices reported by the runtime will return true for this
        /// property whereas devices added manually will return false.
        ///
        /// Devices reported by the runtime will usually come from the Unity engine itself.
        /// </remarks>
        /// <seealso cref="IInputRuntime"/>
        /// <seealso cref="IInputRuntime.onDeviceDiscovered"/>
        public bool native => (m_DeviceFlags & DeviceFlags.Native) == DeviceFlags.Native;

        /// <summary>
        /// Whether the device requires an extra update before rendering.
        /// </summary>
        /// <remarks>
        /// The value of this property is determined by <see cref="InputControlLayout.updateBeforeRender"/> in
        /// the device's <see cref="InputControlLayout">control layout</see>.
        ///
        /// The extra update is necessary for tracking devices that are used in rendering code. For example,
        /// the eye transforms of an HMD should be refreshed right before rendering as refreshing only in the
        /// beginning of the frame will lead to a noticeable lag.
        /// </remarks>
        /// <seealso cref="InputUpdateType.BeforeRender"/>
        public bool updateBeforeRender => (m_DeviceFlags & DeviceFlags.UpdateBeforeRender) == DeviceFlags.UpdateBeforeRender;

        /// <summary>
        /// Unique numeric ID for the device.
        /// </summary>
        /// <remarks>
        /// This is only assigned once a device has been added to the system. No two devices will receive the same
        /// ID and no device will receive an ID that another device used before even if the device was removed.
        ///
        /// IDs are assigned by the input runtime.
        /// </remarks>
        /// <seealso cref="IInputRuntime.AllocateDeviceId"/>
        public int id => m_Id;

        /// <summary>
        /// Timestamp of last state event used to update the device.
        /// </summary>
        /// <remarks>
        /// Events other than <see cref="LowLevel.StateEvent"/> and <see cref="LowLevel.DeltaStateEvent"/> will
        /// not cause lastUpdateTime to be changed.
        /// </remarks>
        public double lastUpdateTime => m_LastUpdateTimeInternal - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

        public bool wasUpdatedThisFrame
        {
            get
            {
                var updateType = InputUpdate.s_LastUpdateType;
                if (updateType == InputUpdateType.Dynamic || updateType == InputUpdateType.BeforeRender)
                    return m_CurrentDynamicUpdateCount == InputUpdate.s_DynamicUpdateCount;
                if (updateType == InputUpdateType.Fixed)
                    return m_CurrentFixedUpdateCount == InputUpdate.s_FixedUpdateCount;

                ////REVIEW: how should this behave in the editor
                return false;
            }
        }

        /// <summary>
        /// A flattened list of controls that make up the device.
        /// </summary>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputControl> allControls
        {
            get
            {
                // Since m_ChildrenForEachControl contains the device's children as well as the children
                // of each control in the hierarchy, and since each control can only have a single parent,
                // this list will actually deliver a flattened list of all controls in the hierarchy (and without
                // the device itself being listed).
                return new ReadOnlyArray<InputControl>(m_ChildrenForEachControl);
            }
        }

        ////REVIEW: This violates the constraint of controls being required to not have reference types as value types.
        public override Type valueType => typeof(byte[]);

        public override int valueSizeInBytes => (int)m_StateBlock.alignedSizeInBytes;

        /// <summary>
        /// Return all input devices currently added to the system.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <see cref="InputSystem.devices"/>.
        /// </remarks>
        public static ReadOnlyArray<InputDevice> all => InputSystem.devices;

        // This has to be public for Activator.CreateInstance() to be happy.
        public InputDevice()
        {
            m_Id = InvalidDeviceId;
            m_ParticipantId = LocalParticipantId;
            m_DeviceIndex = kInvalidDeviceIndex;
        }

        ////REVIEW: Is making devices be byte[] values really all that useful? Seems better than returning nulls but
        ////        at the same time, seems questionable.

        public override unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize)
        {
            throw new NotImplementedException();
        }

        public override unsafe object ReadValueFromStateAsObject(void* statePtr)
        {
            if (m_DeviceIndex == kInvalidDeviceIndex)
                return null;

            var numBytes = stateBlock.alignedSizeInBytes;
            var array = new byte[numBytes];
            fixed(byte* arrayPtr = array)
            {
                var adjustedStatePtr = (byte*)statePtr + m_StateBlock.byteOffset;
                UnsafeUtility.MemCpy(arrayPtr, adjustedStatePtr, numBytes);
            }

            return array;
        }

        public override unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize)
        {
            if (statePtr == null)
                throw new ArgumentNullException("statePtr");
            if (bufferPtr == null)
                throw new ArgumentNullException("bufferPtr");
            if (bufferSize < valueSizeInBytes)
                throw new ArgumentException(string.Format("Buffer tool small (expected: {0}, actual: {1}",
                    valueSizeInBytes, bufferSize));

            var adjustedStatePtr = (byte*)statePtr + m_StateBlock.byteOffset;
            UnsafeUtility.MemCpy(bufferPtr, adjustedStatePtr, m_StateBlock.alignedSizeInBytes);
        }

        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            if (firstStatePtr == null)
                throw new ArgumentNullException("firstStatePtr");
            if (secondStatePtr == null)
                throw new ArgumentNullException("secondStatePtr");

            var adjustedFirstStatePtr = (byte*)firstStatePtr + m_StateBlock.byteOffset;
            var adjustedSecondStatePtr = (byte*)firstStatePtr + m_StateBlock.byteOffset;

            return UnsafeUtility.MemCmp(adjustedFirstStatePtr, adjustedSecondStatePtr,
                m_StateBlock.alignedSizeInBytes) == 0;
        }

        /// <summary>
        /// Called by the system when the configuration of the device has changed.
        /// </summary>
        /// <seealso cref="DeviceConfigurationEvent"/>
        internal void OnConfigurationChanged()
        {
            // Mark all controls in the hierarchy as having their config out of date.
            // We don't want to update configuration right away but rather wait until
            // someone actually depends on it.
            isConfigUpToDate = false;
            for (var i = 0; i < m_ChildrenForEachControl.Length; ++i)
                m_ChildrenForEachControl[i].isConfigUpToDate = false;

            // Make sure we fetch the enabled/disabled state again.
            m_DeviceFlags &= ~DeviceFlags.DisabledStateHasBeenQueried;
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

        protected virtual void OnAdded()
        {
        }

        protected virtual void OnRemoved()
        {
        }

        ////TODO: this should be overridable directly on the device in some form; can't be virtual because of AOT problems; need some other solution
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
        public unsafe long ExecuteCommand<TCommand>(ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            // Give callbacks first shot.
            var manager = InputSystem.s_Manager;
            var callbacks = manager.m_DeviceCommandCallbacks;
            for (var i = 0; i < callbacks.length; ++i)
            {
                var result = callbacks[i](this, (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command));
                if (result.HasValue)
                    return result.Value;
            }

            return InputRuntime.s_Instance.DeviceCommand(id, ref command);
        }

        [Flags]
        internal enum DeviceFlags
        {
            UpdateBeforeRender = 1 << 0,
            HasStateCallbacks = 1 << 1,
            HasControlsWithDefaultState = 1 << 2,
            Remote = 1 << 3, // It's a local mirror of a device from a remote player connection.
            Native = 1 << 4, // It's a device created from data surfaced by NativeInputRuntime.
            Disabled = 1 << 5,
            DisabledStateHasBeenQueried = 1 << 6, // Whether we have fetched the current enable/disable state from the runtime.
        }

        internal DeviceFlags m_DeviceFlags;
        internal int m_Id;
        internal int m_ParticipantId;
        internal int m_DeviceIndex; // Index in InputManager.m_Devices.
        internal InputDeviceDescription m_Description;

        /// <summary>
        /// Timestamp of last event we received.
        /// </summary>
        /// <seealso cref="InputEvent.time"/>
        internal double m_LastUpdateTimeInternal;

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

        // NOTE: We don't store processors in a combined array the same way we do for
        //       usages and children as that would require lots of casting from 'object'.

        /// <summary>
        /// If true, the device has at least one control that has an explicit default state.
        /// </summary>
        internal bool hasControlsWithDefaultState
        {
            get { return (m_DeviceFlags & DeviceFlags.HasControlsWithDefaultState) == DeviceFlags.HasControlsWithDefaultState; }
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasControlsWithDefaultState;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasControlsWithDefaultState;
            }
        }

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

        internal void NotifyAdded()
        {
            OnAdded();
        }

        internal void NotifyRemoved()
        {
            OnRemoved();
        }
    }
}
