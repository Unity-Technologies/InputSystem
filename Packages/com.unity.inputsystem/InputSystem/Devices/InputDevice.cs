using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

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
    ///     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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

        ////REVIEW: When we can break the API, probably makes sense to replace this single bool with one for sending and one for receiving events
        /// <summary>
        /// Whether the device is currently enabled (that is, sends and receives events).
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
                #if UNITY_EDITOR
                if (InputState.currentUpdateType == InputUpdateType.Editor && (m_DeviceFlags & DeviceFlags.DisabledWhileInBackground) != 0)
                    return true;
                #endif

                if ((m_DeviceFlags & (DeviceFlags.DisabledInFrontend | DeviceFlags.DisabledWhileInBackground)) != 0)
                    return false;

                return QueryEnabledStateFromRuntime();
            }
        }

        ////TODO: rename this to canReceiveInputInBackground (once we can break API)
        /// <summary>
        /// If true, the device is capable of delivering input while the application is running in the background, i.e.
        /// while <c>Application.isFocused</c> is false.
        /// </summary>
        /// <value>Whether the device can generate input while in the background.</value>
        /// <remarks>
        /// The value of this property is determined by three separator factors.
        ///
        /// For one, <see cref="native"/> devices have an inherent value for this property that can be retrieved through
        /// <see cref="QueryCanRunInBackground"/>. This determines whether at the input collection level, the device is
        /// capable of producing input independent of application. This is rare and only a select set of hardware, platform,
        /// and SDK/API combinations support this. The prominent class of input devices that in general do support this
        /// behavior are VR devices.
        ///
        /// Furthermore, the property may be force-set through a device's <see cref="InputControl.layout"/> by
        /// means of <see cref="InputControlLayout.canRunInBackground"/>.
        ///
        /// Lastly, in the editor, the value of the property may be overridden depending on <see cref="InputSettings.editorInputBehaviorInPlayMode"/>
        /// in case certain devices are automatically kept running in play mode even when no Game View has focus.
        ///
        /// Be aware that as far as players are concerned, only certain platforms support running Unity while not having focus.
        /// On mobile platforms, for example, this is generally not supported. In this case, the value of this property
        /// has no impact on input while the application does not have focus. See <see cref="InputSettings.backgroundBehavior"/>
        /// for more details.
        /// </remarks>
        /// <seealso cref="InputSettings.backgroundBehavior"/>
        /// <seealso cref="InputControlLayout.canRunInBackground"/>
        public bool canRunInBackground
        {
            get
            {
                // In the editor, "background" refers to "game view not focused", not to the editor not being active.
                // So, we modulate canRunInBackground depending on how input should behave WRT game view according
                // to the input settings.
                #if UNITY_EDITOR
                var gameViewFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
                if (gameViewFocus == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    return false; // No device considered being able to run without game view focus.
                if (gameViewFocus == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                    return !(this is Pointer || this is Keyboard); // Anything but pointers and keyboards considered as being able to run in background.
                #endif

                if ((m_DeviceFlags & DeviceFlags.CanRunInBackgroundHasBeenQueried) != 0)
                    return (m_DeviceFlags & DeviceFlags.CanRunInBackground) != 0;

                var command = QueryCanRunInBackground.Create();
                m_DeviceFlags |= DeviceFlags.CanRunInBackgroundHasBeenQueried;
                if (ExecuteCommand(ref command) >= 0 && command.canRunInBackground)
                {
                    m_DeviceFlags |= DeviceFlags.CanRunInBackground;
                    return true;
                }

                m_DeviceFlags &= ~DeviceFlags.CanRunInBackground;
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
        /// The "timeline" is reset to 0 when entering play mode. If there are any events incoming or device
        /// updates which occur prior to entering play mode, these will appear negative.
        /// </remarks>
        public double lastUpdateTime => m_LastUpdateTimeInternal - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

        public bool wasUpdatedThisFrame => m_CurrentUpdateStepCount == InputUpdate.s_UpdateStepCount;

        /// <summary>
        /// A flattened list of controls that make up the device.
        /// </summary>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputControl> allControls =>
            // Since m_ChildrenForEachControl contains the device's children as well as the children
            // of each control in the hierarchy, and since each control can only have a single parent,
            // this list will actually deliver a flattened list of all controls in the hierarchy (and without
            // the device itself being listed).
            new ReadOnlyArray<InputControl>(m_ChildrenForEachControl);

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
        internal void NotifyConfigurationChanged()
        {
            // Mark all controls in the hierarchy as having their config out of date.
            // We don't want to update configuration right away but rather wait until
            // someone actually depends on it.
            isConfigUpToDate = false;
            for (var i = 0; i < m_ChildrenForEachControl.Length; ++i)
                m_ChildrenForEachControl[i].isConfigUpToDate = false;

            // Make sure we fetch the enabled/disabled state again.
            m_DeviceFlags &= ~DeviceFlags.DisabledStateHasBeenQueriedFromRuntime;

            OnConfigurationChanged();
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
        /// by simply flooding the system with events. Hence why by default,  noise on <c>.current</c> getters
        /// will be filtered out and a device will only see <c>MakeCurrent</c> getting called if their input
        /// was detected on non-noisy controls.
        /// </remarks>
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

        /// <summary>
        /// Called by the system when the device configuration is changed. This happens when the backend sends
        /// a <see cref="DeviceConfigurationEvent"/> for the device.
        /// </summary>
        /// <remarks>
        /// This method can be used to flush out cached information. An example of where this happens is <see cref="Controls.KeyControl"/>
        /// caching information about the display name of a control. As this depends on the current keyboard layout, the information
        /// has to be fetched dynamically (this happens using <see cref="QueryKeyNameCommand"/>). Whenever the keyboard layout changes,
        /// the system sends a <see cref="DeviceConfigurationEvent"/> for the <see cref="Keyboard"/> at which point the device flushes
        /// all cached key names.
        /// </remarks>
        /// <seealso cref="InputManager.OnUpdate"/>
        /// <seealso cref="InputDeviceChange.ConfigurationChanged"/>
        /// <seealso cref="OnConfigurationChanged"/>///
        protected virtual void OnConfigurationChanged()
        {
        }

        ////TODO: add overridable OnDisable/OnEnable that fire the device commands

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
        /// This is a low-level API. It works in a similar way to <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa363216%28v=vs.85%29.aspx?f=255&amp;MSPPError=-2147217396" target="_blank">
        /// DeviceIoControl</a> on Windows and <a href="https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/ioctl.2.html#//apple_ref/doc/man/2/ioctl" target="_blank">ioctl</a>
        /// on UNIX-like systems.
        /// </remarks>
        public unsafe long ExecuteCommand<TCommand>(ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            var commandPtr = (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command);

            // Give callbacks first shot.
            var manager = InputSystem.s_Manager;
            manager.m_DeviceCommandCallbacks.LockForChanges();
            for (var i = 0; i < manager.m_DeviceCommandCallbacks.length; ++i)
            {
                try
                {
                    var result = manager.m_DeviceCommandCallbacks[i](this, commandPtr);
                    if (result.HasValue)
                        return result.Value;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{exception.GetType().Name} while executing 'InputSystem.onDeviceCommand' callbacks");
                    Debug.LogException(exception);
                }
            }
            manager.m_DeviceCommandCallbacks.UnlockForChanges();

            return ExecuteCommand((InputDeviceCommand*)UnsafeUtility.AddressOf(ref command));
        }

        protected virtual unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
        {
            return InputRuntime.s_Instance.DeviceCommand(deviceId, commandPtr);
        }

        internal bool QueryEnabledStateFromRuntime()
        {
            // Fetch state from runtime, if necessary.
            if ((m_DeviceFlags & DeviceFlags.DisabledStateHasBeenQueriedFromRuntime) == 0)
            {
                var command = QueryEnabledStateCommand.Create();
                if (ExecuteCommand(ref command) >= 0)
                {
                    if (command.isEnabled)
                        m_DeviceFlags &= ~DeviceFlags.DisabledInRuntime;
                    else
                        m_DeviceFlags |= DeviceFlags.DisabledInRuntime;
                }
                else
                {
                    // We got no response on the enable/disable state. Assume device is enabled.
                    m_DeviceFlags &= ~DeviceFlags.DisabledInRuntime;
                }

                // Only fetch enable/disable state again if we get a configuration change event.
                m_DeviceFlags |= DeviceFlags.DisabledStateHasBeenQueriedFromRuntime;
            }

            return (m_DeviceFlags & DeviceFlags.DisabledInRuntime) == 0;
        }

        [Serializable]
        [Flags]
        internal enum DeviceFlags
        {
            UpdateBeforeRender = 1 << 0,

            HasStateCallbacks = 1 << 1,
            HasControlsWithDefaultState = 1 << 2,
            HasDontResetControls = 1 << 10,
            HasEventMerger = 1 << 13,
            HasEventPreProcessor = 1 << 14,

            Remote = 1 << 3, // It's a local mirror of a device from a remote player connection.
            Native = 1 << 4, // It's a device created from data surfaced by NativeInputRuntime.

            DisabledInFrontend = 1 << 5, // Explicitly disabled on the managed side.
            DisabledInRuntime = 1 << 7, // Disabled in the native runtime.
            DisabledWhileInBackground = 1 << 8, // Disabled while the player is running in the background.
            DisabledStateHasBeenQueriedFromRuntime = 1 << 6, // Whether we have fetched the current enable/disable state from the runtime.

            CanRunInBackground = 1 << 11,
            CanRunInBackgroundHasBeenQueried = 1 << 12,
        }

        internal bool disabledInFrontend
        {
            get => (m_DeviceFlags & DeviceFlags.DisabledInFrontend) != 0;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.DisabledInFrontend;
                else
                    m_DeviceFlags &= ~DeviceFlags.DisabledInFrontend;
            }
        }

        internal bool disabledInRuntime
        {
            get => (m_DeviceFlags & DeviceFlags.DisabledInRuntime) != 0;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.DisabledInRuntime;
                else
                    m_DeviceFlags &= ~DeviceFlags.DisabledInRuntime;
            }
        }

        internal bool disabledWhileInBackground
        {
            get => (m_DeviceFlags & DeviceFlags.DisabledWhileInBackground) != 0;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.DisabledWhileInBackground;
                else
                    m_DeviceFlags &= ~DeviceFlags.DisabledWhileInBackground;
            }
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

        // An ordered list of ints each containing a bit offset into the state of the device (*without* the added global
        // offset), a bit count for the size of the state of the control, and an associated index into m_ChildrenForEachControl
        // for the corresponding control.
        // NOTE: This contains *leaf* controls only.
        internal uint[] m_StateOffsetToControlMap;

        // Holds the nodes that represent the tree of memory ranges that each control occupies. This is used when
        // determining what controls have changed given a state event or partial state update.
        internal ControlBitRangeNode[] m_ControlTreeNodes;

        // An indirection table for control bit range nodes to point at zero or more controls. Indices are used to
        // point into the m_ChildrenForEachControl array.
        internal ushort[] m_ControlTreeIndices;

        // When a device gets built from a layout, we create a binary tree from its controls where each node in the tree
        // represents the range of bits that cover the left or right section of the parent range. For example, starting
        // with the entire device state block as the parent, where the state block is 100 bits long, the left node will
        // cover from bits 0-50, and the right from bits 51-99. For the left node, we'll get two more child nodes where
        // the left will cover bits 0-25, and the right bits 26-49 and so on. Each node will point at any controls that
        // either fit exactly into its range, or overlap the splitting point between both nodes. In reality, picking the
        // mid-point to split each parent node is a little convoluted and will rarely be the absolute mid-point, but that's
        // the basic idea.
        //
        // At runtime, when state events come in, we can then really quickly perform a bunch of memcmps on both sides of
        // the tree and recurse down the branches that have changed. When nodes have controls, we can then check if those
        // controls have changes, and mark them as stale so their cached values get updated the next time their values
        // are read.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ControlBitRangeNode
        {
            // only store the end bit offset of each range because we always do a full tree traversal so
            // the start offset is always calculated at each level.
            public ushort endBitOffset;

            // points to the location in the nodes array where the left child of this node lives, or -1 if there
            // is no child. The right child is always at the next index.
            public short leftChildIndex;

            // each node can point at multiple controls (because multiple controls can use the same range in memory and
            // also because of overlaps in bit ranges). The control indicies for each node are stored contiguously in the
            // m_ControlTreeIndicies array on the device, which acts as an indirection table, and these two values tell
            // us where to start for each node and how many controls this node points at. This is an unsigned short so that
            // we could in theory support devices with up to 65535 controls. Each node however can only support 255 controls.
            public ushort controlStartIndex;
            public byte controlCount;

            public ControlBitRangeNode(ushort endOffset)
            {
                controlStartIndex = 0;
                controlCount = 0;
                endBitOffset = endOffset;
                leftChildIndex = -1;
            }
        }

        // ATM we pack everything into 32 bits. Given we're operating on bit offsets and counts, this imposes some tight limits
        // on controls and their associated state memory. Should this turn out to be a problem, bump m_StateOffsetToControlMap
        // to a ulong[] and up the counts here to account for having 64 bits available instead of only 32.
        internal const int kControlIndexBits = 10; // 1024 controls max.
        internal const int kStateOffsetBits = 13; // 1024 bytes max state size for entire device.
        internal const int kStateSizeBits = 9; // 64 bytes max for an individual leaf control.

        internal static uint EncodeStateOffsetToControlMapEntry(uint controlIndex, uint stateOffsetInBits, uint stateSizeInBits)
        {
            Debug.Assert(kControlIndexBits < 32, $"Expected kControlIndexBits < 32, so we fit into the 32 bit wide bitmask");
            Debug.Assert(kStateOffsetBits < 32, $"Expected kStateOffsetBits < 32, so we fit into the 32 bit wide bitmask");
            Debug.Assert(kStateSizeBits < 32, $"Expected kStateSizeBits < 32, so we fit into the 32 bit wide bitmask");
            Debug.Assert(controlIndex < (1U << kControlIndexBits), "Control index beyond what is supported");
            Debug.Assert(stateOffsetInBits < (1U << kStateOffsetBits), "State offset beyond what is supported");
            Debug.Assert(stateSizeInBits < (1U << kStateSizeBits), "State size beyond what is supported");
            return stateOffsetInBits << (kControlIndexBits + kStateSizeBits) | stateSizeInBits << kControlIndexBits | controlIndex;
        }

        internal static void DecodeStateOffsetToControlMapEntry(uint entry, out uint controlIndex,
            out uint stateOffset, out uint stateSize)
        {
            controlIndex = entry & (1U << kControlIndexBits) - 1;
            stateOffset = entry >> (kControlIndexBits + kStateSizeBits);
            stateSize = (entry >> kControlIndexBits) & (((1U << (kControlIndexBits + kStateSizeBits)) - 1) >> kControlIndexBits);
        }

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

        internal bool hasDontResetControls
        {
            get => (m_DeviceFlags & DeviceFlags.HasDontResetControls) == DeviceFlags.HasDontResetControls;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasDontResetControls;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasDontResetControls;
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

        internal bool hasEventMerger
        {
            get => (m_DeviceFlags & DeviceFlags.HasEventMerger) == DeviceFlags.HasEventMerger;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasEventMerger;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasEventMerger;
            }
        }

        internal bool hasEventPreProcessor
        {
            get => (m_DeviceFlags & DeviceFlags.HasEventPreProcessor) == DeviceFlags.HasEventPreProcessor;
            set
            {
                if (value)
                    m_DeviceFlags |= DeviceFlags.HasEventPreProcessor;
                else
                    m_DeviceFlags &= ~DeviceFlags.HasEventPreProcessor;
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

        internal bool RequestSync()
        {
            SetOptimizedControlDataTypeRecursively();

            var syncCommand = RequestSyncCommand.Create();
            return device.ExecuteCommand(ref syncCommand) >= 0;
        }

        internal bool RequestReset()
        {
            SetOptimizedControlDataTypeRecursively();

            var resetCommand = RequestResetCommand.Create();
            return device.ExecuteCommand(ref resetCommand) >= 0;
        }

        internal bool ExecuteEnableCommand()
        {
            SetOptimizedControlDataTypeRecursively();

            var command = EnableDeviceCommand.Create();
            return device.ExecuteCommand(ref command) >= 0;
        }

        internal bool ExecuteDisableCommand()
        {
            var command = DisableDeviceCommand.Create();
            return device.ExecuteCommand(ref command) >= 0;
        }

        internal void NotifyAdded()
        {
            OnAdded();
        }

        internal void NotifyRemoved()
        {
            OnRemoved();
        }

        internal static TDevice Build<TDevice>(string layoutName = default, string layoutVariants = default, InputDeviceDescription deviceDescription = default, bool noPrecompiledLayouts = false)
            where TDevice : InputDevice
        {
            var internedLayoutName = new InternedString(layoutName);

            if (internedLayoutName.IsEmpty())
            {
                internedLayoutName = InputControlLayout.s_Layouts.TryFindLayoutForType(typeof(TDevice));
                if (internedLayoutName.IsEmpty())
                    internedLayoutName = new InternedString(typeof(TDevice).Name);
            }

            // Fast path: see if we can use a precompiled version.
            // NOTE: We currently do not support layout variants with precompiled layouts.
            // NOTE: We remove precompiled layouts when they are invalidated by layout changes. So, we don't have to perform
            //       checks here.
            if (!noPrecompiledLayouts &&
                string.IsNullOrEmpty(layoutVariants) &&
                InputControlLayout.s_Layouts.precompiledLayouts.TryGetValue(internedLayoutName, out var precompiledLayout))
            {
                // Yes. This is pretty much a direct new() of the device.
                return (TDevice)precompiledLayout.factoryMethod();
            }

            // Slow path: use InputDeviceBuilder to construct the device from the InputControlLayout.
            using (InputDeviceBuilder.Ref())
            {
                InputDeviceBuilder.instance.Setup(internedLayoutName, new InternedString(layoutVariants),
                    deviceDescription: deviceDescription);
                var device = InputDeviceBuilder.instance.Finish();
                if (!(device is TDevice deviceOfType))
                    throw new ArgumentException(
                        $"Expected device of type '{typeof(TDevice).Name}' but got device of type '{device.GetType().Name}' instead",
                        "TDevice");

                return deviceOfType;
            }
        }

        internal unsafe void WriteChangedControlStates(byte* deviceStateBuffer, void* statePtr, uint stateSizeInBytes,
            uint stateOffsetInDevice)
        {
            Debug.Assert(m_ControlTreeNodes != null && m_ControlTreeIndices != null);

            if (m_ControlTreeNodes.Length == 0)
                return;

            // if we're dealing with a delta state event or just an individual control update through InputState.ChangeState
            // the size of the new data will not be the same size as the device state block, so use the 'partial' change state
            // method to update just those controls that overlap with the changed state.
            if (m_StateBlock.sizeInBits != stateSizeInBytes * 8)
            {
                if (m_ControlTreeNodes[0].leftChildIndex != -1)
                    WritePartialChangedControlStatesInternal(statePtr, stateSizeInBytes * 8,
                        stateOffsetInDevice * 8, deviceStateBuffer, m_ControlTreeNodes[0], 0);
            }
            else
            {
                if (m_ControlTreeNodes[0].leftChildIndex != -1)
                    WriteChangedControlStatesInternal(statePtr, stateSizeInBytes * 8,
                        deviceStateBuffer, m_ControlTreeNodes[0], 0);
            }
        }

        private unsafe void WritePartialChangedControlStatesInternal(void* statePtr, uint stateSizeInBits,
            uint stateOffsetInDeviceInBits, byte* deviceStatePtr, ControlBitRangeNode parentNode, uint startOffset)
        {
            var leftNode = m_ControlTreeNodes[parentNode.leftChildIndex];
            // TODO recheck
            if (Math.Max(stateOffsetInDeviceInBits, startOffset) <=
                Math.Min(stateOffsetInDeviceInBits + stateSizeInBits, leftNode.endBitOffset))
            {
                var controlEndIndex = leftNode.controlStartIndex + leftNode.controlCount;
                for (int i = leftNode.controlStartIndex; i < controlEndIndex; i++)
                {
                    var controlIndex = m_ControlTreeIndices[i];
                    m_ChildrenForEachControl[controlIndex].MarkAsStale();
                }

                if (leftNode.leftChildIndex != -1)
                    WritePartialChangedControlStatesInternal(statePtr, stateSizeInBits, stateOffsetInDeviceInBits,
                        deviceStatePtr, leftNode, startOffset);
            }

            var rightNode = m_ControlTreeNodes[parentNode.leftChildIndex + 1];
            // TODO recheck
            if (Math.Max(stateOffsetInDeviceInBits, leftNode.endBitOffset) <=
                Math.Min(stateOffsetInDeviceInBits + stateSizeInBits, rightNode.endBitOffset))
            {
                var controlEndIndex = rightNode.controlStartIndex + rightNode.controlCount;
                for (int i = rightNode.controlStartIndex; i < controlEndIndex; i++)
                {
                    var controlIndex = m_ControlTreeIndices[i];
                    m_ChildrenForEachControl[controlIndex].MarkAsStale();
                }

                if (rightNode.leftChildIndex != -1)
                    WritePartialChangedControlStatesInternal(statePtr, stateSizeInBits, stateOffsetInDeviceInBits,
                        deviceStatePtr, rightNode, leftNode.endBitOffset);
            }
        }

        private void DumpControlBitRangeNode(int nodeIndex, ControlBitRangeNode node, uint startOffset, uint sizeInBits, List<string> output)
        {
            var names = new List<string>();
            for (var i = 0; i < node.controlCount; i++)
            {
                var controlIndex = m_ControlTreeIndices[node.controlStartIndex + i];
                var control = m_ChildrenForEachControl[controlIndex];
                names.Add(control.path);
            }
            var namesStr = string.Join(", ", names);
            var children = node.leftChildIndex != -1 ? $" <{node.leftChildIndex}, {node.leftChildIndex + 1}>" : "";
            output.Add($"{nodeIndex} [{startOffset}, {startOffset + sizeInBits}]{children}->{namesStr}");
        }

        private void DumpControlTree(ControlBitRangeNode parentNode, uint startOffset, List<string> output)
        {
            var leftNode = m_ControlTreeNodes[parentNode.leftChildIndex];
            var rightNode = m_ControlTreeNodes[parentNode.leftChildIndex + 1];
            DumpControlBitRangeNode(parentNode.leftChildIndex, leftNode, startOffset, leftNode.endBitOffset - startOffset, output);
            DumpControlBitRangeNode(parentNode.leftChildIndex + 1, rightNode, leftNode.endBitOffset, (uint)(rightNode.endBitOffset - leftNode.endBitOffset), output);

            if (leftNode.leftChildIndex != -1)
                DumpControlTree(leftNode, startOffset, output);

            if (rightNode.leftChildIndex != -1)
                DumpControlTree(rightNode, leftNode.endBitOffset, output);
        }

        internal string DumpControlTree()
        {
            var output = new List<string>();
            DumpControlTree(m_ControlTreeNodes[0], 0, output);
            return string.Join("\n", output);
        }

        private unsafe void WriteChangedControlStatesInternal(void* statePtr, uint stateSizeInBits,
            byte* deviceStatePtr, ControlBitRangeNode parentNode, uint startOffset)
        {
            var leftNode = m_ControlTreeNodes[parentNode.leftChildIndex];

            // have any bits in the region defined by the left node changed?
            // TODO recheck
            if (HasDataChangedInRange(deviceStatePtr, statePtr, startOffset, leftNode.endBitOffset - startOffset + 1))
            {
                // update the state of any controls pointed to by the left node
                var controlEndIndex = leftNode.controlStartIndex + leftNode.controlCount;
                for (int i = leftNode.controlStartIndex; i < controlEndIndex; i++)
                {
                    var controlIndex = m_ControlTreeIndices[i];
                    var control = m_ChildrenForEachControl[controlIndex];

                    // nodes aren't always an exact fit for control memory ranges so check here if the control pointed
                    // at by this node has actually changed state so we don't mark controls as stale needlessly.
                    // We need to offset the device and new state pointers by the byte offset of the device state block
                    // because all controls have this offset baked into them, but deviceStatePtr points at the already
                    // offset block of device memory (remember, all devices share one big block of memory) and statePtr
                    // points at a block of memory of the same size as the device state.
                    if (!control.CompareState(deviceStatePtr - m_StateBlock.byteOffset,
                        (byte*)statePtr - m_StateBlock.byteOffset, null))
                        control.MarkAsStale();
                }

                // process the left child node if it exists
                if (leftNode.leftChildIndex != -1)
                    WriteChangedControlStatesInternal(statePtr, stateSizeInBits, deviceStatePtr,
                        leftNode, startOffset);
            }

            // process the right child node if it exists
            var rightNode = m_ControlTreeNodes[parentNode.leftChildIndex + 1];

            Debug.Assert(leftNode.endBitOffset + (rightNode.endBitOffset - leftNode.endBitOffset) < m_StateBlock.sizeInBits,
                "Tried to check state memory outside the bounds of the current device.");

            // if no bits in the range defined by the right node have changed, return
            // TODO recheck
            if (!HasDataChangedInRange(deviceStatePtr, statePtr, leftNode.endBitOffset,
                (uint)(rightNode.endBitOffset - leftNode.endBitOffset + 1)))
                return;

            // update the state of any controls pointed to by the right node
            var rightNodeControlEndIndex = rightNode.controlStartIndex + rightNode.controlCount;
            for (int i = rightNode.controlStartIndex; i < rightNodeControlEndIndex; i++)
            {
                var controlIndex = m_ControlTreeIndices[i];
                var control = m_ChildrenForEachControl[controlIndex];

                if (!control.CompareState(deviceStatePtr - m_StateBlock.byteOffset,
                    (byte*)statePtr - m_StateBlock.byteOffset, null))
                    control.MarkAsStale();
            }

            if (rightNode.leftChildIndex != -1)
                WriteChangedControlStatesInternal(statePtr, stateSizeInBits, deviceStatePtr,
                    rightNode, leftNode.endBitOffset);
        }

        private static unsafe bool HasDataChangedInRange(byte* deviceStatePtr, void* statePtr, uint startOffset, uint sizeInBits)
        {
            if (sizeInBits == 1)
                return MemoryHelpers.ReadSingleBit(deviceStatePtr, startOffset) !=
                    MemoryHelpers.ReadSingleBit(statePtr, startOffset);

            return !MemoryHelpers.MemCmpBitRegion(deviceStatePtr, statePtr,
                startOffset, sizeInBits);
        }
    }
}
