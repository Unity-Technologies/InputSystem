using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Controls;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;
using Unity.Profiling;
using UnityEngineInternal.Input;
#if UNITY_6000_5_OR_NEWER
using UnityEngine.Assemblies;
#endif

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

#if UNITY_EDITOR
using CustomBindingPathValidator = System.Func<string, System.Action>;
#endif

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
    internal partial class InputManager
    {
        public ReadOnlyArray<InputDevice> devices => new ReadOnlyArray<InputDevice>(m_Devices, 0, m_DevicesCount);

        public TypeTable processors => m_Processors;
        public TypeTable interactions => m_Interactions;
        public TypeTable composites => m_Composites;

        static readonly ProfilerMarker k_InputUpdateProfilerMarker = new ProfilerMarker("InputUpdate");
        static readonly ProfilerMarker k_InputTryFindMatchingControllerMarker = new ProfilerMarker("InputSystem.TryFindMatchingControlLayout");
        static readonly ProfilerMarker k_InputAddDeviceMarker = new ProfilerMarker("InputSystem.AddDevice");
        static readonly ProfilerMarker k_InputRestoreDevicesAfterReloadMarker = new ProfilerMarker("InputManager.RestoreDevicesAfterDomainReload");
        static readonly ProfilerMarker k_InputRegisterCustomTypesMarker = new ProfilerMarker("InputManager.RegisterCustomTypes");

        static readonly ProfilerMarker k_InputOnBeforeUpdateMarker = new ProfilerMarker("InputSystem.onBeforeUpdate");
        static readonly ProfilerMarker k_InputOnAfterUpdateMarker = new ProfilerMarker("InputSystem.onAfterUpdate");
        static readonly ProfilerMarker k_InputOnSettingsChangeMarker = new ProfilerMarker("InputSystem.onSettingsChange");
        static readonly ProfilerMarker k_InputOnDeviceSettingsChangeMarker = new ProfilerMarker("InputSystem.onDeviceSettingsChange");
        static readonly ProfilerMarker k_InputOnEventMarker = new ProfilerMarker("InputSystem.onEvent");
        static readonly ProfilerMarker k_InputOnLayoutChangeMarker = new ProfilerMarker("InputSystem.onLayoutChange");
        static readonly ProfilerMarker k_InputOnDeviceChangeMarker = new ProfilerMarker("InpustSystem.onDeviceChange");
        static readonly ProfilerMarker k_InputOnActionsChangeMarker = new ProfilerMarker("InpustSystem.onActionsChange");


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

        public InputActionAsset actions
        {
            get
            {
                return m_Actions;
            }

            set
            {
                m_Actions = value;
                ApplyActions();
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
                if (m_CurrentUpdate != default)
                    return m_CurrentUpdate;

                #if UNITY_EDITOR
                if (!m_RunPlayerUpdatesInEditMode && (!gameIsPlaying || !gameHasFocus))
                    return InputUpdateType.Editor;
                #endif

                return m_UpdateMask.GetUpdateTypeForPlayer();
            }
        }

        public InputSettings.ScrollDeltaBehavior scrollDeltaBehavior
        {
            get => m_ScrollDeltaBehavior;
            set
            {
                if (m_ScrollDeltaBehavior == value)
                    return;

                m_ScrollDeltaBehavior = value;

#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
                InputRuntime.s_Instance.normalizeScrollWheelDelta =
                    m_ScrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms;
#endif
            }
        }

        public float pollingFrequency
        {
            get
            {
                #if UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
                return m_Runtime.pollingFrequency;
                #else
                return m_PollingFrequency;
                #endif
            }

            set
            {
                ////REVIEW: allow setting to zero to turn off polling altogether?
                if (value <= 0)
                    throw new ArgumentException("Polling frequency must be greater than zero", "value");

                #if UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
                m_Runtime.pollingFrequency = value;
                #else
                m_PollingFrequency = value;
                if (m_Runtime != null)
                    m_Runtime.pollingFrequency = value;
                #endif
            }
        }

        /// <summary>
        /// The policy to be applied when processing input events that has been marked as "handled" by setting
        /// <see cref="InputEvent.handled"/> or <see cref="InputEventPtr.handled"/> to true.
        /// </summary>
        /// <remarks>
        /// The default setting of this property is <see cref="InputEventHandledPolicy.SuppressStateUpdates"/> which
        /// implies that events are completely suppressed which means that associated state will not be updated.
        /// Hence, any state dependent classes such as <see cref="InputAction"/> or associated interactions will
        /// not be updated either. A side-effect of this setting is that succeeding events that are not suppressed
        /// may trigger new unexpected events since they may trigger state changes due to monitoring instances not
        /// seeing previous changes.
        ///
        /// The setting <see cref="InputEventHandledPolicy.SuppressActionEventNotifications"/> will instead allow state change
        /// propagation to happen, including updating interaction state, but will instead suppress any associated
        /// notifications.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">If attempting to set this property to an unsupported
        /// value.</exception>
        internal InputEventHandledPolicy inputEventHandledPolicy
        {
            get => m_InputEventHandledPolicy;
            set
            {
                switch (value)
                {
                    case InputEventHandledPolicy.SuppressActionEventNotifications:
                    case InputEventHandledPolicy.SuppressStateUpdates:
                        m_InputEventHandledPolicy = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Unsupported input event handling policy: {value}");
                }
            }
        }

        public event DeviceChangeListener onDeviceChange
        {
            add => m_DeviceChangeListeners.AddCallback(value);
            remove => m_DeviceChangeListeners.RemoveCallback(value);
        }

        public event DeviceStateChangeListener onDeviceStateChange
        {
            add => m_DeviceStateChangeListeners.AddCallback(value);
            remove => m_DeviceStateChangeListeners.RemoveCallback(value);
        }

        public event InputDeviceCommandDelegate onDeviceCommand
        {
            add => m_DeviceCommandCallbacks.AddCallback(value);
            remove => m_DeviceCommandCallbacks.RemoveCallback(value);
        }

        ////REVIEW: would be great to have a way to sort out precedence between two callbacks
        public event InputDeviceFindControlLayoutDelegate onFindControlLayoutForDevice
        {
            add
            {
                m_DeviceFindLayoutCallbacks.AddCallback(value);

                // Having a new callback on this event can change the set of devices we recognize.
                // See if there's anything in the list of available devices that we can now turn
                // into an InputDevice whereas we couldn't before.
                //
                // NOTE: A callback could also impact already existing devices and theoretically alter
                //       what layout we would have used for those. We do *NOT* retroactively apply
                //       those changes.
                AddAvailableDevicesThatAreNowRecognized();
            }
            remove => m_DeviceFindLayoutCallbacks.RemoveCallback(value);
        }

        public event LayoutChangeListener onLayoutChange
        {
            add => m_LayoutChangeListeners.AddCallback(value);
            remove => m_LayoutChangeListeners.RemoveCallback(value);
        }

        ////TODO: add InputEventBuffer struct that uses NativeArray underneath
        ////TODO: make InputEventTrace use NativeArray
        ////TODO: introduce an alternative that consumes events in bulk
        public event EventListener onEvent
        {
            add => m_EventListeners.AddCallback(value);
            remove => m_EventListeners.RemoveCallback(value);
        }

        public event UpdateListener onBeforeUpdate
        {
            add
            {
                InstallBeforeUpdateHookIfNecessary();
                m_BeforeUpdateListeners.AddCallback(value);
            }
            remove => m_BeforeUpdateListeners.RemoveCallback(value);
        }

        public event UpdateListener onAfterUpdate
        {
            add => m_AfterUpdateListeners.AddCallback(value);
            remove => m_AfterUpdateListeners.RemoveCallback(value);
        }

        public event Action onSettingsChange
        {
            add => m_SettingsChangedListeners.AddCallback(value);
            remove => m_SettingsChangedListeners.RemoveCallback(value);
        }

        public event Action onActionsChange
        {
            add => m_ActionsChangedListeners.AddCallback(value);
            remove => m_ActionsChangedListeners.RemoveCallback(value);
        }

        public bool isProcessingEvents => m_InputEventStream.isOpen;

#if UNITY_EDITOR
        /// <summary>
        /// Callback that can be used to display a warning and draw additional custom Editor UI for bindings.
        /// </summary>
        /// <seealso cref="InputSystem.customBindingPathValidators"/>
        /// <remarks>
        /// This is not intended to be called directly.
        /// Please use <see cref="InputSystem.customBindingPathValidators"/> instead.
        /// </remarks>
        internal event CustomBindingPathValidator customBindingPathValidators
        {
            add => m_customBindingPathValidators.AddCallback(value);
            remove => m_customBindingPathValidators.RemoveCallback(value);
        }

        /// <summary>
        /// Invokes any custom UI rendering code for this Binding Path in the editor.
        /// </summary>
        /// <seealso cref="InputSystem.customBindingPathValidators"/>
        /// <remarks>
        /// This is not intended to be called directly.
        /// Please use <see cref="InputSystem.OnDrawCustomWarningForBindingPath"/> instead.
        /// </remarks>
        internal void OnDrawCustomWarningForBindingPath(string bindingPath)
        {
            DelegateHelpers.InvokeCallbacksSafe_AndInvokeReturnedActions(
                ref m_customBindingPathValidators,
                bindingPath,
                "InputSystem.OnDrawCustomWarningForBindingPath");
        }

        /// <summary>
        /// Determines if any warning icon is to be displayed for this Binding Path in the editor.
        /// </summary>
        /// <seealso cref="InputSystem.customBindingPathValidators"/>
        /// <remarks>
        /// This is not intended to be called directly.
        /// Please use <see cref="InputSystem.OnDrawCustomWarningForBindingPath"/> instead.
        /// </remarks>
        internal bool ShouldDrawWarningIconForBinding(string bindingPath)
        {
            return DelegateHelpers.InvokeCallbacksSafe_AnyCallbackReturnsObject(
                ref m_customBindingPathValidators,
                bindingPath,
                "InputSystem.ShouldDrawWarningIconForBinding");
        }

#endif // UNITY_EDITOR

#if UNITY_EDITOR
        private bool m_RunPlayerUpdatesInEditMode;

        /// <summary>
        /// If true, consider the editor to be in "perpetual play mode". Meaning, we ignore editor
        /// updates and just go and continuously process Dynamic/Fixed/BeforeRender regardless of
        /// whether we're in play mode or not.
        ///
        /// In this mode, we also ignore game view focus.
        /// </summary>
        public bool runPlayerUpdatesInEditMode
        {
            get => m_RunPlayerUpdatesInEditMode;
            set => m_RunPlayerUpdatesInEditMode = value;
        }
#endif

        private bool gameIsPlaying =>
#if UNITY_EDITOR
            (m_Runtime.isInPlayMode && !UnityEditor.EditorApplication.isPaused) || m_RunPlayerUpdatesInEditMode;
#else
            true;
#endif

        private bool gameHasFocus =>
#if UNITY_EDITOR
                     m_RunPlayerUpdatesInEditMode || m_HasFocus || gameShouldGetInputRegardlessOfFocus;
#else
            m_HasFocus || gameShouldGetInputRegardlessOfFocus;
#endif

        private bool gameShouldGetInputRegardlessOfFocus =>
            m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus
#if UNITY_EDITOR
            && m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView
#endif
        ;

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
            var isReplacement = m_Layouts.HasLayout(internedName);

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
            var isReplacement = m_Layouts.HasLayout(internedLayoutName);
            if (isReplacement && isOverride)
            {   // Do not allow a layout override to replace a "base layout" by name, but allow layout overrides
                // to replace an existing layout override.
                // This is required to guarantee that its a hierarchy (directed graph) rather
                // than a cyclic graph.

                var isReplacingOverride = m_Layouts.layoutOverrideNames.Contains(internedLayoutName);
                if (!isReplacingOverride)
                {
                    throw new ArgumentException($"Failed to register layout override '{internedLayoutName}'" +
                        $"since a layout named '{internedLayoutName}' already exist. Layout overrides must " +
                        $"have unique names with respect to existing layouts.");
                }
            }

            m_Layouts.layoutStrings[internedLayoutName] = json;
            if (isOverride)
            {
                m_Layouts.layoutOverrideNames.Add(internedLayoutName);
                for (var i = 0; i < baseLayouts.length; ++i)
                {
                    var baseLayoutName = baseLayouts[i];
                    m_Layouts.layoutOverrides.TryGetValue(baseLayoutName, out var overrideList);
                    if (!isReplacement)
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
            var isReplacement = m_Layouts.HasLayout(internedLayoutName);

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

            // Nuke any precompiled layouts that are invalidated by the layout registration.
            m_Layouts.precompiledLayouts.Remove(layoutName);
            if (m_Layouts.precompiledLayouts.Count > 0)
            {
                foreach (var layout in m_Layouts.precompiledLayouts.Keys.ToArray())
                {
                    var metadata = m_Layouts.precompiledLayouts[layout].metadata;

                    // If it's an override, we remove any precompiled layouts to which overrides are applied.
                    if (isOverride)
                    {
                        for (var i = 0; i < baseLayouts.length; ++i)
                            if (layout == baseLayouts[i] ||
                                StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(metadata,
                                    baseLayouts[i], ';'))
                                m_Layouts.precompiledLayouts.Remove(layout);
                    }
                    else
                    {
                        // Otherwise, we remove any precompile layouts that use the layout we just changed.
                        if (StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(metadata,
                            layoutName, ';'))
                            m_Layouts.precompiledLayouts.Remove(layout);
                    }
                }
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
            DelegateHelpers.InvokeCallbacksSafe(ref m_LayoutChangeListeners, layoutName.ToString(), change, k_InputOnLayoutChangeMarker, "InputSystem.onLayoutChange");
        }

        public void RegisterPrecompiledLayout<TDevice>(string metadata)
            where TDevice : InputDevice, new()
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var deviceType = typeof(TDevice).BaseType;
            var layoutName = FindOrRegisterDeviceLayoutForType(deviceType);

            m_Layouts.precompiledLayouts[layoutName] = new InputControlLayout.Collection.PrecompiledLayout
            {
                factoryMethod = () => new TDevice(),
                metadata = metadata
            };
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
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.DisabledStateHasBeenQueriedFromRuntime;
                newDevice.m_DeviceFlags |= InputDevice.DeviceFlags.DisabledInFrontend;
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

        public void RemoveControlLayout(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

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
            ++m_LayoutRegistrationVersion;

            ////TODO: check all layout inheritance chain for whether they are based on the layout and if so
            ////      remove those layouts, too

            // Let listeners know.
            DelegateHelpers.InvokeCallbacksSafe(ref m_LayoutChangeListeners, name, InputControlLayoutChange.Removed, k_InputOnLayoutChangeMarker, "InputSystem.onLayoutChange");
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
            InternedString layoutName = new InternedString(string.Empty);
            try
            {
                k_InputTryFindMatchingControllerMarker.Begin();
                ////TODO: this will want to take overrides into account

                // See if we can match by description.
                layoutName = m_Layouts.TryFindMatchingLayout(deviceDescription);
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
                    m_DeviceFindLayoutCallbacks.LockForChanges();
                    for (var i = 0; i < m_DeviceFindLayoutCallbacks.length; ++i)
                    {
                        try
                        {
                            var newLayout = m_DeviceFindLayoutCallbacks[i](ref deviceDescription, layoutName, m_DeviceFindExecuteCommandDelegate);
                            if (!string.IsNullOrEmpty(newLayout) && !haveOverriddenLayoutName)
                            {
                                layoutName = new InternedString(newLayout);
                                haveOverriddenLayoutName = true;
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"{exception.GetType().Name} while executing 'InputSystem.onFindLayoutForDevice' callbacks");
                            Debug.LogException(exception);
                        }
                    }
                    m_DeviceFindLayoutCallbacks.UnlockForChanges();
                }
            }
            finally
            {
                k_InputTryFindMatchingControllerMarker.End();
            }
            return layoutName;
        }

        private InternedString FindOrRegisterDeviceLayoutForType(Type type)
        {
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
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, InputDeviceChange.UsageChanged, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");

            ////REVIEW: This was for the XRController leftHand and rightHand getters but these do lookups dynamically now; remove?
            // Usage may affect current device so update.
            device.MakeCurrent();
        }

        ////TODO: make sure that no device or control with a '/' in the name can creep into the system

        internal bool HasDevice(InputDevice device)
        {
            return device.m_DeviceIndex < m_DevicesCount && ReferenceEquals(m_Devices[device.m_DeviceIndex], device);
        }

        public InputDevice AddDevice(Type type, string name = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Find the layout name that the given type was registered with.
            var layoutName = FindOrRegisterDeviceLayoutForType(type);
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
            InitializeDeviceState(device);

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

            // If we're running in the background, find out whether the device can run in
            // the background. If not, disable it.
            var isPlaying = true;
            #if UNITY_EDITOR
            isPlaying = m_Runtime.isInPlayMode;
            #endif
            if (isPlaying && !gameHasFocus
                && m_Settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus
                && m_Runtime.runInBackground
                && device.QueryEnabledStateFromRuntime()
                && !ShouldRunDeviceInBackground(device))
            {
                EnableOrDisableDevice(device, false, DeviceDisableScope.TemporaryWhilePlayerIsInBackground);
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

            // If the device has event merger, make a note of it.
            if (device is IEventMerger)
                device.hasEventMerger = true;

            // If the device has event preprocessor, make a note of it.
            if (device is IEventPreProcessor)
                device.hasEventPreProcessor = true;

            // If the device wants before-render updates, enable them if they
            // aren't already.
            if (device.updateBeforeRender)
                updateMask |= InputUpdateType.BeforeRender;

            // Notify device.
            device.NotifyAdded();

            ////REVIEW: is this really a good thing to do? just plugging in a device shouldn't make
            ////        it current, no?
            // Make the device current.
            // BEWARE: if this will not happen for whatever reason, you will break Android sensors,
            // as they rely on .current for enabling native backend, see https://fogbugz.unity3d.com/f/cases/1371204/
            device.MakeCurrent();

            // Notify listeners.
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, InputDeviceChange.Added, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");

            // Request device to send us an initial state update.
            if (device.enabled)
                device.RequestSync();

            device.SetOptimizedControlDataTypeRecursively();
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
            k_InputAddDeviceMarker.Begin();
            // Look for matching layout.
            var layout = TryFindMatchingControlLayout(ref description, deviceId);

            // If no layout was found, bail out.
            if (layout.IsEmpty())
            {
                if (throwIfNoLayoutFound)
                {
                    k_InputAddDeviceMarker.End();
                    throw new ArgumentException($"Cannot find layout matching device description '{description}'", nameof(description));
                }

                // If it's a device coming from the runtime, disable it.
                if (deviceId != InputDevice.InvalidDeviceId)
                {
                    var command = DisableDeviceCommand.Create();
                    m_Runtime.DeviceCommand(deviceId, ref command);
                }

                k_InputAddDeviceMarker.End();
                return null;
            }

            var device = AddDevice(layout, deviceId, deviceName, description, deviceFlags);
            device.m_Description = description;
            k_InputAddDeviceMarker.End();
            return device;
        }

        public InputDevice AddDevice(InputDeviceDescription description, InternedString layout, string deviceName = null,
            int deviceId = InputDevice.InvalidDeviceId, InputDevice.DeviceFlags deviceFlags = 0)
        {
            try
            {
                k_InputAddDeviceMarker.Begin();
                var device = AddDevice(layout, deviceId, deviceName, description, deviceFlags);
                device.m_Description = description;
                return device;
            }
            finally
            {
                k_InputAddDeviceMarker.End();
            }
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

            ////TODO: When we remove a native device like this, make sure we tell the backend to disable it (and re-enable it when re-add it)

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
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, InputDeviceChange.Removed, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");

            // Try setting next device of same type as current
            InputSystem.GetDevice(device.GetType())?.MakeCurrent();
        }

        public void FlushDisconnectedDevices()
        {
            m_DisconnectedDevices.Clear(m_DisconnectedDevicesCount);
            m_DisconnectedDevicesCount = 0;
        }

        public unsafe void ResetDevice(InputDevice device, bool alsoResetDontResetControls = false, bool? issueResetCommand = null)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (!device.added)
                throw new InvalidOperationException($"Device '{device}' has not been added to the system");

            var isHardReset = alsoResetDontResetControls || !device.hasDontResetControls;

            // Trigger reset notification.
            var change = isHardReset ? InputDeviceChange.HardReset : InputDeviceChange.SoftReset;
            InputActionState.OnDeviceChange(device, change);
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, change, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");

            // If the device implements its own reset, let it handle it.
            if (!alsoResetDontResetControls && device is ICustomDeviceReset customReset)
            {
                customReset.Reset();
            }
            else
            {
                var defaultStatePtr = device.defaultStatePtr;
                var deviceStateBlockSize = device.stateBlock.alignedSizeInBytes;

                // Allocate temp memory to hold one state event.
                ////REVIEW: the need for an event here is sufficiently obscure to warrant scrutiny; likely, there's a better way
                ////        to tell synthetic input (or input sources in general) apart
                // NOTE: We wrap the reset in an artificial state event so that it appears to the rest of the system
                //       like any other input. If we don't do that but rather just call UpdateState() with a null event
                //       pointer, the change will be considered an internal state change and will get ignored by some
                //       pieces of code (such as EnhancedTouch which filters out internal state changes of Touchscreen
                //       by ignoring any change that is not coming from an input event).
                using (var tempBuffer =
                           new NativeArray<byte>(InputEvent.kBaseEventSize + sizeof(int) + (int)deviceStateBlockSize, Allocator.Temp))
                {
                    var stateEventPtr = (StateEvent*)tempBuffer.GetUnsafePtr();
                    var statePtr = stateEventPtr->state;
                    var currentTime = m_Runtime.currentTime;

                    // Set up the state event.
                    ref var stateBlock = ref device.m_StateBlock;
                    stateEventPtr->baseEvent.type = StateEvent.Type;
                    stateEventPtr->baseEvent.sizeInBytes = InputEvent.kBaseEventSize + sizeof(int) + deviceStateBlockSize;
                    stateEventPtr->baseEvent.time = currentTime;
                    stateEventPtr->baseEvent.deviceId = device.deviceId;
                    stateEventPtr->baseEvent.eventId = -1;
                    stateEventPtr->stateFormat = device.m_StateBlock.format;

                    // Decide whether we perform a soft reset or a hard reset.
                    if (isHardReset)
                    {
                        // Perform a hard reset where we wipe the entire device and set a full
                        // reset request to the backend.
                        UnsafeUtility.MemCpy(statePtr,
                            (byte*)defaultStatePtr + stateBlock.byteOffset,
                            deviceStateBlockSize);
                    }
                    else
                    {
                        // Perform a soft reset where we exclude any dontReset control (which is automatically
                        // toggled on for noisy controls) and do *NOT* send a reset request to the backend.

                        var currentStatePtr = device.currentStatePtr;
                        var resetMaskPtr = m_StateBuffers.resetMaskBuffer;

                        // To preserve values from dontReset controls, we need to first copy their current values.
                        UnsafeUtility.MemCpy(statePtr,
                            (byte*)currentStatePtr + stateBlock.byteOffset,
                            deviceStateBlockSize);

                        // And then we copy over default values masked by dontReset bits.
                        MemoryHelpers.MemCpyMasked(statePtr,
                            (byte*)defaultStatePtr + stateBlock.byteOffset,
                            (int)deviceStateBlockSize,
                            (byte*)resetMaskPtr + stateBlock.byteOffset);
                    }

                    UpdateState(device, defaultUpdateType, statePtr, 0, deviceStateBlockSize, currentTime,
                        new InputEventPtr((InputEvent*)stateEventPtr));
                }
            }

            // In the editor, we don't want to issue RequestResetCommand to devices based on focus of the game view
            // as this would also reset device state for the editor. And we don't need the reset commands in this case
            // as -- unlike in the player --, Unity keeps running and we will keep seeing OS messages for these devices.
            // So, in the editor, we generally suppress reset commands.
            //
            // The only exception is when the editor itself loses focus. We issue sync requests to all devices when
            // coming back into focus. But for any device that doesn't support syncs, we actually do want to have a
            // reset command reach the background.
            //
            // Finally, in the player, we also avoid reset commands when disabling a device as these are pointless.
            // We sync/reset when enabling a device in the backend.
            var doIssueResetCommand = isHardReset;
            if (issueResetCommand != null)
                doIssueResetCommand = issueResetCommand.Value;
            #if UNITY_EDITOR
            else if (m_Settings.editorInputBehaviorInPlayMode != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                doIssueResetCommand = false;
            #endif

            if (doIssueResetCommand)
                device.RequestReset();
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

        // We have three different levels of disabling a device.
        internal enum DeviceDisableScope
        {
            Everywhere, // Device is disabled globally and explicitly. Should neither send nor receive events.
            InFrontendOnly, // Device is only disabled on managed side but not in backend. Should keep sending events but should not receive them (useful for redirecting their data).
            TemporaryWhilePlayerIsInBackground, // Device has been disabled automatically and temporarily by system while application is running in the background.
        }

        public void EnableOrDisableDevice(InputDevice device, bool enable, DeviceDisableScope scope = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // Synchronize the enable/disabled state of the device.
            if (enable)
            {
                ////REVIEW: Do we really want to allow overriding disabledWhileInBackground like it currently does?

                // Enable device.
                switch (scope)
                {
                    case DeviceDisableScope.Everywhere:
                        device.disabledWhileInBackground = false;
                        if (!device.disabledInFrontend && !device.disabledInRuntime)
                            return;
                        if (device.disabledInRuntime)
                        {
                            device.ExecuteEnableCommand();
                            device.disabledInRuntime = false;
                        }
                        if (device.disabledInFrontend)
                        {
                            if (!device.RequestSync())
                                ResetDevice(device);
                            device.disabledInFrontend = false;
                        }
                        break;

                    case DeviceDisableScope.InFrontendOnly:
                        device.disabledWhileInBackground = false;
                        if (!device.disabledInFrontend && device.disabledInRuntime)
                            return;
                        if (!device.disabledInRuntime)
                        {
                            device.ExecuteDisableCommand();
                            device.disabledInRuntime = true;
                        }
                        if (device.disabledInFrontend)
                        {
                            if (!device.RequestSync())
                                ResetDevice(device);
                            device.disabledInFrontend = false;
                        }
                        break;

                    case DeviceDisableScope.TemporaryWhilePlayerIsInBackground:
                        if (device.disabledWhileInBackground)
                        {
                            if (device.disabledInRuntime)
                            {
                                device.ExecuteEnableCommand();
                                device.disabledInRuntime = false;
                            }
                            if (!device.RequestSync())
                                ResetDevice(device);
                            device.disabledWhileInBackground = false;
                        }
                        break;
                }
            }
            else
            {
                // Disable device.
                switch (scope)
                {
                    case DeviceDisableScope.Everywhere:
                        device.disabledWhileInBackground = false;
                        if (device.disabledInFrontend && device.disabledInRuntime)
                            return;
                        if (!device.disabledInRuntime)
                        {
                            device.ExecuteDisableCommand();
                            device.disabledInRuntime = true;
                        }
                        if (!device.disabledInFrontend)
                        {
                            // When disabling a device, also issuing a reset in the backend is pointless.
                            ResetDevice(device, issueResetCommand: false);
                            device.disabledInFrontend = true;
                        }
                        break;

                    case DeviceDisableScope.InFrontendOnly:
                        device.disabledWhileInBackground = false;
                        if (!device.disabledInRuntime && device.disabledInFrontend)
                            return;
                        if (device.disabledInRuntime)
                        {
                            device.ExecuteEnableCommand();
                            device.disabledInRuntime = false;
                        }
                        if (!device.disabledInFrontend)
                        {
                            // When disabling a device, also issuing a reset in the backend is pointless.
                            ResetDevice(device, issueResetCommand: false);
                            device.disabledInFrontend = true;
                        }
                        break;

                    case DeviceDisableScope.TemporaryWhilePlayerIsInBackground:
                        // Won't flag a device as DisabledWhileInBackground if it is explicitly disabled in
                        // the frontend.
                        if (device.disabledInFrontend || device.disabledWhileInBackground)
                            return;
                        device.disabledWhileInBackground = true;
                        ResetDevice(device, issueResetCommand: false);
                        #if UNITY_EDITOR
                        if (m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                        #endif
                        {
                            device.ExecuteDisableCommand();
                            device.disabledInRuntime = true;
                        }
                        break;
                }
            }

            // Let listeners know.
            var deviceChange = enable ? InputDeviceChange.Enabled : InputDeviceChange.Disabled;
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, deviceChange, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");
        }

        private unsafe void QueueEvent(InputEvent* eventPtr)
        {
            // If we're currently in OnUpdate(), the m_InputEventStream will be open. In that case,
            // append events directly to that buffer and do *NOT* go into native.
            if (m_InputEventStream.isOpen)
            {
                m_InputEventStream.Write(eventPtr);
                return;
            }

            // Don't bother keeping the data on the managed side. Just stuff the raw data directly
            // into the native buffers. This also means this method is thread-safe.
            m_Runtime.QueueEvent(eventPtr);
        }

        public unsafe void QueueEvent(InputEventPtr ptr)
        {
            QueueEvent(ptr.data);
        }

        public unsafe void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            QueueEvent((InputEvent*)UnsafeUtility.AddressOf(ref inputEvent));
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

            InitializeActions();
            InitializeData();
            InstallRuntime(runtime);
            InstallGlobals();

            ApplySettings();
            ApplyActions();
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

            // Project-wide Actions are never temporary so we do not destroy them.
        }

        // Initialize project-wide actions:
        // - In editor (edit mode or play-mode) we always use the editor build preferences persisted setting.
        // - In player build we always attempt to find a preloaded asset.
        private void InitializeActions()
        {
#if UNITY_EDITOR
            m_Actions = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
#else
            m_Actions = null;
            var candidates = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            foreach (var candidate in candidates)
            {
                if (candidate.m_IsProjectWide)
                {
                    m_Actions = candidate;
                    break;
                }
            }
#endif // UNITY_EDITOR
        }

        internal void InitializeData()
        {
            m_Layouts.Allocate();
            m_Processors.Initialize(this);
            m_Interactions.Initialize(this);
            m_Composites.Initialize(this);
            m_DevicesById = new Dictionary<int, InputDevice>();

            // Determine our default set of enabled update types. By
            // default we enable both fixed and dynamic update because
            // we don't know which one the user is going to use. The user
            // can manually turn off one of them to optimize operation.
            m_UpdateMask = InputUpdateType.Dynamic | InputUpdateType.Fixed;
            m_HasFocus = Application.isFocused;
#if UNITY_EDITOR
            m_EditorIsActive = true;
            m_UpdateMask |= InputUpdateType.Editor;
#endif

            m_ScrollDeltaBehavior = InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms;

            #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
            // Default polling frequency is 60 Hz.
            m_PollingFrequency = 60;
            #endif

            // Default input event handled policy.
            m_InputEventHandledPolicy = InputEventHandledPolicy.SuppressStateUpdates;

            // Register layouts.
            // NOTE: Base layouts must be registered before their derived layouts
            //       for the detection of base layouts to work.
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
            RegisterControlLayout("Delta", typeof(DeltaControl));
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

            // Precompiled layouts.
            RegisterPrecompiledLayout<FastKeyboard>(FastKeyboard.metadata);
            RegisterPrecompiledLayout<FastTouchscreen>(FastTouchscreen.metadata);
            RegisterPrecompiledLayout<FastMouse>(FastMouse.metadata);

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
            composites.AddTypeRegistration("3DVector", typeof(Vector3Composite));
            composites.AddTypeRegistration("Axis", typeof(AxisComposite));// Alias for pre-0.2 name.
            composites.AddTypeRegistration("Dpad", typeof(Vector2Composite));// Alias for pre-0.2 name.
            composites.AddTypeRegistration("ButtonWithOneModifier", typeof(ButtonWithOneModifier));
            composites.AddTypeRegistration("ButtonWithTwoModifiers", typeof(ButtonWithTwoModifiers));
            composites.AddTypeRegistration("OneModifier", typeof(OneModifierComposite));
            composites.AddTypeRegistration("TwoModifiers", typeof(TwoModifiersComposite));

            // ISXB-1766: Defer loading custom types by reflection unless we have to since referenced from
            // .inputaction JSON assets. This is managed via TypeTable.cs.
        }

        static void RegisterCustomTypes(Type[] types)
        {
            foreach (Type type in types)
            {
                if (!type.IsClass
                    || type.IsAbstract
                    || type.IsGenericType)
                    continue;
                if (typeof(InputProcessor).IsAssignableFrom(type))
                {
                    InputSystem.RegisterProcessor(type);
                }
                else if (typeof(IInputInteraction).IsAssignableFrom(type))
                {
                    InputSystem.RegisterInteraction(type);
                }
                else if (typeof(InputBindingComposite).IsAssignableFrom(type))
                {
                    InputSystem.RegisterBindingComposite(type, null);
                }
            }
        }

        private bool m_CustomTypesRegistered;

        internal bool RegisterCustomTypes()
        {
            // If we have already attempted to register custom types, there is no need to reattempt since we
            // would end up with the same result again. Only with a domain reload would the resulting types
            // be different, and hence it is sufficient to use a static flag that we do not reset.
            if (m_CustomTypesRegistered)
                return false; // Already evaluated

            m_CustomTypesRegistered = true;

            k_InputRegisterCustomTypesMarker.Begin();

            var inputSystemAssembly = typeof(InputProcessor).Assembly;
            var inputSystemName = inputSystemAssembly.GetName().Name;
#if UNITY_6000_5_OR_NEWER
            var assemblies = CurrentAssemblies.GetLoadedAssemblies();
#else
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif
            foreach (var assembly in assemblies)
            {
                try
                {
                    // exclude InputSystem assembly which should be loaded first
                    if (assembly == inputSystemAssembly) continue;

                    // Only register types from assemblies that reference InputSystem
                    foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                    {
                        if (referencedAssembly.Name == inputSystemName)
                        {
                            RegisterCustomTypes(assembly.GetTypes());
                            break;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Ignore exception
                }
            }

            k_InputRegisterCustomTypesMarker.End();

            return true; // Signal that custom types were extracted and registered.
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
                #if UNITY_EDITOR
                m_Runtime.onPlayerLoopInitialization = null;
                #endif
            }

            m_Runtime = runtime;
            m_Runtime.onUpdate = OnUpdate;
            m_Runtime.onDeviceDiscovered = OnNativeDeviceDiscovered;
            m_Runtime.onPlayerFocusChanged = OnFocusChanged;
            m_Runtime.onShouldRunUpdate = ShouldRunUpdate;
            #if UNITY_EDITOR
            m_Runtime.onPlayerLoopInitialization = OnPlayerLoopInitialization;
            #endif
            m_Runtime.pollingFrequency = pollingFrequency;
            m_HasFocus = m_Runtime.isPlayerFocused;

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
                InputStateBuffers.s_ResetMaskBuffer = m_StateBuffers.resetMaskBuffer;
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

            // Invalidate type registrations
            m_CustomTypesRegistered = false;

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
        #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
        private float m_PollingFrequency;
        #endif
        private InputEventHandledPolicy m_InputEventHandledPolicy;

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

        internal InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        private InputUpdateType m_CurrentUpdate;
        internal InputStateBuffers m_StateBuffers;

        private InputSettings.ScrollDeltaBehavior m_ScrollDeltaBehavior;

        #if UNITY_EDITOR
        // remember time offset to correctly restore it after editor mode is done
        private double latestNonEditorTimeOffsetToRealtimeSinceStartup;
        #endif

        // We don't use UnityEvents and thus don't persist the callbacks during domain reloads.
        // Restoration of UnityActions is unreliable and it's too easy to end up with double
        // registrations what will lead to all kinds of misbehavior.
        private CallbackArray<DeviceChangeListener> m_DeviceChangeListeners;
        private CallbackArray<DeviceStateChangeListener> m_DeviceStateChangeListeners;
        private CallbackArray<InputDeviceFindControlLayoutDelegate> m_DeviceFindLayoutCallbacks;
        internal CallbackArray<InputDeviceCommandDelegate> m_DeviceCommandCallbacks;
        private CallbackArray<LayoutChangeListener> m_LayoutChangeListeners;
        private CallbackArray<EventListener> m_EventListeners;
        private CallbackArray<UpdateListener> m_BeforeUpdateListeners;
        private CallbackArray<UpdateListener> m_AfterUpdateListeners;
        private CallbackArray<Action> m_SettingsChangedListeners;
        private CallbackArray<Action> m_ActionsChangedListeners;
        private bool m_NativeBeforeUpdateHooked;
        private bool m_HaveDevicesWithStateCallbackReceivers;
        private bool m_HasFocus;
        private bool m_DiscardOutOfFocusEvents;
        private double m_FocusRegainedTime;
        private InputEventStream m_InputEventStream;

        // We want to sync devices when the editor comes back into focus. Unfortunately, there's no
        // callback for this so we have to poll this state.
        #if UNITY_EDITOR
        private bool m_EditorIsActive;
        #endif

        // Allow external users to hook in validators and draw custom UI in the binding path editor
        #if UNITY_EDITOR
        private Utilities.CallbackArray<CustomBindingPathValidator> m_customBindingPathValidators;
        #endif

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

        // Extract as booleans (from m_Settings) because feature check is in the hot path

        private bool m_OptimizedControlsFeatureEnabled;
        internal bool optimizedControlsFeatureEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_OptimizedControlsFeatureEnabled;
            set => m_OptimizedControlsFeatureEnabled = value;
        }

        private bool m_ReadValueCachingFeatureEnabled;
        internal bool readValueCachingFeatureEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ReadValueCachingFeatureEnabled;
            set => m_ReadValueCachingFeatureEnabled = value;
        }

        private bool m_ParanoidReadValueCachingChecksEnabled;
        internal bool paranoidReadValueCachingChecksEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ParanoidReadValueCachingChecksEnabled;
            set => m_ParanoidReadValueCachingChecksEnabled = value;
        }

        private InputActionAsset m_Actions;

        #if UNITY_EDITOR
        internal IInputDiagnostics m_Diagnostics;
        #endif

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
            InputStateBuffers.s_ResetMaskBuffer = newBuffers.resetMaskBuffer;

            // Switch to buffers.
            InputStateBuffers.SwitchTo(m_StateBuffers,
                InputUpdate.s_LatestUpdateType != InputUpdateType.None ? InputUpdate.s_LatestUpdateType : defaultUpdateType);

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

        private unsafe void InitializeDeviceState(InputDevice device)
        {
            Debug.Assert(device != null, "Device must not be null");
            Debug.Assert(device.added, "Device must have been added");
            Debug.Assert(device.stateBlock.byteOffset != InputStateBlock.InvalidOffset, "Device state block offset is invalid");
            Debug.Assert(device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes <= m_StateBuffers.sizePerBuffer,
                "Device state block is not contained in state buffer");

            var controls = device.allControls;
            var controlCount = controls.Count;
            var resetMaskBuffer = m_StateBuffers.resetMaskBuffer;

            var haveControlsWithDefaultState = device.hasControlsWithDefaultState;

            // Assume that everything in the device is noise. This way we also catch memory regions
            // that are not actually covered by a control and implicitly mark them as noise (e.g. the
            // report ID in HID input reports).
            //
            // NOTE: Noise is indicated by *unset* bits so we don't have to do anything here to start
            //       with all-noise as we expect noise mask memory to be cleared on allocation.
            var noiseMaskBuffer = m_StateBuffers.noiseMaskBuffer;

            // We first toggle all bits *on* and then toggle bits for noisy and dontReset controls *off* individually.
            // We do this instead of just leaving all bits *off* and then going through controls that aren't noisy/dontReset *on*.
            // If we did the latter, we'd have the problem that a parent control such as TouchControl would toggle on bits for
            // the entirety of its state block and thus cover the state of all its child controls.
            MemoryHelpers.SetBitsInBuffer(noiseMaskBuffer, (int)device.stateBlock.byteOffset, 0, (int)device.stateBlock.sizeInBits, false);
            MemoryHelpers.SetBitsInBuffer(resetMaskBuffer, (int)device.stateBlock.byteOffset, 0, (int)device.stateBlock.sizeInBits, true);

            // Go through controls.
            var defaultStateBuffer = m_StateBuffers.defaultStateBuffer;
            for (var n = 0; n < controlCount; ++n)
            {
                var control = controls[n];

                // Don't allow controls that hijack state from other controls to set independent noise or dontReset flags.
                if (control.usesStateFromOtherControl)
                    continue;

                if (!control.noisy || control.dontReset)
                {
                    ref var stateBlock = ref control.m_StateBlock;

                    Debug.Assert(stateBlock.byteOffset != InputStateBlock.InvalidOffset, "Byte offset is invalid on control's state block");
                    Debug.Assert(stateBlock.bitOffset != InputStateBlock.InvalidOffset, "Bit offset is invalid on control's state block");
                    Debug.Assert(stateBlock.sizeInBits != InputStateBlock.InvalidOffset, "Size is invalid on control's state block");
                    Debug.Assert(stateBlock.byteOffset >= device.stateBlock.byteOffset, "Control's offset is located below device's offset");
                    Debug.Assert(stateBlock.byteOffset + stateBlock.alignedSizeInBytes <=
                        device.stateBlock.byteOffset + device.stateBlock.alignedSizeInBytes, "Control state block lies outside of state buffer");

                    // If control isn't noisy, toggle its bits *on* in the noise mask.
                    if (!control.noisy)
                        MemoryHelpers.SetBitsInBuffer(noiseMaskBuffer, (int)stateBlock.byteOffset, (int)stateBlock.bitOffset,
                            (int)stateBlock.sizeInBits, true);

                    // If control shouldn't be reset, toggle its bits *off* in the reset mask.
                    if (control.dontReset)
                        MemoryHelpers.SetBitsInBuffer(resetMaskBuffer, (int)stateBlock.byteOffset, (int)stateBlock.bitOffset,
                            (int)stateBlock.sizeInBits, false);
                }

                // If control has default state, write it into to the device's default state.
                if (haveControlsWithDefaultState && control.hasDefaultState)
                    control.m_StateBlock.Write(defaultStateBuffer, control.m_DefaultState);
            }

            // Copy default state to all front and back buffers.
            if (haveControlsWithDefaultState)
            {
                ref var deviceStateBlock = ref device.m_StateBlock;
                var deviceIndex = device.m_DeviceIndex;
                if (m_StateBuffers.m_PlayerStateBuffers.valid)
                {
                    deviceStateBlock.CopyToFrom(m_StateBuffers.m_PlayerStateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                    deviceStateBlock.CopyToFrom(m_StateBuffers.m_PlayerStateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
                }

                #if UNITY_EDITOR
                if (m_StateBuffers.m_EditorStateBuffers.valid)
                {
                    deviceStateBlock.CopyToFrom(m_StateBuffers.m_EditorStateBuffers.GetFrontBuffer(deviceIndex), defaultStateBuffer);
                    deviceStateBlock.CopyToFrom(m_StateBuffers.m_EditorStateBuffers.GetBackBuffer(deviceIndex), defaultStateBuffer);
                }
                #endif
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
                    device.m_DeviceFlags |= InputDevice.DeviceFlags.Native;
                    device.m_DeviceFlags &= ~InputDevice.DeviceFlags.DisabledInFrontend;
                    device.m_DeviceFlags &= ~InputDevice.DeviceFlags.DisabledWhileInBackground;
                    device.m_DeviceFlags &= ~InputDevice.DeviceFlags.DisabledStateHasBeenQueriedFromRuntime;

                    AddDevice(device);

                    DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners, device, InputDeviceChange.Reconnected,
                        k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");
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

        private JsonParser.JsonString MakeEscapedJsonString(string theString)
        {
            //
            // When we create the device description from the (passed from native) deviceDescriptor string in OnNativeDeviceDiscovered()
            // we remove any escape characters from the capabilties field when we do InputDeviceDescription.FromJson() - this decoded
            // description is used to create the device.
            //
            // This means that the native and managed code can have slightly different representations of the capabilities field.
            //
            // Managed: description.capabilities    string, unescaped
            //                                      eg "{"deviceName":"Oculus Quest", ..."
            //
            // Native:  deviceDescriptor            string, containing a Json encoded "capabilities" name/value pair represented by an escaped Json string
            //                                      eg "{\"deviceName\":\"Oculus Quest\", ..."
            //
            // To avoid a very costly escape-skipping character-by-character string comparison in JsonParser.Json.Equals() we
            // reconstruct an escaped string and make an escaped JsonParser.JsonString and use that for the comparison instead.
            //
            if (string.IsNullOrEmpty(theString))
            {
                return new JsonParser.JsonString
                {
                    text = string.Empty,    // text should be an empty string and not null for consistency on property comparisons
                    hasEscapes = false
                };
            }

            var builder = new StringBuilder();
            var length = theString.Length;
            var hasEscapes = false;
            for (var j = 0; j < length; ++j)
            {
                var ch = theString[j];
                if (ch == '\\' || ch == '\"')
                {
                    builder.Append('\\');
                    hasEscapes = true;
                }
                builder.Append(ch);
            }
            var jsonStringWithEscapes = new JsonParser.JsonString
            {
                text = builder.ToString(),
                hasEscapes = hasEscapes
            };
            return jsonStringWithEscapes;
        }

        private InputDevice TryMatchDisconnectedDevice(string deviceDescriptor)
        {
            for (var i = 0; i < m_DisconnectedDevicesCount; ++i)
            {
                var device = m_DisconnectedDevices[i];
                var description = device.description;

                // We don't parse the full description but rather go property by property in order to not
                // allocate GC memory if we can avoid it.

                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("interface", description.interfaceName, deviceDescriptor))
                    continue;
                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("product", description.product, deviceDescriptor))
                    continue;
                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("manufacturer", description.manufacturer, deviceDescriptor))
                    continue;
                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("type", description.deviceClass, deviceDescriptor))
                    continue;
                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("capabilities", MakeEscapedJsonString(description.capabilities), deviceDescriptor))
                    continue;
                if (!InputDeviceDescription.ComparePropertyToDeviceDescriptor("serial", description.serial, deviceDescriptor))
                    continue;

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

#if UNITY_EDITOR
        private void SyncAllDevicesWhenEditorIsActivated()
        {
            var isActive = m_Runtime.isEditorActive;
            if (isActive == m_EditorIsActive)
                return;

            m_EditorIsActive = isActive;
            if (m_EditorIsActive)
                SyncAllDevices();
        }

        private void SyncAllDevices()
        {
            for (var i = 0; i < m_DevicesCount; ++i)
            {
                // When the editor comes back into focus, we actually do want resets to happen
                // for devices that don't support syncs as they will likely have missed input while
                // we were in the background.
                if (!m_Devices[i].RequestSync())
                    ResetDevice(m_Devices[i], issueResetCommand: true);
            }
        }

        internal void SyncAllDevicesAfterEnteringPlayMode()
        {
            // Because we ignore all events between exiting edit mode and entering play mode,
            // that includes any potential device resets/syncs/etc,
            // we need to resync all devices after we're in play mode proper.
            ////TODO: this is a hacky workaround, implement a proper solution where events from sync/resets are not ignored.
            SyncAllDevices();
        }

#endif

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

            InputUpdate.OnBeforeUpdate(updateType);

            // For devices that have state callbacks, tell them we're carrying state over
            // into the next frame.
            if (m_HaveDevicesWithStateCallbackReceivers && updateType != InputUpdateType.BeforeRender) ////REVIEW: before-render handling is probably wrong
            {
                for (var i = 0; i < m_DevicesCount; ++i)
                {
                    var device = m_Devices[i];
                    if (!device.hasStateCallbacks)
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

            DelegateHelpers.InvokeCallbacksSafe(ref m_BeforeUpdateListeners, k_InputOnBeforeUpdateMarker, "InputSystem.onBeforeUpdate");
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

            scrollDeltaBehavior = m_Settings.scrollDeltaBehavior;

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

            // Apply feature flags.
            if (m_Settings.m_FeatureFlags != null)
            {
                #if UNITY_EDITOR
                runPlayerUpdatesInEditMode = m_Settings.IsFeatureEnabled(InputFeatureNames.kRunPlayerUpdatesInEditMode);
                #endif

                // Extract feature flags into fields since used in hot-path
                m_ReadValueCachingFeatureEnabled = m_Settings.IsFeatureEnabled((InputFeatureNames.kUseReadValueCaching));
                m_OptimizedControlsFeatureEnabled = m_Settings.IsFeatureEnabled((InputFeatureNames.kUseOptimizedControls));
                m_ParanoidReadValueCachingChecksEnabled = m_Settings.IsFeatureEnabled((InputFeatureNames.kParanoidReadValueCachingChecks));
            }

            // Cache some values.
            Touchscreen.s_TapTime = settings.defaultTapTime;
            Touchscreen.s_TapDelayTime = settings.multiTapDelayTime;
            Touchscreen.s_TapRadiusSquared = settings.tapRadius * settings.tapRadius;
            // Extra clamp here as we can't tell what we're getting from serialized data.
            ButtonControl.s_GlobalDefaultButtonPressPoint = Mathf.Clamp(settings.defaultButtonPressPoint, ButtonControl.kMinButtonPressPoint, float.MaxValue);
            ButtonControl.s_GlobalDefaultButtonReleaseThreshold = settings.buttonReleaseThreshold;

            // Update devices control optimization
            foreach (var device in devices)
                device.SetOptimizedControlDataTypeRecursively();

            // Invalidate control caches due to potential changes to processors or value readers
            foreach (var device in devices)
                device.MarkAsStaleRecursively();

            // Let listeners know.
            DelegateHelpers.InvokeCallbacksSafe(ref m_SettingsChangedListeners,
                k_InputOnSettingsChangeMarker, "InputSystem.onSettingsChange");
        }

        internal void ApplyActions()
        {
            // Let listeners know.
            DelegateHelpers.InvokeCallbacksSafe(ref m_ActionsChangedListeners, k_InputOnActionsChangeMarker, "InputSystem.onActionsChange");
        }

        internal unsafe long ExecuteGlobalCommand<TCommand>(ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            var ptr = (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command);
            // device id is irrelevant as we route it based on fourcc internally
            return InputRuntime.s_Instance.DeviceCommand(0, ptr);
        }

        internal void AddAvailableDevicesThatAreNowRecognized()
        {
            for (var i = 0; i < m_AvailableDeviceCount; ++i)
            {
                var id = m_AvailableDevices[i].deviceId;
                if (TryGetDeviceById(id) != null)
                    continue;

                var layout = TryFindMatchingControlLayout(ref m_AvailableDevices[i].description, id);
                if (!IsDeviceLayoutMarkedAsSupportedInSettings(layout)) continue;

                if (layout.IsEmpty())
                {
                    // If it's a device coming from the runtime, disable it.
                    if (id != InputDevice.InvalidDeviceId)
                    {
                        var command = DisableDeviceCommand.Create();
                        m_Runtime.DeviceCommand(id, ref command);
                    }

                    continue;
                }

                try
                {
                    AddDevice(m_AvailableDevices[i].description, layout, deviceId: id,
                        deviceFlags: m_AvailableDevices[i].isNative ? InputDevice.DeviceFlags.Native : 0);
                }
                catch (Exception)
                {
                    // the user might have changed the layout of one device, but others in the system might still have
                    // layouts we can't make sense of. Just quietly swallow exceptions from those so as not to spam
                    // the user with information about devices unrelated to what was actually changed.
                }
            }
        }

        private bool ShouldRunDeviceInBackground(InputDevice device)
        {
            return m_Settings.backgroundBehavior != InputSettings.BackgroundBehavior.ResetAndDisableAllDevices &&
                device.canRunInBackground;
        }

        internal void OnFocusChanged(bool focus)
        {
            #if UNITY_EDITOR
            SyncAllDevicesWhenEditorIsActivated();

            if (!m_Runtime.isInPlayMode)
            {
                m_HasFocus = focus;
                return;
            }

            var gameViewFocus = m_Settings.editorInputBehaviorInPlayMode;
            #endif

            var runInBackground =
                #if UNITY_EDITOR
                // In the editor, the player loop will always be run even if the Game View does not have focus. This
                // amounts to runInBackground being always true in the editor, regardless of what the setting in
                // the Player Settings window is.
                //
                // If, however, "Game View Focus" is set to "Exactly As In Player", we force code here down the same
                // path as in the player.
                gameViewFocus != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView || m_Runtime.runInBackground;
                #else
                m_Runtime.runInBackground;
                #endif

            var backgroundBehavior = m_Settings.backgroundBehavior;
            if (backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus && runInBackground)
            {
                // If runInBackground is true, no device changes should happen, even when focus is gained. So early out.
                // If runInBackground is false, we still want to sync devices when focus is gained. So we need to continue further.
                m_HasFocus = focus;
                return;
            }

            #if UNITY_EDITOR
            // Set the current update type while we process the focus changes to make sure we
            // feed into the right buffer. No need to do this in the player as it doesn't have
            // the editor/player confusion.
            m_CurrentUpdate = m_UpdateMask.GetUpdateTypeForPlayer();
            #endif

            if (!focus)
            {
                // We only react to loss of focus when we will keep running in the background. If not,
                // we'll do nothing and just wait for focus to come back (where we then try to sync all devices).
                if (runInBackground)
                {
                    for (var i = 0; i < m_DevicesCount; ++i)
                    {
                        // Determine whether to run this device in the background.
                        var device = m_Devices[i];
                        if (!device.enabled || ShouldRunDeviceInBackground(device))
                            continue;

                        // Disable the device. This will also soft-reset it.
                        EnableOrDisableDevice(device, false, DeviceDisableScope.TemporaryWhilePlayerIsInBackground);

                        // In case we invoked a callback that messed with our device array, adjust our index.
                        var index = m_Devices.IndexOfReference(device, m_DevicesCount);
                        if (index == -1)
                            --i;
                        else
                            i = index;
                    }
                }
            }
            else
            {
                m_DiscardOutOfFocusEvents = true;
                m_FocusRegainedTime = m_Runtime.currentTime;
                // On focus gain, reenable and sync devices.
                for (var i = 0; i < m_DevicesCount; ++i)
                {
                    var device = m_Devices[i];

                    // Re-enable the device if we disabled it on focus loss. This will also issue a sync.
                    if (device.disabledWhileInBackground)
                        EnableOrDisableDevice(device, true, DeviceDisableScope.TemporaryWhilePlayerIsInBackground);
                    // Try to sync. If it fails and we didn't run in the background, perform
                    // a reset instead. This is to cope with backends that are unable to sync but
                    // may still retain state which now may be outdated because the input device may
                    // have changed state while we weren't running. So at least make the backend flush
                    // its state (if any).
                    else if (device.enabled && !runInBackground && !device.RequestSync())
                        ResetDevice(device);
                }
            }

            #if UNITY_EDITOR
            m_CurrentUpdate = InputUpdateType.None;
            #endif

            // We set this *after* the block above as defaultUpdateType is influenced by the setting.
            m_HasFocus = focus;
        }

#if UNITY_EDITOR
        internal void LeavePlayMode()
        {
            // Reenable all devices and reset their play mode state.
            m_CurrentUpdate = InputUpdate.GetUpdateTypeForPlayer(m_UpdateMask);
            InputStateBuffers.SwitchTo(m_StateBuffers, m_CurrentUpdate);
            for (var i = 0; i < m_DevicesCount; ++i)
            {
                var device = m_Devices[i];
                if (device.disabledWhileInBackground)
                    EnableOrDisableDevice(device, true, scope: DeviceDisableScope.TemporaryWhilePlayerIsInBackground);
                ResetDevice(device, alsoResetDontResetControls: true);
            }
            m_CurrentUpdate = default;
        }

        private void OnPlayerLoopInitialization()
        {
            if (!gameIsPlaying || // if game is not playing
                !InputUpdate.s_LatestUpdateType.IsEditorUpdate() || // or last update was not editor update
                !InputUpdate.s_LatestNonEditorUpdateType.IsPlayerUpdate()) // or update before that was not player update
                return; // then no need to restore anything

            InputUpdate.RestoreStateAfterEditorUpdate();
            InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup = latestNonEditorTimeOffsetToRealtimeSinceStartup;
            InputStateBuffers.SwitchTo(m_StateBuffers, InputUpdate.s_LatestUpdateType);
        }

#endif

        internal bool ShouldRunUpdate(InputUpdateType updateType)
        {
            // We perform a "null" update after domain reloads and on startup to get our devices
            // in place before the runtime calls MonoBehaviour callbacks. See InputSystem.RunInitialUpdate().
            if (updateType == InputUpdateType.None)
                return true;

            var mask = m_UpdateMask;

#if UNITY_EDITOR
            // If the player isn't running, the only thing we run is editor updates, except if
            // explicitly overriden via `runUpdatesInEditMode`.
            // NOTE: This means that in edit mode (outside of play mode) we *never* switch to player
            //       input state. So, any script anywhere will see input state from the editor. If you
            //       have an [ExecuteInEditMode] MonoBehaviour and it polls the gamepad, for example,
            //       it will see gamepad inputs going to the editor and respond to them.
            if (!gameIsPlaying && updateType != InputUpdateType.Editor && !runPlayerUpdatesInEditMode)
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
        /// <exception cref="InvalidOperationException">Thrown if OnUpdate is called recursively.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "TODO: Refactor later.")]
        private unsafe void OnUpdate(InputUpdateType updateType, ref InputEventBuffer eventBuffer)
        {
            // NOTE: This is *not* using try/finally as we've seen unreliability in the EndSample()
            //       execution (and we're not sure where it's coming from).
            k_InputUpdateProfilerMarker.Begin();

            if (m_InputEventStream.isOpen)
            {
                k_InputUpdateProfilerMarker.End();
                throw new InvalidOperationException("Already have an event buffer set! Was OnUpdate() called recursively?");
            }

            // Restore devices before checking update mask. See InputSystem.RunInitialUpdate().
            RestoreDevicesAfterDomainReloadIfNecessary();

            // In the editor, we issue a sync on all devices when the editor comes back to the foreground.
            #if UNITY_EDITOR
            SyncAllDevicesWhenEditorIsActivated();
            #endif

            if ((updateType & m_UpdateMask) == 0)
            {
                k_InputUpdateProfilerMarker.End();
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

            // Update metrics.
            ++m_Metrics.totalUpdateCount;

            #if UNITY_EDITOR
            // If current update is editor update and previous update was non-editor,
            // store the time offset so we can restore it right after editor update is complete
            if (((updateType & InputUpdateType.Editor) == InputUpdateType.Editor) && (m_CurrentUpdate & InputUpdateType.Editor) == 0)
                latestNonEditorTimeOffsetToRealtimeSinceStartup =
                    InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            #endif

            // Store current time offset.
            InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup = m_Runtime.currentTimeOffsetToRealtimeSinceStartup;

            InputStateBuffers.SwitchTo(m_StateBuffers, updateType);

            m_CurrentUpdate = updateType;
            InputUpdate.OnUpdate(updateType);

            // Ensure optimized controls are in valid state
            CheckAllDevicesOptimizedControlsHaveValidState();

            var shouldProcessActionTimeouts = updateType.IsPlayerUpdate() && gameIsPlaying;

            // See if we're supposed to only take events up to a certain time.
            // NOTE: We do not require the events in the queue to be sorted. Instead, we will walk over
            //       all events in the buffer each time. Note that if there are multiple events for the same
            //       device, it depends on the producer of these events to queue them in correct order.
            //       Otherwise, once an event with a newer timestamp has been processed, events coming later
            //       in the buffer and having older timestamps will get rejected.

            var currentTime = updateType == InputUpdateType.Fixed ? m_Runtime.currentTimeForFixedUpdate : m_Runtime.currentTime;
            var timesliceEvents = (updateType == InputUpdateType.Fixed || updateType == InputUpdateType.BeforeRender) &&
                InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate;

            // Determine if we should flush the event buffer which would imply we exit early and do not process
            // any of those events, ever.
            var shouldFlushEventBuffer = ShouldFlushEventBuffer();
            // When we exit early, we may or may not flush the event buffer. It depends if we want to process events
            // later once this method is called.
            var shouldExitEarly =
                eventBuffer.eventCount == 0 || shouldFlushEventBuffer || ShouldExitEarlyFromEventProcessing(updateType);


#if UNITY_EDITOR
            var dropStatusEvents = ShouldDropStatusEvents(eventBuffer, ref shouldExitEarly);
#endif

            if (shouldExitEarly)
            {
                // Normally, we process action timeouts after first processing all events. If we have no
                // events, we still need to check timeouts.
                if (shouldProcessActionTimeouts)
                    ProcessStateChangeMonitorTimeouts();

                k_InputUpdateProfilerMarker.End();
                InvokeAfterUpdateCallback(updateType);
                if (shouldFlushEventBuffer)
                    eventBuffer.Reset();
                m_CurrentUpdate = default;
                return;
            }

            var processingStartTime = Stopwatch.GetTimestamp();
            var totalEventLag = 0.0;

            #if UNITY_EDITOR
            var isPlaying = gameIsPlaying;
            #endif

            try
            {
                m_InputEventStream = new InputEventStream(ref eventBuffer, m_Settings.maxQueuedEventsPerUpdate);
                var totalEventBytesProcessed = 0U;

                InputEvent* skipEventMergingFor = null;

                // Handle events.
                while (m_InputEventStream.remainingEventCount > 0)
                {
                    InputDevice device = null;
                    var currentEventReadPtr = m_InputEventStream.currentEventPtr;

                    Debug.Assert(!currentEventReadPtr->handled, "Event in buffer is already marked as handled");

                    // In before render updates, we only take state events and only those for devices
                    // that have before render updates enabled.
                    if (updateType == InputUpdateType.BeforeRender)
                    {
                        while (m_InputEventStream.remainingEventCount > 0)
                        {
                            Debug.Assert(!currentEventReadPtr->handled,
                                "Iterated to event in buffer that is already marked as handled");

                            device = TryGetDeviceById(currentEventReadPtr->deviceId);
                            if (device != null && device.updateBeforeRender &&
                                (currentEventReadPtr->type == StateEvent.Type ||
                                 currentEventReadPtr->type == DeltaStateEvent.Type))
                                break;

                            currentEventReadPtr = m_InputEventStream.Advance(leaveEventInBuffer: true);
                        }
                    }

                    if (m_InputEventStream.remainingEventCount == 0)
                        break;

                    var currentEventTimeInternal = currentEventReadPtr->internalTime;
                    var currentEventType = currentEventReadPtr->type;

#if UNITY_EDITOR
                    if (dropStatusEvents)
                    {
                        // If the type here is a status event, ask advance not to leave the event in the buffer.  Otherwise, leave it there.
                        if (currentEventType == StateEvent.Type || currentEventType == DeltaStateEvent.Type || currentEventType == IMECompositionEvent.Type)
                            m_InputEventStream.Advance(false);
                        else
                            m_InputEventStream.Advance(true);

                        continue;
                    }

                    // Decide to skip events based on timing or focus state
                    if (ShouldDiscardEventInEditor(currentEventType, currentEventTimeInternal, updateType))
                    {
                        m_InputEventStream.Advance(false);
                        continue;
                    }
#endif

                    // If we're timeslicing, check if the event time is within limits.
                    if (timesliceEvents && currentEventTimeInternal >= currentTime)
                    {
                        m_InputEventStream.Advance(true);
                        continue;
                    }

                    // If we can't find the device, ignore the event.
                    if (device == null)
                        device = TryGetDeviceById(currentEventReadPtr->deviceId);
                    if (device == null)
                    {
#if UNITY_EDITOR
                        ////TODO: see if this is a device we haven't created and if so, just ignore
                        m_Diagnostics?.OnCannotFindDeviceForEvent(new InputEventPtr(currentEventReadPtr));
#endif

                        m_InputEventStream.Advance(false);
                        continue;
                    }

                    // In the editor, we may need to bump events from editor updates into player updates
                    // and vice versa.
#if UNITY_EDITOR
                    if (isPlaying && !gameHasFocus)
                    {
                        if (m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode
                            .PointersAndKeyboardsRespectGameViewFocus &&
                            m_Settings.backgroundBehavior !=
                            InputSettings.BackgroundBehavior.ResetAndDisableAllDevices)
                        {
                            var isPointerOrKeyboard = device is Pointer || device is Keyboard;
                            if (updateType != InputUpdateType.Editor)
                            {
                                // Let everything but pointer and keyboard input through.
                                // If the event is from a pointer or keyboard, leave it in the buffer so it can be dealt with
                                // in a subsequent editor update. Otherwise, take it out.
                                if (isPointerOrKeyboard)
                                {
                                    m_InputEventStream.Advance(true);
                                    continue;
                                }
                            }
                            else
                            {
                                // Let only pointer and keyboard input through.
                                if (!isPointerOrKeyboard)
                                {
                                    m_InputEventStream.Advance(true);
                                    continue;
                                }
                            }
                        }
                    }
#endif

                    // If device is disabled, we let the event through only in certain cases.
                    // Removal and configuration change events should always be processed.
                    if (!device.enabled &&
                        currentEventType != DeviceRemoveEvent.Type &&
                        currentEventType != DeviceConfigurationEvent.Type &&
                        (device.m_DeviceFlags & (InputDevice.DeviceFlags.DisabledInRuntime |
                                                 InputDevice.DeviceFlags.DisabledWhileInBackground)) != 0)
                    {
#if UNITY_EDITOR
                        // If the device is disabled in the backend, getting events for them
                        // is something that indicates a problem in the backend so diagnose.
                        if ((device.m_DeviceFlags & InputDevice.DeviceFlags.DisabledInRuntime) != 0)
                            m_Diagnostics?.OnEventForDisabledDevice(currentEventReadPtr, device);
#endif

                        m_InputEventStream.Advance(false);
                        continue;
                    }

                    // Check if the device wants to merge successive events.
                    if (!settings.disableRedundantEventsMerging && device.hasEventMerger && currentEventReadPtr != skipEventMergingFor)
                    {
                        // NOTE: This relies on events in the buffer being consecutive for the same device. This is not
                        //       necessarily the case for events coming in from the background event queue where parallel
                        //       producers may create interleaved input sequences. This will be fixed once we have the
                        //       new buffering scheme for input events working in the native runtime.

                        var nextEvent = m_InputEventStream.Peek();
                        // If there is next event after current one.
                        if ((nextEvent != null)
                            // And if next event is for the same device.
                            && (currentEventReadPtr->deviceId == nextEvent->deviceId)
                            // And if next event is in the same timeslicing slot.
                            && (timesliceEvents ? (nextEvent->internalTime < currentTime) : true)
                        )
                        {
                            // Then try to merge current event into next event.
                            if (((IEventMerger)device).MergeForward(currentEventReadPtr, nextEvent))
                            {
                                // And if succeeded, skip current event, as it was merged into next event.
                                m_InputEventStream.Advance(false);
                                continue;
                            }

                            // If we can't merge current event with next one for any reason, we assume the next event
                            // carries crucial entropy (button changed state, phase changed, counter changed, etc).
                            // Hence semantic meaning for current event is "can't merge current with next because next is different".
                            // But semantic meaning for next event is "next event carries important information and should be preserved",
                            // from that point of view next event should not be merged with current nor with _next after next_ event.
                            //
                            // For example, given such stream of events:
                            // Mouse       Mouse       Mouse       Mouse       Mouse       Mouse       Mouse
                            // Event no1   Event no2   Event no3   Event no4   Event no5   Event no6   Event no7
                            // Time 1      Time 2      Time 3      Time 4      Time 5      Time 6      Time 7
                            // Pos(10,20)  Pos(12,21)  Pos(13,23)  Pos(14,24)  Pos(16,25)  Pos(17,27)  Pos(18,28)
                            // Delta(1,1)  Delta(2,1)  Delta(1,2)  Delta(1,1)  Delta(2,1)  Delta(1,2)  Delta(1,1)
                            // BtnLeft(0)  BtnLeft(0)  BtnLeft(0)  BtnLeft(1)  BtnLeft(1)  BtnLeft(1)  BtnLeft(1)
                            //
                            // if we then merge without skipping next event here:
                            //                         Mouse                                           Mouse
                            //                         Event no3                                       Event no7
                            //                         Time 3                                          Time 7
                            //                         Pos(13,23)                                      Pos(18,28)
                            //                         Delta(4,4)                                      Delta(5,5)
                            //                         BtnLeft(0)                                      BtnLeft(1)
                            //
                            // As you can see, the event no4 containing mouse button press was lost,
                            // and with it we lose the important information of timestamp of mouse button press.
                            //
                            // With skipping merging next event we will get:
                            //                         Mouse       Mouse                               Mouse
                            //                         Time 3      Time 4                              Time 7
                            //                         Event no3   Event no4                           Event no7
                            //                         Pos(13,23)  Pos(14,24)                          Pos(18,28)
                            //                         Delta(3,3)  Delta(1,1)                          Delta(4,4)
                            //                         BtnLeft(0)  BtnLeft(1)                          BtnLeft(1)
                            //
                            // And no4 is preserved, with the exact timestamp of button press.
                            skipEventMergingFor = nextEvent;
                        }
                    }

                    // Give the device a chance to do something with data before we propagate it to event listeners.
                    if (device.hasEventPreProcessor)
                    {
#if UNITY_EDITOR
                        var eventSizeBeforePreProcessor = currentEventReadPtr->sizeInBytes;
#endif
                        var shouldProcess = ((IEventPreProcessor)device).PreProcessEvent(currentEventReadPtr);
#if UNITY_EDITOR
                        if (currentEventReadPtr->sizeInBytes > eventSizeBeforePreProcessor)
                        {
                            k_InputUpdateProfilerMarker.End();
                            throw new AccessViolationException($"'{device}'.PreProcessEvent tries to grow an event from {eventSizeBeforePreProcessor} bytes to {currentEventReadPtr->sizeInBytes} bytes, this will potentially corrupt events after the current event and/or cause out-of-bounds memory access.");
                        }
#endif
                        if (!shouldProcess)
                        {
                            // Skip event if PreProcessEvent considers it to be irrelevant.
                            m_InputEventStream.Advance(false);
                            continue;
                        }
                    }

                    // Give listeners a shot at the event.
                    // NOTE: We call listeners also for events where the device is disabled. This is crucial for code
                    //       such as TouchSimulation that disables the originating devices and then uses its events to
                    //       create simulated events from.
                    if (m_EventListeners.length > 0)
                    {
                        DelegateHelpers.InvokeCallbacksSafe(ref m_EventListeners,
                            new InputEventPtr(currentEventReadPtr), device, k_InputOnEventMarker, "InputSystem.onEvent");

                        // If a listener marks the event as handled, we don't process it further.
                        if (m_InputEventHandledPolicy == InputEventHandledPolicy.SuppressStateUpdates &&
                            currentEventReadPtr->handled)
                        {
                            m_InputEventStream.Advance(false);
                            continue;
                        }
                    }

                    // Update metrics.
                    if (currentEventTimeInternal <= currentTime)
                        totalEventLag += currentTime - currentEventTimeInternal;
                    ++m_Metrics.totalEventCount;
                    m_Metrics.totalEventBytes += (int)currentEventReadPtr->sizeInBytes;

                    // Process.
                    switch (currentEventType)
                    {
                        case StateEvent.Type:
                        case DeltaStateEvent.Type:

                            var eventPtr = new InputEventPtr(currentEventReadPtr);

                            // Ignore the event if the last state update we received for the device was
                            // newer than this state event is. We don't allow devices to go back in time.
                            //
                            // NOTE: We make an exception here for devices that implement IInputStateCallbackReceiver (such
                            //       as Touchscreen). For devices that dynamically incorporate state it can be hard ensuring
                            //       a global ordering of events as there may be multiple substreams (e.g. each individual touch)
                            //       that are generated in the backend and would require considerable work to ensure monotonically
                            //       increasing timestamps across all such streams.
                            var deviceIsStateCallbackReceiver = device.hasStateCallbacks;
                            if (currentEventTimeInternal < device.m_LastUpdateTimeInternal &&
                                !(deviceIsStateCallbackReceiver && device.stateBlock.format != eventPtr.stateFormat))
                            {
#if UNITY_EDITOR
                                m_Diagnostics?.OnEventTimestampOutdated(new InputEventPtr(currentEventReadPtr), device);
#elif UNITY_ANDROID
                                // Android keyboards can send events out of order: Holding down a key will send multiple
                                // presses after a short time, like on most platforms. Unfortunately, on Android, the
                                // last of these "presses" can be timestamped to be after the event of the key release.
                                // If that happens, we'd skip the keyUp here, and the device state will have the key
                                // "stuck" pressed. So, special case here to not skip keyboard events on Android. ISXB-475
                                // N.B. Android seems to have similar issues with touch input (OnStateEvent, Touchscreen.cs)
                                if (!(device is Keyboard))
#endif
                                break;
                            }

                            // Update the state of the device from the event. If the device is an IInputStateCallbackReceiver,
                            // let the device handle the event. If not, we do it ourselves.
                            var haveChangedStateOtherThanNoise = true;
                            if (deviceIsStateCallbackReceiver)
                            {
                                m_ShouldMakeCurrentlyUpdatingDeviceCurrent = true;
                                // NOTE: We leave it to the device to make sure the event has the right format. This allows the
                                //       device to handle multiple different incoming formats.
                                ((IInputStateCallbackReceiver)device).OnStateEvent(eventPtr);

                                haveChangedStateOtherThanNoise = m_ShouldMakeCurrentlyUpdatingDeviceCurrent;
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

                            totalEventBytesProcessed += eventPtr.sizeInBytes;

                            device.m_CurrentProcessedEventBytesOnUpdate += eventPtr.sizeInBytes;

                            // Update timestamp on device.
                            // NOTE: We do this here and not in UpdateState() so that InputState.Change() will *NOT* change timestamps.
                            //       Only events should. If running play mode updates in editor, we want to defer to the play mode
                            //       callbacks to set the last update time to avoid dropping events only processed by the editor state.
                            if (device.m_LastUpdateTimeInternal <= eventPtr.internalTime
#if UNITY_EDITOR
                                && !(updateType == InputUpdateType.Editor && runPlayerUpdatesInEditMode)
#endif
                            )
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
                                ArrayHelpers.AppendWithCapacity(ref m_DisconnectedDevices,
                                    ref m_DisconnectedDevicesCount, device);
                                DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners,
                                    device, InputDeviceChange.Disconnected, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");
                            }

                            break;
                        }

                        case DeviceConfigurationEvent.Type:
                            device.NotifyConfigurationChanged();
                            InputActionState.OnDeviceChange(device, InputDeviceChange.ConfigurationChanged);
                            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceChangeListeners,
                                device, InputDeviceChange.ConfigurationChanged, k_InputOnDeviceChangeMarker, "InputSystem.onDeviceChange");
                            break;

                        case DeviceResetEvent.Type:
                            ResetDevice(device,
                                alsoResetDontResetControls: ((DeviceResetEvent*)currentEventReadPtr)->hardReset);
                            break;
                    }

                    m_InputEventStream.Advance(leaveEventInBuffer: false);

                    // Discard events in case the maximum event bytes per update has been exceeded
                    if (AreMaximumEventBytesPerUpdateExceeded(totalEventBytesProcessed))
                        break;
                }

                m_Metrics.totalEventProcessingTime +=
                    ((double)(Stopwatch.GetTimestamp() - processingStartTime)) / Stopwatch.Frequency;
                m_Metrics.totalEventLagTime += totalEventLag;

                ResetCurrentProcessedEventBytesForDevices();

                m_InputEventStream.Close(ref eventBuffer);
            }
            catch (Exception)
            {
                // We need to restore m_InputEventStream to a sound state
                // to avoid failing recursive OnUpdate check next frame.
                k_InputUpdateProfilerMarker.End();
                m_InputEventStream.CleanUpAfterException();
                throw;
            }

            m_DiscardOutOfFocusEvents = false;

            if (shouldProcessActionTimeouts)
                ProcessStateChangeMonitorTimeouts();

            k_InputUpdateProfilerMarker.End();
            ////FIXME: need to ensure that if someone calls QueueEvent() from an onAfterUpdate callback, we don't end up with a
            ////       mess in the event buffer
            ////       same goes for events that someone may queue from a change monitor callback
            InvokeAfterUpdateCallback(updateType);
            //send pointer data to backend for OnMouseEvents
#if UNITY_INPUTSYSTEM_SUPPORTS_MOUSE_SCRIPT_EVENTS
            var pointer = Pointer.current;
            if (pointer != null && pointer.added && gameIsPlaying)
                NativeInputSystem.DoSendMouseEvents(pointer.press.isPressed, pointer.press.wasPressedThisFrame, pointer.position.x.value, pointer.position.y.value);
#endif
            m_CurrentUpdate = default;
        }

        /// <summary>
        /// Determines if the event buffer should be flushed without processing events.
        /// </summary>
        /// <returns>True if the buffer should be flushed, false otherwise.</returns>
        private bool ShouldFlushEventBuffer()
        {
#if UNITY_EDITOR
            // If out of focus and runInBackground is off and ExactlyAsInPlayer is on, discard input.
            if (!gameHasFocus &&
                m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView
                &&
                (!m_Runtime.runInBackground || m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.ResetAndDisableAllDevices))
                return true;
#else
            // In player builds, flush if out of focus and not running in background
            if (!gameHasFocus && !m_Runtime.runInBackground)
                return true;
#endif
            return false;
        }

        /// <summary>
        /// Determines if we should exit early from event processing without handling events.
        /// </summary>
        /// <param name="eventBuffer">The current event buffer</param>
        /// <param name="canFlushBuffer">Whether the buffer can be flushed</param>
        /// <param name="updateType">The current update type</param>
        /// <returns>True if we should exit early, false otherwise.</returns>
        private bool ShouldExitEarlyFromEventProcessing(InputUpdateType updateType)
        {
#if UNITY_EDITOR
            // Check various PlayMode specific early exit conditions
            if (ShouldExitEarlyBasedOnBackgroundBehavior(updateType))
                return true;

            // When the game is playing and has focus, we never process input in editor updates.
            // All we do is just switch to editor state buffers and then exit.
            if ((gameIsPlaying && gameHasFocus && updateType == InputUpdateType.Editor))
                return true;
#endif

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Checks background behavior conditions for early exit from event processing.
        /// </summary>
        /// <param name="updateType">The current update type</param>
        /// <returns>True if we should exit early, false otherwise.</returns>
        /// <remarks>
        /// Whenever this method returns true, it usually means that events are left in the buffer and should be
        /// processed in a next update call.
        /// </remarks>
        private bool ShouldExitEarlyBasedOnBackgroundBehavior(InputUpdateType updateType)
        {
            // In Play Mode, if we're in the background and not supposed to process events in this update
            if ((!gameHasFocus || gameShouldGetInputRegardlessOfFocus) && updateType != InputUpdateType.Editor)
            {
                if (m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.ResetAndDisableAllDevices ||
                    m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    return true;
            }

            // Special case for IgnoreFocus behavior with AllDeviceInputAlwaysGoesToGameView in editor updates
            if ((!gameHasFocus || gameShouldGetInputRegardlessOfFocus) &&
                m_Settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus &&
                m_Settings.editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView &&
                updateType == InputUpdateType.Editor)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if status events should be dropped and modifies early exit behavior accordingly.
        /// </summary>
        /// <param name="eventBuffer">The current event buffer</param>
        /// <param name="canEarlyOut">Reference to the early exit flag that may be modified</param>
        /// <returns>True if status events should be dropped, false otherwise.</returns>
        private bool ShouldDropStatusEvents(InputEventBuffer eventBuffer, ref bool canEarlyOut)
        {
            // If the game is not playing but we're sending all input events to the game,
            // the buffer can just grow unbounded. So, in that case, set a flag to say we'd
            // like to drop status events, and do not early out.
            if (!gameIsPlaying && gameShouldGetInputRegardlessOfFocus && (eventBuffer.sizeInBytes > (100 * 1024)))
            {
                canEarlyOut = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if an event should be discarded based on timing or focus state.
        /// </summary>
        /// <param name="eventType">The type of event</param>
        /// <param name="eventTime">The internal time of the current event</param>
        /// <param name="updateType">The current update type</param>
        /// <returns>True if the event should be discarded, false otherwise.</returns>
        private bool ShouldDiscardEventInEditor(FourCC eventType, double eventTime, InputUpdateType updateType)
        {
            // Check if this is an event that occurred during edit mode transition
            if (ShouldDiscardEditModeTransitionEvent(eventType, eventTime, updateType))
                return true;

            // Check if this is an out-of-focus event that should be discarded
            if (ShouldDiscardOutOfFocusEvent(eventTime))
                return true;

            return false;
        }

        /// <summary>
        /// In the editor, we discard all input events that occur in-between exiting edit mode and having
        /// entered play mode as otherwise we'll spill a bunch of UI events that have occurred while the
        /// UI was sort of neither in this mode nor in that mode. This would usually lead to the game receiving
        /// an accumulation of spurious inputs right in one of its first updates.
        ///
        /// NOTE: There's a chance the solution here will prove inadequate on the long run. We may do things
        ///       here such as throwing partial touches away and then letting the rest of a touch go through.
        ///       Could be that ultimately we need to issue a full reset of all devices at the beginning of
        ///       play mode in the editor.
        /// </summary>
        private bool ShouldDiscardEditModeTransitionEvent(FourCC eventType, double eventTime, InputUpdateType updateType)
        {
            return (eventType == StateEvent.Type || eventType == DeltaStateEvent.Type) &&
                (updateType & InputUpdateType.Editor) == 0 &&
                InputSystem.s_SystemObject.exitEditModeTime > 0 &&
                eventTime >= InputSystem.s_SystemObject.exitEditModeTime &&
                (eventTime < InputSystem.s_SystemObject.enterPlayModeTime ||
                    InputSystem.s_SystemObject.enterPlayModeTime == 0);
        }

        /// <summary>
        /// Checks if an event should be discarded because it occurred while out of focus, under specific settings.
        /// </summary>
        private bool ShouldDiscardOutOfFocusEvent(double eventTime)
        {
            // If we care about focus, check if the event occurred while out of focus based on its timestamp.
            if (gameHasFocus && m_Settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
                return m_DiscardOutOfFocusEvents && eventTime < m_FocusRegainedTime;
            return false;
        }

#endif

        bool AreMaximumEventBytesPerUpdateExceeded(uint totalEventBytesProcessed)
        {
            if (m_Settings.maxEventBytesPerUpdate > 0 &&
                totalEventBytesProcessed >= m_Settings.maxEventBytesPerUpdate)
            {
                var eventsProcessedByDeviceLog = String.Empty;
                // Only log the events processed by devices in last update call if we are in debug mode.
                // This is to avoid the slightest overhead in release builds of having to iterate over all devices and
                // reset the byte count, by the end of every update call with ResetCurrentProcessedEventBytesForDevices().
                if (Debug.isDebugBuild)
                    eventsProcessedByDeviceLog = $"Total events processed by devices in last update call:\n{MakeStringWithEventsProcessedByDevice()}";

                Debug.LogError(
                    "Exceeded budget for maximum input event throughput per InputSystem.Update(). Discarding remaining events. "
                    + "Increase InputSystem.settings.maxEventBytesPerUpdate or set it to 0 to remove the limit.\n"
                    + eventsProcessedByDeviceLog);

                return true;
            }

            return false;
        }

        private string MakeStringWithEventsProcessedByDevice()
        {
            var eventsProcessedByDeviceLog = new StringBuilder();
            for (int i = 0; i < m_DevicesCount; i++)
            {
                var deviceToLog = devices[i];
                if (deviceToLog != null && deviceToLog.m_CurrentProcessedEventBytesOnUpdate > 0)
                    eventsProcessedByDeviceLog.Append($" - {deviceToLog.m_CurrentProcessedEventBytesOnUpdate} bytes processed by {deviceToLog}\n");
            }
            return eventsProcessedByDeviceLog.ToString();
        }

        // Reset the number of bytes processed by devices in the current update, for debug builds.
        // This is to avoid the slightest overhead in release builds of having to iterate over all devices connected.
        private void ResetCurrentProcessedEventBytesForDevices()
        {
            if (Debug.isDebugBuild)
            {
                for (var i = 0; i < m_DevicesCount; i++)
                {
                    var device = m_Devices[i];
                    if (device != null && device.m_CurrentProcessedEventBytesOnUpdate > 0)
                    {
                        device.m_CurrentProcessedEventBytesOnUpdate = 0;
                    }
                }
            }
        }

        // Only do this check in editor in hope that it will be sufficient to catch any misuse during development.
        [Conditional("UNITY_EDITOR")]
        void CheckAllDevicesOptimizedControlsHaveValidState()
        {
            if (!InputSystem.s_Manager.m_OptimizedControlsFeatureEnabled)
                return;

            foreach (var device in devices)
                device.EnsureOptimizationTypeHasNotChanged();
        }

        private void InvokeAfterUpdateCallback(InputUpdateType updateType)
        {
            // don't invoke the after update callback if this is an editor update and the game is playing. We
            // skip event processing when playing in the editor and the game has focus, which means that any
            // handlers for this delegate that query input state during this update will get no values.
            if (updateType == InputUpdateType.Editor && gameIsPlaying)
                return;

            DelegateHelpers.InvokeCallbacksSafe(ref m_AfterUpdateListeners,
                k_InputOnAfterUpdateMarker, "InputSystem.onAfterUpdate");
        }

        private bool m_ShouldMakeCurrentlyUpdatingDeviceCurrent;

        // This is a dirty hot fix to expose entropy from device back to input manager to make a choice if we want to make device current or not.
        // A proper fix would be to change IInputStateCallbackReceiver.OnStateEvent to return bool to make device current or not.
        internal void DontMakeCurrentlyUpdatingDeviceCurrent()
        {
            m_ShouldMakeCurrentlyUpdatingDeviceCurrent = false;
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

            // If state monitors need to be re-sorted, do it now.
            // NOTE: This must happen with the monitors in non-signalled state!
            SortStateChangeMonitorsIfNecessary(deviceIndex);

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
            var noiseMask = device.noisy
                ? (byte*)InputStateBuffers.s_NoiseMaskBuffer + deviceStateOffset
                : null;
            // Compare the current state of the device to the newly received state but overlay
            // the comparison by the noise mask.
            var makeDeviceCurrent = !MemoryHelpers.MemCmpBitRegion(deviceStatePtr, statePtr,
                0, stateSize * 8, mask: noiseMask);

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

            if (makeDeviceCurrent)
            {
                // Update the pressed/not pressed state of all buttons that have changed this update
                // With enough ButtonControls being checked, it's faster to find out which have actually changed rather than test all.
                if (InputSystem.s_Manager.m_ReadValueCachingFeatureEnabled || device.m_UseCachePathForButtonPresses)
                {
                    foreach (var button in device.m_UpdatedButtons)
                    {
                        #if UNITY_EDITOR
                        if (updateType == InputUpdateType.Editor)
                        {
                            ((ButtonControl)device.allControls[button]).UpdateWasPressedEditor();
                        }
                        else
                        #endif
                        ((ButtonControl)device.allControls[button]).UpdateWasPressed();
                    }
                }
                else
                {
                    int buttonCount = 0;
                    foreach (var button in device.m_ButtonControlsCheckingPressState)
                    {
                        #if UNITY_EDITOR
                        if (updateType == InputUpdateType.Editor)
                        {
                            button.UpdateWasPressedEditor();
                        }
                        else
                        #endif
                        button.UpdateWasPressed();

                        ++buttonCount;
                    }

                    // From testing, this is the point at which it becomes more efficient to use the same path as
                    // ReadValueCaching to work out which ButtonControls have updated, rather than querying all.
                    if (buttonCount > 45)
                        device.m_UseCachePathForButtonPresses = true;
                }
            }

            // Notify listeners.
            DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceStateChangeListeners,
                device, eventPtr, k_InputOnDeviceSettingsChangeMarker, "InputSystem.onDeviceStateChange");

            // Now that we've committed the new state to memory, if any of the change
            // monitors fired, let the associated actions know.
            if (haveSignalledMonitors)
                FireStateChangeNotifications(deviceIndex, internalTime, eventPtr);

            return makeDeviceCurrent;
        }

        private unsafe void WriteStateChange(InputStateBuffers.DoubleBuffers buffers, int deviceIndex,
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

            // If we have enough ButtonControls being checked for wasPressedThisFrame/wasReleasedThisFrame,
            // use this path to find out which have actually changed here.
            if (InputSystem.s_Manager.m_ReadValueCachingFeatureEnabled || m_Devices[deviceIndex].m_UseCachePathForButtonPresses)
            {
                // if the buffers have just been flipped, and we're doing a full state update, then the state from the
                // previous update is now in the back buffer, and we should be comparing to that when checking what
                // controls have changed
                var buffer = (byte*)frontBuffer;
                if (flippedBuffers && deviceStateSize == stateSizeInBytes)
                    buffer = (byte*)buffers.GetBackBuffer(deviceIndex);

                m_Devices[deviceIndex].WriteChangedControlStates(buffer + deviceStateBlock.byteOffset, statePtr,
                    stateSizeInBytes, stateOffsetInDevice);
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
            if (updateType == InputUpdateType.Editor)
            {
                ////REVIEW: This isn't right. The editor does have update ticks which constitute the equivalent of player frames.
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
        ///
        /// WARNING
        ///
        /// Making changes to serialized data format will likely to break upgrading projects from older versions.
        /// That is until you restart the editor, then we recreate everything from clean state.
        /// </remarks>
        [Serializable]
        internal struct SerializedState
        {
            public int layoutRegistrationVersion;
            public float pollingFrequency;
            public InputEventHandledPolicy inputEventHandledPolicy;
            public DeviceState[] devices;
            public AvailableDevice[] availableDevices;
            public InputStateBuffers buffers;
            public InputUpdate.SerializedState updateState;
            public InputUpdateType updateMask;
            public InputSettings.ScrollDeltaBehavior scrollDeltaBehavior;
            public InputMetrics metrics;
            public InputSettings settings;
            public InputActionAsset actions;

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
                #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
                pollingFrequency = m_PollingFrequency,
                #endif
                inputEventHandledPolicy =  m_InputEventHandledPolicy,
                devices = deviceArray,
                availableDevices = m_AvailableDevices?.Take(m_AvailableDeviceCount).ToArray(),
                buffers = m_StateBuffers,
                updateState = InputUpdate.Save(),
                updateMask = m_UpdateMask,
                scrollDeltaBehavior = m_ScrollDeltaBehavior,
                metrics = m_Metrics,
                settings = m_Settings,
                actions = m_Actions,

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
            scrollDeltaBehavior = state.scrollDeltaBehavior;
            m_Metrics = state.metrics;
            #if !UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY
            m_PollingFrequency = state.pollingFrequency;
            #endif
            m_InputEventHandledPolicy = state.inputEventHandledPolicy;

            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);

            settings = state.settings;

            // Note that we just reassign actions and never destroy them since always mapped to persisted asset
            // and hence ownership lies with ADB.
            m_Actions = state.actions;

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
            k_InputRestoreDevicesAfterReloadMarker.Begin();

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

            k_InputRestoreDevicesAfterReloadMarker.End();
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
