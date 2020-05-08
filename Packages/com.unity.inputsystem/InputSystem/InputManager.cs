using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Controls;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////TODO: make diagnostics available in dev players and give it a public API to enable them

////TODO: work towards InputManager having no direct knowledge of actions

////TODO: allow pushing events into the system any which way; decouple from the buffer in NativeInputSystem being the only source

////TODO: make sure we discard events in editor updates when lockInputToGameView is true and the player isn't running or paused

////REVIEW: change the event properties over to using IObservable?

////REVIEW: instead of RegisterInteraction and RegisterProcessor, have a generic RegisterInterface (or something)?

////REVIEW: can we do away with the 'previous == previous frame' and simply buffer flip on every value write?

////REVIEW: should we force keeping mouse/pen/keyboard/touch around in editor even if not in list of supported devices?

////REVIEW: do we want to filter out state events that result in no state change?

#pragma warning disable CS0649
namespace UnityEngine.InputSystem
{
    using DeviceChangeListener = Action<InputDevice, InputDeviceChange>;
    using DeviceStateChangeListener = Action<InputDevice, InputEventPtr>;
    using LayoutChangeListener = Action<string, InputControlLayoutChange>;
    using EventListener = Action<InputEventPtr, InputDevice>;
    using UpdateListener = Action;

    /// <summary>
    /// Hub of the input system.
    /// </summary>
    /// <remarks>
    /// Not exposed. Use <see cref="InputSystem"/> as the public entry point to the system.
    ///
    /// Manages devices, layouts, and event processing.
    /// </remarks>
    internal class InputManager
    {
        public ReadOnlyArray<InputDevice> devices => new ReadOnlyArray<InputDevice>(m_Devices, 0, m_DevicesCount);

        public TypeTable processors => m_Processors;
        public TypeTable interactions => m_Interactions;
        public TypeTable composites => m_Composites;

        public InputMetrics metrics
        {
            get
            {
                var result = m_Metrics;

                result.currentNumDevices = m_DevicesCount;
                result.currentStateSizeInBytes = (int)m_StateBuffers.totalSize;

                // Count controls.
                result.currentControlCount = m_DevicesCount;
                for (var i = 0; i < m_DevicesCount; ++i)
                    result.currentControlCount += m_Devices[i].allControls.Count;

                // Count layouts.
                result.currentLayoutCount = m_Layouts.layoutTypes.Count;
                result.currentLayoutCount += m_Layouts.layoutStrings.Count;
                result.currentLayoutCount += m_Layouts.layoutBuilders.Count;
                result.currentLayoutCount += m_Layouts.layoutOverrides.Count;

                return result;
            }
        }

        public InputSettings settings
        {
            get
            {
                Debug.Assert(m_Settings != null);
                return m_Settings;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (m_Settings == value)
                    return;

                m_Settings = value;
                ApplySettings();
            }
        }

        public InputUpdateType updateMask
        {
            get => m_UpdateMask;
            set
            {
                // In editor, we don't allow disabling editor updates.
                #if UNITY_EDITOR
                value |= InputUpdateType.Editor;
                #endif

                if (m_UpdateMask == value)
                    return;

                m_UpdateMask = value;

                // Recreate state buffers.
                if (m_DevicesCount > 0)
                    ReallocateStateBuffers();
            }
        }

        public InputUpdateType defaultUpdateType
        {
            get
            {
                ////TODO: if we're *inside* an update, this should use the current update type

                #if UNITY_EDITOR
                if (!gameIsPlayingAndHasFocus)
                    return InputUpdateType.Editor;
                #endif

                if ((m_UpdateMask & InputUpdateType.Manual) != 0)
                    return InputUpdateType.Manual;

                if ((m_UpdateMask & InputUpdateType.Dynamic) != 0)
                    return InputUpdateType.Dynamic;

                if ((m_UpdateMask & InputUpdateType.Fixed) != 0)
                    return InputUpdateType.Fixed;

                return InputUpdateType.None;
            }
        }

        public float pollingFrequency
        {
            get => m_PollingFrequency;
            set
            {
                ////REVIEW: allow setting to zero to turn off polling altogether?
                if (value <= 0)
                    throw new ArgumentException("Polling frequency must be greater than zero", "value");

                m_PollingFrequency = value;
                if (m_Runtime != null)
                    m_Runtime.pollingFrequency = value;
            }
        }

        public event DeviceChangeListener onDeviceChange
        {
            add => m_DeviceChangeListeners.AppendWithCapacity(value);
            remove
            {
                var index = m_DeviceChangeListeners.IndexOf(value);
                if (index >= 0)
                    m_DeviceChangeListeners.RemoveAtWithCapacity(index);
            }
        }

        public event DeviceStateChangeListener onDeviceStateChange
        {
            add => m_DeviceStateChangeListeners.AppendWithCapacity(value);
            remove
            {
                var index = m_DeviceStateChangeListeners.IndexOf(value);
                if (index >= 0)
                    m_DeviceStateChangeListeners.RemoveAtWithCapacity(index);
            }
        }

        public event InputDeviceCommandDelegate onDeviceCommand
        {
            add => m_DeviceCommandCallbacks.Append(value);
            remove
            {
                var index = m_DeviceCommandCallbacks.IndexOf(value);
                if (index >= 0)
                    m_DeviceCommandCallbacks.RemoveAtWithCapacity(index);
            }
        }

        ////REVIEW: would be great to have a way to sort out precedence between two callbacks
        public event InputDeviceFindControlLayoutDelegate onFindControlLayoutForDevice
        {
            add
            {
                m_DeviceFindLayoutCallbacks.AppendWithCapacity(value);

                // Having a new callback on this event can change the set of devices we recognize.
                // See if there's anything in the list of available devices that we can now turn
                // into an InputDevice whereas we couldn't before.
                //
                // NOTE: A callback could also impact already existing devices and theoretically alter
                //       what layout we would have used for those. We do *NOT* retroactively apply
                //       those changes.
                AddAvailableDevicesThatAreNowRecognized();
            }
            remove
            {
                var index = m_DeviceFindLayoutCallbacks.IndexOf(value);
                if (index >= 0)
                    m_DeviceFindLayoutCallbacks.RemoveAtWithCapacity(index);
            }
        }

        public event LayoutChangeListener onLayoutChange
        {
            add => m_LayoutChangeListeners.AppendWithCapacity(value);
            remove
            {
                var index = m_LayoutChangeListeners.IndexOf(value);
                if (index >= 0)
                    m_LayoutChangeListeners.RemoveAtWithCapacity(index);
            }
        }

        ////TODO: add InputEventBuffer struct that uses NativeArray underneath
        ////TODO: make InputEventTrace use NativeArray
        ////TODO: introduce an alternative that consumes events in bulk
        public event EventListener onEvent
        {
            add
            {
                if (!m_EventListeners.Contains(value))
                    m_EventListeners.AppendWithCapacity(value);
            }
            remove
            {
                var index = m_EventListeners.IndexOf(value);
                if (index >= 0)
                    m_EventListeners.RemoveAtWithCapacity(index);
            }
        }

        public event UpdateListener onBeforeUpdate
        {
            add
            {
                InstallBeforeUpdateHookIfNecessary();
                if (!m_BeforeUpdateListeners.Contains(value))
                    m_BeforeUpdateListeners.AppendWithCapacity(value);
            }
            remove
            {
                var index = m_BeforeUpdateListeners.IndexOf(value);
                if (index >= 0)
                    m_BeforeUpdateListeners.RemoveAtWithCapacity(index);
            }
        }

        public event UpdateListener onAfterUpdate
        {
            add
            {
                if (!m_AfterUpdateListeners.Contains(value))
                    m_AfterUpdateListeners.AppendWithCapacity(value);
            }
            remove
            {
                var index = m_AfterUpdateListeners.IndexOf(value);
                if (index >= 0)
                    m_AfterUpdateListeners.RemoveAtWithCapacity(index);
            }
        }

        public event Action onSettingsChange
        {
            add
            {
                if (!m_SettingsChangedListeners.Contains(value))
                    m_SettingsChangedListeners.AppendWithCapacity(value);
            }
            remove
            {
                var index = m_SettingsChangedListeners.IndexOf(value);
                if (index >= 0)
                    m_SettingsChangedListeners.RemoveAtWithCapacity(index);
            }
        }

        private bool gameIsPlayingAndHasFocus =>
#if UNITY_EDITOR
                     m_Runtime.isInPlayMode && !m_Runtime.isPaused && (m_HasFocus || InputEditorUserSettings.lockInputToGameView);
#else
            true;
#endif

        ////TODO: when registering a layout that exists as a layout of a different type (type vs string vs constructor),
        ////      remove the existing registration

        // Add a layout constructed from a type.
        // If a layout with the same name already exists, the new layout
        // takes its place.
        public void RegisterControlLayout(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Note that since InputDevice derives from InputControl, isDeviceLayout implies
            // isControlLayout to be true as well.
            var isDeviceLayout = typeof(InputDevice).IsAssignableFrom(type);
            var isControlLayout = typeof(InputControl).IsAssignableFrom(type);

            if (!isDeviceLayout && !isControlLayout)
                throw new ArgumentException($"Types used as layouts have to be InputControls or InputDevices; '{type.Name}' is a '{type.BaseType.Name}'",
                    nameof(type));

            var internedName = new InternedString(name);
            var isReplacement = DoesLayoutExist(internedName);

            // All we do is enter the type into a map. We don't construct an InputControlLayout
            // from it until we actually need it in an InputDeviceBuilder to create a device.
            // This not only avoids us creating a bunch of objects on the managed heap but
            // also avoids us laboriously constructing a XRController layout, for example,
            // in a game that never uses XR.
            m_Layouts.layoutTypes[internedName] = type;

            ////TODO: make this independent of initialization order
            ////TODO: re-scan base type information after domain reloads

            // Walk class hierarchy all the way up to InputControl to see
            // if there's another type that's been registered as a layout.
            // If so, make it a base layout for this one.
            string baseLayout = null;
            for (var baseType = type.BaseType; baseLayout == null && baseType != typeof(InputControl);
                 baseType = baseType.BaseType)
            {
                foreach (var entry in m_Layouts.layoutTypes)
                    if (entry.Value == baseType)
                    {
                        baseLayout = entry.Key;
                        break;
                    }
            }

            PerformLayoutPostRegistration(internedName, new InlinedArray<InternedString>(new InternedString(baseLayout)),
                isReplacement, isKnownToBeDeviceLayout: isDeviceLayout);
        }

        public void RegisterControlLayout(string json, string name = null, bool isOverride = false)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            ////REVIEW: as long as no one has instantiated the layout, the base layout information is kinda pointless

            // Parse out name, device description, and base layout.
            InputControlLayout.ParseHeaderFieldsFromJson(json, out var nameFromJson, out var baseLayouts,
                out var deviceMatcher);

            // Decide whether to take name from JSON or from code.
            var internedLayoutName = new InternedString(name);
            if (internedLayoutName.IsEmpty())
            {
                internedLayoutName = nameFromJson;

                // Make sure we have a name.
                if (internedLayoutName.IsEmpty())
                    throw new ArgumentException("Layout name has not been given and is not set in JSON layout",
                        nameof(name));
            }

            // If it's an override, it must have a layout the overrides apply to.
            if (isOverride && baseLayouts.length == 0)
            {
                throw new ArgumentException(
                    $"Layout override '{internedLayoutName}' must have 'extend' property mentioning layout to which to apply the overrides",
                    nameof(json));
            }

            // Add it to our records.
            var isReplacement = DoesLayoutExist(internedLayoutName);
            m_Layouts.layoutStrings[internedLayoutName] = json;
            if (isOverride)
            {
                m_Layouts.layoutOverrideNames.Add(internedLayoutName);
                for (var i = 0; i < baseLayouts.length; ++i)
                {
                    var baseLayoutName = baseLayouts[i];
                    m_Layouts.layoutOverrides.TryGetValue(baseLayoutName, out var overrideList);
                    ArrayHelpers.Append(ref overrideList, internedLayoutName);
                    m_Layouts.layoutOverrides[baseLayoutName] = overrideList;
                }
            }

            PerformLayoutPostRegistration(internedLayoutName, baseLayouts,
                isReplacement: isReplacement, isOverride: isOverride);

            // If the layout contained a device matcher, register it.
            if (!deviceMatcher.empty)
                RegisterControlLayoutMatcher(internedLayoutName, deviceMatcher);
        }

        public void RegisterControlLayoutBuilder(Func<InputControlLayout> method, string name,
            string baseLayout = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var internedLayoutName = new InternedString(name);
            var internedBaseLayoutName = new InternedString(baseLayout);
            var isReplacement = DoesLayoutExist(internedLayoutName);

            m_Layouts.layoutBuilders[internedLayoutName] = method;

            PerformLayoutPostRegistration(internedLayoutName, new InlinedArray<InternedString>(internedBaseLayoutName),
                isReplacement);
        }

        private void PerformLayoutPostRegistration(InternedString layoutName, InlinedArray<InternedString> baseLayouts,
            bool isReplacement, bool isKnownToBeDeviceLayout = false, bool isOverride = false)
        {
            ++m_LayoutRegistrationVersion;

            // Force-clear layout cache. Don't clear reference count so that
            // the cache gets cleared out properly when released in case someone
            // is using it ATM.
            InputControlLayout.s_CacheInstance.Clear();

            // For layouts that aren't overrides, add the name of the base
            // layout to the lookup table.
            if (!isOverride && baseLayouts.length > 0)
            {
                if (baseLayouts.length > 1)
                    throw new NotSupportedException(
                        $"Layout '{layoutName}' has multiple base layouts; this is only supported on layout overrides");

                var baseLayoutName = baseLayouts[0];
                if (!baseLayoutName.IsEmpty())
                    m_Layouts.baseLayoutTable[layoutName] = baseLayoutName;
            }

            // Recreate any devices using the layout. If it's an override, recreate devices using any of the base layouts.
            if (isOverride)
            {
                for (var i = 0; i < baseLayouts.length; ++i)
                    RecreateDevicesUsingLayout(baseLayouts[i], isKnownToBeDeviceLayout: isKnownToBeDeviceLayout);
            }
            else
            {
                RecreateDevicesUsingLayout(layoutName, isKnownToBeDeviceLayout: isKnownToBeDeviceLayout);
            }

            // In the editor, layouts may become available successively after a domain reload so
            // we may end up retaining device information all the way until we run the first full
            // player update. For every layout we register, we check here whether we have a saved
            // device state using a layout with the same name but not having a device description
            // (the latter is important as in that case, we should go through the normal matching
            // process and not just rely on the name of the layout). If so, we try here to recreate
            // the device with the just registered layout.
            #if UNITY_EDITOR
            for (var i = 0; i < m_SavedDeviceStates.LengthSafe(); ++i)
            {
                ref var deviceState = ref m_SavedDeviceStates[i];
                if (layoutName != deviceState.layout || !deviceState.description.empty)
                    continue;

                if (RestoreDeviceFromSavedState(ref deviceState, layoutName))
                {
                    ArrayHelpers.EraseAt(ref m_SavedDeviceStates, i);
                    --i;
                }
            }
            #endif

            // Let listeners know.
            var change = isReplacement ? InputControlLayoutChange.Replaced : InputControlLayoutChange.Added;
            for (var i = 0; i < m_LayoutChangeListeners.length; ++i)
                m_LayoutChangeListeners[i](layoutName.ToString(), change);
        }

        private void RecreateDevicesUsingLayout(InternedString layout, bool isKnownToBeDeviceLayout = false)
        {
            if (m_DevicesCount == 0)
                return;

            List<InputDevice> devicesUsingLayout = null;

            // Find all devices using the layout.
            for (var i = 0; i < m_DevicesCount; ++i)
            {
                var device = m_Devices[i];

                bool usesLayout;
                if (isKnownToBeDeviceLayout)
                    usesLayout = IsControlUsingLayout(device, layout);
                else
                    usesLayout = IsControlOrChildUsingLayoutRecursive(device, layout);

                if (usesLayout)
                {
                    if (devicesUsingLayout == null)
                        devicesUsingLayout = new List<InputDevice>();
                    devicesUsingLayout.Add(device);
                }
            }

            // If there's none, we're good.
            if (devicesUsingLayout == null)
                return;

            // Remove and re-add the matching devices.
            using (InputDeviceBuilder.Ref())
            {
                for (var i = 0; i < devicesUsingLayout.Count; ++i)
                {
                    var device = devicesUsingLayout[i];
                    RecreateDevice(device, device.m_Layout);
                }
            }
        }

        private bool IsControlOrChildUsingLayoutRecursive(InputControl control, InternedString layout)
        {
            // Check control itself.
            if (IsControlUsingLayout(control, layout))
                return true;

            // Check children.
            var children = control.children;
            for (var i = 0; i < children.Count; ++i)
                if (IsControlOrChildUsingLayoutRecursive(children[i], layout))
                    return true;

            return false;
        }

        private bool IsControlUsingLayout(InputControl control, InternedString layout)
        {
            // Check direct match.
            if (control.layout == layout)
                return true;

            // Check base layout chain.
            var baseLayout = control.m_Layout;
            while (m_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout))
                if (baseLayout == layout)
                    return true;

            return false;
        }

        public void RegisterControlLayoutMatcher(string layoutName, InputDeviceMatcher matcher)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentNullException(nameof(layoutName));
            if (matcher.empty)
                throw new ArgumentException("Matcher cannot be empty", nameof(matcher));

            // Add to table.
            var internedLayoutName = new InternedString(layoutName);
            m_Layouts.AddMatcher(internedLayoutName, matcher);

            // Recreate any device that we match better than its current layout.
            RecreateDevicesUsingLayoutWithInferiorMatch(matcher);

            // See if we can make sense of any device we couldn't make sense of before.
            AddAvailableDevicesMatchingDescription(matcher, internedLayoutName);
        }

        public void RegisterControlLayoutMatcher(Type type, InputDeviceMatcher matcher)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (matcher.empty)
                throw new ArgumentException("Matcher cannot be empty", nameof(matcher));

            var layoutName = m_Layouts.TryFindLayoutForType(type);
            if (layoutName.IsEmpty())
                throw new ArgumentException(
                    $"Type '{type.Name}' has not been registered as a control layout", nameof(type));

            RegisterControlLayoutMatcher(layoutName, matcher);
        }

        private void RecreateDevicesUsingLayoutWithInferiorMatch(InputDeviceMatcher deviceMatcher)
        {
            if (m_DevicesCount == 0)
                return;

            using (InputDeviceBuilder.Ref())
            {
                var deviceCount = m_DevicesCount;
                for (var i = 0; i < deviceCount; ++i)
                {
                    var device = m_Devices[i];
                    var deviceDescription = device.description;

                    if (deviceDescription.empty || !(deviceMatcher.MatchPercentage(deviceDescription) > 0))
                        continue;

                    var layoutName = TryFindMatchingControlLayout(ref deviceDescription, device.deviceId);
                    if (layoutName != device.m_Layout)
                    {
                        device.m_Description = deviceDescription;

                        RecreateDevice(device, layoutName);

                        // We're removing devices in the middle of the array and appending
                        // them at the end. Adjust our index and device count to make sure
                        // we're not iterating all the way into already processed devices.

                        --i;
                        --deviceCount;
                    }
                }
            }
        }

        private void RecreateDevice(InputDevice oldDevice, InternedString newLayout)
        {
            // Remove.
            RemoveDevice(oldDevice, keepOnListOfAvailableDevices: true);

            // Re-setup device.
            var newDevice = InputDevice.Build<InputDevice>(newLayout, oldDevice.m_Variants,
                deviceDescription: oldDevice.m_Description);

            // Preserve device properties that should not be changed by the re-creation
            // of a device.
            newDevice.m_DeviceId = oldDevice.m_DeviceId;
            newDevice.m_Description = oldDevice.m_Description;
            if (oldDevice.native)
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.Native;
            if (oldDevice.remote)
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
            if (!oldDevice.enabled)
            {
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.DisabledStateHasBeenQueried;
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.Disabled;
            }

            // Re-add.
            AddDevice(newDevice);
        }

        private void AddAvailableDevicesMatchingDescription(InputDeviceMatcher matcher, InternedString layout)
        {
            #if UNITY_EDITOR
            // If we still have some devices saved from the last domain reload, see
            // if they are matched by the given matcher. If so, turn them into devices.
            for (var i = 0; i < m_SavedDeviceStates.LengthSafe(); ++i)
            {
                ref var deviceState = ref m_SavedDeviceStates[i];
                if (matcher.MatchPercentage(deviceState.description) > 0)
                {
                    RestoreDeviceFromSavedState(ref deviceState, layout);
                    ArrayHelpers.EraseAt(ref m_SavedDeviceStates, i);
                    --i;
                }
            }
            #endif

            // See if the new description to layout mapping allows us to make
            // sense of a device we couldn't make sense of so far.
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                // Ignore if it's a device that has been explicitly removed.
                if (m_AvailableDevices[i].isRemoved)
                    continue;

                var deviceId = m_AvailableDevices[i].deviceId;
                if (TryGetDeviceById(deviceId) != null)
                    continue;

                if (matcher.MatchPercentage(m_AvailableDevices[i].description) > 0f)
                {
                    // Try to create InputDevice instance.
                    try
                    {
                        AddDevice(layout, deviceId, deviceDescription: m_AvailableDevices[i].description,
                            deviceFlags: m_AvailableDevices[i].isNative ? InputDevice.DeviceFlags.Native : 0);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"Layout '{layout}' matches existing device '{m_AvailableDevices[i].description}' but failed to instantiate: {exception}");
                        Debug.LogException(exception);
                        continue;
                    }

                    // Re-enable device.
                    var command = EnableDeviceCommand.Create();
                    m_Runtime.DeviceCommand(deviceId, ref command);
                }
            }
        }

        public void RemoveControlLayout(string name, string @namespace = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (@namespace != null)
                name = $"{@namespace}::{name}";

            var internedName = new InternedString(name);

            // Remove all devices using the layout.
            for (var i = 0; i < m_DevicesCount;)
            {
                var device = m_Devices[i];
                if (IsControlOrChildUsingLayoutRecursive(device, internedName))
                {
                    RemoveDevice(device, keepOnListOfAvailableDevices: true);
                }
                else
                {
                    ++i;
                }
            }

            // Remove layout record.
            m_Layouts.layoutTypes.Remove(internedName);
            m_Layouts.layoutStrings.Remove(internedName);
            m_Layouts.layoutBuilders.Remove(internedName);
            m_Layouts.baseLayoutTable.Remove(internedName);

            ////TODO: check all layout inheritance chain for whether they are based on the layout and if so
            ////      remove those layouts, too

            // Let listeners know.
            for (var i = 0; i < m_LayoutChangeListeners.length; ++i)
                m_LayoutChangeListeners[i](name, InputControlLayoutChange.Removed);
        }

        public InputControlLayout TryLoadControlLayout(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!typeof(InputControl).IsAssignableFrom(type))
                throw new ArgumentException($"Type '{type.Name}' is not an InputControl", nameof(type));

            // Find the layout name that the given type was registered with.
            var layoutName = m_Layouts.TryFindLayoutForType(type);
            if (layoutName.IsEmpty())
                throw new ArgumentException(
                    $"Type '{type.Name}' has not been registered as a control layout", nameof(type));

            return m_Layouts.TryLoadLayout(layoutName);
        }

        public InputControlLayout TryLoadControlLayout(InternedString name)
        {
            return m_Layouts.TryLoadLayout(name);
        }

        ////FIXME: allowing the description to be modified as part of this is surprising; find a better way
        public InternedString TryFindMatchingControlLayout(ref InputDeviceDescription deviceDescription, int deviceId = InputDevice.InvalidDeviceId)
        {
            Profiler.BeginSample("InputSystem.TryFindMatchingControlLayout");
            ////TODO: this will want to take overrides into account

            // See if we can match by description.
            var layoutName = m_Layouts.TryFindMatchingLayout(deviceDescription);
            if (layoutName.IsEmpty())
            {
                // No, so try to match by device class. If we have a "Gamepad" layout,
                // for example, a device that classifies itself as a "Gamepad" will match
                // that layout.
                //
                // NOTE: Have to make sure here that we get a device layout and not a
                //       control layout.
                if (!string.IsNullOrEmpty(deviceDescription.deviceClass))
                {
                    var deviceClassLowerCase = new InternedString(deviceDescription.deviceClass);
                    var type = m_Layouts.GetControlTypeForLayout(deviceClassLowerCase);
                    if (type != null && typeof(InputDevice).IsAssignableFrom(type))
                        layoutName = new InternedString(deviceDescription.deviceClass);
                }
            }

            ////REVIEW: listeners registering new layouts from in here may potentially lead to the creation of devices; should we disallow that?
            ////REVIEW: if a callback picks a layout, should we re-run through the list of callbacks? or should we just remove haveOverridenLayoutName?
            // Give listeners a shot to select/create a layout.
            if (m_DeviceFindLayoutCallbacks.length > 0)
            {
                // First time we get here, put our delegate for executing device commands
                // in place. We wrap the call to IInputRuntime.DeviceCommand so that we don't
                // need to expose the runtime to the onFindLayoutForDevice callbacks.
                if (m_DeviceFindExecuteCommandDelegate == null)
                    m_DeviceFindExecuteCommandDelegate =
                        (ref InputDeviceCommand commandRef) =>
                    {
                        if (m_DeviceFindExecuteCommandDeviceId == InputDevice.InvalidDeviceId)
                            return InputDeviceCommand.GenericFailure;
                        return m_Runtime.DeviceCommand(m_DeviceFindExecuteCommandDeviceId, ref commandRef);
                    };
                m_DeviceFindExecuteCommandDeviceId = deviceId;

                var haveOverriddenLayoutName = false;
                for (var i = 0; i < m_DeviceFindLayoutCallbacks.length; ++i)
                {
                    var newLayout = m_DeviceFindLayoutCallbacks[i](ref deviceDescription, layoutName,
                                                                   m_DeviceFindExecuteCommandDelegate);

                    if (!string.IsNullOrEmpty(newLayout) && !haveOverriddenLayoutName)
                    {
                        layoutName = new InternedString(newLayout);
                        haveOverriddenLayoutName = true;
                    }
                }
            }

            Profiler.EndSample();
            return layoutName;
        }

        /// <summary>
        /// Return true if the given device layout is supported by the game according to <see cref="InputSettings.supportedDevices"/>.
        /// </summary>
        /// <param name="layoutName">Name of the device layout.</param>
        /// <returns>True if a device with the given layout should be created for the game, false otherwise.</returns>
        private bool IsDeviceLayoutMarkedAsSupportedInSettings(InternedString layoutName)
        {
            // In the editor, "Supported Devices" can be overridden by a user setting. This causes
            // all available devices to be added regardless of what "Supported Devices" says. This
            // is useful to ensure that things like keyboard, mouse, and pen keep working in the editor
            // even if not supported as devices in the game.
            #if UNITY_EDITOR
            if (InputEditorUserSettings.addDevicesNotSupportedByProject)
                return true;
            #endif

            var supportedDevices = m_Settings.supportedDevices;
            if (supportedDevices.Count == 0)
            {
                // If supportedDevices is empty, all device layouts are considered supported.
                return true;
            }

            for (var n = 0; n < supportedDevices.Count; ++n)
            {
                var supportedLayout = new InternedString(supportedDevices[n]);
                if (layoutName == supportedLayout || m_Layouts.IsBasedOn(supportedLayout, layoutName))
                    return true;
            }

            return false;
        }

        private bool DoesLayoutExist(InternedString name)
        {
            return m_Layouts.layoutTypes.ContainsKey(name) ||
                m_Layouts.layoutStrings.ContainsKey(name) ||
                m_Layouts.layoutBuilders.ContainsKey(name);
        }

        public IEnumerable<string> ListControlLayouts(string basedOn = null)
        {
            ////FIXME: this may add a name twice

            if (!string.IsNullOrEmpty(basedOn))
            {
                var internedBasedOn = new InternedString(basedOn);
                foreach (var entry in m_Layouts.layoutTypes)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        yield return entry.Key;
                foreach (var entry in m_Layouts.layoutStrings)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        yield return entry.Key;
                foreach (var entry in m_Layouts.layoutBuilders)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        yield return entry.Key;
            }
            else
            {
                foreach (var entry in m_Layouts.layoutTypes)
                    yield return entry.Key;
                foreach (var entry in m_Layouts.layoutStrings)
                    yield return entry.Key;
                foreach (var entry in m_Layouts.layoutBuilders)
                    yield return entry.Key;
            }
        }

        // Adds all controls that match the given path spec to the given list.
        // Returns number of controls added to the list.
        // NOTE: Does not create garbage.

        /// <summary>
        /// Adds to the given list all controls that match the given <see cref="InputControlPath">path spec</see>
        /// and are assignable to the given type.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="controls"></param>
        /// <typeparam name="TControl"></typeparam>
        /// <returns></returns>
        public int GetControls<TControl>(string path, ref InputControlList<TControl> controls)
            where TControl : InputControl
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            if (m_DevicesCount == 0)
                return 0;

            var deviceCount = m_DevicesCount;
            var numMatches = 0;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                numMatches += InputControlPath.TryFindControls(device, path, 0, ref controls);
            }

            return numMatches;
        }

        public void SetDeviceUsage(InputDevice device, InternedString usage)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.usages.Count == 1 && device.usages[0] == usage)
                return;
            if (device.usages.Count == 0 && usage.IsEmpty())
                return;

            device.ClearDeviceUsages();
            if (!usage.IsEmpty())
                device.AddDeviceUsage(usage);
            NotifyUsageChanged(device);
        }

        public void AddDeviceUsage(InputDevice device, InternedString usage)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (usage.IsEmpty())
                throw new ArgumentException("Usage string cannot be empty", nameof(usage));
            if (device.usages.Contains(usage))
                return;

            device.AddDeviceUsage(usage);
            NotifyUsageChanged(device);
        }

        public void RemoveDeviceUsage(InputDevice device, InternedString usage)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (usage.IsEmpty())
                throw new ArgumentException("Usage string cannot be empty", nameof(usage));
            if (!device.usages.Contains(usage))
                return;

            device.RemoveDeviceUsage(usage);
            NotifyUsageChanged(device);
        }

        private void NotifyUsageChanged(InputDevice device)
        {
            InputActionState.OnDeviceChange(device, InputDeviceChange.UsageChanged);

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.UsageChanged);

            ////REVIEW: This was for the XRController leftHand and rightHand getters but these do lookups dynamically now; remove?
            // Usage may affect current device so update.
            device.MakeCurrent();
        }

        ////TODO: make sure that no device or control with a '/' in the name can creep into the system

        public InputDevice AddDevice(Type type, string name = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Find the layout name that the given type was registered with.
            var layoutName = m_Layouts.TryFindLayoutForType(type);
            if (layoutName.IsEmpty())
            {
                // Automatically register the given type as a layout.
                if (layoutName.IsEmpty())
                {
                    layoutName = new InternedString(type.Name);
                    RegisterControlLayout(type.Name, type);
                }
            }

            Debug.Assert(!layoutName.IsEmpty(), name);

            // Note that since we go through the normal by-name lookup here, this will
            // still work if the layout from the type was override with a string layout.
            return AddDevice(layoutName, name);
        }

        // Creates a device from the given layout and adds it to the system.
        // NOTE: Creates garbage.
        public InputDevice AddDevice(string layout, string name = null, InternedString variants = new InternedString())
        {
            if (string.IsNullOrEmpty(layout))
                throw new ArgumentNullException(nameof(layout));

            var device = InputDevice.Build<InputDevice>(layout, variants);

            if (!string.IsNullOrEmpty(name))
                device.m_Name = new InternedString(name);

            AddDevice(device);

            return device;
        }

        // Add device with a forced ID. Used when creating devices reported to us by native.
        private InputDevice AddDevice(InternedString layout, int deviceId,
            string deviceName = null,
            InputDeviceDescription deviceDescription = new InputDeviceDescription(),
            InputDevice.DeviceFlags deviceFlags = 0,
            InternedString variants = default)
        {
            var device = InputDevice.Build<InputDevice>(new InternedString(layout),
                deviceDescription: deviceDescription,
                layoutVariants: variants);

            device.m_DeviceId = deviceId;
            device.m_Description = deviceDescription;
            device.m_DeviceFlags |= deviceFlags;
            if (!string.IsNullOrEmpty(deviceName))
                device.m_Name = new InternedString(deviceName);

            // Default display name to product name.
            if (!string.IsNullOrEmpty(deviceDescription.product))
                device.m_DisplayName = deviceDescription.product;

            AddDevice(device);

            return device;
        }

        public void AddDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrEmpty(device.layout))
                throw new InvalidOperationException("Device has no associated layout");

            // Ignore if the same device gets added multiple times.
            if (ArrayHelpers.Contains(m_Devices, device))
                return;

            MakeDeviceNameUnique(device);
            AssignUniqueDeviceId(device);

            // Add to list.
            device.m_DeviceIndex = ArrayHelpers.AppendWithCapacity(ref m_Devices, ref m_DevicesCount, device);

            ////REVIEW: Not sure a full-blown dictionary is the right way here. Alternatives are to keep
            ////        a sparse array that directly indexes using the linearly increasing IDs (though that
            ////        may get large over time). Or to just do a linear search through m_Devices (but
            ////        that may end up tapping a bunch of memory locations in the heap to find the right
            ////        device; could be improved by sorting m_Devices by ID and picking a good starting
            ////        point based on the ID we have instead of searching from [0] always).
            m_DevicesById[device.deviceId] = device;

            // Let InputStateBuffers know this device doesn't have any associated state yet.
            device.m_StateBlock.byteOffset = InputStateBlock.InvalidOffset;

            // Update state buffers.
            ReallocateStateBuffers();
            InitializeDefaultState(device);
            InitializeNoiseMask(device);

            // Update metrics.
            m_Metrics.maxNumDevices = Mathf.Max(m_DevicesCount, m_Metrics.maxNumDevices);
            m_Metrics.maxStateSizeInBytes = Mathf.Max((int)m_StateBuffers.totalSize, m_Metrics.maxStateSizeInBytes);

            // Make sure that if the device ID is listed in m_AvailableDevices, the device
            // is no longer marked as removed.
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                if (m_AvailableDevices[i].deviceId == device.deviceId)
                    m_AvailableDevices[i].isRemoved = false;
            }

            ////REVIEW: we may want to suppress this during the initial device discovery phase
            // Let actions re-resolve their paths.
            InputActionState.OnDeviceChange(device, InputDeviceChange.Added);

            // If the device wants automatic callbacks before input updates,
            // put it on the list.
            if (device is IInputUpdateCallbackReceiver beforeUpdateCallbackReceiver)
                onBeforeUpdate += beforeUpdateCallbackReceiver.OnUpdate;

            // If the device has state callbacks, make a note of it.
            if (device is IInputStateCallbackReceiver)
            {
                InstallBeforeUpdateHookIfNecessary();
                device.m_DeviceFlags |= InputDevice.DeviceFlags.HasStateCallbacks;
                m_HaveDevicesWithStateCallbackReceivers = true;
            }

            // If the device wants before-render updates, enable them if they
            // aren't already.
            if (device.updateBeforeRender)
                updateMask |= InputUpdateType.BeforeRender;

            // Notify device.
            device.NotifyAdded();

            ////REVIEW: is this really a good thing to do? just plugging in a device shouldn't make
            ////        it current, no?
            // Make the device current.
            device.MakeCurrent();

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Added);
        }

        ////TODO: this path should really put the device on the list of available devices
        ////TODO: this path should discover disconnected devices
        public InputDevice AddDevice(InputDeviceDescription description)
        {
            ////REVIEW: is throwing here really such a useful thing?
            return AddDevice(description, throwIfNoLayoutFound: true);
        }

        public InputDevice AddDevice(InputDeviceDescription description, bool throwIfNoLayoutFound,
            string deviceName = null, int deviceId = InputDevice.InvalidDeviceId, InputDevice.DeviceFlags deviceFlags = 0)
        {
            Profiler.BeginSample("InputSystem.AddDevice");
            // Look for matching layout.
            var layout = TryFindMatchingControlLayout(ref description, deviceId);

            // If no layout was found, bail out.
            if (layout.IsEmpty())
            {
                if (throwIfNoLayoutFound)
                    throw new ArgumentException($"Cannot find layout matching device description '{description}'", nameof(description));

                // If it's a device coming from the runtime, disable it.
                if (deviceId != InputDevice.InvalidDeviceId)
                {
                    var command = DisableDeviceCommand.Create();
                    m_Runtime.DeviceCommand(deviceId, ref command);
                }

                Profiler.EndSample();
                return null;
            }

            var device = AddDevice(layout, deviceId, deviceName, description, deviceFlags);
            device.m_Description = description;
            Profiler.EndSample();
            return device;
        }

        public void RemoveDevice(InputDevice device, bool keepOnListOfAvailableDevices = false)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // If device has not been added, ignore.
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            // Remove state monitors while device index is still valid.
            RemoveStateChangeMonitors(device);

            // Remove from device array.
            var deviceIndex = device.m_DeviceIndex;
            var deviceId = device.deviceId;
            if (deviceIndex < m_StateChangeMonitors.LengthSafe())
            {
                // m_StateChangeMonitors mirrors layout of m_Devices *but* may be shorter.
                var count = m_StateChangeMonitors.Length;
                ArrayHelpers.EraseAtWithCapacity(m_StateChangeMonitors, ref count, deviceIndex);
            }
            ArrayHelpers.EraseAtWithCapacity(m_Devices, ref m_DevicesCount, deviceIndex);

            m_DevicesById.Remove(deviceId);

            if (m_Devices != null)
            {
                // Remove from state buffers.
                ReallocateStateBuffers();
            }
            else
            {
                // No more devices. Kill state buffers.
                m_StateBuffers.FreeAll();
            }

            // Update device indices. Do this after reallocating state buffers as that call requires
            // the old indices to still be in place.
            for (var i = deviceIndex; i < m_DevicesCount; ++i)
                --m_Devices[i].m_DeviceIndex; // Indices have shifted down by one.
            device.m_DeviceIndex = InputDevice.kInvalidDeviceIndex;

            // Update list of available devices.
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                if (m_AvailableDevices[i].deviceId == deviceId)
                {
                    if (keepOnListOfAvailableDevices)
                        m_AvailableDevices[i].isRemoved = true;
                    else
                        ArrayHelpers.EraseAtWithCapacity(m_AvailableDevices, ref m_AvailableDeviceCount, i);
                    break;
                }
            }

            // Unbake offset into global state buffers.
            device.BakeOffsetIntoStateBlockRecursive((uint)-device.m_StateBlock.byteOffset);

            // Force enabled actions to remove controls from the device.
            // We've already set the device index to be invalid so we any attempts
            // by actions to uninstall state monitors will get ignored.
            InputActionState.OnDeviceChange(device, InputDeviceChange.Removed);

            // Kill before update callback, if applicable.
            if (device is IInputUpdateCallbackReceiver beforeUpdateCallbackReceiver)
                onBeforeUpdate -= beforeUpdateCallbackReceiver.OnUpdate;

            // Disable before-render updates if this was the last device
            // that requires them.
            if (device.updateBeforeRender)
            {
                var haveDeviceRequiringBeforeRender = false;
                for (var i = 0; i < m_DevicesCount; ++i)
                    if (m_Devices[i].updateBeforeRender)
                    {
                        haveDeviceRequiringBeforeRender = true;
                        break;
                    }

                if (!haveDeviceRequiringBeforeRender)
                    updateMask &= ~InputUpdateType.BeforeRender;
            }

            // Let device know.
            device.NotifyRemoved();

            // Let listeners know.
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Removed);
        }

        public void FlushDisconnectedDevices()
        {
            m_DisconnectedDevices.Clear(m_DisconnectedDevicesCount);
            m_DisconnectedDevicesCount = 0;
        }

        public InputDevice TryGetDevice(string nameOrLayout)
        {
            if (string.IsNullOrEmpty(nameOrLayout))
                throw new ArgumentException("Name is null or empty.", nameof(nameOrLayout));

            if (m_DevicesCount == 0)
                return null;

            var nameOrLayoutLowerCase = nameOrLayout.ToLower();

            for (var i = 0; i < m_DevicesCount; ++i)
            {
                var device = m_Devices[i];
                if (device.m_Name.ToLower() == nameOrLayoutLowerCase ||
                    device.m_Layout.ToLower() == nameOrLayoutLowerCase)
                    return device;
            }

            return null;
        }

        public InputDevice GetDevice(string nameOrLayout)
        {
            var device = TryGetDevice(nameOrLayout);
            if (device == null)
                throw new ArgumentException($"Cannot find device with name or layout '{nameOrLayout}'", nameof(nameOrLayout));

            return device;
        }

        public InputDevice TryGetDevice(Type layoutType)
        {
            var layoutName = m_Layouts.TryFindLayoutForType(layoutType);
            if (layoutName.IsEmpty())
                return null;

            return TryGetDevice(layoutName);
        }

        public InputDevice TryGetDeviceById(int id)
        {
            if (m_DevicesById.TryGetValue(id, out var result))
                return result;
            return null;
        }

        // Adds any device that's been reported to the system but could not be matched to
        // a layout to the given list.
        public int GetUnsupportedDevices(List<InputDeviceDescription> descriptions)
        {
            if (descriptions == null)
                throw new ArgumentNullException(nameof(descriptions));

            var numFound = 0;
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                if (TryGetDeviceById(m_AvailableDevices[i].deviceId) != null)
                    continue;

                descriptions.Add(m_AvailableDevices[i].description);
                ++numFound;
            }

            return numFound;
        }

        ////TODO: this should reset the device to its default state
        public void EnableOrDisableDevice(InputDevice device, bool enable)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // Ignore if device already enabled/disabled.
            if (device.enabled == enable)
                return;

            // Set/clear flag.
            if (!enable)
                device.m_DeviceFlags |= InputDevice.DeviceFlags.Disabled;
            else
                device.m_DeviceFlags &= ~InputDevice.DeviceFlags.Disabled;

            // Send command to tell backend about status change.
            if (enable)
            {
                var command = EnableDeviceCommand.Create();
                device.ExecuteCommand(ref command);
            }
            else
            {
                var command = DisableDeviceCommand.Create();
                device.ExecuteCommand(ref command);
            }

            // Let listeners know.
            var deviceChange = enable ? InputDeviceChange.Enabled : InputDeviceChange.Disabled;
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, deviceChange);
        }

        ////TODO: support combining monitors for bitfields
        public void AddStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex)
        {
            Debug.Assert(m_DevicesCount > 0);

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            // Allocate/reallocate monitor arrays, if necessary.
            // We lazy-sync it to array of devices.
            if (m_StateChangeMonitors == null)
                m_StateChangeMonitors = new StateChangeMonitorsForDevice[m_DevicesCount];
            else if (m_StateChangeMonitors.Length <= deviceIndex)
                Array.Resize(ref m_StateChangeMonitors, m_DevicesCount);

            // Add record.
            m_StateChangeMonitors[deviceIndex].Add(control, monitor, monitorIndex);
        }

        private void RemoveStateChangeMonitors(InputDevice device)
        {
            if (m_StateChangeMonitors == null)
                return;

            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            if (deviceIndex >= m_StateChangeMonitors.Length)
                return;

            m_StateChangeMonitors[deviceIndex].Clear();

            // Clear timeouts pending on any control on the device.
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
                if (m_StateChangeMonitorTimeouts[i].control?.device == device)
                    m_StateChangeMonitorTimeouts[i] = default;
        }

        public void RemoveStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex)
        {
            if (m_StateChangeMonitors == null)
                return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            // Ignore if device has already been removed.
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            // Ignore if there are no state monitors set up for the device.
            if (deviceIndex >= m_StateChangeMonitors.Length)
                return;

            m_StateChangeMonitors[deviceIndex].Remove(monitor, monitorIndex);

            // Remove pending timeouts on the monitor.
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
                if (m_StateChangeMonitorTimeouts[i].monitor == monitor &&
                    m_StateChangeMonitorTimeouts[i].monitorIndex == monitorIndex)
                    m_StateChangeMonitorTimeouts[i] = default;
        }

        public void AddStateChangeMonitorTimeout(InputControl control, IInputStateChangeMonitor monitor, double time, long monitorIndex, int timerIndex)
        {
            m_StateChangeMonitorTimeouts.Append(
                new StateChangeMonitorTimeout
                {
                    control = control,
                    time = time,
                    monitor = monitor,
                    monitorIndex = monitorIndex,
                    timerIndex = timerIndex,
                });
        }

        public void RemoveStateChangeMonitorTimeout(IInputStateChangeMonitor monitor, long monitorIndex, int timerIndex)
        {
            var timeoutCount = m_StateChangeMonitorTimeouts.length;
            for (var i = 0; i < timeoutCount; ++i)
            {
                ////REVIEW: can we avoid the repeated array lookups without copying the struct out?
                if (ReferenceEquals(m_StateChangeMonitorTimeouts[i].monitor, monitor)
                    && m_StateChangeMonitorTimeouts[i].monitorIndex == monitorIndex
                    && m_StateChangeMonitorTimeouts[i].timerIndex == timerIndex)
                {
                    m_StateChangeMonitorTimeouts[i] = default;
                    break;
                }
            }
        }

        public unsafe void QueueEvent(InputEventPtr ptr)
        {
            m_Runtime.QueueEvent(ptr.data);
        }

        public unsafe void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            // Don't bother keeping the data on the managed side. Just stuff the raw data directly
            // into the native buffers. This also means this method is thread-safe.
            m_Runtime.QueueEvent((InputEvent*)UnsafeUtility.AddressOf(ref inputEvent));
        }

        public void Update()
        {
            Update(defaultUpdateType);
        }

        public void Update(InputUpdateType updateType)
        {
            m_Runtime.Update(updateType);
        }

        internal void Initialize(IInputRuntime runtime, InputSettings settings)
        {
            Debug.Assert(settings != null);

            m_Settings = settings;

            InitializeData();
            InstallRuntime(runtime);
            InstallGlobals();

            ApplySettings();
        }

        internal void Destroy()
        {
            // There isn't really much of a point in removing devices but we still
            // want to clear out any global state they may be keeping. So just tell
            // the devices that they got removed without actually removing them.
            for (var i = 0; i < m_DevicesCount; ++i)
                m_Devices[i].NotifyRemoved();

            // Free all state memory.
            m_StateBuffers.FreeAll();

            // Uninstall globals.
            UninstallGlobals();

            // Destroy settings if they are temporary.
            if (m_Settings != null && m_Settings.hideFlags == HideFlags.HideAndDontSave)
                Object.DestroyImmediate(m_Settings);
        }

        internal void InitializeData()
        {
            m_Layouts.Allocate();
            m_Processors.Initialize();
            m_Interactions.Initialize();
            m_Composites.Initialize();
            m_DevicesById = new Dictionary<int, InputDevice>();

            // Determine our default set of enabled update types. By
            // default we enable both fixed and dynamic update because
            // we don't know which one the user is going to use. The user
            // can manually turn off one of them to optimize operation.
            m_UpdateMask = InputUpdateType.Dynamic | InputUpdateType.Fixed;
            m_HasFocus = Application.isFocused;
#if UNITY_EDITOR
            m_UpdateMask |= InputUpdateType.Editor;
#endif

            // Default polling frequency is 60 Hz.
            m_PollingFrequency = 60;

            // Register layouts.
            RegisterControlLayout("Axis", typeof(AxisControl)); // Controls.
            RegisterControlLayout("Button", typeof(ButtonControl));
            RegisterControlLayout("DiscreteButton", typeof(DiscreteButtonControl));
            RegisterControlLayout("Key", typeof(KeyControl));
            RegisterControlLayout("Analog", typeof(AxisControl));
            RegisterControlLayout("Integer", typeof(IntegerControl));
            RegisterControlLayout("Digital", typeof(IntegerControl));
            RegisterControlLayout("Double", typeof(DoubleControl));
            RegisterControlLayout("Vector2", typeof(Vector2Control));
            RegisterControlLayout("Vector3", typeof(Vector3Control));
            RegisterControlLayout("Quaternion", typeof(QuaternionControl));
            RegisterControlLayout("Stick", typeof(StickControl));
            RegisterControlLayout("Dpad", typeof(DpadControl));
            RegisterControlLayout("DpadAxis", typeof(DpadControl.DpadAxisControl));
            RegisterControlLayout("AnyKey", typeof(AnyKeyControl));
            RegisterControlLayout("Touch", typeof(TouchControl));
            RegisterControlLayout("TouchPhase", typeof(TouchPhaseControl));
            RegisterControlLayout("TouchPress", typeof(TouchPressControl));

            RegisterControlLayout("Gamepad", typeof(Gamepad)); // Devices.
            RegisterControlLayout("Joystick", typeof(Joystick));
            RegisterControlLayout("Keyboard", typeof(Keyboard));
            RegisterControlLayout("Pointer", typeof(Pointer));
            RegisterControlLayout("Mouse", typeof(Mouse));
            RegisterControlLayout("Pen", typeof(Pen));
            RegisterControlLayout("Touchscreen", typeof(Touchscreen));
            RegisterControlLayout("Sensor", typeof(Sensor));
            RegisterControlLayout("Accelerometer", typeof(Accelerometer));
            RegisterControlLayout("Gyroscope", typeof(Gyroscope));
            RegisterControlLayout("GravitySensor", typeof(GravitySensor));
            RegisterControlLayout("AttitudeSensor", typeof(AttitudeSensor));
            RegisterControlLayout("LinearAccelerationSensor", typeof(LinearAccelerationSensor));
            RegisterControlLayout("MagneticFieldSensor", typeof(MagneticFieldSensor));
            RegisterControlLayout("LightSensor", typeof(LightSensor));
            RegisterControlLayout("PressureSensor", typeof(PressureSensor));
            RegisterControlLayout("HumiditySensor", typeof(HumiditySensor));
            RegisterControlLayout("AmbientTemperatureSensor", typeof(AmbientTemperatureSensor));
            RegisterControlLayout("StepCounter", typeof(StepCounter));
            RegisterControlLayout("TrackedDevice", typeof(TrackedDevice));

            // Register processors.
            processors.AddTypeRegistration("Invert", typeof(InvertProcessor));
            processors.AddTypeRegistration("InvertVector2", typeof(InvertVector2Processor));
            processors.AddTypeRegistration("InvertVector3", typeof(InvertVector3Processor));
            processors.AddTypeRegistration("Clamp", typeof(ClampProcessor));
            processors.AddTypeRegistration("Normalize", typeof(NormalizeProcessor));
            processors.AddTypeRegistration("NormalizeVector2", typeof(NormalizeVector2Processor));
            processors.AddTypeRegistration("NormalizeVector3", typeof(NormalizeVector3Processor));
            processors.AddTypeRegistration("Scale", typeof(ScaleProcessor));
            processors.AddTypeRegistration("ScaleVector2", typeof(ScaleVector2Processor));
            processors.AddTypeRegistration("ScaleVector3", typeof(ScaleVector3Processor));
            processors.AddTypeRegistration("StickDeadzone", typeof(StickDeadzoneProcessor));
            processors.AddTypeRegistration("AxisDeadzone", typeof(AxisDeadzoneProcessor));
            processors.AddTypeRegistration("CompensateDirection", typeof(CompensateDirectionProcessor));
            processors.AddTypeRegistration("CompensateRotation", typeof(CompensateRotationProcessor));

            #if UNITY_EDITOR
            processors.AddTypeRegistration("AutoWindowSpace", typeof(EditorWindowSpaceProcessor));
            #endif

            // Register interactions.
            interactions.AddTypeRegistration("Hold", typeof(HoldInteraction));
            interactions.AddTypeRegistration("Tap", typeof(TapInteraction));
            interactions.AddTypeRegistration("SlowTap", typeof(SlowTapInteraction));
            interactions.AddTypeRegistration("MultiTap", typeof(MultiTapInteraction));
            interactions.AddTypeRegistration("Press", typeof(PressInteraction));

            // Register composites.
            composites.AddTypeRegistration("1DAxis", typeof(AxisComposite));
            composites.AddTypeRegistration("2DVector", typeof(Vector2Composite));
            composites.AddTypeRegistration("Axis", typeof(AxisComposite));// Alias for pre-0.2 name.
            composites.AddTypeRegistration("Dpad", typeof(Vector2Composite));// Alias for pre-0.2 name.
            composites.AddTypeRegistration("ButtonWithOneModifier", typeof(ButtonWithOneModifier));
            composites.AddTypeRegistration("ButtonWithTwoModifiers", typeof(ButtonWithTwoModifiers));
        }

        internal void InstallRuntime(IInputRuntime runtime)
        {
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onBeforeUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
                m_Runtime.onPlayerFocusChanged = null;
                m_Runtime.onShouldRunUpdate = null;
            }

            m_Runtime = runtime;
            m_Runtime.onUpdate = OnUpdate;
            m_Runtime.onDeviceDiscovered = OnNativeDeviceDiscovered;
            m_Runtime.onPlayerFocusChanged = OnFocusChanged;
            m_Runtime.onShouldRunUpdate = ShouldRunUpdate;
            m_Runtime.pollingFrequency = pollingFrequency;

            // We only hook NativeInputSystem.onBeforeUpdate if necessary.
            if (m_BeforeUpdateListeners.length > 0 || m_HaveDevicesWithStateCallbackReceivers)
            {
                m_Runtime.onBeforeUpdate = OnBeforeUpdate;
                m_NativeBeforeUpdateHooked = true;
            }

            #if UNITY_ANALYTICS || UNITY_EDITOR
            InputAnalytics.Initialize(this);
            m_Runtime.onShutdown = () => InputAnalytics.OnShutdown(this);
            #endif
        }

        internal void InstallGlobals()
        {
            Debug.Assert(m_Runtime != null);

            InputControlLayout.s_Layouts = m_Layouts;
            InputProcessor.s_Processors = m_Processors;
            InputInteraction.s_Interactions = m_Interactions;
            InputBindingComposite.s_Composites = m_Composites;

            InputRuntime.s_Instance = m_Runtime;
            InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup =
                m_Runtime.currentTimeOffsetToRealtimeSinceStartup;

            // Reset update state.
            InputUpdate.Restore(new InputUpdate.SerializedState());

            unsafe
            {
                InputStateBuffers.SwitchTo(m_StateBuffers, InputUpdateType.Dynamic);
                InputStateBuffers.s_DefaultStateBuffer = m_StateBuffers.defaultStateBuffer;
                InputStateBuffers.s_NoiseMaskBuffer = m_StateBuffers.noiseMaskBuffer;
            }
        }

        internal void UninstallGlobals()
        {
            if (ReferenceEquals(InputControlLayout.s_Layouts.baseLayoutTable, m_Layouts.baseLayoutTable))
                InputControlLayout.s_Layouts = new InputControlLayout.Collection();
            if (ReferenceEquals(InputProcessor.s_Processors.table, m_Processors.table))
                InputProcessor.s_Processors = new TypeTable();
            if (ReferenceEquals(InputInteraction.s_Interactions.table, m_Interactions.table))
                InputInteraction.s_Interactions = new TypeTable();
            if (ReferenceEquals(InputBindingComposite.s_Composites.table, m_Composites.table))
                InputBindingComposite.s_Composites = new TypeTable();

            // Clear layout cache.
            InputControlLayout.s_CacheInstance = default;
            InputControlLayout.s_CacheInstanceRef = 0;

            // Detach from runtime.
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
                m_Runtime.onBeforeUpdate = null;
                m_Runtime.onPlayerFocusChanged = null;
                m_Runtime.onShouldRunUpdate = null;

                if (ReferenceEquals(InputRuntime.s_Instance, m_Runtime))
                    InputRuntime.s_Instance = null;
            }
        }

        [Serializable]
        internal struct AvailableDevice
        {
            public InputDeviceDescription description;
            public int deviceId;
            public bool isNative;
            public bool isRemoved;
        }

        // Used by EditorInputControlLayoutCache to determine whether its state is outdated.
        internal int m_LayoutRegistrationVersion;
        private float m_PollingFrequency;

        internal InputControlLayout.Collection m_Layouts;
        private TypeTable m_Processors;
        private TypeTable m_Interactions;
        private TypeTable m_Composites;

        private int m_DevicesCount;
        private InputDevice[] m_Devices;

        private Dictionary<int, InputDevice> m_DevicesById;
        internal int m_AvailableDeviceCount;
        internal AvailableDevice[] m_AvailableDevices; // A record of all devices reported to the system (from native or user code).

        ////REVIEW: should these be weak-referenced?
        internal int m_DisconnectedDevicesCount;
        internal InputDevice[] m_DisconnectedDevices;

        private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        internal InputStateBuffers m_StateBuffers;

        // We don't use UnityEvents and thus don't persist the callbacks during domain reloads.
        // Restoration of UnityActions is unreliable and it's too easy to end up with double
        // registrations what will lead to all kinds of misbehavior.
        private InlinedArray<DeviceChangeListener> m_DeviceChangeListeners;
        private InlinedArray<DeviceStateChangeListener> m_DeviceStateChangeListeners;
        private InlinedArray<InputDeviceFindControlLayoutDelegate> m_DeviceFindLayoutCallbacks;
        internal InlinedArray<InputDeviceCommandDelegate> m_DeviceCommandCallbacks;
        private InlinedArray<LayoutChangeListener> m_LayoutChangeListeners;
        private InlinedArray<EventListener> m_EventListeners;
        private InlinedArray<UpdateListener> m_BeforeUpdateListeners;
        private InlinedArray<UpdateListener> m_AfterUpdateListeners;
        private InlinedArray<Action> m_SettingsChangedListeners;
        private bool m_NativeBeforeUpdateHooked;
        private bool m_HaveDevicesWithStateCallbackReceivers;
        private bool m_HasFocus;

        // We allocate the 'executeDeviceCommand' closure passed to 'onFindLayoutForDevice'
        // only once to avoid creating garbage.
        private InputDeviceExecuteCommandDelegate m_DeviceFindExecuteCommandDelegate;
        private int m_DeviceFindExecuteCommandDeviceId;

        #if UNITY_ANALYTICS || UNITY_EDITOR
        private bool m_HaveSentStartupAnalytics;
        #endif

        internal IInputRuntime m_Runtime;
        internal InputMetrics m_Metrics;
        internal InputSettings m_Settings;

        #if UNITY_EDITOR
        internal IInputDiagnostics m_Diagnostics;
        #endif

        // Maps a single control to an action interested in the control. If
        // multiple actions are interested in the same control, we will end up
        // processing the control repeatedly but we assume this is the exception
        // and so optimize for the case where there's only one action going to
        // a control.
        //
        // Split into two structures to keep data needed only when there is an
        // actual value change out of the data we need for doing the scanning.
        internal struct StateChangeMonitorListener
        {
            public InputControl control;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
        }
        internal struct StateChangeMonitorsForDevice
        {
            public MemoryHelpers.BitRegion[] memoryRegions;
            public StateChangeMonitorListener[] listeners;
            public DynamicBitfield signalled;

            public int count => signalled.length;

            public void Add(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex)
            {
                // NOTE: This method must only *append* to arrays. This way we can safely add data while traversing
                //       the arrays in FireStateChangeNotifications. Note that appending *may* mean that the arrays
                //       are switched to larger arrays.

                // Record listener.
                var listenerCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref listeners, ref listenerCount,
                    new StateChangeMonitorListener {monitor = monitor, monitorIndex = monitorIndex, control = control});

                // Record memory region.
                ref var controlStateBlock = ref control.m_StateBlock;
                var memoryRegionCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref memoryRegions, ref memoryRegionCount,
                    new MemoryHelpers.BitRegion(controlStateBlock.byteOffset - control.device.stateBlock.byteOffset,
                        controlStateBlock.bitOffset, controlStateBlock.sizeInBits));

                signalled.SetLength(signalled.length + 1);
            }

            public void Remove(IInputStateChangeMonitor monitor, long monitorIndex)
            {
                // NOTE: This must *not* actually destroy the record for the monitor as we may currently be traversing the
                //       arrays in FireStateChangeNotifications. Instead, we only invalidate entries here and leave it to
                //       ProcessStateChangeMonitors to compact arrays.

                if (listeners == null)
                    return;

                for (var i = 0; i < signalled.length; ++i)
                    if (ReferenceEquals(listeners[i].monitor, monitor) && listeners[i].monitorIndex == monitorIndex)
                    {
                        listeners[i] = default;
                        memoryRegions[i] = default;
                        signalled.ClearBit(i);
                        break;
                    }
            }

            public void Clear()
            {
                // We don't actually release memory we've potentially allocated but rather just reset
                // our count to zero.
                listeners.Clear(count);
                signalled.SetLength(0);
            }
        }

        // Indices correspond with those in m_Devices.
        internal StateChangeMonitorsForDevice[] m_StateChangeMonitors;

        /// <summary>
        /// Record for a timeout installed on a state change monitor.
        /// </summary>
        private struct StateChangeMonitorTimeout
        {
            public InputControl control;
            public double time;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
            public int timerIndex;
        }

        private InlinedArray<StateChangeMonitorTimeout> m_StateChangeMonitorTimeouts;

        ////REVIEW: Make it so that device names *always* have a number appended? (i.e. Gamepad1, Gamepad2, etc. instead of Gamepad, Gamepad1, etc)

        private void MakeDeviceNameUnique(InputDevice device)
        {
            if (m_DevicesCount == 0)
                return;

            var deviceName = StringHelpers.MakeUniqueName(device.name, m_Devices, x => x != null ? x.name : string.Empty);
            if (deviceName != device.name)
            {
                // If we have changed the name of the device, nuke all path strings in the control
                // hierarchy so that they will get re-recreated when queried.
                ResetControlPathsRecursive(device);

                // Assign name.
                device.m_Name = new InternedString(deviceName);
            }
        }

        private static void ResetControlPathsRecursive(InputControl control)
        {
            control.m_Path = null;

            var children = control.children;
            var childCount = children.Count;

            for (var i = 0; i < childCount; ++i)
                ResetControlPathsRecursive(children[i]);
        }

        private void AssignUniqueDeviceId(InputDevice device)
        {
            // If the device already has an ID, make sure it's unique.
            if (device.deviceId != InputDevice.InvalidDeviceId)
            {
                // Safety check to make sure out IDs are really unique.
                // Given they are assigned by the native system they should be fine
                // but let's make sure.
                var existingDeviceWithId = TryGetDeviceById(device.deviceId);
                if (existingDeviceWithId != null)
                    throw new InvalidOperationException(
                        $"Duplicate device ID {device.deviceId} detected for devices '{device.name}' and '{existingDeviceWithId.name}'");
            }
            else
            {
                device.m_DeviceId = m_Runtime.AllocateDeviceId();
            }
        }

        // (Re)allocates state buffers and assigns each device that's been added
        // a segment of the buffer. Preserves the current state of devices.
        // NOTE: Installs the buffers globally.
        private unsafe void ReallocateStateBuffers()
        {
            var oldBuffers = m_StateBuffers;

            // Allocate new buffers.
            var newBuffers = new InputStateBuffers();
            newBuffers.AllocateAll(m_Devices, m_DevicesCount);

            // Migrate state.
            newBuffers.MigrateAll(m_Devices, m_DevicesCount, oldBuffers);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            InputStateBuffers.s_DefaultStateBuffer = newBuffers.defaultStateBuffer;
            InputStateBuffers.s_NoiseMaskBuffer = newBuffers.noiseMaskBuffer;

            // Switch to buffers.
            InputStateBuffers.SwitchTo(m_StateBuffers,
                InputUpdate.s_LastUpdateType != InputUpdateType.None ? InputUpdate.s_LastUpdateType : defaultUpdateType);

            ////TODO: need to update state change monitors
        }

        /// <summary>
        /// Initialize default state for given device.
        /// </summary>
        /// <param name="device">A newly added input device.</param>
        /// <remarks>
        /// For every device, one copy of its state is kept around which is initialized with the default
        /// values for the device. If the device has no control that has an explicitly specified control
        /// value, the buffer simply contains all zeroes.
        ///
        /// The default state buffer is initialized once when a device is added to the system and then
        /// migrated by <see cref="InputStateBuffers"/> like other device state and removed when the device
        /// is removed from the system.
        /// </remarks>
        private unsafe void InitializeDefaultState(InputDevice device)
        {
            // Nothing to do if device has a default state of all zeroes.
            if (!device.hasControlsWithDefaultState)
                return;

            // Otherwise go through each control and write its default value.
            var controls = device.allControls;
            var controlCount = controls.Count;
            var defaultStateBuffer = m_StateBuffers.defaultStateBuffer;
            for (var n = 0; n < controlCount; ++n)
            {
                var control = controls[n];
                if (!control.hasDefaultState)
                    continue;

                control.m_StateBlock.Write(defaultStateBuffer, control.m_DefaultState);
            }

            // Copy default state to all front and back buffers.
            var stateBlock = device.m_StateBlock;
            var deviceIndex = device.m_DeviceIndex;
            if (m_StateBuffers.m_PlayerStateBuffers.valid)
            {
                stateBlock.CopyToFrom(m_StateBuffers.m_PlayerStateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                stateBlock.CopyToFrom(m_StateBuffers.m_PlayerStateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
            }

            #if UNITY_EDITOR
            if (m_StateBuffers.m_EditorStateBuffers.valid)
            {
                stateBlock.CopyToFrom(m_StateBuffers.m_EditorStateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                stateBlock.CopyToFrom(m_StateBuffers.m_EditorStateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
            }
            #endif
        }

        private unsafe void InitializeNoiseMask(InputDevice device)
        {
            Debug.Assert(device != null, "Device must not be null");
            Debug.Assert(device.added, "Device must have been added");
            Debug.Assert(device.stateBlock.byteOffset != InputStateBlock.InvalidOffset, "Device state block offset is invalid");
            Debug.Assert(
                device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes <= m_StateBuffers.sizePerBuffer,
                "Device state block is not contained in state buffer");

            var controls = device.allControls;
            var controlCount = controls.Count;

            // Assume that everything in the device is noise. This way we also catch memory regions
            // that are not actually covered by a control and implicitly mark them as noise (e.g. the
            // report ID in HID input reports).
            //
            // NOTE: Noise is indicated by *unset* bits so we don't have to do anything here to start
            //       with all-noise as we expect noise mask memory to be cleared on allocation.

            var noiseMaskBuffer = m_StateBuffers.noiseMaskBuffer;

            ////FIXME: this needs to properly take leaf vs non-leaf controls into account

            // Go through controls and for each one that isn't noisy, set the control's
            // bits in the mask.
            for (var n = 0; n < controlCount; ++n)
            {
                var control = controls[n];
                if (control.noisy)
                    continue;

                ref var stateBlock = ref control.m_StateBlock;

                Debug.Assert(stateBlock.byteOffset != InputStateBlock.InvalidOffset, "Byte offset is invalid on control's state block");
                Debug.Assert(stateBlock.bitOffset != InputStateBlock.InvalidOffset, "Bit offset is invalid on control's state block");
                Debug.Assert(stateBlock.sizeInBits != InputStateBlock.InvalidOffset, "Size is invalid on control's state block");
                Debug.Assert(stateBlock.byteOffset >= device.stateBlock.byteOffset, "Control's offset is located below device's offset");
                Debug.Assert(stateBlock.byteOffset + stateBlock.alignedSizeInBytes <=
                    device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes, "Control state block lies outside of state buffer");

                MemoryHelpers.SetBitsInBuffer(noiseMaskBuffer, (int)stateBlock.byteOffset, (int)stateBlock.bitOffset,
                    (int)stateBlock.sizeInBits, true);
            }
        }

        private void OnNativeDeviceDiscovered(int deviceId, string deviceDescriptor)
        {
            // Make sure we're not adding to m_AvailableDevices before we restored what we
            // had before a domain reload.
            RestoreDevicesAfterDomainReloadIfNecessary();

            // See if we have a disconnected device we can revive.
            // NOTE: We do this all the way up here as the first thing before we even parse the JSON descriptor so
            //       if we do have a device we can revive, we can do so without allocating any GC memory.
            var device = TryMatchDisconnectedDevice(deviceDescriptor);

            // Parse description, if need be.
            var description = device?.description ?? InputDeviceDescription.FromJson(deviceDescriptor);

            // Add it.
            var markAsRemoved = false;
            try
            {
                // If we have a restricted set of supported devices, first check if it's a device
                // we should support.
                if (m_Settings.supportedDevices.Count > 0)
                {
                    var layout = device != null ? device.m_Layout : TryFindMatchingControlLayout(ref description, deviceId);
                    if (!IsDeviceLayoutMarkedAsSupportedInSettings(layout))
                    {
                        // Not supported. Ignore device. Still will get added to m_AvailableDevices
                        // list in finally clause below. If later the set of supported devices changes
                        // so that the device is now supported, ApplySettings() will pull it back out
                        // and create the device.
                        markAsRemoved = true;
                        return;
                    }
                }

                if (device != null)
                {
                    // It's a device we pulled from the disconnected list. Update the device with the
                    // new ID, re-add it and notify that we've reconnected.

                    device.m_DeviceId = deviceId;
                    AddDevice(device);

                    for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                        m_DeviceChangeListeners[i](device, InputDeviceChange.Reconnected);
                }
                else
                {
                    // Go through normal machinery to try to create a new device.
                    AddDevice(description, throwIfNoLayoutFound: false, deviceId: deviceId,
                        deviceFlags: InputDevice.DeviceFlags.Native);
                }
            }
            // We're catching exceptions very aggressively here. The reason is that we don't want
            // exceptions thrown as a result of trying to create devices from device discoveries reported
            // by native to break the system as a whole. Instead, we want to make the error visible but then
            // go and work with whatever devices we *did* manage to create successfully.
            catch (Exception exception)
            {
                Debug.LogError($"Could not create a device for '{description}' (exception: {exception})");
            }
            finally
            {
                // Remember it. Do this *after* the AddDevice() call above so that if there's
                // a listener creating layouts on the fly we won't end up matching this device and
                // create an InputDevice right away (which would then conflict with the one we
                // create in AddDevice).
                ArrayHelpers.AppendWithCapacity(ref m_AvailableDevices, ref m_AvailableDeviceCount,
                    new AvailableDevice
                    {
                        description = description,
                        deviceId = deviceId,
                        isNative = true,
                        isRemoved = markAsRemoved,
                    });
            }
        }

        private InputDevice TryMatchDisconnectedDevice(string deviceDescriptor)
        {
            for (var i = 0; i < m_DisconnectedDevicesCount; ++i)
            {
                var device = m_DisconnectedDevices[i];
                var description = device.description;

                // We don't parse the full description but rather go property by property in order to not
                // allocate GC memory if we can avoid it.

                if (!string.IsNullOrEmpty(description.interfaceName) &&
                    !InputDeviceDescription.ComparePropertyToDeviceDescriptor("interface", description.interfaceName, deviceDescriptor))
                    continue;
                if (!string.IsNullOrEmpty(description.product) &&
                    !InputDeviceDescription.ComparePropertyToDeviceDescriptor("product", description.product, deviceDescriptor))
                    continue;
                if (!string.IsNullOrEmpty(description.manufacturer) &&
                    !InputDeviceDescription.ComparePropertyToDeviceDescriptor("manufacturer", description.manufacturer, deviceDescriptor))
                    continue;
                if (!string.IsNullOrEmpty(description.deviceClass) &&
                    !InputDeviceDescription.ComparePropertyToDeviceDescriptor("type", description.deviceClass, deviceDescriptor))
                    continue;

                // We ignore capabilities here.

                ArrayHelpers.EraseAtWithCapacity(m_DisconnectedDevices, ref m_DisconnectedDevicesCount, i);
                return device;
            }

            return null;
        }

        private void InstallBeforeUpdateHookIfNecessary()
        {
            if (m_NativeBeforeUpdateHooked || m_Runtime == null)
                return;

            m_Runtime.onBeforeUpdate = OnBeforeUpdate;
            m_NativeBeforeUpdateHooked = true;
        }

        private void RestoreDevicesAfterDomainReloadIfNecessary()
        {
            #if UNITY_EDITOR
            if (m_SavedDeviceStates != null)
                RestoreDevicesAfterDomainReload();
            #endif
        }

        private void WarnAboutDevicesFailingToRecreateAfterDomainReload()
        {
            // If we still have any saved device states, we have devices that we couldn't figure
            // out how to recreate after a domain reload. Log a warning for each of them and
            // let go of them.
            #if UNITY_EDITOR
            if (m_SavedDeviceStates == null)
                return;

            for (var i = 0; i < m_SavedDeviceStates.Length; ++i)
            {
                ref var state = ref m_SavedDeviceStates[i];
                Debug.LogWarning($"Could not recreate device '{state.name}' with layout '{state.layout}' after domain reload");
            }

            // At this point, we throw the device states away and forget about
            // what we had before the domain reload.
            m_SavedDeviceStates = null;
            #endif
        }

        private void OnBeforeUpdate(InputUpdateType updateType)
        {
            // Restore devices before checking update mask. See InputSystem.RunInitialUpdate().
            RestoreDevicesAfterDomainReloadIfNecessary();

            if ((updateType & m_UpdateMask) == 0)
                return;

            InputStateBuffers.SwitchTo(m_StateBuffers, updateType);

            // For devices that have state callbacks, tell them we're carrying state over
            // into the next frame.
            if (m_HaveDevicesWithStateCallbackReceivers && updateType != InputUpdateType.BeforeRender) ////REVIEW: before-render handling is probably wrong
            {
                ////TODO: have to handle updatecount here, too
                InputUpdate.s_LastUpdateType = updateType;

                for (var i = 0; i < m_DevicesCount; ++i)
                {
                    var device = m_Devices[i];
                    if ((device.m_DeviceFlags & InputDevice.DeviceFlags.HasStateCallbacks) == 0)
                        continue;

                    // NOTE: We do *not* perform a buffer flip here as we do not want to change what is the
                    //       current and what is the previous state when we carry state forward. Rather,
                    //       OnBeforeUpdate, if it modifies state, it modifies the current state directly.
                    //       Also, for the same reasons, we do not modify the dynamic/fixed update counts
                    //       on the device. If an event comes in in the upcoming update, it should lead to
                    //       a buffer flip.

                    ((IInputStateCallbackReceiver)device).OnNextUpdate();
                }
            }

            DelegateHelpers.InvokeCallbacksSafe(ref m_BeforeUpdateListeners, "onBeforeUpdate");
        }

        /// <summary>
        /// Apply the settings in <see cref="m_Settings"/>.
        /// </summary>
        internal void ApplySettings()
        {
            // Sync update mask.
            var newUpdateMask = InputUpdateType.Editor;
            if ((m_UpdateMask & InputUpdateType.BeforeRender) != 0)
            {
                // BeforeRender updates are enabled in response to devices needing BeforeRender updates
                // so we always preserve this if set.
                newUpdateMask |= InputUpdateType.BeforeRender;
            }
            if (m_Settings.updateMode == InputSettings.s_OldUnsupportedFixedAndDynamicUpdateSetting)
                m_Settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            switch (m_Settings.updateMode)
            {
                case InputSettings.UpdateMode.ProcessEventsInDynamicUpdate:
                    newUpdateMask |= InputUpdateType.Dynamic;
                    break;
                case InputSettings.UpdateMode.ProcessEventsInFixedUpdate:
                    newUpdateMask |= InputUpdateType.Fixed;
                    break;
                case InputSettings.UpdateMode.ProcessEventsManually:
                    newUpdateMask |= InputUpdateType.Manual;
                    break;
                default:
                    throw new NotSupportedException("Invalid input update mode: " + m_Settings.updateMode);
            }

            #if UNITY_EDITOR
            // In the editor, we force editor updates to be on even if InputEditorUserSettings.lockInputToGameView is
            // on as otherwise we'll end up accumulating events in edit mode without anyone flushing the
            // queue out regularly.
            newUpdateMask |= InputUpdateType.Editor;
            #endif
            updateMask = newUpdateMask;

            ////TODO: optimize this so that we don't repeatedly recreate state if we add/remove multiple devices
            ////      (same goes for not resolving actions repeatedly)

            // Check if there's any native device we aren't using ATM which now fits
            // the set of supported devices.
            AddAvailableDevicesThatAreNowRecognized();

            // If the settings restrict the set of supported devices, demote any native
            // device we currently have that doesn't fit the requirements.
            if (settings.supportedDevices.Count > 0)
            {
                for (var i = 0; i < m_DevicesCount; ++i)
                {
                    var device = m_Devices[i];
                    var layout = device.m_Layout;

                    // If it's not in m_AvailableDevices, we don't automatically remove it.
                    // Whatever has been added directly through AddDevice(), we keep and don't
                    // restrict by `supportDevices`.
                    var isInAvailableDevices = false;
                    for (var n = 0; n < m_AvailableDeviceCount; ++n)
                    {
                        if (m_AvailableDevices[n].deviceId == device.deviceId)
                        {
                            isInAvailableDevices = true;
                            break;
                        }
                    }
                    if (!isInAvailableDevices)
                        continue;

                    // If the device layout isn't supported according to the current settings,
                    // remove the device.
                    if (!IsDeviceLayoutMarkedAsSupportedInSettings(layout))
                    {
                        RemoveDevice(device, keepOnListOfAvailableDevices: true);
                        --i;
                    }
                }
            }

            // Cache some values.
            Touchscreen.s_TapTime = settings.defaultTapTime;
            Touchscreen.s_TapDelayTime = settings.multiTapDelayTime;
            Touchscreen.s_TapRadiusSquared = settings.tapRadius * settings.tapRadius;
            ButtonControl.s_GlobalDefaultButtonPressPoint = settings.defaultButtonPressPoint;

            // Let listeners know.
            for (var i = 0; i < m_SettingsChangedListeners.length; ++i)
                m_SettingsChangedListeners[i]();
        }

        internal void AddAvailableDevicesThatAreNowRecognized()
        {
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                var id = m_AvailableDevices[i].deviceId;
                if (TryGetDeviceById(id) != null)
                    continue;

                var layout = TryFindMatchingControlLayout(ref m_AvailableDevices[i].description, id);
                if (IsDeviceLayoutMarkedAsSupportedInSettings(layout))
                {
                    try
                    {
                        AddDevice(m_AvailableDevices[i].description, false,
                            deviceId: id,
                            deviceFlags: m_AvailableDevices[i].isNative ? InputDevice.DeviceFlags.Native : 0);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private unsafe void OnFocusChanged(bool focus)
        {
            ////REVIEW: should we also flush the event queue on focus loss?

            // On focus loss, reset devices.
            if (!focus)
            {
                // When running in background is enabled for the application, we only reset devices that aren't
                // marked as canRunInBackground.
                var runInBackground = m_Runtime.runInBackground;

                // Find the size of the largest state block. This determines the amount of temporary memory we
                // need to allocate.
                var largestDeviceStateBlock = 0;
                var deviceCount = m_DevicesCount;
                for (var i = 0; i < deviceCount; ++i)
                    largestDeviceStateBlock = Math.Max(largestDeviceStateBlock, (int)m_Devices[i].m_StateBlock.alignedSizeInBytes);

                // Allocate temp memory to hold one state event.
                ////REVIEW: the need for an event here is sufficiently obscure to warrant scrutiny; likely, there's a better way
                ////        to tell synthetic input (or input sources in general) apart
                // NOTE: We wrap the reset in an artificial state event so that it appears to the rest of the system
                //       like any other input. If we don't do that but rather just call UpdateState() with a null event
                //       pointer, the change will be considered an internal state change and will get ignored by some
                //       pieces of code (such as EnhancedTouch which filters out internal state changes of Touchscreen
                //       by ignoring any change that is not coming from an input event).
                using (var tempBuffer =
                           new NativeArray<byte>(InputEvent.kBaseEventSize + sizeof(int) + largestDeviceStateBlock, Allocator.Temp))
                {
                    var stateEventPtr = (StateEvent*)tempBuffer.GetUnsafePtr();
                    var statePtr = stateEventPtr->state;
                    var currentTime = m_Runtime.currentTime;
                    var updateType = defaultUpdateType;

                    for (var i = 0; i < deviceCount; ++i)
                    {
                        var device = m_Devices[i];

                        // Skip disabled devices.
                        if (!device.enabled)
                            continue;

                        // If the app will keep running in the background and the device is marked as being
                        // able to run in the background, don't touch it.
                        if (runInBackground && device.canRunInBackground)
                            continue;

                        // Set up the state event.
                        ref var stateBlock = ref device.m_StateBlock;
                        var deviceStateBlockSize = stateBlock.alignedSizeInBytes;
                        stateEventPtr->baseEvent.type = StateEvent.Type;
                        stateEventPtr->baseEvent.sizeInBytes = InputEvent.kBaseEventSize + sizeof(int) + deviceStateBlockSize;
                        stateEventPtr->baseEvent.time = currentTime;
                        stateEventPtr->baseEvent.deviceId = device.deviceId;
                        stateEventPtr->baseEvent.eventId = -1;
                        stateEventPtr->stateFormat = device.m_StateBlock.format;

                        // Set up new state.
                        var defaultStatePtr = device.defaultStatePtr;
                        if (device.noisy)
                        {
                            // The device has noisy controls. We don't want to reset those as they mostly
                            // represent sensor input and resetting sensor samples to default values isn't a good
                            // a good idea.
                            //
                            // Copy everything from defaultStatePtr except for the bits that are flagged in the
                            // device's noise mask.

                            var currentStatePtr = device.currentStatePtr;
                            var noiseMaskPtr = device.noiseMaskPtr;

                            // To preserve values from noisy controls, we need to first copy their current values.
                            UnsafeUtility.MemCpy(statePtr,
                                (byte*)currentStatePtr + stateBlock.byteOffset,
                                deviceStateBlockSize);

                            // And then we copy over default values masked by noise bits.
                            MemoryHelpers.MemCpyMasked(statePtr,
                                (byte*)defaultStatePtr + stateBlock.byteOffset,
                                (int)deviceStateBlockSize,
                                (byte*)noiseMaskPtr + stateBlock.byteOffset);
                        }
                        else
                        {
                            // No noisy controls in device. Just take the default state and put it in the event
                            // as is.
                            UnsafeUtility.MemCpy(statePtr,
                                (byte*)defaultStatePtr + stateBlock.byteOffset,
                                deviceStateBlockSize);
                        }

                        // Perform the reset.
                        UpdateState(device, updateType, statePtr, 0, deviceStateBlockSize, currentTime,
                            new InputEventPtr((InputEvent*)stateEventPtr));

                        // Tell the backend to reset.
                        device.RequestReset();
                    }
                }
            }

            // We set this *after* the block above as defaultUpdateType is influenced by the setting.
            m_HasFocus = focus;
        }

        private bool ShouldRunUpdate(InputUpdateType updateType)
        {
            // We perform a "null" update after domain reloads and on startup to get our devices
            // in place before the runtime calls MonoBehaviour callbacks. See InputSystem.RunInitialUpdate().
            if (updateType == InputUpdateType.None)
                return true;

            var mask = m_UpdateMask;
#if UNITY_EDITOR
            // Ignore editor updates when the game is playing and has focus. All input goes to player.
            if (gameIsPlayingAndHasFocus)
                mask &= ~InputUpdateType.Editor;
            // If the player isn't running, the only thing we run is editor updates.
            else if (updateType != InputUpdateType.Editor)
                return false;
#endif
            return (updateType & mask) != 0;
        }

        /// <summary>
        /// Process input events.
        /// </summary>
        /// <param name="updateType"></param>
        /// <param name="eventBuffer"></param>
        /// <remarks>
        /// This method is the core workhorse of the input system. It is called from <see cref="UnityEngineInternal.Input.NativeInputSystem"/>.
        /// Usually this happens in response to the player loop running and triggering updates at set points. However,
        /// updates can also be manually triggered through <see cref="InputSystem.Update"/>.
        ///
        /// The method receives the event buffer used internally by the runtime to collect events.
        ///
        /// Note that update types do *NOT* say what the events we receive are for. The update type only indicates
        /// where in the Unity's application loop we got called from. Where the event data goes depends wholly on
        /// which buffers we activate in the update and write the event data into.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "TODO: Refactor later.")]
        private unsafe void OnUpdate(InputUpdateType updateType, ref InputEventBuffer eventBuffer)
        {
            ////TODO: switch from Profiler to CustomSampler API
            // NOTE: This is *not* using try/finally as we've seen unreliability in the EndSample()
            //       execution (and we're not sure where it's coming from).
            Profiler.BeginSample("InputUpdate");

            // Restore devices before checking update mask. See InputSystem.RunInitialUpdate().
            RestoreDevicesAfterDomainReloadIfNecessary();

            if ((updateType & m_UpdateMask) == 0)
            {
                Profiler.EndSample();
                return;
            }

            WarnAboutDevicesFailingToRecreateAfterDomainReload();

            // First update sends out startup analytics.
            #if UNITY_ANALYTICS || UNITY_EDITOR
            if (!m_HaveSentStartupAnalytics)
            {
                InputAnalytics.OnStartup(this);
                m_HaveSentStartupAnalytics = true;
            }
            #endif

            ////TODO: manual mode must be treated like lockInputToGameView in editor

            // Update metrics.
            m_Metrics.totalEventCount += eventBuffer.eventCount - (int)InputUpdate.s_LastUpdateRetainedEventCount;
            m_Metrics.totalEventBytes += (int)eventBuffer.sizeInBytes - (int)InputUpdate.s_LastUpdateRetainedEventBytes;
            ++m_Metrics.totalUpdateCount;

            InputUpdate.s_LastUpdateRetainedEventCount = 0;
            InputUpdate.s_LastUpdateRetainedEventBytes = 0;

            // Store current time offset.
            InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup = m_Runtime.currentTimeOffsetToRealtimeSinceStartup;

            InputUpdate.s_LastUpdateType = updateType;
            InputStateBuffers.SwitchTo(m_StateBuffers, updateType);

            var isBeforeRenderUpdate = false;
            if (updateType == InputUpdateType.Dynamic || updateType == InputUpdateType.Manual || updateType == InputUpdateType.Fixed)
            {
                ++InputUpdate.s_UpdateStepCount;
            }
            else if (updateType == InputUpdateType.BeforeRender)
            {
                isBeforeRenderUpdate = true;
            }

            // See if we're supposed to only take events up to a certain time.
            // NOTE: We do not require the events in the queue to be sorted. Instead, we will walk over
            //       all events in the buffer each time. Note that if there are multiple events for the same
            //       device, it depends on the producer of these events to queue them in correct order.
            //       Otherwise, once an event with a newer timestamp has been processed, events coming later
            //       in the buffer and having older timestamps will get rejected.

            var currentTime = updateType == InputUpdateType.Fixed ? m_Runtime.currentTimeForFixedUpdate : m_Runtime.currentTime;
            var timesliceEvents = gameIsPlayingAndHasFocus && InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate;

            // Early out if there's no events to process.
            if (eventBuffer.eventCount <= 0)
            {
                // Normally, we process action timeouts after first processing all events. If we have no
                // events, we still need to check timeouts.
                if (gameIsPlayingAndHasFocus)
                    ProcessStateChangeMonitorTimeouts();

                #if ENABLE_PROFILER
                Profiler.EndSample();
                #endif
                InvokeAfterUpdateCallback();
                eventBuffer.Reset();
                return;
            }

            var currentEventReadPtr =
                (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(eventBuffer.data);
            var remainingEventCount = eventBuffer.eventCount;
            var processingStartTime = Time.realtimeSinceStartup;

            // When timeslicing events or in before-render updates, we may be leaving events in the buffer
            // for later processing. We do this by compacting the event buffer and moving events down such
            // that the events we leave in the buffer form one contiguous chunk of memory at the beginning
            // of the buffer.
            var currentEventWritePtr = currentEventReadPtr;
            var numEventsRetainedInBuffer = 0;

            var totalEventLag = 0.0;

            // Handle events.
            while (remainingEventCount > 0)
            {
                InputDevice device = null;

                Debug.Assert(!currentEventReadPtr->handled);

                // In before render updates, we only take state events and only those for devices
                // that have before render updates enabled.
                if (isBeforeRenderUpdate)
                {
                    while (remainingEventCount > 0)
                    {
                        Debug.Assert(!currentEventReadPtr->handled);

                        device = TryGetDeviceById(currentEventReadPtr->deviceId);
                        if (device != null && device.updateBeforeRender &&
                            (currentEventReadPtr->type == StateEvent.Type ||
                             currentEventReadPtr->type == DeltaStateEvent.Type))
                            break;

                        eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                            ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: true);
                    }
                }
                if (remainingEventCount == 0)
                    break;

                var currentEventTimeInternal = currentEventReadPtr->internalTime;

                // In the editor, we discard all input events that occur in-between exiting edit mode and having
                // entered play mode as otherwise we'll spill a bunch of UI events that have occurred while the
                // UI was sort of neither in this mode nor in that mode. This would usually lead to the game receiving
                // an accumulation of spurious inputs right in one of its first updates.
                //
                // NOTE: There's a chance the solution here will prove inadequate on the long run. We may do things
                //       here such as throwing partial touches away and then letting the rest of a touch go through.
                //       Could be that ultimately we need to issue a full reset of all devices at the beginning of
                //       play mode in the editor.
                #if UNITY_EDITOR
                if ((updateType & InputUpdateType.Editor) == 0 &&
                    InputSystem.s_SystemObject.exitEditModeTime > 0 &&
                    currentEventTimeInternal >= InputSystem.s_SystemObject.exitEditModeTime &&
                    (currentEventTimeInternal < InputSystem.s_SystemObject.enterPlayModeTime ||
                     InputSystem.s_SystemObject.enterPlayModeTime == 0))
                {
                    eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                        ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);
                    continue;
                }
                #endif

                // If we're timeslicing, check if the event time is within limits.
                if (timesliceEvents && currentEventTimeInternal >= currentTime)
                {
                    eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                        ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: true);
                    continue;
                }

                if (currentEventTimeInternal <= currentTime)
                    totalEventLag += currentTime - currentEventTimeInternal;

                // Grab device for event. In before-render updates, we already had to
                // check the device.
                if (device == null)
                    device = TryGetDeviceById(currentEventReadPtr->deviceId);
                if (device == null)
                {
                    #if UNITY_EDITOR
                    ////TODO: see if this is a device we haven't created and if so, just ignore
                    m_Diagnostics?.OnCannotFindDeviceForEvent(new InputEventPtr(currentEventReadPtr));
                    #endif

                    eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                        ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);

                    // No device found matching event. Ignore it.
                    continue;
                }

                // Give listeners a shot at the event.
                if (m_EventListeners.length > 0)
                {
                    for (var i = 0; i < m_EventListeners.length; ++i)
                        m_EventListeners[i](new InputEventPtr(currentEventReadPtr), device);

                    // If a listener marks the event as handled, we don't process it further.
                    if (currentEventReadPtr->handled)
                    {
                        eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                            ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);
                        continue;
                    }
                }

                // Process.
                var currentEventType = currentEventReadPtr->type;
                switch (currentEventType)
                {
                    case StateEvent.Type:
                    case DeltaStateEvent.Type:

                        var eventPtr = new InputEventPtr(currentEventReadPtr);

                        // Ignore state changes if device is disabled.
                        if (!device.enabled)
                        {
                            #if UNITY_EDITOR
                            m_Diagnostics?.OnEventForDisabledDevice(eventPtr, device);
                            #endif
                            break;
                        }

                        var deviceIsStateCallbackReceiver = (device.m_DeviceFlags & InputDevice.DeviceFlags.HasStateCallbacks) ==
                            InputDevice.DeviceFlags.HasStateCallbacks;

                        // Ignore the event if the last state update we received for the device was
                        // newer than this state event is. We don't allow devices to go back in time.
                        //
                        // NOTE: We make an exception here for devices that implement IInputStateCallbackReceiver (such
                        //       as Touchscreen). For devices that dynamically incorporate state it can be hard ensuring
                        //       a global ordering of events as there may be multiple substreams (e.g. each individual touch)
                        //       that are generated in the backend and would require considerable work to ensure monotonically
                        //       increasing timestamps across all such streams.
                        if (currentEventTimeInternal < device.m_LastUpdateTimeInternal &&
                            !(deviceIsStateCallbackReceiver && device.stateBlock.format != eventPtr.stateFormat))
                        {
                            #if UNITY_EDITOR
                            m_Diagnostics?.OnEventTimestampOutdated(new InputEventPtr(currentEventReadPtr), device);
                            #endif
                            break;
                        }

                        // Update the state of the device from the event. If the device is an IInputStateCallbackReceiver,
                        // let the device handle the event. If not, we do it ourselves.
                        var haveChangedStateOtherThanNoise = true;
                        if (deviceIsStateCallbackReceiver)
                        {
                            // NOTE: We leave it to the device to make sure the event has the right format. This allows the
                            //       device to handle multiple different incoming formats.
                            ((IInputStateCallbackReceiver)device).OnStateEvent(eventPtr);
                        }
                        else
                        {
                            // If the state format doesn't match, ignore the event.
                            if (device.stateBlock.format != eventPtr.stateFormat)
                            {
                                #if UNITY_EDITOR
                                m_Diagnostics?.OnEventFormatMismatch(currentEventReadPtr, device);
                                #endif
                                break;
                            }

                            haveChangedStateOtherThanNoise = UpdateState(device, eventPtr, updateType);
                        }

                        // Update timestamp on device.
                        // NOTE: We do this here and not in UpdateState() so that InputState.Change() will *NOT* change timestamps.
                        //       Only events should.
                        if (device.m_LastUpdateTimeInternal <= eventPtr.internalTime)
                            device.m_LastUpdateTimeInternal = eventPtr.internalTime;

                        // Make device current. Again, only do this when receiving events.
                        if (haveChangedStateOtherThanNoise)
                            device.MakeCurrent();

                        break;

                    case TextEvent.Type:
                    {
                        var textEventPtr = (TextEvent*)currentEventReadPtr;
                        if (device is ITextInputReceiver textInputReceiver)
                        {
                            var utf32Char = textEventPtr->character;
                            if (utf32Char >= 0x10000)
                            {
                                // Send surrogate pair.
                                utf32Char -= 0x10000;
                                var highSurrogate = 0xD800 + ((utf32Char >> 10) & 0x3FF);
                                var lowSurrogate = 0xDC00 + (utf32Char & 0x3FF);

                                textInputReceiver.OnTextInput((char)highSurrogate);
                                textInputReceiver.OnTextInput((char)lowSurrogate);
                            }
                            else
                            {
                                // Send single, plain character.
                                textInputReceiver.OnTextInput((char)utf32Char);
                            }
                        }
                        break;
                    }

                    case IMECompositionEvent.Type:
                    {
                        var imeEventPtr = (IMECompositionEvent*)currentEventReadPtr;
                        var textInputReceiver = device as ITextInputReceiver;
                        textInputReceiver?.OnIMECompositionChanged(imeEventPtr->compositionString);
                        break;
                    }

                    case DeviceRemoveEvent.Type:
                    {
                        RemoveDevice(device, keepOnListOfAvailableDevices: false);

                        // If it's a native device with a description, put it on the list of disconnected
                        // devices.
                        if (device.native && !device.description.empty)
                        {
                            ArrayHelpers.AppendWithCapacity(ref m_DisconnectedDevices, ref m_DisconnectedDevicesCount, device);
                            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                                m_DeviceChangeListeners[i](device, InputDeviceChange.Disconnected);
                        }
                        break;
                    }

                    case DeviceConfigurationEvent.Type:
                        device.OnConfigurationChanged();
                        InputActionState.OnDeviceChange(device, InputDeviceChange.ConfigurationChanged);
                        for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                            m_DeviceChangeListeners[i](device, InputDeviceChange.ConfigurationChanged);
                        break;
                }

                eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                    ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);
            }

            m_Metrics.totalEventProcessingTime += Time.realtimeSinceStartup - processingStartTime;
            m_Metrics.totalEventLagTime += totalEventLag;

            // Remember how much data we retained so that we don't count it against the next
            // batch of events that we receive.
            InputUpdate.s_LastUpdateRetainedEventCount = (uint)numEventsRetainedInBuffer;
            InputUpdate.s_LastUpdateRetainedEventBytes = (uint)((byte*)currentEventWritePtr -
                (byte*)NativeArrayUnsafeUtility
                    .GetUnsafeBufferPointerWithoutChecks(eventBuffer
                    .data));

            // Update event buffer. If we have retained events, update event count
            // and buffer size. If not, just reset.
            if (numEventsRetainedInBuffer > 0)
            {
                var bufferPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(eventBuffer.data);
                Debug.Assert((byte*)currentEventWritePtr > (byte*)bufferPtr);
                var newBufferSize = (byte*)currentEventWritePtr - (byte*)bufferPtr;
                eventBuffer = new InputEventBuffer((InputEvent*)bufferPtr, numEventsRetainedInBuffer, (int)newBufferSize,
                    (int)eventBuffer.capacityInBytes);
            }
            else
            {
                eventBuffer.Reset();
            }

            if (gameIsPlayingAndHasFocus)
                ProcessStateChangeMonitorTimeouts();

            ////TODO: fire event that allows code to update state *from* state we just updated

            Profiler.EndSample();

            ////FIXME: need to ensure that if someone calls QueueEvent() from an onAfterUpdate callback, we don't end up with a
            ////       mess in the event buffer
            ////       same goes for events that someone may queue from a change monitor callback
            InvokeAfterUpdateCallback();
            ////TODO: check if there's new events in the event buffer; if so, do a pass over those events right away
        }

        private void InvokeAfterUpdateCallback()
        {
            for (var i = 0; i < m_AfterUpdateListeners.length; ++i)
                m_AfterUpdateListeners[i]();
        }

        // NOTE: 'newState' can be a subset of the full state stored at 'oldState'. In this case,
        //       'newStateOffsetInBytes' must give the offset into the full state and 'newStateSizeInBytes' must
        //       give the size of memory slice to be updated.
        private unsafe bool ProcessStateChangeMonitors(int deviceIndex, void* newStateFromEvent, void* oldStateOfDevice, uint newStateSizeInBytes, uint newStateOffsetInBytes)
        {
            if (m_StateChangeMonitors == null)
                return false;

            // We resize the monitor arrays only when someone adds to them so they
            // may be out of sync with the size of m_Devices.
            if (deviceIndex >= m_StateChangeMonitors.Length)
                return false;

            var memoryRegions = m_StateChangeMonitors[deviceIndex].memoryRegions;
            if (memoryRegions == null)
                return false; // No one cares about state changes on this device.

            var numMonitors = m_StateChangeMonitors[deviceIndex].count;
            var signalled = false;
            var signals = m_StateChangeMonitors[deviceIndex].signalled;
            var haveChangedSignalsBitfield = false;

            // For every memory region that overlaps what we got in the event, compare memory contents
            // between the old device state and what's in the event. If the contents different, the
            // respective state monitor signals.
            var newEventMemoryRegion = new MemoryHelpers.BitRegion(newStateOffsetInBytes, 0, newStateSizeInBytes * 8);
            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = memoryRegions[i];

                // Check if the monitor record has been wiped in the meantime. If so, remove it.
                if (memoryRegion.sizeInBits == 0)
                {
                    ////REVIEW: Do we really care? It is nice that it's predictable this way but hardly a hard requirement
                    // NOTE: We're using EraseAtWithCapacity here rather than EraseAtByMovingTail to preserve
                    //       order which makes the order of callbacks somewhat more predictable.

                    var listenerCount = numMonitors;
                    var memoryRegionCount = numMonitors;
                    ArrayHelpers.EraseAtWithCapacity(m_StateChangeMonitors[deviceIndex].listeners, ref listenerCount, i);
                    ArrayHelpers.EraseAtWithCapacity(memoryRegions, ref memoryRegionCount, i);
                    signals.SetLength(numMonitors - 1);
                    haveChangedSignalsBitfield = true;
                    --numMonitors;
                    --i;
                    continue;
                }

                var overlap = newEventMemoryRegion.Overlap(memoryRegion);
                if (overlap.isEmpty || MemoryHelpers.Compare(oldStateOfDevice, (byte*)newStateFromEvent - newStateOffsetInBytes, overlap))
                    continue;

                signals.SetBit(i);
                haveChangedSignalsBitfield = true;
                signalled = true;
            }

            if (haveChangedSignalsBitfield)
                m_StateChangeMonitors[deviceIndex].signalled = signals;

            return signalled;
        }

        private unsafe void FireStateChangeNotifications(int deviceIndex, double internalTime, InputEvent* eventPtr)
        {
            Debug.Assert(m_StateChangeMonitors != null);
            Debug.Assert(m_StateChangeMonitors.Length > deviceIndex);

            // NOTE: This method must be safe for mutating the state change monitor arrays from *within*
            //       NotifyControlStateChanged()! This includes all monitors for the device being wiped
            //       completely or arbitrary additions and removals having occurred.

            ref var signals = ref m_StateChangeMonitors[deviceIndex].signalled;
            ref var listeners = ref m_StateChangeMonitors[deviceIndex].listeners;
            var time = internalTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            // Call IStateChangeMonitor.NotifyControlStateChange for every monitor that is in
            // signalled state.
            for (var i = 0; i < signals.length; ++i)
            {
                if (!signals.TestBit(i))
                    continue;

                var listener = listeners[i];
                try
                {
                    listener.monitor.NotifyControlStateChanged(listener.control, time, eventPtr,
                        listener.monitorIndex);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Exception '{exception.GetType().Name}' thrown from state change monitor '{listener.monitor.GetType().Name}' on '{listener.control}'");
                    Debug.LogException(exception);
                }

                signals.ClearBit(i);
            }
        }

        private void ProcessStateChangeMonitorTimeouts()
        {
            if (m_StateChangeMonitorTimeouts.length == 0)
                return;

            // Go through the list and both trigger expired timers and remove any irrelevant
            // ones by compacting the array.
            // NOTE: We do not actually release any memory we may have allocated.
            var currentTime = m_Runtime.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            var remainingTimeoutCount = 0;
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
            {
                // If we have reset this entry in RemoveStateChangeMonitorTimeouts(),
                // skip over it and let compaction get rid of it.
                if (m_StateChangeMonitorTimeouts[i].control == null)
                    continue;

                var timerExpirationTime = m_StateChangeMonitorTimeouts[i].time;
                if (timerExpirationTime <= currentTime)
                {
                    var timeout = m_StateChangeMonitorTimeouts[i];
                    timeout.monitor.NotifyTimerExpired(timeout.control,
                        currentTime, timeout.monitorIndex, timeout.timerIndex);

                    // Compaction will get rid of the entry.
                }
                else
                {
                    // Rather than repeatedly calling RemoveAt() and thus potentially
                    // moving the same data over and over again, we compact the array
                    // on the fly and move entries in the array down as needed.
                    if (i != remainingTimeoutCount)
                        m_StateChangeMonitorTimeouts[remainingTimeoutCount] = m_StateChangeMonitorTimeouts[i];
                    ++remainingTimeoutCount;
                }
            }

            m_StateChangeMonitorTimeouts.SetLength(remainingTimeoutCount);
        }

        internal unsafe bool UpdateState(InputDevice device, InputEvent* eventPtr, InputUpdateType updateType)
        {
            Debug.Assert(eventPtr != null, "Received NULL event ptr");

            var stateBlockOfDevice = device.m_StateBlock;
            var stateBlockSizeOfDevice = stateBlockOfDevice.sizeInBits / 8; // Always byte-aligned; avoid calling alignedSizeInBytes.
            var offsetInDeviceStateToCopyTo = 0u;
            uint sizeOfStateToCopy;
            uint receivedStateSize;
            byte* ptrToReceivedState;
            FourCC receivedStateFormat;

            // Grab state data from event and decide where to copy to and how much to copy.
            if (eventPtr->type == StateEvent.Type)
            {
                var stateEventPtr = (StateEvent*)eventPtr;
                receivedStateFormat = stateEventPtr->stateFormat;
                receivedStateSize = stateEventPtr->stateSizeInBytes;
                ptrToReceivedState = (byte*)stateEventPtr->state;

                // Ignore extra state at end of event.
                sizeOfStateToCopy = receivedStateSize;
                if (sizeOfStateToCopy > stateBlockSizeOfDevice)
                    sizeOfStateToCopy = stateBlockSizeOfDevice;
            }
            else
            {
                Debug.Assert(eventPtr->type == DeltaStateEvent.Type, "Given event must either be a StateEvent or a DeltaStateEvent");

                var deltaEventPtr = (DeltaStateEvent*)eventPtr;
                receivedStateFormat = deltaEventPtr->stateFormat;
                receivedStateSize = deltaEventPtr->deltaStateSizeInBytes;
                ptrToReceivedState = (byte*)deltaEventPtr->deltaState;
                offsetInDeviceStateToCopyTo = deltaEventPtr->stateOffset;

                // Ignore extra state at end of event.
                sizeOfStateToCopy = receivedStateSize;
                if (offsetInDeviceStateToCopyTo + sizeOfStateToCopy > stateBlockSizeOfDevice)
                {
                    if (offsetInDeviceStateToCopyTo >= stateBlockSizeOfDevice)
                        return false; // Entire delta state is out of range.

                    sizeOfStateToCopy = stateBlockSizeOfDevice - offsetInDeviceStateToCopyTo;
                }
            }

            Debug.Assert(device.m_StateBlock.format == receivedStateFormat, "Received state format does not match format of device");

            // Write state.
            return UpdateState(device, updateType, ptrToReceivedState, offsetInDeviceStateToCopyTo,
                sizeOfStateToCopy, eventPtr->internalTime, eventPtr);
        }

        /// <summary>
        /// This method is the workhorse for updating input state in the system. It runs all the logic of incorporating
        /// new state into devices and triggering whatever change monitors are attached to the state memory that gets
        /// touched.
        /// </summary>
        /// <remarks>
        /// This method can be invoked from outside the event processing loop and the given data does not have to come
        /// from an event.
        ///
        /// This method does NOT respect <see cref="IInputStateCallbackReceiver"/>. This means that the device will
        /// NOT get a shot at intervening in the state write.
        /// </remarks>
        /// <param name="device">Device to update state on. <paramref name="stateOffsetInDevice"/> is relative to device's
        /// starting offset in memory.</param>
        /// <param name="eventPtr">Pointer to state event from which the state change was initiated. Null if the state
        /// change is not coming from an event.</param>
        internal unsafe bool UpdateState(InputDevice device, InputUpdateType updateType,
            void* statePtr, uint stateOffsetInDevice, uint stateSize, double internalTime, InputEventPtr eventPtr = default)
        {
            var deviceIndex = device.m_DeviceIndex;
            ref var stateBlockOfDevice = ref device.m_StateBlock;

            ////TODO: limit stateSize and StateOffset by the device's state memory

            var deviceBuffer = (byte*)InputStateBuffers.GetFrontBufferForDevice(deviceIndex);

            // Before we update state, let change monitors compare the old and the new state.
            // We do this instead of first updating the front buffer and then comparing to the
            // back buffer as that would require a buffer flip for each state change in order
            // for the monitors to work reliably. By comparing the *event* data to the current
            // state, we can have multiple state events in the same frame yet still get reliable
            // change notifications.
            var haveSignalledMonitors =
                ProcessStateChangeMonitors(deviceIndex, statePtr,
                    deviceBuffer + stateBlockOfDevice.byteOffset,
                    stateSize, stateOffsetInDevice);

            var deviceStateOffset = device.m_StateBlock.byteOffset + stateOffsetInDevice;
            var deviceStatePtr = deviceBuffer + deviceStateOffset;

            ////REVIEW: Should we do this only for events but not for InputState.Change()?
            // If noise filtering on .current is turned on and the device may have noise,
            // determine if the event carries signal or not.
            var makeDeviceCurrent = true;
            if (device.noisy && m_Settings.filterNoiseOnCurrent)
            {
                // Compare the current state of the device to the newly received state but overlay
                // the comparison by the noise mask.

                var noiseMask = (byte*)InputStateBuffers.s_NoiseMaskBuffer + deviceStateOffset;

                makeDeviceCurrent =
                    !MemoryHelpers.MemCmpBitRegion(deviceStatePtr, statePtr,
                        0, stateSize * 8, mask: noiseMask);
            }

            // Buffer flip.
            var flipped = FlipBuffersForDeviceIfNecessary(device, updateType);

            // Now write the state.
            #if UNITY_EDITOR
            if (updateType == InputUpdateType.Editor)
            {
                WriteStateChange(m_StateBuffers.m_EditorStateBuffers, deviceIndex, ref stateBlockOfDevice, stateOffsetInDevice,
                    statePtr, stateSize, flipped);
            }
            else
            #endif
            {
                WriteStateChange(m_StateBuffers.m_PlayerStateBuffers, deviceIndex, ref stateBlockOfDevice,
                    stateOffsetInDevice, statePtr, stateSize, flipped);
            }

            // Notify listeners.
            for (var i = 0; i < m_DeviceStateChangeListeners.length; ++i)
                m_DeviceStateChangeListeners[i](device, eventPtr);

            // Now that we've committed the new state to memory, if any of the change
            // monitors fired, let the associated actions know.
            if (haveSignalledMonitors)
                FireStateChangeNotifications(deviceIndex, internalTime, eventPtr);

            return makeDeviceCurrent;
        }

        private static unsafe void WriteStateChange(InputStateBuffers.DoubleBuffers buffers, int deviceIndex,
            ref InputStateBlock deviceStateBlock, uint stateOffsetInDevice, void* statePtr, uint stateSizeInBytes, bool flippedBuffers)
        {
            var frontBuffer = buffers.GetFrontBuffer(deviceIndex);
            Debug.Assert(frontBuffer != null);

            // If we're updating less than the full state, we need to preserve the parts we are not updating.
            // Instead of trying to optimize here and only copy what we really need, we just go and copy the
            // entire state of the device over.
            //
            // NOTE: This copying must only happen once, right after a buffer flip. Otherwise we may copy old,
            //       stale input state from the back buffer over state that has already been updated with newer
            //       data.
            var deviceStateSize = deviceStateBlock.sizeInBits / 8; // Always byte-aligned; avoid calling alignedSizeInBytes.
            if (flippedBuffers && deviceStateSize != stateSizeInBytes)
            {
                var backBuffer = buffers.GetBackBuffer(deviceIndex);
                Debug.Assert(backBuffer != null);

                UnsafeUtility.MemCpy(
                    (byte*)frontBuffer + deviceStateBlock.byteOffset,
                    (byte*)backBuffer + deviceStateBlock.byteOffset,
                    deviceStateSize);
            }

            UnsafeUtility.MemCpy((byte*)frontBuffer + deviceStateBlock.byteOffset + stateOffsetInDevice, statePtr,
                stateSizeInBytes);
        }

        // Flip front and back buffer for device, if necessary. May flip buffers for more than just
        // the given update type.
        // Returns true if there was a buffer flip.
        private bool FlipBuffersForDeviceIfNecessary(InputDevice device, InputUpdateType updateType)
        {
            if (updateType == InputUpdateType.BeforeRender)
            {
                ////REVIEW: I think this is wrong; if we haven't flipped in the current dynamic or fixed update, we should do so now
                // We never flip buffers for before render. Instead, we already write
                // into the front buffer.
                return false;
            }

#if UNITY_EDITOR
            ////REVIEW: should this use the editor update ticks as quasi-frame-boundaries?
            // Updates go to the editor only if the game isn't playing or does not have focus.
            // Otherwise we fall through to the logic that flips for the *next* dynamic and
            // fixed updates.
            if (updateType == InputUpdateType.Editor && !gameIsPlayingAndHasFocus)
            {
                // The editor doesn't really have a concept of frame-to-frame operation the
                // same way the player does. So we simply flip buffers on a device whenever
                // a new state event for it comes in.
                m_StateBuffers.m_EditorStateBuffers.SwapBuffers(device.m_DeviceIndex);
                return true;
            }
#endif

            // Flip buffers if we haven't already for this frame.
            if (device.m_CurrentUpdateStepCount != InputUpdate.s_UpdateStepCount)
            {
                m_StateBuffers.m_PlayerStateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentUpdateStepCount = InputUpdate.s_UpdateStepCount;
                return true;
            }

            return false;
        }

        // Domain reload survival logic. Also used for pushing and popping input system
        // state for testing.

        // Stuff everything that we want to survive a domain reload into
        // a m_SerializedState.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable]
        internal struct DeviceState
        {
            // Preserving InputDevices is somewhat tricky business. Serializing
            // them in full would involve pretty nasty work. We have the restriction,
            // however, that everything needs to be created from layouts (it partly
            // exists for the sake of reload survivability), so we should be able to
            // just go and recreate the device from the layout. This also has the
            // advantage that if the layout changes between reloads, the change
            // automatically takes effect.
            public string name;
            public string layout;
            public string variants;
            public string[] usages;
            public int deviceId;
            public int participantId;
            public InputDevice.DeviceFlags flags;
            public InputDeviceDescription description;

            public void Restore(InputDevice device)
            {
                var usageCount = usages.LengthSafe();
                for (var i = 0; i < usageCount; ++i)
                    device.AddDeviceUsage(new InternedString(usages[i]));
                device.m_ParticipantId = participantId;
            }
        }

        /// <summary>
        /// State we take across domain reloads.
        /// </summary>
        /// <remarks>
        /// Most of the state we re-recreate in-between reloads and do not store
        /// in this structure. In particular, we do not preserve anything from
        /// the various RegisterXXX().
        /// </remarks>
        [Serializable]
        internal struct SerializedState
        {
            public int layoutRegistrationVersion;
            public float pollingFrequency;
            public DeviceState[] devices;
            public AvailableDevice[] availableDevices;
            public InputStateBuffers buffers;
            public InputUpdate.SerializedState updateState;
            public InputUpdateType updateMask;
            public InputMetrics metrics;
            public InputSettings settings;

            #if UNITY_ANALYTICS || UNITY_EDITOR
            public bool haveSentStartupAnalytics;
            #endif
        }

        internal SerializedState SaveState()
        {
            // Devices.
            var deviceCount = m_DevicesCount;
            var deviceArray = new DeviceState[deviceCount];
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                string[] usages = null;
                if (device.usages.Count > 0)
                    usages = device.usages.Select(x => x.ToString()).ToArray();

                var deviceState = new DeviceState
                {
                    name = device.name,
                    layout = device.layout,
                    variants = device.variants,
                    deviceId = device.deviceId,
                    participantId = device.m_ParticipantId,
                    usages = usages,
                    description = device.m_Description,
                    flags = device.m_DeviceFlags
                };
                deviceArray[i] = deviceState;
            }

            return new SerializedState
            {
                layoutRegistrationVersion = m_LayoutRegistrationVersion,
                pollingFrequency = m_PollingFrequency,
                devices = deviceArray,
                availableDevices = m_AvailableDevices?.Take(m_AvailableDeviceCount).ToArray(),
                buffers = m_StateBuffers,
                updateState = InputUpdate.Save(),
                updateMask = m_UpdateMask,
                metrics = m_Metrics,
                settings = m_Settings,

                #if UNITY_ANALYTICS || UNITY_EDITOR
                haveSentStartupAnalytics = m_HaveSentStartupAnalytics,
                #endif
            };
        }

        internal void RestoreStateWithoutDevices(SerializedState state)
        {
            m_StateBuffers = state.buffers;
            m_LayoutRegistrationVersion = state.layoutRegistrationVersion + 1;
            updateMask = state.updateMask;
            m_Metrics = state.metrics;
            m_PollingFrequency = state.pollingFrequency;

            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);
            m_Settings = state.settings;

            #if UNITY_ANALYTICS || UNITY_EDITOR
            m_HaveSentStartupAnalytics = state.haveSentStartupAnalytics;
            #endif

            ////REVIEW: instead of accessing globals here, we could move this to when we re-create devices

            // Update state.
            InputUpdate.Restore(state.updateState);
        }

        // If these are set, we clear them out on the first input update.
        internal DeviceState[] m_SavedDeviceStates;
        internal AvailableDevice[] m_SavedAvailableDevices;

        /// <summary>
        /// Recreate devices based on the devices we had before a domain reload.
        /// </summary>
        /// <remarks>
        /// Note that device indices may change between domain reloads.
        ///
        /// We recreate devices using the layout information as it exists now as opposed to
        /// as it existed before the domain reload. This means we'll be picking up any changes that
        /// have happened to layouts as part of the reload (including layouts having been removed
        /// entirely).
        /// </remarks>
        internal void RestoreDevicesAfterDomainReload()
        {
            Profiler.BeginSample("InputManager.RestoreDevicesAfterDomainReload");

            using (InputDeviceBuilder.Ref())
            {
                DeviceState[] retainedDeviceStates = null;
                var deviceStates = m_SavedDeviceStates;
                var deviceCount = m_SavedDeviceStates.LengthSafe();
                m_SavedDeviceStates = null; // Prevent layout matcher registering themselves on the fly from picking anything off this list.
                for (var i = 0; i < deviceCount; ++i)
                {
                    ref var deviceState = ref deviceStates[i];

                    var device = TryGetDeviceById(deviceState.deviceId);
                    if (device != null)
                        continue;

                    var layout = TryFindMatchingControlLayout(ref deviceState.description,
                        deviceState.deviceId);
                    if (layout.IsEmpty())
                    {
                        var previousLayout = new InternedString(deviceState.layout);
                        if (m_Layouts.HasLayout(previousLayout))
                            layout = previousLayout;
                    }
                    if (layout.IsEmpty() || !RestoreDeviceFromSavedState(ref deviceState, layout))
                        ArrayHelpers.Append(ref retainedDeviceStates, deviceState);
                }

                // See if we can make sense of an available device now that we couldn't make sense of
                // before. This can be the case if there's new layout information that wasn't available
                // before.
                if (m_SavedAvailableDevices != null)
                {
                    m_AvailableDevices = m_SavedAvailableDevices;
                    m_AvailableDeviceCount = m_SavedAvailableDevices.LengthSafe();
                    for (var i = 0; i < m_AvailableDeviceCount; ++i)
                    {
                        var device = TryGetDeviceById(m_AvailableDevices[i].deviceId);
                        if (device != null)
                            continue;

                        if (m_AvailableDevices[i].isRemoved)
                            continue;

                        var layout = TryFindMatchingControlLayout(ref m_AvailableDevices[i].description,
                            m_AvailableDevices[i].deviceId);
                        if (!layout.IsEmpty())
                        {
                            try
                            {
                                AddDevice(layout, m_AvailableDevices[i].deviceId,
                                    deviceDescription: m_AvailableDevices[i].description,
                                    deviceFlags: m_AvailableDevices[i].isNative ? InputDevice.DeviceFlags.Native : 0);
                            }
                            catch (Exception)
                            {
                                // Just ignore. Simply means we still can't really turn the device into something useful.
                            }
                        }
                    }
                }

                // Done. Discard saved arrays.
                m_SavedDeviceStates = retainedDeviceStates;
                m_SavedAvailableDevices = null;
            }

            Profiler.EndSample();
        }

        // We have two general types of devices we need to care about when recreating devices
        // after domain reloads:
        //
        // A) device with InputDeviceDescription
        // B) device created directly from specific layout
        //
        // A) should go through the normal matching process whereas B) should get recreated with
        // layout of same name (if still available).
        //
        // So we kick device recreation off from two points:
        //
        // 1) From RegisterControlLayoutMatcher to catch A)
        // 2) From RegisterControlLayout to catch B)
        //
        // Additionally, we have the complication that a layout a device was using was something
        // dynamically registered from onFindLayoutForDevice. We don't do anything special about that.
        // The first full input update will flush out the list of saved device states and at that
        // point, any onFindLayoutForDevice hooks simply have to be in place. If they are, devices
        // will get recreated appropriately.
        //
        // It would be much simpler to recreate all devices as the first thing in the first full input
        // update but that would mean that devices would become available only very late. They would
        // not, for example, be available when MonoBehaviour.Start methods are invoked.

        private bool RestoreDeviceFromSavedState(ref DeviceState deviceState, InternedString layout)
        {
            // We assign the same device IDs here to newly created devices that they had
            // before the domain reload. This is safe as device ID allocation is under the
            // control of the runtime and not expected to be affected by a domain reload.

            InputDevice device;
            try
            {
                device = AddDevice(layout,
                    deviceDescription: deviceState.description,
                    deviceId: deviceState.deviceId,
                    deviceName: deviceState.name,
                    deviceFlags: deviceState.flags,
                    variants: new InternedString(deviceState.variants));
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not recreate input device '{deviceState.description}' with layout '{deviceState.layout}' and variants '{deviceState.variants}' after domain reload");
                Debug.LogException(exception);
                return true; // Don't try again.
            }

            deviceState.Restore(device);
            return true;
        }

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
    }
}
