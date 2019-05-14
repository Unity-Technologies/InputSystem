using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Controls;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEditor;
#endif

////TODO: make diagnostics available in dev players and give it a public API to enable them

////TODO: work towards InputManager having no direct knowledge of actions

////TODO: allow pushing events into the system any which way; decouple from the buffer in NativeInputSystem being the only source

////TODO: merge InputManager into InputSystem and have InputSystemObject store SerializedState directly

////REVIEW: change the event properties over to using IObservable?

////REVIEW: instead of RegisterInteraction and RegisterControlProcessor, have a generic RegisterInterface (or something)?

////REVIEW: can we do away with the 'previous == previous frame' and simply buffer flip on every value write?

////REVIEW: should we force keeping mouse/pen/keyboard/touch around in editor even if not in list of supported devices?

////REVIEW: do we want to filter out state events that result in no state change?

#pragma warning disable CS0649
namespace UnityEngine.InputSystem
{
    using DeviceChangeListener = Action<InputDevice, InputDeviceChange>;
    using LayoutChangeListener = Action<string, InputControlLayoutChange>;
    using EventListener = Action<InputEventPtr>;
    using UpdateListener = Action<InputUpdateType>;

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
            add => m_DeviceFindLayoutCallbacks.AppendWithCapacity(value);
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
            add => m_EventListeners.AppendWithCapacity(value);
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
            add => m_AfterUpdateListeners.AppendWithCapacity(value);
            remove
            {
                var index = m_AfterUpdateListeners.IndexOf(value);
                if (index >= 0)
                    m_AfterUpdateListeners.RemoveAtWithCapacity(index);
            }
        }

        public event Action onSettingsChange
        {
            add => m_SettingsChangedListeners.AppendWithCapacity(value);
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

        public void RegisterControlLayoutBuilder(MethodInfo method, object instance, string name,
            string baseLayout = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.IsGenericMethod)
                throw new ArgumentException($"Method must not be generic ({method})", nameof(method));
            if (method.GetParameters().Length > 0)
                throw new ArgumentException($"Method must not take arguments ({method})", nameof(method));
            if (!typeof(InputControlLayout).IsAssignableFrom(method.ReturnType))
                throw new ArgumentException($"Method must return InputControlLayout ({method})", nameof(method));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ////FIXME: this is no longer necessary; kill it
            // If we have an instance, make sure it is [Serializable].
            if (instance != null)
            {
                var type = instance.GetType();
                if (type.GetCustomAttribute<SerializableAttribute>(true) == null)
                    throw new ArgumentException(
                        $"Instance used with {method} to construct a layout must be [Serializable] but {type} is not",
                        nameof(instance));
            }

            var internedLayoutName = new InternedString(name);
            var internedBaseLayoutName = new InternedString(baseLayout);
            var isReplacement = DoesLayoutExist(internedLayoutName);

            m_Layouts.layoutBuilders[internedLayoutName] = new InputControlLayout.BuilderInfo
            {
                method = method,
                instance = instance
            };

            PerformLayoutPostRegistration(internedLayoutName, new InlinedArray<InternedString>(internedBaseLayoutName),
                isReplacement);
        }

        private void PerformLayoutPostRegistration(InternedString layoutName, InlinedArray<InternedString> baseLayouts,
            bool isReplacement, bool isKnownToBeDeviceLayout = false, bool isOverride = false)
        {
            ++m_LayoutRegistrationVersion;

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
            var setup = new InputDeviceBuilder();
            for (var i = 0; i < devicesUsingLayout.Count; ++i)
            {
                ////TODO: preserve state where possible
                var device = devicesUsingLayout[i];
                RecreateDevice(device, device.m_Layout, setup);
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

            InputDeviceBuilder builder = null;
            var deviceCount = m_DevicesCount;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                var deviceDescription = device.description;

                if (deviceDescription.empty || !(deviceMatcher.MatchPercentage(deviceDescription) > 0))
                    continue;

                var layoutName = TryFindMatchingControlLayout(ref deviceDescription, device.id);
                if (layoutName != device.m_Layout)
                {
                    device.m_Description = deviceDescription;

                    if (builder == null)
                        builder = new InputDeviceBuilder();

                    RecreateDevice(device, layoutName, builder);

                    // We're removing devices in the middle of the array and appending
                    // them at the end. Adjust our index and device count to make sure
                    // we're not iterating all the way into already processed devices.

                    --i;
                    --deviceCount;
                }
            }
        }

        private InputDevice RecreateDevice(InputDevice device, InternedString newLayout, InputDeviceBuilder builder)
        {
            // Remove.
            RemoveDevice(device, keepOnListOfAvailableDevices: true);

            // Re-setup device.
            builder.Setup(newLayout, device.m_Variants, deviceDescription: device.m_Description,
                existingDevice: device);
            var newDevice = builder.Finish();

            // Re-add.
            AddDevice(newDevice);
            return newDevice;
        }

        private void AddAvailableDevicesMatchingDescription(InputDeviceMatcher matcher, InternedString layout)
        {
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
            ////REVIEW: if a callback picks a layout, should we re-run through the list of callbacks?
            // Give listeners a shot to select/create a layout.
            var haveOverriddenLayoutName = false;
            for (var i = 0; i < m_DeviceFindLayoutCallbacks.length; ++i)
            {
                var newLayout = m_DeviceFindLayoutCallbacks[i](deviceId, ref deviceDescription, layoutName, m_Runtime);
                if (!string.IsNullOrEmpty(newLayout) && !haveOverriddenLayoutName)
                {
                    layoutName = new InternedString(newLayout);
                    haveOverriddenLayoutName = true;
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

        public int ListControlLayouts(List<string> layouts, string basedOn = null)
        {
            if (layouts == null)
                throw new ArgumentNullException(nameof(layouts));

            var countBefore = layouts.Count;

            ////FIXME: this may add a name twice
            ////REVIEW: are we handling layout overrides correctly here? they shouldn't end up on the list

            if (!string.IsNullOrEmpty(basedOn))
            {
                var internedBasedOn = new InternedString(basedOn);
                foreach (var entry in m_Layouts.layoutTypes)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        layouts.Add(entry.Key);
                foreach (var entry in m_Layouts.layoutStrings)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        layouts.Add(entry.Key);
                foreach (var entry in m_Layouts.layoutBuilders)
                    if (m_Layouts.IsBasedOn(internedBasedOn, entry.Key))
                        layouts.Add(entry.Key);
            }
            else
            {
                foreach (var entry in m_Layouts.layoutTypes)
                    layouts.Add(entry.Key.ToString());
                foreach (var entry in m_Layouts.layoutStrings)
                    layouts.Add(entry.Key.ToString());
                foreach (var entry in m_Layouts.layoutBuilders)
                    layouts.Add(entry.Key.ToString());
            }

            return layouts.Count - countBefore;
        }

        // Processes a path specification that may match more than a single control.
        // Adds all controls that match to the given list.
        // Returns true if at least one control was matched.
        // Must not generate garbage!
        public bool TryGetControls(string path, List<InputControl> controls)
        {
            throw new NotImplementedException();
        }

        // Return the first match for the given path or null if no control matches.
        // Must not generate garbage!
        public InputControl TryGetControl(string path)
        {
            throw new NotImplementedException();
        }

        public InputControl GetControl(string path)
        {
            throw new NotImplementedException();
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

        public void SetLayoutVariant(InputControl control, string variant)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (string.IsNullOrEmpty(variant))
                variant = "Default";

            //how can we do this efficiently without having to take the control's device out of the system?

            throw new NotImplementedException();
        }

        public void SetUsage(InputDevice device, InternedString usage)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            device.SetUsage(usage);

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.UsageChanged);

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
            return AddDevice(layoutName);
        }

        // Creates a device from the given layout and adds it to the system.
        // NOTE: Creates garbage.
        public InputDevice AddDevice(string layout, string name = null, InternedString variants = new InternedString())
        {
            if (string.IsNullOrEmpty(layout))
                throw new ArgumentNullException(nameof(layout));

            var internedLayoutName = new InternedString(layout);

            var setup = new InputDeviceBuilder();
            setup.Setup(internedLayoutName, variants);
            var device = setup.Finish();

            if (!string.IsNullOrEmpty(name))
                device.m_Name = new InternedString(name);

            AddDevice(device);

            return device;
        }

        // Add device with a forced ID. Used when creating devices reported to us by native.
        private InputDevice AddDevice(InternedString layout, int deviceId,
            InputDeviceDescription deviceDescription = new InputDeviceDescription(),
            InputDevice.DeviceFlags deviceFlags = 0,
            InternedString variants = default)
        {
            var setup = new InputDeviceBuilder();
            setup.Setup(new InternedString(layout), deviceDescription: deviceDescription, variants: variants);
            var device = setup.Finish();

            device.m_Id = deviceId;
            device.m_Description = deviceDescription;
            device.m_DeviceFlags |= deviceFlags;

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
                throw new ArgumentException("Device has no associated layout", nameof(device));

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
            m_DevicesById[device.id] = device;

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
                if (m_AvailableDevices[i].deviceId == device.id)
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
            if (device is IInputStateCallbackReceiver stateCallbackReceiver)
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

            // Make the device current.
            device.MakeCurrent();

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Added);
        }

        public InputDevice AddDevice(InputDeviceDescription description)
        {
            return AddDevice(description, throwIfNoLayoutFound: true);
        }

        public InputDevice AddDevice(InputDeviceDescription description, bool throwIfNoLayoutFound,
            int deviceId = InputDevice.InvalidDeviceId, InputDevice.DeviceFlags deviceFlags = 0)
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

            var device = AddDevice(layout, deviceId, description, deviceFlags);
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
            var deviceId = device.id;
            ArrayHelpers.EraseAtWithCapacity(m_Devices, ref m_DevicesCount, deviceIndex);
            device.m_DeviceIndex = InputDevice.kInvalidDeviceIndex;
            m_DevicesById.Remove(deviceId);

            if (m_Devices != null)
            {
                var oldDeviceIndices = new int[m_DevicesCount];
                for (var i = 0; i < m_DevicesCount; ++i)
                {
                    oldDeviceIndices[i] = m_Devices[i].m_DeviceIndex;
                    m_Devices[i].m_DeviceIndex = i;
                }

                // Remove from state buffers.
                ReallocateStateBuffers(oldDeviceIndices);
            }
            else
            {
                // No more devices. Kill state buffers.
                m_StateBuffers.FreeAll();
            }

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
            device.BakeOffsetIntoStateBlockRecursive((uint)(-device.m_StateBlock.byteOffset));

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

        public InputDevice TryGetDevice(string nameOrLayout)
        {
            if (string.IsNullOrEmpty(nameOrLayout))
                throw new ArgumentException("nameOrLayout");

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
                throw new Exception($"Cannot find device with name or layout '{nameOrLayout}'");

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
                    ////TODO: leave state empty and compact array lazily on traversal
                    m_StateChangeMonitorTimeouts.RemoveAt(i);
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
            ////FIXME: This is nonsense; dynamic updates may not even be enabled; make a more appropriate choice here
            Update(InputUpdateType.Dynamic);
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
            if (ReferenceEquals(InputControlLayout.s_Layouts.baseLayoutTable, m_Layouts.baseLayoutTable))
                InputControlLayout.s_Layouts = new InputControlLayout.Collection();
            if (ReferenceEquals(InputProcessor.s_Processors.table, m_Processors.table))
                InputProcessor.s_Processors = new TypeTable();
            if (ReferenceEquals(InputInteraction.s_Interactions.table, m_Interactions.table))
                InputInteraction.s_Interactions = new TypeTable();
            if (ReferenceEquals(InputBindingComposite.s_Composites.table, m_Composites.table))
                InputBindingComposite.s_Composites = new TypeTable();

            // Detach from runtime.
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
                m_Runtime.onBeforeUpdate = null;
                m_Runtime.onFocusChanged = null;
                m_Runtime.onShouldRunUpdate = null;

                if (ReferenceEquals(InputRuntime.s_Instance, m_Runtime))
                    InputRuntime.s_Instance = null;
            }

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
            RegisterControlLayout("Digital", typeof(IntegerControl));
            RegisterControlLayout("Integer", typeof(IntegerControl));
            RegisterControlLayout("PointerPhase", typeof(PointerPhaseControl));
            RegisterControlLayout("Vector2", typeof(Vector2Control));
            RegisterControlLayout("Vector3", typeof(Vector3Control));
            RegisterControlLayout("Quaternion", typeof(QuaternionControl));
            RegisterControlLayout("Stick", typeof(StickControl));
            RegisterControlLayout("Dpad", typeof(DpadControl));
            RegisterControlLayout("DpadAxis", typeof(DpadControl.DpadAxisControl));
            RegisterControlLayout("AnyKey", typeof(AnyKeyControl));
            RegisterControlLayout("Touch", typeof(TouchControl));

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
        }

        internal void InstallRuntime(IInputRuntime runtime)
        {
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onBeforeUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
                m_Runtime.onFocusChanged = null;
                m_Runtime.onShouldRunUpdate = null;
            }

            m_Runtime = runtime;
            m_Runtime.onUpdate = OnUpdate;
            m_Runtime.onDeviceDiscovered = OnNativeDeviceDiscovered;
            m_Runtime.onFocusChanged = OnFocusChanged;
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
        internal int m_DeviceSetupVersion;////TODO
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

        internal int m_DisconnectedDevicesCount;
        internal InputDevice[] m_DisconnectedDevices;

        private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        internal InputStateBuffers m_StateBuffers;

        // We don't use UnityEvents and thus don't persist the callbacks during domain reloads.
        // Restoration of UnityActions is unreliable and it's too easy to end up with double
        // registrations what will lead to all kinds of misbehavior.
        private InlinedArray<DeviceChangeListener> m_DeviceChangeListeners;
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

        #if UNITY_ANALYTICS || UNITY_EDITOR
        private bool m_HaveSentStartupAnalytics;
        private bool m_HaveSentFirstUserInteractionAnalytics;
        #endif

        internal IInputRuntime m_Runtime;
        internal InputMetrics m_Metrics;
        internal InputSettings m_Settings;

        #if UNITY_EDITOR
        internal IInputDiagnostics m_Diagnostics;
        #endif

        private static void AddTypeRegistration(Dictionary<InternedString, Type> table, string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var internedName = new InternedString(name);
            table[internedName] = type;
        }

        private static Type LookupTypeRegisteration(Dictionary<InternedString, Type> table, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            var internedName = new InternedString(name);
            return table.TryGetValue(internedName, out var type) ? type : null;
        }

        // Maps a single control to an action interested in the control. If
        // multiple actions are interested in the same control, we will end up
        // processing the control repeatedly but we assume this is the exception
        // and so optimize for the case where there's only one action going to
        // a control.
        //
        // Split into two structures to keep data needed only when there is an
        // actual value change out of the data we need for doing the scanning.
        internal struct StateChangeMonitorMemoryRegion
        {
            public uint offsetRelativeToDevice;
            public uint sizeInBits; // Size of memory region to compare.
            public uint bitOffset;
        }
        internal struct StateChangeMonitorListener
        {
            public InputControl control;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
        }
        internal struct StateChangeMonitorsForDevice
        {
            public StateChangeMonitorMemoryRegion[] memoryRegions;
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
                var memoryRegionCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref memoryRegions, ref memoryRegionCount,
                    new StateChangeMonitorMemoryRegion
                    {
                        offsetRelativeToDevice = control.stateBlock.byteOffset - control.device.stateBlock.byteOffset,
                        sizeInBits = control.stateBlock.sizeInBits,
                        bitOffset = control.stateBlock.bitOffset
                    });

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
                        listeners[i] = new StateChangeMonitorListener();
                        memoryRegions[i] = new StateChangeMonitorMemoryRegion();
                        signalled.ClearBit(i);
                        break;
                    }
            }

            public void Clear()
            {
                // We don't actually release memory we've potentially allocated but rather just reset
                // our count to zero.
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
            if (device.id != InputDevice.InvalidDeviceId)
            {
                // Safety check to make sure out IDs are really unique.
                // Given they are assigned by the native system they should be fine
                // but let's make sure.
                var existingDeviceWithId = TryGetDeviceById(device.id);
                if (existingDeviceWithId != null)
                    throw new Exception(
                        $"Duplicate device ID {device.id} detected for devices '{device.name}' and '{existingDeviceWithId.name}'");
            }
            else
            {
                device.m_Id = m_Runtime.AllocateDeviceId();
            }
        }

        // (Re)allocates state buffers and assigns each device that's been added
        // a segment of the buffer. Preserves the current state of devices.
        // NOTE: Installs the buffers globally.
        private unsafe void ReallocateStateBuffers(int[] oldDeviceIndices = null)
        {
            var oldBuffers = m_StateBuffers;

            // Allocate new buffers.
            var newBuffers = new InputStateBuffers();
            var newStateBlockOffsets = newBuffers.AllocateAll(m_UpdateMask, m_Devices, m_DevicesCount);

            // Migrate state.
            newBuffers.MigrateAll(m_Devices, m_DevicesCount, newStateBlockOffsets, oldBuffers, oldDeviceIndices);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            InputStateBuffers.s_DefaultStateBuffer = newBuffers.defaultStateBuffer;
            InputStateBuffers.s_NoiseMaskBuffer = newBuffers.noiseMaskBuffer;

            // Make sure that even if we didn't have an update yet, we already have buffers active. This is
            // especially important if only dynamic updates are enabled. If we don't switch to the dynamic buffers
            // here, the first fixed update will run without any buffers active.
            if (m_Settings.updateMode == InputSettings.UpdateMode.ProcessEventsManually)
                InputStateBuffers.SwitchTo(m_StateBuffers, InputUpdateType.Manual);
            else
                InputStateBuffers.SwitchTo(m_StateBuffers,
                    InputUpdate.s_LastUpdateType != 0 ? InputUpdate.s_LastUpdateType : InputUpdateType.Dynamic);

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
                if (!control.hasDefaultValue)
                    continue;

                if (control.m_DefaultValue.isArray)
                    throw new NotImplementedException("default value arrays");

                control.m_StateBlock.Write(defaultStateBuffer, control.m_DefaultValue.primitiveValue);
            }

            // Copy default state to all front and back buffers.
            var stateBlock = device.m_StateBlock;
            var deviceIndex = device.m_DeviceIndex;
            if (m_StateBuffers.m_DynamicUpdateBuffers.valid)
            {
                stateBlock.CopyToFrom(m_StateBuffers.m_DynamicUpdateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                stateBlock.CopyToFrom(m_StateBuffers.m_DynamicUpdateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
            }
            if (m_StateBuffers.m_FixedUpdateBuffers.valid)
            {
                stateBlock.CopyToFrom(m_StateBuffers.m_FixedUpdateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                stateBlock.CopyToFrom(m_StateBuffers.m_FixedUpdateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
            }

            #if UNITY_EDITOR
            if (m_StateBuffers.m_EditorUpdateBuffers.valid)
            {
                stateBlock.CopyToFrom(m_StateBuffers.m_EditorUpdateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                stateBlock.CopyToFrom(m_StateBuffers.m_EditorUpdateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
            }
            #endif
        }

        private unsafe void InitializeNoiseMask(InputDevice device)
        {
            Debug.Assert(device != null);
            Debug.Assert(device.added);
            Debug.Assert(device.stateBlock.byteOffset != InputStateBlock.InvalidOffset);
            Debug.Assert(device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes <= m_StateBuffers.sizePerBuffer);

            var controls = device.allControls;
            var controlCount = controls.Count;

            // Assume that everything in the device is noise. This way we also catch memory regions
            // that are not actually covered by a control and implicitly mark them as noise (e.g. the
            // report ID in HID input reports).

            ////FIXME: this needs to properly take leaf vs non-leaf controls into account

            // Go through controls and for each one that isn't noisy, enable the control's
            // bits in the mask.
            var noiseMaskBuffer = m_StateBuffers.noiseMaskBuffer;
            for (var n = 0; n < controlCount; ++n)
            {
                var control = controls[n];
                if (control.noisy)
                    continue;

                if (control.m_DefaultValue.isArray)
                    throw new NotImplementedException("default value arrays");

                var stateBlock = control.m_StateBlock;
                Debug.Assert(stateBlock.byteOffset != InputStateBlock.InvalidOffset);
                Debug.Assert(stateBlock.bitOffset != InputStateBlock.InvalidOffset);
                Debug.Assert(stateBlock.sizeInBits != InputStateBlock.InvalidOffset);
                Debug.Assert(stateBlock.byteOffset >= device.stateBlock.byteOffset);
                Debug.Assert(stateBlock.byteOffset + stateBlock.alignedSizeInBytes <=
                    device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes);

                MemoryHelpers.SetBitsInBuffer(noiseMaskBuffer, (int)stateBlock.byteOffset, (int)stateBlock.bitOffset,
                    (int)stateBlock.sizeInBits, true);
            }
        }

        private void OnNativeDeviceDiscovered(int deviceId, string deviceDescriptor)
        {
            // Make sure we're not adding to m_AvailableDevices before we restored what we
            // had before a domain reload.
            RestoreDevicesAfterDomainReloadIfNecessary();

            // Parse description.
            var description = InputDeviceDescription.FromJson(deviceDescriptor);

            // Add it.
            var markAsRemoved = false;
            try
            {
                // If we have a restricted set of supported devices, first check if it's a device
                // we should support.
                if (m_Settings.supportedDevices.Count > 0)
                {
                    var layout = TryFindMatchingControlLayout(ref description, deviceId);
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

                // See if we have a disconnected device we can revive.
                InputDevice device = null;
                for (var i = 0; i < m_DisconnectedDevicesCount; ++i)
                {
                    if (m_DisconnectedDevices[i].description == description)
                    {
                        device = m_DisconnectedDevices[i];
                        ArrayHelpers.EraseAtWithCapacity(m_DisconnectedDevices, ref m_DisconnectedDevicesCount, i);
                        break;
                    }
                }

                if (device != null)
                {
                    // It's a device we pulled from the disconnected list. Update the device with the
                    // new ID, re-add it and notify that we've reconnected.

                    device.m_Id = deviceId;
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

        private unsafe void OnBeforeUpdate(InputUpdateType updateType)
        {
            // Restore devices before checking update mask. See InputSystem.RunInitialUpdate().
            RestoreDevicesAfterDomainReloadIfNecessary();

            ////FIXME: this shouldn't happen; looks like are sometimes getting before-update calls from native when we shouldn't
            if ((updateType & m_UpdateMask) == 0)
                return;

            // For devices that have state callbacks, tell them we're carrying state over
            // into the next frame.
            if (m_HaveDevicesWithStateCallbackReceivers && updateType != InputUpdateType.BeforeRender) ////REVIEW: before-render handling is probably wrong
            {
                var stateBuffers = m_StateBuffers.GetDoubleBuffersFor(updateType);
                var isDynamicOrFixedUpdate =
                    updateType == InputUpdateType.Dynamic || updateType == InputUpdateType.Fixed;

                ////REVIEW: should we rather allocate a temp buffer on a per-device basis? the current code makes one
                ////        temp allocation equal to the combined state of all devices in the system; even with touch in the
                ////        mix, that should amount to less than 3k in most cases

                // For the sake of action state monitors, we need to be able to detect when
                // an OnCarryStateForward() method writes new values into a state buffer. To do
                // so, we create a temporary buffer, copy state blocks into that buffer, and then
                // run the normal action change logic on the temporary and the current state buffer.
                using (var tempBuffer = new NativeArray<byte>((int)m_StateBuffers.sizePerBuffer, Allocator.Temp))
                {
                    var tempBufferPtr = (byte*)tempBuffer.GetUnsafeReadOnlyPtr();
                    var currentTimeExternal = m_Runtime.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

                    for (var i = 0; i < m_DevicesCount; ++i)
                    {
                        var device = m_Devices[i];
                        if ((device.m_DeviceFlags & InputDevice.DeviceFlags.HasStateCallbacks) != InputDevice.DeviceFlags.HasStateCallbacks)
                            continue;

                        // Depending on update ordering, we are writing events into *upcoming* updates inside of
                        // OnUpdate(). E.g. we may receive an event in fixed update and write it concurrently into
                        // the fixed and dynamic update buffer for the device.
                        //
                        // This means that we have to be extra careful here not to overwrite state which has already
                        // been updated with events. To check for this, we simply determine whether the device's update
                        // count for the current update type already corresponds to the count of the upcoming update.
                        //
                        // NOTE: This is only relevant for non-editor updates.
                        if (isDynamicOrFixedUpdate)
                        {
                            if (updateType == InputUpdateType.Dynamic)
                            {
                                if (device.m_CurrentDynamicUpdateCount == InputUpdate.s_DynamicUpdateCount + 1)
                                    continue; // Device already received state for upcoming dynamic update.
                            }
                            else if (updateType == InputUpdateType.Fixed)
                            {
                                if (device.m_CurrentFixedUpdateCount == InputUpdate.s_FixedUpdateCount + 1)
                                    continue; // Device already received state for upcoming fixed update.
                            }
                        }

                        var deviceStateOffset = device.m_StateBlock.byteOffset;
                        var deviceStateSize = device.m_StateBlock.alignedSizeInBytes;

                        // Grab current front buffer.
                        var frontBuffer = stateBuffers.GetFrontBuffer(device.m_DeviceIndex);

                        // Copy to temporary buffer.
                        var statePtr = (byte*)frontBuffer + deviceStateOffset;
                        var tempStatePtr = tempBufferPtr + deviceStateOffset;
                        UnsafeUtility.MemCpy(tempStatePtr, statePtr, deviceStateSize);

                        // NOTE: We do *not* perform a buffer flip here as we do not want to change what is the
                        //       current and what is the previous state when we carry state forward. Rather,
                        //       OnCarryStateForward, if it modifies state, it modifies the current state directly.
                        //       Also, for the same reasons, we do not modify the dynamic/fixed update counts
                        //       on the device. If an event comes in in the upcoming update, it should lead to
                        //       a buffer flip.

                        // Show to device.
                        if (((IInputStateCallbackReceiver)device).OnCarryStateForward(frontBuffer))
                        {
                            ////REVIEW: should this make the device current? (and update m_LastUpdateTimeInternal)

                            // Let listeners know the device's state has changed.
                            for (var n = 0; n < m_DeviceChangeListeners.length; ++n)
                                m_DeviceChangeListeners[n](device, InputDeviceChange.StateChanged);

                            // Process action state change monitors.
                            if (ProcessStateChangeMonitors(i, statePtr, tempStatePtr, deviceStateSize, 0))
                            {
                                FireStateChangeNotifications(i, currentTimeExternal, null);
                            }
                        }
                    }
                }
            }

            ////REVIEW: should we activate the buffers for the given update here?
            DelegateHelpers.InvokeCallbacksSafe(ref m_BeforeUpdateListeners, updateType, "onBeforeUpdate");
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
            switch (m_Settings.updateMode)
            {
                case InputSettings.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate:
                    newUpdateMask |= InputUpdateType.Fixed;
                    newUpdateMask |= InputUpdateType.Dynamic;
                    break;
                case InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly:
                    newUpdateMask |= InputUpdateType.Dynamic;
                    break;
                case InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly:
                    newUpdateMask |= InputUpdateType.Fixed;
                    break;
                case InputSettings.UpdateMode.ProcessEventsManually:
                    newUpdateMask |= InputUpdateType.Manual;
                    break;
                default:
                    throw new NotImplementedException("Input update mode: " + m_Settings.updateMode);
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
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                var id = m_AvailableDevices[i].deviceId;
                if (TryGetDeviceById(id) != null)
                    continue;

                var layout = TryFindMatchingControlLayout(ref m_AvailableDevices[i].description, id);
                if (IsDeviceLayoutMarkedAsSupportedInSettings(layout))
                {
                    AddDevice(m_AvailableDevices[i].description, false, id,
                        m_AvailableDevices[i].isNative ? InputDevice.DeviceFlags.Native : 0);
                }
            }

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
                        if (m_AvailableDevices[n].deviceId == device.id)
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

            // Let listeners know.
            for (var i = 0; i < m_SettingsChangedListeners.length; ++i)
                m_SettingsChangedListeners[i]();
        }

        private void OnFocusChanged(bool focus)
        {
            m_HasFocus = focus;
            var deviceCount = m_DevicesCount;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                if (!InputSystem.TrySyncDevice(device))
                    InputSystem.TryResetDevice(device);
            }
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
        /// updates can also be manually triggered through <see cref="InputSystem.Update()"/>.
        ///
        /// The method receives the event buffer used internally by the runtime to collect events.
        ///
        /// Note that update types do *NOT* say what the events we receive are for. The update type only indicates
        /// where in the Unity's application loop we got called from. Where the event data goes depends wholly on
        /// which buffers we activate in the update and write the event data into.
        /// </remarks>
        private unsafe void OnUpdate(InputUpdateType updateType, ref InputEventBuffer eventBuffer)
        {
            ////TODO: switch from Profiler to CustomSampler API
            // NOTE: This is *not* using try/finally as we've seen unreliability in the EndSample()
            //       execution (and we're not sure where it's coming from).
            Profiler.BeginSample("InputUpdate");

            // Restore devices before checking update mask. See InputSystem.RunInitialUpdate().
            RestoreDevicesAfterDomainReloadIfNecessary();

            ////FIXME: this shouldn't happen; looks like are sometimes getting before-update calls from native when we shouldn't
            if ((updateType & m_UpdateMask) == 0)
            {
                Profiler.EndSample();
                return;
            }

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
            if (updateType == InputUpdateType.Dynamic)
            {
                ++InputUpdate.s_DynamicUpdateCount;
            }
            else if (updateType == InputUpdateType.Fixed)
            {
                ++InputUpdate.s_FixedUpdateCount;
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
            var timesliceEvents = false;
            timesliceEvents = gameIsPlayingAndHasFocus && m_Settings.timesliceEvents; // We never timeslice for editor updates.

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
                InvokeAfterUpdateCallback(updateType);
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

                // If we're timeslicing, check if the event time is within limits.
                if (timesliceEvents && currentEventReadPtr->internalTime >= currentTime)
                {
                    eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                        ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: true);
                    continue;
                }

                if (currentEventReadPtr->time <= currentTime)
                    totalEventLag += currentTime - currentEventReadPtr->time;

                // Give listeners a shot at the event.
                var listenerCount = m_EventListeners.length;
                if (listenerCount > 0)
                {
                    for (var i = 0; i < listenerCount; ++i)
                        m_EventListeners[i](new InputEventPtr(currentEventReadPtr));

                    // If a listener marks the event as handled, we don't process it further.
                    if (currentEventReadPtr->handled)
                    {
                        eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                            ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);
                        continue;
                    }
                }

                // Grab device for event. In before-render updates, we already had to
                // check the device.
                if (!isBeforeRenderUpdate)
                    device = TryGetDeviceById(currentEventReadPtr->deviceId);
                if (device == null)
                {
                    #if UNITY_EDITOR
                    m_Diagnostics?.OnCannotFindDeviceForEvent(new InputEventPtr(currentEventReadPtr));
                    #endif

                    eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                        ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);

                    // No device found matching event. Ignore it.
                    continue;
                }

                // Process.
                var currentEventType = currentEventReadPtr->type;
                var currentEventTimeInternal = currentEventReadPtr->internalTime;
                switch (currentEventType)
                {
                    case StateEvent.Type:
                    case DeltaStateEvent.Type:

                        // Ignore state changes if device is disabled.
                        if (!device.enabled)
                        {
                            #if UNITY_EDITOR
                            m_Diagnostics?.OnEventForDisabledDevice(new InputEventPtr(currentEventReadPtr), device);
                            #endif
                            break;
                        }

                        var deviceHasStateCallbacks = (device.m_DeviceFlags & InputDevice.DeviceFlags.HasStateCallbacks) ==
                            InputDevice.DeviceFlags.HasStateCallbacks;
                        IInputStateCallbackReceiver stateCallbacks = null;
                        var deviceIndex = device.m_DeviceIndex;
                        var stateBlockOfDevice = device.m_StateBlock;
                        var stateBlockSizeOfDevice = stateBlockOfDevice.alignedSizeInBytes;
                        var offsetInDeviceStateToCopyTo = 0u;
                        uint sizeOfStateToCopy;
                        uint receivedStateSize;
                        byte* ptrToReceivedState;
                        FourCC receivedStateFormat;
                        var needToCopyFromBackBuffer = false;

                        // Grab state data from event and decide where to copy to and how much to copy.
                        if (currentEventType == StateEvent.Type)
                        {
                            var stateEventPtr = (StateEvent*)currentEventReadPtr;
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
                            Debug.Assert(currentEventType == DeltaStateEvent.Type);

                            var deltaEventPtr = (DeltaStateEvent*)currentEventReadPtr;
                            receivedStateFormat = deltaEventPtr->stateFormat;
                            receivedStateSize = deltaEventPtr->deltaStateSizeInBytes;
                            ptrToReceivedState = (byte*)deltaEventPtr->deltaState;
                            offsetInDeviceStateToCopyTo = deltaEventPtr->stateOffset;

                            // Ignore extra state at end of event.
                            sizeOfStateToCopy = receivedStateSize;
                            if (offsetInDeviceStateToCopyTo + sizeOfStateToCopy > stateBlockSizeOfDevice)
                            {
                                if (offsetInDeviceStateToCopyTo >= stateBlockSizeOfDevice)
                                    break; // Entire delta state is out of range.

                                sizeOfStateToCopy = stateBlockSizeOfDevice - offsetInDeviceStateToCopyTo;
                            }
                        }

                        // Ignore the event if the last state update we received for the device was
                        // newer than this state event is. We don't allow devices to go back in time.
                        //
                        // NOTE: We make an exception here for devices that implement IInputStateCallbackReceiver (such
                        //       as Touchscreen). For devices that dynamically incorporate state it can be hard ensuring
                        //       a global ordering of events as there may be multiple substreams (e.g. each individual touch)
                        //       that are generated in the backend and would require considerable work to ensure monotonically
                        //       increasing timestamps across all such streams.
                        //
                        //       This exception isn't elegant. But then, the entire IInputStateCallbackReceiver thing
                        //       is anything but elegant...
                        if (currentEventTimeInternal < device.m_LastUpdateTimeInternal &&
                            !(deviceHasStateCallbacks && stateBlockOfDevice.format != receivedStateFormat))
                        {
                            #if UNITY_EDITOR
                            m_Diagnostics?.OnEventTimestampOutdated(new InputEventPtr(currentEventReadPtr), device);
                            #endif
                            break;
                        }

                        // If the state format doesn't match, see if the device knows what to do.
                        // If not, ignore the event.
                        if (stateBlockOfDevice.format != receivedStateFormat)
                        {
                            var canIncorporateUnrecognizedState = false;
                            if (deviceHasStateCallbacks)
                            {
                                if (stateCallbacks == null)
                                    stateCallbacks = (IInputStateCallbackReceiver)device;
                                canIncorporateUnrecognizedState =
                                    stateCallbacks.OnReceiveStateWithDifferentFormat(ptrToReceivedState, receivedStateFormat,
                                        receivedStateSize, ref offsetInDeviceStateToCopyTo);
                            }

                            if (!canIncorporateUnrecognizedState)
                            {
                                #if UNITY_EDITOR
                                m_Diagnostics?.OnEventFormatMismatch(new InputEventPtr(currentEventReadPtr), device);
                                #endif
                                break;
                            }
                        }

                        // If the device has state callbacks, give it a shot at running custom logic on
                        // the new state before we integrate it into the system.
                        if (deviceHasStateCallbacks)
                        {
                            if (stateCallbacks == null)
                                stateCallbacks = (IInputStateCallbackReceiver)device;

                            ////FIXME: for delta state events, this may lead to invalid memory accesses on newState; in fact the entire logic is screwed up here for delta events
                            ////FIXME: this will read state from the current update, then combine it with the new state, and then write into all states

                            var currentState = InputStateBuffers.GetFrontBufferForDevice(deviceIndex);
                            var newState = ptrToReceivedState - stateBlockOfDevice.byteOffset;  // Account for device offset in buffers.

                            stateCallbacks.OnBeforeWriteNewState(currentState, newState);
                        }

                        var deviceBuffer = InputStateBuffers.GetFrontBufferForDevice(deviceIndex);

                        // Before we update state, let change monitors compare the old and the new state.
                        // We do this instead of first updating the front buffer and then comparing to the
                        // back buffer as that would require a buffer flip for each state change in order
                        // for the monitors to work reliably. By comparing the *event* data to the current
                        // state, we can have multiple state events in the same frame yet still get reliable
                        // change notifications.
                        var haveSignalledMonitors =
                            gameIsPlayingAndHasFocus &&
                            ProcessStateChangeMonitors(deviceIndex, ptrToReceivedState,
                                (byte*)deviceBuffer + stateBlockOfDevice.byteOffset,
                                sizeOfStateToCopy, offsetInDeviceStateToCopyTo);

                        var deviceStateOffset = device.m_StateBlock.byteOffset + offsetInDeviceStateToCopyTo;

                        // If noise filtering on .current is turned on and the device may have noise,
                        // determine if the event carries signal or not.
                        var makeDeviceCurrent = true;
                        if (device.noisy && m_Settings.filterNoiseOnCurrent)
                        {
                            // Compare the current state of the device to the newly received state but overlay
                            // the comparison by the noise mask.

                            var offset = device.m_StateBlock.byteOffset + offsetInDeviceStateToCopyTo;
                            var currentState = (byte*)InputStateBuffers.GetFrontBufferForDevice(deviceIndex) + offset;
                            var noiseMask = (byte*)InputStateBuffers.s_NoiseMaskBuffer + offset;

                            makeDeviceCurrent =
                                !MemoryHelpers.MemCmpBitRegion(currentState, ptrToReceivedState,
                                    0, receivedStateSize * 8, mask: noiseMask);
                        }

                        // Buffer flip.
                        if (FlipBuffersForDeviceIfNecessary(device, updateType))
                        {
                            // In case of a delta state event we need to carry forward all state we're
                            // not updating. Instead of optimizing the copy here, we're just bringing the
                            // entire state forward.
                            //
                            // Also, if we received a mismatching state format that the device chose to
                            // incorporate using OnReceiveStateWithDifferentFormat, we're potentially performing
                            // a partial state update, so bring the current state forward like for delta
                            // state events.
                            if (currentEventType == DeltaStateEvent.Type || receivedStateFormat != stateBlockOfDevice.format)
                                needToCopyFromBackBuffer = true;
                        }

                        // Now write the state.
                        #if UNITY_EDITOR
                        if (!gameIsPlayingAndHasFocus)
                        {
                            var frontBuffer = m_StateBuffers.m_EditorUpdateBuffers.GetFrontBuffer(deviceIndex);
                            Debug.Assert(frontBuffer != null);

                            if (needToCopyFromBackBuffer)
                            {
                                var backBuffer = m_StateBuffers.m_EditorUpdateBuffers.GetBackBuffer(deviceIndex);
                                Debug.Assert(backBuffer != null);

                                UnsafeUtility.MemCpy(
                                    (byte*)frontBuffer + (int)stateBlockOfDevice.byteOffset,
                                    (byte*)backBuffer + (int)stateBlockOfDevice.byteOffset,
                                    stateBlockSizeOfDevice);
                            }

                            UnsafeUtility.MemCpy((byte*)frontBuffer + (int)deviceStateOffset, ptrToReceivedState, sizeOfStateToCopy);
                        }
                        else
                        #endif
                        if (updateType == InputUpdateType.Manual)
                        {
                            var frontBuffer = m_StateBuffers.m_ManualUpdateBuffers.GetFrontBuffer(deviceIndex);
                            Debug.Assert(frontBuffer != null);

                            if (needToCopyFromBackBuffer)
                            {
                                var backBuffer = m_StateBuffers.m_ManualUpdateBuffers.GetBackBuffer(deviceIndex);
                                Debug.Assert(backBuffer != null);

                                UnsafeUtility.MemCpy(
                                    (byte*)frontBuffer + (int)stateBlockOfDevice.byteOffset,
                                    (byte*)backBuffer + (int)stateBlockOfDevice.byteOffset,
                                    stateBlockSizeOfDevice);
                            }

                            UnsafeUtility.MemCpy((byte*)frontBuffer + (int)deviceStateOffset, ptrToReceivedState, sizeOfStateToCopy);
                        }
                        else
                        {
                            // For dynamic and fixed updates, we have to write into the front buffer
                            // of both updates as a state change event comes in only once and we have
                            // to reflect the most current state in both update types.
                            //
                            // If one or the other update is disabled, however, we will perform a single
                            // memcpy here.
                            if (m_StateBuffers.m_DynamicUpdateBuffers.valid)
                            {
                                var frontBuffer = m_StateBuffers.m_DynamicUpdateBuffers.GetFrontBuffer(deviceIndex);
                                Debug.Assert(frontBuffer != null);

                                if (needToCopyFromBackBuffer)
                                {
                                    var backBuffer = m_StateBuffers.m_DynamicUpdateBuffers.GetBackBuffer(deviceIndex);
                                    Debug.Assert(backBuffer != null);

                                    UnsafeUtility.MemCpy(
                                        (byte*)frontBuffer + (int)stateBlockOfDevice.byteOffset,
                                        (byte*)backBuffer + (int)stateBlockOfDevice.byteOffset,
                                        stateBlockSizeOfDevice);
                                }

                                UnsafeUtility.MemCpy((byte*)frontBuffer + (int)deviceStateOffset, ptrToReceivedState, sizeOfStateToCopy);
                            }
                            if (m_StateBuffers.m_FixedUpdateBuffers.valid)
                            {
                                var frontBuffer = m_StateBuffers.m_FixedUpdateBuffers.GetFrontBuffer(deviceIndex);
                                Debug.Assert(frontBuffer != null);

                                if (needToCopyFromBackBuffer)
                                {
                                    var backBuffer = m_StateBuffers.m_FixedUpdateBuffers.GetBackBuffer(deviceIndex);
                                    Debug.Assert(backBuffer != null);

                                    UnsafeUtility.MemCpy(
                                        (byte*)frontBuffer + (int)stateBlockOfDevice.byteOffset,
                                        (byte*)backBuffer + (int)stateBlockOfDevice.byteOffset,
                                        stateBlockSizeOfDevice);
                                }

                                UnsafeUtility.MemCpy((byte*)frontBuffer + (int)deviceStateOffset, ptrToReceivedState, sizeOfStateToCopy);
                            }
                        }

                        if (device.m_LastUpdateTimeInternal <= currentEventTimeInternal)
                            device.m_LastUpdateTimeInternal = currentEventTimeInternal;
                        if (makeDeviceCurrent)
                            device.MakeCurrent();

                        // Notify listeners.
                        for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                            m_DeviceChangeListeners[i](device, InputDeviceChange.StateChanged);

                        // Now that we've committed the new state to memory, if any of the change
                        // monitors fired, let the associated actions know.
                        ////FIXME: this needs to happen with player buffers active
                        if (haveSignalledMonitors)
                            FireStateChangeNotifications(deviceIndex, currentEventTimeInternal, currentEventReadPtr);

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
                        for (var i = 0; i < m_DeviceChangeListeners.length; ++i)
                            m_DeviceChangeListeners[i](device, InputDeviceChange.ConfigurationChanged);
                        break;
                }

                eventBuffer.AdvanceToNextEvent(ref currentEventReadPtr, ref currentEventWritePtr,
                    ref numEventsRetainedInBuffer, ref remainingEventCount, leaveEventInBuffer: false);
            }

            m_Metrics.totalEventProcessingTime += Time.realtimeSinceStartup - processingStartTime;
            m_Metrics.totalEventLagTime += totalEventLag;

            ////REVIEW: This was moved to 2019.2 while the timeslice code was wrong; should probably be reenabled now
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
            InvokeAfterUpdateCallback(updateType);
            ////TODO: check if there's new events in the event buffer; if so, do a pass over those events right away
        }

        private void InvokeAfterUpdateCallback(InputUpdateType updateType)
        {
            for (var i = 0; i < m_AfterUpdateListeners.length; ++i)
                m_AfterUpdateListeners[i](updateType);
        }

        /// <summary>
        /// Switch the current set of state buffers (<see cref="InputStateBuffers"/>) to the
        /// ones that are tracked by input actions.
        /// </summary>
        /// <remarks>
        /// If both fixed and dynamic updates are enabled, we use the buffers dictated by <see cref="InputSettings.actionUpdateMode"/>.
        /// Otherwise we use fixed, dynamic, or manual update buffers depending on <see cref="InputSettings.updateMode"/>.
        /// </remarks>
        private void SwitchToBuffersForActions()
        {
            InputUpdateType updateType;
            switch (m_Settings.updateMode)
            {
                case InputSettings.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate:
                    if (m_Settings.actionUpdateMode == InputSettings.ActionUpdateMode.UpdateActionsInFixedUpdate)
                        updateType = InputUpdateType.Fixed;
                    else
                        updateType = InputUpdateType.Dynamic;
                    break;

                case InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly:
                    updateType = InputUpdateType.Dynamic;
                    break;

                case InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly:
                    updateType = InputUpdateType.Fixed;
                    break;

                case InputSettings.UpdateMode.ProcessEventsManually:
                    updateType = InputUpdateType.Manual;
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unrecognized update mode '{m_Settings.updateMode}'; don't know which buffers to use for actions");
            }

            InputStateBuffers.SwitchTo(m_StateBuffers, updateType);
        }

        // NOTE: 'newState' can be a subset of the full state stored at 'oldState'. In this case,
        //       'newStateOffset' must give the offset into the full state and 'newStateSize' must
        //       give the size of memory slice to be updated.
        private unsafe bool ProcessStateChangeMonitors(int deviceIndex, void* newState, void* oldState, uint newStateSize, uint newStateOffset)
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

            // Bake offsets into state pointers so that we don't have to adjust for
            // them repeatedly.
            if (newStateOffset != 0)
            {
                newState = (byte*)newState - newStateOffset;
                oldState = (byte*)oldState + newStateOffset;
            }

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

                var offset = (int)memoryRegion.offsetRelativeToDevice;
                var sizeInBits = memoryRegion.sizeInBits;
                var bitOffset = memoryRegion.bitOffset;

                // If we've updated only part of the state, see if the monitored region and the
                // updated region overlap. Ignore monitor if they don't.
                if (newStateOffset != 0 &&
                    !MemoryHelpers.MemoryOverlapsBitRegion((uint)offset, bitOffset, sizeInBits, newStateOffset, (uint)newStateSize))
                    continue;

                // See if we are comparing bits or bytes.
                if (sizeInBits % 8 != 0 || bitOffset != 0)
                {
                    // Not-so-simple path: compare bits.

                    // Check if bit offset is out of range of state we have.
                    if (MemoryHelpers.ComputeFollowingByteOffset((uint)offset + newStateOffset, bitOffset + sizeInBits) > newStateSize)
                        continue;

                    if (sizeInBits > 1)
                    {
                        // Multi-bit value.
                        if (MemoryHelpers.MemCmpBitRegion((byte*)newState + offset,
                            (byte*)oldState + offset, bitOffset, sizeInBits))
                            continue;
                    }
                    else
                    {
                        // Single-bit value.
                        if (MemoryHelpers.ReadSingleBit((byte*)newState + offset, bitOffset) ==
                            MemoryHelpers.ReadSingleBit((byte*)oldState + offset, bitOffset))
                            continue;
                    }
                }
                else
                {
                    // Simple path: compare whole bytes.

                    var sizeInBytes = sizeInBits / 8;
                    if (offset - newStateOffset + sizeInBytes > newStateSize)
                        continue;

                    if (UnsafeUtility.MemCmp((byte*)newState + offset, (byte*)oldState + offset, sizeInBytes) == 0)
                        continue;
                }

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
            var timeoutCount = m_StateChangeMonitorTimeouts.length;
            if (timeoutCount == 0)
                return;

            SwitchToBuffersForActions();

            // Go through the list and both trigger expired timers and remove any irrelevant
            // ones by compacting the array.
            // NOTE: We do not actually release any memory we may have allocated.
            var currentTime = m_Runtime.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            var remainingTimeoutCount = 0;
            for (var i = 0; i < timeoutCount; ++i)
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
            // Updates go to the editor only if the game isn't playing or does not have focus.
            // Otherwise we fall through to the logic that flips for the *next* dynamic and
            // fixed updates.
            if (updateType == InputUpdateType.Editor && !gameIsPlayingAndHasFocus)
            {
                // The editor doesn't really have a concept of frame-to-frame operation the
                // same way the player does. So we simply flip buffers on a device whenever
                // a new state event for it comes in.
                m_StateBuffers.m_EditorUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                return true;
            }
#endif

            var flipped = false;

            // If it is *NOT* a fixed update, we need to flip for the *next* coming fixed
            // update if we haven't already.
            if (updateType != InputUpdateType.Fixed &&
                device.m_CurrentFixedUpdateCount != InputUpdate.s_FixedUpdateCount + 1)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentFixedUpdateCount = InputUpdate.s_FixedUpdateCount + 1;
                flipped = true;
            }

            // If it is *NOT* a dynamic update, we need to flip for the *next* coming
            // dynamic update if we haven't already.
            if (updateType != InputUpdateType.Dynamic &&
                device.m_CurrentDynamicUpdateCount != InputUpdate.s_DynamicUpdateCount + 1)
            {
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentDynamicUpdateCount = InputUpdate.s_DynamicUpdateCount + 1;
                flipped = true;
            }

            // If it *is* a fixed update and we haven't flipped for the current update
            // yet, do it.
            if (updateType == InputUpdateType.Fixed &&
                device.m_CurrentFixedUpdateCount != InputUpdate.s_FixedUpdateCount)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentFixedUpdateCount = InputUpdate.s_FixedUpdateCount;
                flipped = true;
            }

            // If it *is* a dynamic update and we haven't flipped for the current update
            // yet, do it.
            if (updateType == InputUpdateType.Dynamic &&
                device.m_CurrentDynamicUpdateCount != InputUpdate.s_DynamicUpdateCount)
            {
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentDynamicUpdateCount = InputUpdate.s_DynamicUpdateCount;
                flipped = true;
            }

            // In manual mode, we always flip. There are no frame boundaries.
            if (updateType == InputUpdateType.Manual)
            {
                m_StateBuffers.m_ManualUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                flipped = true;
            }

            return flipped;
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
                if (usages.LengthSafe() > 0)
                {
                    var index = ArrayHelpers.Append(ref device.m_UsagesForEachControl, usages.Select(x => new InternedString(x)));
                    device.m_UsagesReadOnly = new ReadOnlyArray<InternedString>(device.m_UsagesForEachControl, index, usages.Length);
                    device.UpdateUsageArraysOnControls();
                }

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
            public int deviceSetupVersion;
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
            public bool haveSentFirstUserInteractionAnalytics;
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
                    deviceId = device.id,
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
                deviceSetupVersion = m_DeviceSetupVersion,
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
                haveSentFirstUserInteractionAnalytics = m_HaveSentFirstUserInteractionAnalytics,
                #endif
            };
        }

        internal void RestoreStateWithoutDevices(SerializedState state)
        {
            m_StateBuffers = state.buffers;
            m_LayoutRegistrationVersion = state.layoutRegistrationVersion + 1;
            m_DeviceSetupVersion = state.deviceSetupVersion + 1;
            updateMask = state.updateMask;
            m_Metrics = state.metrics;
            m_PollingFrequency = state.pollingFrequency;

            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);
            m_Settings = state.settings;

            #if UNITY_ANALYTICS || UNITY_EDITOR
            m_HaveSentStartupAnalytics = state.haveSentStartupAnalytics;
            m_HaveSentFirstUserInteractionAnalytics = state.haveSentFirstUserInteractionAnalytics;
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
            Debug.Assert(m_SavedDeviceStates != null);
            Profiler.BeginSample("InputManager.RestoreDevicesAfterDomainReload");

            var deviceCount = m_SavedDeviceStates.Length;
            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceState = m_SavedDeviceStates[i];

                InputDevice device;
                try
                {
                    // If the device has a description, we have it go through the normal matching
                    // process so that it comes out as whatever corresponds to the current layout
                    // registration state (which may be different from before the domain reload).
                    // Only if it's a device added with AddDevice(string) directly do we just try
                    // to create a device with the same layout.
                    if (!deviceState.description.empty)
                    {
                        device = AddDevice(deviceState.description, throwIfNoLayoutFound: true,
                            deviceId: deviceState.deviceId, deviceFlags: deviceState.flags);
                    }
                    else
                    {
                        // See if we still have the layout that the device used. Might have
                        // come from a type that was removed in the meantime. If so, just
                        // don't re-add the device.
                        var layout = new InternedString(deviceState.layout);
                        if (!m_Layouts.HasLayout(layout))
                        {
                            Debug.Log(
                                $"Removing input device '{deviceState.name}' with layout '{deviceState.layout}' which has been removed");
                            continue;
                        }

                        device = AddDevice(layout, deviceState.deviceId,
                            deviceFlags: deviceState.flags,
                            variants: new InternedString(deviceState.variants));
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Could not re-recreate input device '{deviceState.description}' with layout '{deviceState.layout}' and variants '{deviceState.variants}' after domain reload");
                    Debug.LogException(exception);
                    continue;
                }

                deviceState.Restore(device);
            }

            // See if we can make sense of an available device now that we couldn't make sense of
            // before. This can be the case if there's new layout information that wasn't available
            // before.
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

            // Done. Discard saved arrays.
            m_SavedDeviceStates = null;
            m_SavedAvailableDevices = null;

            Profiler.EndSample();
        }

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
    }
}
