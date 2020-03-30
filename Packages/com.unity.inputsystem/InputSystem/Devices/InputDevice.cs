using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

////TODO: runtime remapping of control usages on a per-device basis

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
    /// Represents an input device which is always the root of a hierarchy of <see cref="InputControl"/> instances.
    /// </summary>
    /// <remarks>
    /// Input devices act as the container for control hierarchies. Every hierarchy has to have
    /// a device at the root. Devices cannot occur as children of other controls.
    ///
    /// Devices are usually created automatically in response to hardware being discovered by the Unity
    /// runtime. However, it is possible to manually add devices using methods such as <see
    /// cref="InputSystem.AddDevice{TDevice}(string)"/>.
    ///
    /// <example>
    /// <code>
    /// // Add a "synthetic" gamepad that isn't actually backed by hardware.
    /// var gamepad = InputSystem.AddDevice&lt;Gamepad&gt;();
    /// </code>
    /// </example>
    ///
    /// There are subclasses representing the most common types of devices, like <see cref="Mouse"/>,
    /// <see cref="Keyboard"/>, <see cref="Gamepad"/>, and <see cref="Touchscreen"/>.
    ///
    /// To create your own types of devices, you can derive from InputDevice and register your device
    /// as a new "layout".
    ///
    /// <example>
    /// <code>
    /// // InputControlLayoutAttribute attribute is only necessary if you want
    /// // to override default behavior that occurs when registering your device
    /// // as a layout.
    /// // The most common use of InputControlLayoutAttribute is to direct the system
    /// // to a custom "state struct" through the `stateType` property. See below for details.
    /// [InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
    /// #if UNITY_EDITOR
    /// [InitializeOnLoad]
    /// #endif
    /// public class MyDevice : InputDevice
    /// {
    ///     public ButtonControl button { get; private set; }
    ///     public AxisControl axis { get; private set; }
    ///
    ///     // Register the device.
    ///     static MyDevice()
    ///     {
    ///         // In case you want instance of your device to automatically be created
    ///         // when specific hardware is detected by the Unity runtime, you have to
    ///         // add one or more "device matchers" (InputDeviceMatcher) for the layout.
    ///         // These matchers are compared to an InputDeviceDescription received from
    ///         // the Unity runtime when a device is connected. You can add them either
    ///         // using InputSystem.RegisterLayoutMatcher() or by directly specifying a
    ///         // matcher when registering the layout.
    ///         InputSystem.RegisterLayout&lt;MyDevice&gt;(
    ///             // For the sake of demonstration, let's assume your device is a HID
    ///             // and you want to match by PID and VID.
    ///             matches: new InputDeviceMatcher()
    ///                 .WithInterface("HID")
    ///                 .WithCapability("PID", 1234)
    ///                 .WithCapability("VID", 5678));
    ///     }
    ///
    ///     // This is only to trigger the static class constructor to automatically run
    ///     // in the player.
    ///     [RuntimeInitializeOnLoadMethod]
    ///     private static void InitializeInPlayer() {}
    ///
    ///     protected override void FinishSetup()
    ///     {
    ///         base.FinishSetup();
    ///         button = GetChildControl&lt;ButtonControl&gt;("button");
    ///         axis = GetChildControl&lt;AxisControl&gt;("axis");
    ///     }
    /// }
    ///
    /// // A "state struct" describes the memory format used by a device. Each device can
    /// // receive and store memory in its custom format. InputControls are then connected
    /// // the individual pieces of memory and read out values from them.
    /// [StructLayout(LayoutKind.Explicit, Size = 32)]
    /// public struct MyDeviceState : IInputStateTypeInfo
    /// {
    ///     // In the case of a HID (which we assume for the sake of this demonstration),
    ///     // the format will be "HID". In practice, the format will depend on how your
    ///     // particular device is connected and fed into the input system.
    ///     // The format is a simple FourCC code that "tags" state memory blocks for the
    ///     // device to give a base level of safety checks on memory operations.
    ///     public FourCC format => return new FourCC('H', 'I', 'D');
    ///
    ///     // InputControlAttributes on fields tell the input system to create controls
    ///     // for the public fields found in the struct.
    ///
    ///     // Assume a 16bit field of buttons. Create one button that is tied to
    ///     // bit #3 (zero-based). Note that buttons do not need to be stored as bits.
    ///     // They can also be stored as floats or shorts, for example.
    ///     [InputControl(name = "button", layout = "Button", bit = 3)]
    ///     public ushort buttons;
    ///
    ///     // Create a floating-point axis. The name, if not supplied, is taken from
    ///     // the field.
    ///     [InputControl(layout = "Axis")]
    ///     public short axis;
    /// }
    /// </code>
    /// </example>
    ///
    /// Devices can have usages like any other control (<see cref="InputControl.usages"/>). Unlike other controls,
    /// however, usages of InputDevices are allowed to be changed on the fly without requiring a change to the
    /// device layout (see <see cref="InputSystem.SetDeviceUsage(InputDevice,string)"/>).
    ///
    /// For a more complete example of how to implement custom input devices, check out the "Custom Device"
    /// sample which you can install from the Unity package manager.
    ///
    /// And, as always, you can also find more information in the <a href="../manual/Devices.html">manual</a>.
    /// </remarks>
    /// <seealso cref="InputControl"/>
    /// <seealso cref="Mouse"/>
    /// <seealso cref="Keyboard"/>
    /// <seealso cref="Gamepad"/>
    /// <seealso cref="Touchscreen"/>
    [Scripting.Preserve]
    public class InputDevice : InputControl
    {
        /// <summary>
        /// Value of an invalid <see cref="deviceId"/>.
        /// </summary>
        /// <remarks>
        /// The input system will not assigned this ID to any device.
        /// </remarks>
        public const int InvalidDeviceId = 0;

        internal const int kLocalParticipantId = 0;
        internal const int kInvalidDeviceIndex = -1;

        /// <summary>
        /// Metadata describing the device (product name etc.).
        /// </summary>
        /// <remarks>
        /// The description of a device is unchanging over its lifetime and does not
        /// comprise data about a device's configuration (which is considered mutable).
        ///
        /// In most cases, the description for a device is supplied by the Unity runtime.
        /// This it the case for all <see cref="native"/> input devices. However, it is
        /// also possible to inject new devices in the form of device descriptions into
        /// the system using <see cref="InputSystem.AddDevice(InputDeviceDescription)"/>.
        ///
        /// The description of a device is what is matched by an <see cref="InputDeviceMatcher"/>
        /// to find the <see cref="InputControl.layout"/> to use for a device.
        /// </remarks>
        public InputDeviceDescription description => m_Description;

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
                if ((m_DeviceFlags & DeviceFlags.DisabledStateHasBeenQueried) == 0)
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

        ////TODO: rename this to canReceiveInputInBackground
        /// <summary>
        /// If true, the device is capable of delivering input while the application is running in the background, i.e.
        /// while <c>Application.isFocused</c> is false.
        /// </summary>
        /// <value>Whether the device can generate input while in the background.</value>
        /// <remarks>
        /// Note that processing input in the background requires <c>Application.runInBackground</c> to be enabled in the
        /// player preferences. If this is enabled, the input system will keep running by virtue of being part of the Unity
        /// player loop which will keep running in the background. Note, however, that this does not necessarily mean that
        /// the application will necessarily receive input.
        ///
        /// Only a select set of hardware, platform, and SDK/API combinations support gathering input while not having
        /// input focus. The most notable set of devices are HMDs and VR controllers.
        ///
        /// The value of this property is determined by sending <see cref="QueryCanRunInBackground"/> to the device.
        /// </remarks>
        public bool canRunInBackground
        {
            get
            {
                var command = QueryCanRunInBackground.Create();
                if (ExecuteCommand(ref command) >= 0)
                    return command.canRunInBackground;
                return false;
            }
        }

        /// <summary>
        /// Whether the device has been added to the system.
        /// </summary>
        /// <value>If true, the device is currently among the devices in <see cref="InputSystem.devices"/>.</value>
        /// <remarks>
        /// Devices may be removed at any time. Either when their hardware is unplugged or when they
        /// are manually removed through <see cref="InputSystem.RemoveDevice"/> or by being excluded
        /// through <see cref="InputSettings.supportedDevices"/>. When a device is removed, its instance,
        /// however, will not disappear. This property can be used to check whether the device is part
        /// of the current set of active devices.
        /// </remarks>
        /// <seealso cref="InputSystem.devices"/>
        public bool added => m_DeviceIndex != kInvalidDeviceIndex;

        /// <summary>
        /// Whether the device is mirrored from a remote input system and not actually present
        /// as a "real" device in the local system.
        /// </summary>
        /// <value>Whether the device mirrors a device from a remotely connected input system.</value>
        /// <seealso cref="InputSystem.remoting"/>
        /// <seealso cref="InputRemoting"/>
        public bool remote => (m_DeviceFlags & DeviceFlags.Remote) == DeviceFlags.Remote;

        /// <summary>
        /// Whether the device comes from the <see cref="IInputRuntime">runtime</see>
        /// </summary>
        /// <value>Whether the device has been discovered by the Unity runtime.</value>
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
        /// ID and no device will receive an ID that another device used before even if the device was removed. The
        /// only exception to this is if a device gets re-created as part of a layout change. For example, if a new
        /// layout is registered that replaces the <see cref="Mouse"/> layout, all <see cref="Mouse"/> devices will
        /// get recreated but will keep their existing device IDs.
        ///
        /// IDs are assigned by the input runtime.
        /// </remarks>
        /// <seealso cref="IInputRuntime.AllocateDeviceId"/>
        public int deviceId => m_DeviceId;

        /// <summary>
        /// Timestamp of last state event used to update the device.
        /// </summary>
        /// <remarks>
        /// Events other than <see cref="LowLevel.StateEvent"/> and <see cref="LowLevel.DeltaStateEvent"/> will
        /// not cause lastUpdateTime to be changed.
        /// </remarks>
        public double lastUpdateTime => m_LastUpdateTimeInternal - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

        public bool wasUpdatedThisFrame => m_CurrentUpdateStepCount == InputUpdate.s_UpdateStepCount;

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
        /// <inheritdoc/>
        public override Type valueType => typeof(byte[]);

        /// <inheritdoc/>
        public override int valueSizeInBytes => (int)m_StateBlock.alignedSizeInBytes;

        // This one just leads to confusion as you can access it from subclasses and then be surprised
        // that it doesn't only include members of those classes.
        [Obsolete("Use 'InputSystem.devices' instead. (UnityUpgradable) -> InputSystem.devices", error: false)]
        public static ReadOnlyArray<InputDevice> all => InputSystem.devices;

        /// <summary>
        /// This constructor is public for the sake of <c>Activator.CreateInstance</c> only. To construct
        /// devices, use methods such as <see cref="InputSystem.AddDevice{TDevice}(string)"/>. Manually
        /// using <c>new</c> on InputDevice will not result in a usable device.
        /// </summary>
        public InputDevice()
        {
            m_DeviceId = InvalidDeviceId;
            m_ParticipantId = kLocalParticipantId;
            m_DeviceIndex = kInvalidDeviceIndex;
        }

        ////REVIEW: Is making devices be byte[] values really all that useful? Seems better than returning nulls but
        ////        at the same time, seems questionable.

        /// <inheritdoc/>
        public override unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            if (bufferPtr == null)
                throw new ArgumentNullException(nameof(bufferPtr));
            if (bufferSize < valueSizeInBytes)
                throw new ArgumentException($"Buffer too small (expected: {valueSizeInBytes}, actual: {bufferSize}");

            var adjustedStatePtr = (byte*)statePtr + m_StateBlock.byteOffset;
            UnsafeUtility.MemCpy(bufferPtr, adjustedStatePtr, m_StateBlock.alignedSizeInBytes);
        }

        /// <inheritdoc/>
        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            if (firstStatePtr == null)
                throw new ArgumentNullException(nameof(firstStatePtr));
            if (secondStatePtr == null)
                throw new ArgumentNullException(nameof(secondStatePtr));

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
        /// This method is called automatically by the input system when a device is
        /// added or when input is received on it. Many types of devices have <c>.current</c>
        /// getters that allow querying the last used device of a specific type directly (for
        /// example, see <see cref="Gamepad.current"/>).
        ///
        /// There is one special case, however, related to noise. A device that has noisy controls
        /// (i.e. controls for which <see cref="InputControl.noisy"/> is true) may receive input events
        /// that contain no meaningful user interaction but are simply just noise from the device. A
        /// good example of this is the PS4 gamepad which has a built-in gyro and may thus constantly
        /// feed events into the input system even if not being actually in use. If, for example, an
        /// Xbox gamepad and PS4 gamepad are both connected to a PC and the user is playing with the
        /// Xbox gamepad, the PS4 gamepad would still constantly make itself <see cref="Gamepad.current"/>
        /// by simply flooding the system with events.
        ///
        /// By enabling <see cref="InputSettings.filterNoiseOnCurrent"/> (disabled by default),
        /// noise on <c>.current</c> getters will be filtered out and a device will only see <c>MakeCurrent</c>
        /// getting called if there input was detected on non-noisy controls.
        /// </remarks>
        /// <seealso cref="InputSettings.filterNoiseOnCurrent"/>
        /// <seealso cref="Pointer.current"/>
        /// <seealso cref="Gamepad.current"/>
        /// <seealso cref="Mouse.current"/>
        /// <seealso cref="Pen.current"/>
        public virtual void MakeCurrent()
        {
        }

        /// <summary>
        /// Called by the system when the device is added to <see cref="InputSystem.devices"/>.
        /// </summary>
        /// <remarks>
        /// This is called <em>after</em> the device has already been added.
        /// </remarks>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="InputDeviceChange.Added"/>
        /// <seealso cref="OnRemoved"/>
        protected virtual void OnAdded()
        {
        }

        /// <summary>
        /// Called by the system when the device is removed from <see cref="InputSystem.devices"/>.
        /// </summary>
        /// <remarks>
        /// This is called <em>after</em> the device has already been removed.
        /// </remarks>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="InputDeviceChange.Removed"/>
        /// <seealso cref="OnRemoved"/>
        protected virtual void OnRemoved()
        {
        }

        ////TODO: add overridable OnDisable/OnEnable that fire the device commands

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

            return InputRuntime.s_Instance.DeviceCommand(deviceId, ref command);
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
        internal int m_DeviceId;
        internal int m_ParticipantId;
        internal int m_DeviceIndex; // Index in InputManager.m_Devices.
        internal InputDeviceDescription m_Description;

        /// <summary>
        /// Timestamp of last event we received.
        /// </summary>
        /// <seealso cref="InputEvent.time"/>
        internal double m_LastUpdateTimeInternal;

        // Update count corresponding to the current front buffers that are active on the device.
        // We use this to know when to flip buffers.
        internal uint m_CurrentUpdateStepCount;

        // List of aliases for all controls. Each control gets a slice of this array.
        // See 'InputControl.aliases'.
        // NOTE: The device's own aliases are part of this array as well.
        internal InternedString[] m_AliasesForEachControl;

        // List of usages for all controls. Each control gets a slice of this array.
        // See 'InputControl.usages'.
        // NOTE: The device's own usages are part of this array as well. They are always
        //       at the *end* of the array.
        internal InternedString[] m_UsagesForEachControl;
        // This one does NOT contain the device itself, i.e. it only contains controls on the device
        // and may this be shorter than m_UsagesForEachControl.
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
            get => (m_DeviceFlags & DeviceFlags.HasControlsWithDefaultState) == DeviceFlags.HasControlsWithDefaultState;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasControlsWithDefaultState;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasControlsWithDefaultState;
            }
        }

        internal bool hasStateCallbacks
        {
            get => (m_DeviceFlags & DeviceFlags.HasStateCallbacks) == DeviceFlags.HasStateCallbacks;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasStateCallbacks;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasStateCallbacks;
            }
        }

        internal void AddDeviceUsage(InternedString usage)
        {
            var controlUsageCount = m_UsageToControl.LengthSafe();
            var totalUsageCount = controlUsageCount + m_UsageCount;
            if (m_UsageCount == 0)
                m_UsageStartIndex = totalUsageCount;
            ArrayHelpers.AppendWithCapacity(ref m_UsagesForEachControl, ref totalUsageCount, usage);
            ++m_UsageCount;
        }

        internal void RemoveDeviceUsage(InternedString usage)
        {
            var controlUsageCount = m_UsageToControl.LengthSafe();
            var totalUsageCount = controlUsageCount + m_UsageCount;

            var index = ArrayHelpers.IndexOfValue(m_UsagesForEachControl, usage, m_UsageStartIndex, totalUsageCount);
            if (index == -1)
                return;

            Debug.Assert(m_UsageCount > 0);
            ArrayHelpers.EraseAtWithCapacity(m_UsagesForEachControl, ref totalUsageCount, index);
            --m_UsageCount;

            if (m_UsageCount == 0)
                m_UsageStartIndex = default;
        }

        internal void ClearDeviceUsages()
        {
            for (var i = m_UsageStartIndex; i < m_UsageCount; ++i)
                m_UsagesForEachControl[i] = default;
            m_UsageCount = default;
        }

        internal bool RequestReset()
        {
            var resetCommand = RequestResetCommand.Create();
            var result = device.ExecuteCommand(ref resetCommand);
            return result >= 0;
        }

        internal void NotifyAdded()
        {
            OnAdded();
        }

        internal void NotifyRemoved()
        {
            OnRemoved();
        }

        internal static TDevice Build<TDevice>(string layoutName = default, string layoutVariants = default, InputDeviceDescription deviceDescription = default)
            where TDevice : InputDevice
        {
            if (string.IsNullOrEmpty(layoutName))
            {
                layoutName = InputControlLayout.s_Layouts.TryFindLayoutForType(typeof(TDevice));
                if (string.IsNullOrEmpty(layoutName))
                    layoutName = typeof(TDevice).Name;
            }

            using (InputDeviceBuilder.Ref())
            {
                InputDeviceBuilder.instance.Setup(new InternedString(layoutName), new InternedString(layoutVariants),
                    deviceDescription: deviceDescription);
                var device = InputDeviceBuilder.instance.Finish();
                if (!(device is TDevice deviceOfType))
                    throw new ArgumentException(
                        $"Expected device of type '{typeof(TDevice).Name}' but got device of type '{device.GetType().Name}' instead",
                        "TDevice");

                return deviceOfType;
            }
        }
    }
}
