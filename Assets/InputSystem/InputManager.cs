using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngineInternal.Input;

namespace ISX
{
    using DeviceChangeListener = Action<InputDevice, InputDeviceChange>;
    using EventListener = Action<InputEventPtr>;

    // The hub of the input system.
    // All state is ultimately gathered here.
    // Not exposed. Use InputSystem as the public entry point to the system.
#if UNITY_EDITOR
    [Serializable]
#endif
    internal class InputManager
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        public ReadOnlyArray<InputDevice> devices => new ReadOnlyArray<InputDevice>(m_Devices);

        public InputUpdateType updateMask
        {
            get { return m_UpdateMask; }
            set
            {
                ////TODO: also actually turn off unnecessary updates on the native side (e.g. if fixed
                ////      updates are disabled, don't even have native fire onUpdate for fixed updates)
                throw new NotImplementedException();
            }
        }

        public event DeviceChangeListener onDeviceChange
        {
            add { m_DeviceChangeListeners.Append(value); }
            remove { m_DeviceChangeListeners.Remove(value); }
        }

        public event EventListener onEvent
        {
            add { m_EventListeners.Append(value); }
            remove { m_EventListeners.Remove(value); }
        }

        // Add a template constructed from a type.
        // If a template with the same name already exists, the new template
        // takes its place.
        public void RegisterTemplate(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var isDeviceTemplate = typeof(InputDevice).IsAssignableFrom(type);
            var isControlTemplate = typeof(InputControl).IsAssignableFrom(type);

            if (!isDeviceTemplate && !isControlTemplate)
                throw new ArgumentException("Types used as templates have to be InputControls are InputDevices",
                    nameof(type));

            // All we do is enter the type into a map. We don't construct an InputTemplate
            // from it until we actually need it in an InputControlSetup to create a device.
            // This not only avoids us creating a bunch of objects on the managed heap but
            // also avoids us laboriously constructing a VRController template, for example,
            // in a game that never uses VR.
            var nameLowerCase = name.ToLower();
            m_TemplateTypes[nameLowerCase] = type;
            ++m_TemplateSetupVersion;

            // Re-create any devices using the template.
            RecreateDevicesUsingTemplate(nameLowerCase, isDeviceTemplate);
        }

        // Add a template constructed from a JSON string.
        public void RegisterTemplate(string json, string name = null)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException(nameof(json));

            ////REVIEW: as long as no one has instantiated the template, the base template information is kinda pointless

            // Parse out name, device description, and base template.
            InputDeviceDescription deviceDescription;
            string baseTemplate;
            var nameFromJson = InputTemplate.ParseHeaderFromJson(json, out deviceDescription, out baseTemplate);

            // Decide whether to take name from JSON or from code.
            if (string.IsNullOrEmpty(name))
            {
                name = nameFromJson;

                // Make sure we have a name.
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException($"Template name has not been given and is not set in JSON template",
                        nameof(name));
            }

            // Add it to our records.
            var nameLowerCase = name.ToLower();
            m_TemplateStrings[nameLowerCase] = json;
            if (!string.IsNullOrEmpty(baseTemplate))
                m_BaseTemplateTable[nameLowerCase] = baseTemplate.ToLower();
            ++m_TemplateSetupVersion;

            // Re-create any devices using the template.
            RecreateDevicesUsingTemplate(nameLowerCase);

            // If the template has a device description, see if it allows us
            // to make sense of any device we couldn't make sense of so far.
            if (!deviceDescription.empty)
            {
                m_SupportedDevices.Add(new SupportedDevice
                {
                    description = deviceDescription,
                    template = name
                });

                for (var i = 0; i < m_AvailableDevices.Count; ++i)
                {
                    var deviceId = m_AvailableDevices[i].deviceId;
                    if (TryGetDeviceById(deviceId) != null)
                        continue;

                    if (deviceDescription.Matches(m_AvailableDevices[i].description))
                    {
                        AddDevice(name, deviceId, deviceDescription);
                    }
                }
            }
        }

        private void RecreateDevicesUsingTemplate(string templateLowerCase, bool isKnownToBeDeviceTemplate = false)
        {
            if (m_Devices == null)
                return;

            for (var i = 0; i < m_Devices.Length; ++i)
            {
                var device = m_Devices[i];

                var usesTemplate = false;
                if (isKnownToBeDeviceTemplate)
                    usesTemplate = IsControlUsingTemplate(device, templateLowerCase);
                else
                    usesTemplate = IsControlOrChildUsingTemplateRecursive(device, templateLowerCase);

                if (usesTemplate)
                    throw new NotImplementedException();
            }
        }

        private bool IsControlOrChildUsingTemplateRecursive(InputControl control, string templateLowerCase)
        {
            // Check control itself.
            if (IsControlUsingTemplate(control, templateLowerCase))
                return true;

            // Check children.
            var children = control.children;
            for (var i = 0; i < children.Count; ++i)
                if (IsControlOrChildUsingTemplateRecursive(children[i], templateLowerCase))
                    return true;

            return false;
        }

        private bool IsControlUsingTemplate(InputControl control, string templateLowerCase)
        {
            // Check direct match.
            var controlTemplateLowerCase = control.template.ToLower();
            if (controlTemplateLowerCase == templateLowerCase)
                return true;

            // Check base template chain.
            string baseTemplate = controlTemplateLowerCase;
            while (m_BaseTemplateTable.TryGetValue(baseTemplate, out baseTemplate))
                if (baseTemplate == templateLowerCase)
                    return true;

            return false;
        }

        public string TryFindMatchingTemplate(InputDeviceDescription deviceDescription)
        {
            ////TODO: this will want to take overrides into account

            // See if we can match by description.
            for (var i = 0; i < m_SupportedDevices.Count; ++i)
            {
                if (m_SupportedDevices[i].description.Matches(deviceDescription))
                    return m_SupportedDevices[i].template;
            }

            // No, so try to match by device class. If we have a "Gamepad" template,
            // for example, a device that classifies itself as a "Gamepad" will match
            // that template.
            if (!string.IsNullOrEmpty(deviceDescription.deviceClass))
            {
                var deviceClassLowerCase = deviceDescription.deviceClass.ToLower();
                if (m_TemplateStrings.ContainsKey(deviceClassLowerCase) ||
                    m_TemplateTypes.ContainsKey(deviceClassLowerCase))
                    return deviceDescription.deviceClass;
            }

            return null;
        }

        public int ListTemplates(List<string> templates)
        {
            if (templates == null)
                throw new ArgumentNullException(nameof(templates));

            var countBefore = templates.Count;

            ////FIXME: this may add a name twice; also allocates

            templates.AddRange(m_TemplateTypes.Keys);
            templates.AddRange(m_TemplateStrings.Keys);

            return templates.Count - countBefore;
        }

        public void RegisterProcessor(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ////REVIEW: probably good to typecheck here but it would require dealing with generic type stuff

            m_Processors[name.ToLower()] = type;
        }

        public Type TryGetProcessor(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            Type type;
            if (m_Processors.TryGetValue(name, out type))
                return type;
            return null;
        }

        public void RegisterModifier(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            m_Modifiers[name.ToLower()] = type;
        }

        public Type TryGetModifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            Type type;
            if (m_Modifiers.TryGetValue(name, out type))
                return type;
            return null;
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
        public int GetControls(string path, List<InputControl> controls)
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            if (controls == null)
                throw new ArgumentNullException(nameof(controls));
            if (m_Devices == null)
                return 0;

            var indexInPath = 0;
            if (path[0] == '/')
                ++indexInPath;

            var deviceCount = m_Devices.Length;
            var numMatches = 0;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                numMatches += PathHelpers.FindControls(device, path, indexInPath, controls);
            }

            return numMatches;
        }

        ////TODO: make sure that no device or control with a '/' in the name can creep into the system

        // Creates a device from the given template and adds it to the system.
        // NOTE: Creates garbage.
        public InputDevice AddDevice(string template, string name = null)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentException(nameof(template));

            var setup = new InputControlSetup(template);
            var device = setup.Finish();

            if (!string.IsNullOrEmpty(name))
                device.m_Name = name;

            AddDevice(device);

            return device;
        }

        // Add device with a forced ID. Used when creating devices reported to us by native.
        private InputDevice AddDevice(string template, int deviceId, InputDeviceDescription description)
        {
            var setup = new InputControlSetup(template);
            var device = setup.Finish();
            device.m_Id = deviceId;
            device.m_Description = description;

            if (!string.IsNullOrEmpty(description.product) && description.product != "Generic")////REVIEW: probably want a better approach to filtering out nonsense product names
                device.m_Name = description.product;

            AddDevice(device);

            return device;
        }

        public void AddDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrEmpty(device.template))
                throw new ArgumentException("Device has no associated template", nameof(device));

            // Ignore if the same device gets added multiple times.
            if (ArrayHelpers.Contains(m_Devices, device))
                return;

            MakeDeviceNameUnique(device);
            AssignUniqueDeviceId(device);

            // Add to list.
            device.m_DeviceIndex = ArrayHelpers.Append(ref m_Devices, device);

            ////REVIEW: Not sure a full-blown dictionary is the right way here. Alternatives are to keep
            ////        a sparse array that directly indexes using the linearly increasing IDs (though that
            ////        may get large over time). Or to just do a linear search through m_Devices (but
            ////        that may end up tapping a bunch of memory locations in the heap to find the right
            ////        device; could be improved by sorting m_Devices by ID and picking a good starting
            ////        point based on the ID we have instead of searching from [0] always).
            m_DevicesById[device.id] = device;

            // Let InputStateBuffers know this device doesn't have any associated state yet.
            device.m_StateBlock.byteOffset = InputStateBlock.kInvalidOffset;

            // Mark as connected.
            device.m_Flags |= InputDevice.Flags.Connected;

            // Let InputStateBuffers allocate state buffers.
            ReallocateStateBuffers();

            // Make the device current.
            device.MakeCurrent();

            ////REVIEW: what about device connects and disconnects? should actions stay with controls
            ////        of disconnected devices or do we want to restrict them to just connected devices?
            // Let actions re-resolve their paths.
            InputActionSet.RefreshEnabledActions();

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Added);
        }

        public InputDevice AddDevice(InputDeviceDescription description)
        {
            var template = TryFindMatchingTemplate(description);
            if (template == null)
                throw new ArgumentException("Cannot find template matching device description", nameof(description));

            var device = AddDevice(template, description.product);
            device.m_Description = description;

            return device;
        }

        public void RemoveDevice(InputDevice device)
        {
            //need to make sure that all actions rescan their source paths
            throw new NotImplementedException();
        }

        public InputDevice TryGetDevice(string nameOrTemplate)
        {
            if (string.IsNullOrEmpty(nameOrTemplate))
                throw new ArgumentException(nameof(nameOrTemplate));

            if (m_Devices == null)
                return null;

            var nameOrTemplateLowerCase = nameOrTemplate.ToLower();

            for (var i = 0; i < m_Devices.Length; ++i)
            {
                var device = m_Devices[i];
                if (device.name.ToLower() == nameOrTemplateLowerCase ||
                    device.template.ToLower() == nameOrTemplateLowerCase)
                    return device;
            }

            return null;
        }

        public InputDevice GetDevice(string nameOrTemplate)
        {
            var device = TryGetDevice(nameOrTemplate);
            if (device == null)
                throw new Exception($"Cannot find device with name or template '{nameOrTemplate}'");

            return device;
        }

        public InputDevice TryGetDeviceById(int id)
        {
            InputDevice result;
            if (m_DevicesById.TryGetValue(id, out result))
                return result;
            return null;
        }

        // Adds any device that's been reported to the system but could not be matched to
        // a template to the given list.
        public int GetUnrecognizedDevices(List<InputDeviceDescription> descriptions)
        {
            if (descriptions == null)
                throw new ArgumentNullException(nameof(descriptions));

            var numFound = 0;
            for (var i = 0; i < m_AvailableDevices.Count; ++i)
            {
                if (TryGetDeviceById(m_AvailableDevices[i].deviceId) != null)
                    continue;

                descriptions.Add(m_AvailableDevices[i].description);
                ++numFound;
            }

            return numFound;
        }

        // Report the availability of a device. The system will try to find a template that matches
        // the device and instantiate it. If no template matches but a template is added some time
        // in the future, the device will be created when the template becomes available.
        public void ReportAvailableDevice(InputDeviceDescription description)
        {
            if (string.IsNullOrEmpty(description.product) && string.IsNullOrEmpty(description.manufacturer) &&
                string.IsNullOrEmpty(description.deviceClass))
                throw new ArgumentException(
                    "Description must have at least one of 'product', 'manufacturer', or 'deviceClass'",
                    nameof(description));

            var deviceId = NativeInputSystem.AllocateDeviceId();
            ReportAvailableDevice(description, deviceId);
        }

        private void ReportAvailableDevice(InputDeviceDescription description, int deviceId)
        {
            // Remember it.
            m_AvailableDevices.Add(new AvailableDevice
            {
                description = description,
                deviceId = deviceId
            });

            // Try to turn it into a device instance.
            var template = TryFindMatchingTemplate(description);
            if (template != null)
            {
                AddDevice(template, deviceId, description);
            }
        }

        public void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            // Don't bother keeping the data on the managed side. Just stuff the raw data directly
            // into the native buffers. This also means this method is thread-safe.
            NativeInputSystem.SendInput(ref inputEvent);
        }

        public void Update()
        {
            Update(m_CurrentUpdate);
        }

        public void Update(InputUpdateType updateType)
        {
            if ((updateType & InputUpdateType.Dynamic) == InputUpdateType.Dynamic)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Dynamic);
            }
            if ((updateType & InputUpdateType.Fixed) == InputUpdateType.Fixed)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Fixed);
            }
            if ((updateType & InputUpdateType.BeforeRender) == InputUpdateType.BeforeRender)
            {
                NativeInputSystem.Update(NativeInputUpdateType.BeforeRender);
            }
#if UNITY_EDITOR
            if ((updateType & InputUpdateType.Editor) == InputUpdateType.Editor)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Editor);
            }
#endif
        }

        internal void Initialize()
        {
            m_TemplateTypes = new Dictionary<string, Type>();
            m_TemplateStrings = new Dictionary<string, string>();
            m_BaseTemplateTable = new Dictionary<string, string>();
            m_SupportedDevices = new List<SupportedDevice>();
            m_Processors = new Dictionary<string, Type>();
            m_Modifiers = new Dictionary<string, Type>();
            m_DevicesById = new Dictionary<int, InputDevice>();
            m_AvailableDevices = new List<AvailableDevice>();

            // Determine our default set of enabled update types. By
            // default we enable both fixed and dynamic update because
            // we don't know which one the user is going to use. The user
            // can manually turn off one of them to optimize operation.
            m_UpdateMask = InputUpdateType.Dynamic | InputUpdateType.Fixed;
#if UNITY_EDITOR
            m_UpdateMask |= InputUpdateType.Editor;
#endif
            m_CurrentUpdate = InputUpdateType.Dynamic;

            // Register templates.
            RegisterTemplate("Button", typeof(ButtonControl)); // Inputs.
            RegisterTemplate("Axis", typeof(AxisControl));
            RegisterTemplate("Analog", typeof(AxisControl));
            RegisterTemplate("Digital", typeof(DiscreteControl));
            RegisterTemplate("Vector2", typeof(Vector2Control));
            RegisterTemplate("Vector3", typeof(Vector3Control));
            RegisterTemplate("Magnitude2", typeof(Magnitude2Control));
            RegisterTemplate("Magnitude3", typeof(Magnitude3Control));
            RegisterTemplate("Quaternion", typeof(QuaternionControl));
            RegisterTemplate("Pose", typeof(PoseControl));
            RegisterTemplate("Stick", typeof(StickControl));
            RegisterTemplate("Dpad", typeof(DpadControl));

            RegisterTemplate("Motor", typeof(MotorControl)); // Outputs.

            RegisterTemplate("Gamepad", typeof(Gamepad)); // Devices.
            RegisterTemplate("Keyboard", typeof(Keyboard));
            RegisterTemplate("Mouse", typeof(Mouse));
            RegisterTemplate("Touchscreen", typeof(Touchscreen));
            RegisterTemplate("HMD", typeof(HMD));
            RegisterTemplate("XRController", typeof(XRController));

            ////REVIEW: #if templates to the platforms they make sense on?

            // Register processors.
            RegisterProcessor("Invert", typeof(InvertProcessor));
            RegisterProcessor("Clamp", typeof(ClampProcessor));
            RegisterProcessor("Normalize", typeof(NormalizeProcessor));
            RegisterProcessor("Deadzone", typeof(DeadzoneProcessor));
            RegisterProcessor("Curve", typeof(CurveProcessor));

            // Register action modifiers.
            RegisterModifier("Hold", typeof(HoldModifier));
            RegisterModifier("Tap", typeof(TapModifier));
            RegisterModifier("SlowTap", typeof(SlowTapModifier));
            RegisterModifier("DoubleTap", typeof(DoubleTapModifier));
            RegisterModifier("Swipe", typeof(SwipeModifier));

            BuiltinDeviceTemplates.RegisterTemplates(this);

            InstallGlobals();
        }

        internal void Destroy()
        {
            if (ReferenceEquals(InputTemplate.s_TemplateTypes, m_TemplateTypes))
                InputTemplate.s_TemplateTypes = null;
            if (ReferenceEquals(InputTemplate.s_TemplateStrings, m_TemplateStrings))
                InputTemplate.s_TemplateStrings = null;
            if (ReferenceEquals(InputTemplate.s_BaseTemplateTable, m_BaseTemplateTable))
                InputTemplate.s_BaseTemplateTable = null;
            if (ReferenceEquals(InputProcessor.s_Processors, m_Processors))
                InputProcessor.s_Processors = null;

            NativeInputSystem.onUpdate = null;
            NativeInputSystem.onDeviceDiscovered = null;
        }

        // Revive after domain reload.
        internal void InstallGlobals()
        {
            InputTemplate.s_TemplateTypes = m_TemplateTypes;
            InputTemplate.s_TemplateStrings = m_TemplateStrings;
            InputTemplate.s_BaseTemplateTable = m_BaseTemplateTable;
            InputProcessor.s_Processors = m_Processors;

            NativeInputSystem.onUpdate = OnNativeUpdate;
            NativeInputSystem.onDeviceDiscovered = OnNativeDeviceDiscovered;
        }

        // Bundles a template name and a device description.
        [Serializable]
        internal struct SupportedDevice
        {
            public InputDeviceDescription description;
            public string template;
        }

        [Serializable]
        internal struct AvailableDevice
        {
            public InputDeviceDescription description;
            public int deviceId;
        }

        internal int m_TemplateSetupVersion;
        private Dictionary<string, Type> m_TemplateTypes;
        private Dictionary<string, string> m_TemplateStrings;
        private Dictionary<string, string> m_BaseTemplateTable; // Maps a template name to its base template name.
        private Dictionary<string, Type> m_Processors;
        private Dictionary<string, Type> m_Modifiers;

        private List<SupportedDevice> m_SupportedDevices; // A record of all device descriptions found in templates.
        private List<AvailableDevice> m_AvailableDevices; // A record of all devices reported to the system (from native or user code).

        private InputDevice[] m_Devices;
        private Dictionary<int, InputDevice> m_DevicesById;

        private InputUpdateType m_CurrentUpdate;
        private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        private InputStateBuffers m_StateBuffers;

        private int m_CurrentDynamicUpdateCount;
        private int m_CurrentFixedUpdateCount;

        // We don't use UnityEvents and thus don't persist the callbacks during domain reloads.
        // Restoration of UnityActions is unreliable and it's too easy to end up with double
        // registrations what will lead to all kinds of misbehavior.
        private InlinedArray<DeviceChangeListener> m_DeviceChangeListeners;
        private InlinedArray<EventListener> m_EventListeners;

        ////REVIEW: Right now actions are pretty tightly tied into the system; should this be opened up more
        ////        to present mechanisms that the user could build different action systems on?

        // Maps a single control to an action interested in the control. If
        // multiple actions are interested in the same control, we will end up
        // processing the control repeatedly but we assume this is the exception
        // and so optimize for the case where there's only one action going to
        // a control.
        //
        // Split into two structures to keep data needed only when there is an
        // actual value change out of the data we need for doing the scanning.
        private struct StateChangeMonitorMemoryRegion
        {
            public uint offsetRelativeToDevice;
            public uint sizeInBits; // Size of memory region to compare.
            public uint bitOffset;
        }
        private struct StateChangeMonitorListener
        {
            public InputControl control;
            ////REVIEW: this could easily be generalized to take an arbitrary user object plus a "user data" value
            public InputAction action;
            public int bindingIndex;
        }

        // Indices correspond with those in m_Devices.
        private List<StateChangeMonitorMemoryRegion>[] m_StateChangeMonitorMemoryRegions;
        private List<StateChangeMonitorListener>[] m_StateChangeMonitorListeners;
        private List<bool>[] m_StateChangeSignalled; ////TODO: make bitfield

        private struct ActionTimeout
        {
            public double time;
            public InputAction action;
            public IInputActionModifier modifier;
        }

        private List<ActionTimeout> m_ActionTimeouts;

        ////TODO: support combining monitors for bitfields
        internal void AddStateChangeMonitor(InputControl control, InputAction action, int bindingIndex)
        {
            var device = control.device;
            Debug.Assert(device != null);

            var deviceIndex = device.m_DeviceIndex;

            // Allocate/reallocate monitor arrays, if necessary.
            if (m_StateChangeMonitorListeners == null)
            {
                var deviceCount = m_Devices.Length;
                m_StateChangeMonitorListeners = new List<StateChangeMonitorListener>[deviceCount];
                m_StateChangeMonitorMemoryRegions = new List<StateChangeMonitorMemoryRegion>[deviceCount];
                m_StateChangeSignalled = new List<bool>[deviceCount];
            }
            else if (m_StateChangeMonitorListeners.Length <= deviceIndex)
            {
                var deviceCount = m_Devices.Length;
                Array.Resize(ref m_StateChangeMonitorListeners, deviceCount);
                Array.Resize(ref m_StateChangeMonitorMemoryRegions, deviceCount);
                Array.Resize(ref m_StateChangeSignalled, deviceCount);
            }

            // Allocate lists, if necessary.
            var listeners = m_StateChangeMonitorListeners[deviceIndex];
            var memoryRegions = m_StateChangeMonitorMemoryRegions[deviceIndex];
            var signals = m_StateChangeSignalled[deviceIndex];
            if (listeners == null)
            {
                listeners = new List<StateChangeMonitorListener>();
                memoryRegions = new List<StateChangeMonitorMemoryRegion>();
                signals = new List<bool>();

                m_StateChangeMonitorListeners[deviceIndex] = listeners;
                m_StateChangeMonitorMemoryRegions[deviceIndex] = memoryRegions;
                m_StateChangeSignalled[deviceIndex] = signals;
            }

            // Add monitor.
            listeners.Add(new StateChangeMonitorListener {action = action, bindingIndex = bindingIndex, control = control});
            memoryRegions.Add(new StateChangeMonitorMemoryRegion
            {
                offsetRelativeToDevice = control.stateBlock.byteOffset - control.device.stateBlock.byteOffset,
                sizeInBits = control.stateBlock.sizeInBits,
                bitOffset = control.stateBlock.bitOffset
            });
            signals.Add(false);
        }

        internal void RemoveStateChangeMonitor(InputControl control, InputAction action)
        {
            throw new NotImplementedException();
        }

        internal void AddActionTimeout(InputAction action, double time, IInputActionModifier modifier)
        {
            if (m_ActionTimeouts == null)
                m_ActionTimeouts = new List<ActionTimeout>();

            m_ActionTimeouts.Add(new ActionTimeout
            {
                time = time,
                action = action,
                modifier = modifier
            });
        }

        private void MakeDeviceNameUnique(InputDevice device)
        {
            if (m_Devices == null)
                return;

            var name = device.name;
            var nameLowerCase = name.ToLower();
            var nameIsUnique = false;
            var namesTried = 0;

            while (!nameIsUnique)
            {
                nameIsUnique = true;
                for (var i = 0; i < m_Devices.Length; ++i)
                {
                    if (m_Devices[i].name.ToLower() == nameLowerCase)
                    {
                        ++namesTried;
                        name = $"{device.name}{namesTried}";
                        nameLowerCase = name.ToLower();
                        nameIsUnique = false;
                        break;
                    }
                }
            }

            device.m_Name = name;
        }

        private void AssignUniqueDeviceId(InputDevice device)
        {
            // If the device already has an ID, make sure it's unique.
            if (device.id != InputDevice.kInvalidDeviceId)
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
                device.m_Id = NativeInputSystem.AllocateDeviceId();
            }
        }

        // (Re)allocates state buffers and assigns each device that's been added
        // a segment of the buffer. Preserves the current state of devices.
        private void ReallocateStateBuffers()
        {
            var devices = m_Devices;
            var oldBuffers = m_StateBuffers;

            // Allocate new buffers.
            var newBuffers = new InputStateBuffers();
            var newStateBlockOffsets = newBuffers.AllocateAll(m_UpdateMask, devices);

            ////TODO: this code will have to be extended when we allow device removals
            // Migrate state.
            newBuffers.MigrateAll(devices, newStateBlockOffsets, oldBuffers, null);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            m_StateBuffers.SwitchTo(m_CurrentUpdate);

            ////TODO: need to update state change monitors
        }

        private void OnNativeDeviceDiscovered(NativeInputDeviceInfo deviceInfo)
        {
            // Parse description.
            var description = InputDeviceDescription.FromJson(deviceInfo.deviceDescriptor);

            // Report it.
            ReportAvailableDevice(description, deviceInfo.deviceId);
        }

        // When we have the C# job system, this should be a job and NativeInputSystem should double
        // buffer input between frames. On top, the state change detection in here can be further
        // split off and put in its own job(s) (might not yield a gain; might be enough to just have
        // this thing in a job). The system can easily sync on a fence when some control goes
        // to the global state buffers so the user won't ever know that updates happen in the background.
        private unsafe void OnNativeUpdate(NativeInputUpdateType updateType, int eventCount, IntPtr eventData)
        {
            ////TODO: have new native callback that is triggered right *before* updates and allows managed devices
            ////      to flush their state into the native event queue

#if ENABLE_PROFILER
            Profiler.BeginSample("InputUpdate");
            try
            {
#endif

            if (m_ActionTimeouts != null)
                ProcessActionTimeouts();

            ////REVIEW: this will become obsolete when we actually turn off unneeded updates in native
            // We *always* have to process events into the current state even if the given update isn't enabled.
            // This is because the current state is for all updates and reflects the most up-to-date device states.

            m_StateBuffers.SwitchTo((InputUpdateType)updateType);

            if (eventCount <= 0)
                return;

            var isBeforeRenderUpdate = false;
            if (updateType == NativeInputUpdateType.Dynamic)
                ++m_CurrentDynamicUpdateCount;
            else if (updateType == NativeInputUpdateType.Fixed)
                ++m_CurrentFixedUpdateCount;
            else if (updateType == NativeInputUpdateType.BeforeRender)
                isBeforeRenderUpdate = true;

            // Before render updates work in a special way. For them, we only want specific devices (and
            // sometimes even just specific controls on those devices) to be updated. What native will do is
            // it will *not* clear the event buffer after showing it to us. This means that in the next
            // normal update, we will see the same events again. This gives us a chance to only fish out
            // what we want.
            //
            // In before render updates, we will only access StateEvents and DeltaEvents (the latter should
            // be used to, for example, *only* update tracking on a device that also contains buttons -- which
            // should not get updated in berfore render).

            var currentEventPtr = (InputEvent*)eventData;
            var remainingEventCount = eventCount;

            // Handle events.
            while (remainingEventCount > 0)
            {
                InputDevice device = null;

                // Bump firstEvent up to the next unhandled event (in before-render updates
                // the event needs to be *both* unhandled *and* for a device with before
                // render updates enabled).
                while (remainingEventCount > 0)
                {
                    if (isBeforeRenderUpdate)
                    {
                        if (!currentEventPtr->handled)
                        {
                            device = TryGetDeviceById(currentEventPtr->deviceId);
                            if (device != null && device.updateBeforeRender)
                                break;
                        }
                    }
                    else if (!currentEventPtr->handled)
                        break;

                    currentEventPtr = InputEvent.GetNextInMemory(currentEventPtr);
                    --remainingEventCount;
                }
                if (remainingEventCount == 0)
                    break;

                // Give listeners a shot at the event.
                var listenerCount = m_EventListeners.Count;
                if (listenerCount > 0)
                {
                    for (var i = 0; i < listenerCount; ++i)
                        m_EventListeners[i](new InputEventPtr(currentEventPtr));
                    if (currentEventPtr->handled)
                        continue;
                }

                // Grab device for event. In before-render updates, we already had to
                // check the device.
                if (!isBeforeRenderUpdate)
                    device = TryGetDeviceById(currentEventPtr->deviceId);
                if (device == null)
                {
                    // No device found matching event. Consider it handled.
                    currentEventPtr->handled = true;
                    continue;
                }

                // Process.
                var currentEventType = currentEventPtr->type;
                var currentEventTime = currentEventPtr->time;
                switch (currentEventType)
                {
                    case StateEvent.Type:

                        // Ignore the event if the last state update we received for the device was
                        // newer than this state event is.
                        if (currentEventTime < device.m_LastUpdateTime)
                            break;

                        var stateEventPtr = (StateEvent*)currentEventPtr;
                        var stateType = stateEventPtr->stateFormat;
                        var stateBlock = device.m_StateBlock;
                        var stateSize = stateEventPtr->stateSizeInBytes;
                        var statePtr = stateEventPtr->state;

                        // Update state on device.
                        if (stateBlock.format == stateType &&
                            stateBlock.alignedSizeInBytes >= stateSize) // Allow device state to have unused control at end.
                        {
                            var deviceIndex = device.m_DeviceIndex;
                            var stateOffset = device.m_StateBlock.byteOffset;

                            // Before we update state, let change monitors compare the old and the new state.
                            // We do this instead of first updating the front buffer and then comparing to the
                            // back buffer as that would require a buffer flip for each state change in order
                            // for the monitors to work reliably. By comparing the *event* data to the current
                            // state, we can have multiple state events in the same frame yet still get reliable
                            // change notifications.
                            var haveSignalledMonitors =
                                ProcessStateChangeMonitors(device.m_DeviceIndex, currentEventTime, statePtr,
                                    InputStateBuffers.GetFrontBuffer(deviceIndex), stateSize);

                            // Buffer flip.
                            FlipBuffersForDeviceIfNecessary(device, updateType);

                            // Now write the state.
                            switch (updateType)
                            {
#if UNITY_EDITOR
                                case NativeInputUpdateType.Editor:
                                {
                                    var buffer =
                                        new IntPtr(
                                            m_StateBuffers.m_EditorUpdateBuffers.GetFrontBuffer(deviceIndex));
                                    Debug.Assert(buffer != IntPtr.Zero);
                                    UnsafeUtility.MemCpy(buffer + (int)stateOffset, statePtr, stateSize);
                                }
                                break;
#endif

                                // For dynamic and fixed updates, we have to write into the front buffer
                                // of both updates as a state change event comes in only once and we have
                                // to reflect the most current state in both update types.
                                //
                                // If one or the other update is disabled, however, we will perform a single
                                // memcpy here.
                                case NativeInputUpdateType.BeforeRender:
                                case NativeInputUpdateType.Dynamic:
                                case NativeInputUpdateType.Fixed:
                                    if (m_StateBuffers.m_DynamicUpdateBuffers.valid)
                                    {
                                        var buffer =
                                            new IntPtr(
                                                m_StateBuffers.m_DynamicUpdateBuffers.GetFrontBuffer(deviceIndex));
                                        Debug.Assert(buffer != IntPtr.Zero);
                                        UnsafeUtility.MemCpy(buffer + (int)stateOffset, statePtr, stateSize);
                                    }
                                    if (m_StateBuffers.m_FixedUpdateBuffers.valid)
                                    {
                                        var buffer =
                                            new IntPtr(
                                                m_StateBuffers.m_FixedUpdateBuffers.GetFrontBuffer(deviceIndex));
                                        Debug.Assert(buffer != IntPtr.Zero);
                                        UnsafeUtility.MemCpy(buffer + (int)stateOffset, statePtr, stateSize);
                                    }
                                    break;
                            }

                            ++device.m_StateChangeCount; ////REVIEW: is this really useful?
                            device.m_LastUpdateTime = currentEventTime;

                            // Now that we've committed the new state to memory, if any of the change
                            // monitors fired, let the associated actions know.
                            if (haveSignalledMonitors)
                                FireActionStateChangeNotifications(deviceIndex, currentEventTime);
                        }

                        break;

                    case ConnectEvent.Type:
                        if (!device.connected)
                        {
                            device.m_Flags |= InputDevice.Flags.Connected;
                            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                                m_DeviceChangeListeners[i](device, InputDeviceChange.Connected);
                        }
                        break;

                    case DisconnectEvent.Type:
                        if (device.connected)
                        {
                            device.m_Flags &= ~InputDevice.Flags.Connected;
                            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                                m_DeviceChangeListeners[i](device, InputDeviceChange.Disconnected);
                        }
                        break;
                }

                // Mark as processed.
                currentEventPtr->handled = true;
                if (remainingEventCount >= 1)
                {
                    currentEventPtr = InputEvent.GetNextInMemory(currentEventPtr);
                    --remainingEventCount;
                }

                // Device received event so make it current.
                device.MakeCurrent();
            }

            ////TODO: fire event that allows code to update state *from* state we just updated

#if ENABLE_PROFILER
        }

        finally
        {
            Profiler.EndSample();
        }
#endif
        }

        // If anyone is listening for state changes on the given device, run state change detections
        // for the two given state blocks of the device. If a value that is covered by a monitor
        // has changed in 'newState' compared to 'oldState', set m_StateChangeSignalled for the
        // monitor to true.
        //
        // Returns true if any monitors got signalled, false otherwise.
        //
        // This could easily be spun off into jobs.
        private bool ProcessStateChangeMonitors(int deviceIndex, double time, IntPtr newState, IntPtr oldState, int stateSize)
        {
            if (m_StateChangeMonitorListeners == null)
                return false;

            // We resize the monitor arrays only when someone adds to them so they
            // may be out of sync with the size of m_Devices.
            if (deviceIndex >= m_StateChangeMonitorListeners.Length)
                return false;

            var changeMonitors = m_StateChangeMonitorMemoryRegions[deviceIndex];
            if (changeMonitors == null)
                return false; // No action cares about state changes on this device.

            var signals = m_StateChangeSignalled[deviceIndex];

            var numMonitors = changeMonitors.Count;
            var signalled = false;

            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = changeMonitors[i];
                var offset = (int)memoryRegion.offsetRelativeToDevice;
                var sizeInBits = memoryRegion.sizeInBits;
                var bitOffset = memoryRegion.bitOffset;

                // See if we are comparing bits or bytes.
                if (sizeInBits % 8 != 0 || bitOffset != 0)
                {
                    // Not-so-simple path: compare bits.

                    if (sizeInBits > 1)
                        throw new NotImplementedException("state change detection on multi-bit fields");

                    if (BitfieldHelpers.ComputeFollowingByteOffset((uint)offset, bitOffset) > stateSize)
                        continue;

                    if (BitfieldHelpers.ReadSingleBit(newState + offset, bitOffset) ==
                        BitfieldHelpers.ReadSingleBit(oldState + offset, bitOffset))
                        continue;
                }
                else
                {
                    // Simple path: compare whole bytes.

                    var sizeInBytes = sizeInBits / 8;
                    if (offset + sizeInBytes > stateSize)
                        continue;

                    if (UnsafeUtility.MemCmp(newState + offset, oldState + offset, (int)sizeInBytes) == 0)
                        continue;
                }

                signals[i] = true;
                signalled = true;
            }

            return signalled;
        }

        private void FireActionStateChangeNotifications(int deviceIndex, double time)
        {
            var signals = m_StateChangeSignalled[deviceIndex];
            var listeners = m_StateChangeMonitorListeners[deviceIndex];

            for (var i = 0; i < signals.Count; ++i)
            {
                if (signals[i])
                {
                    var listener = listeners[i];
                    listener.action.NotifyControlValueChanged(listener.control, listener.bindingIndex, time);
                    signals[i] = false;
                }
            }
        }

        private void ProcessActionTimeouts()
        {
            var time = Time.time;
            for (var i = 0; i < m_ActionTimeouts.Count; ++i)
                if (m_ActionTimeouts[i].time <= time)
                {
                    m_ActionTimeouts[i].action.NotifyTimerExpired(m_ActionTimeouts[i].modifier, time);
                    m_ActionTimeouts.RemoveAt(i);
                }
        }

        private void FlipBuffersForDeviceIfNecessary(InputDevice device, NativeInputUpdateType updateType)
        {
            if (updateType == NativeInputUpdateType.BeforeRender)
            {
                // We never flip buffers for before render. Instead, we already write
                // into the front buffer.
            }

#if UNITY_EDITOR
            else if (updateType == NativeInputUpdateType.Editor)
            {
                // The editor doesn't really have a concept of frame-to-frame operation the
                // same way the player does. So we simply flip buffers on a device whenever
                // a new state event for it comes in.
                m_StateBuffers.m_EditorUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
            }
#endif

            // See if this is the first fixed update this frame. If so, we flip both
            // dynamic and fixed buffers if we haven't already for the device.
            else if (updateType == NativeInputUpdateType.Fixed &&
                     m_CurrentFixedUpdateCount == m_CurrentDynamicUpdateCount - 1 &&
                     device.m_LastFixedUpdate != m_CurrentFixedUpdateCount)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);

                device.m_LastDynamicUpdate = m_CurrentDynamicUpdateCount;
                device.m_LastFixedUpdate = m_CurrentFixedUpdateCount;
            }
            // If it's a dynamic update, flip only if we haven't already in a fixed
            // update. And only if dynamic updates are enabled. Flip only dynamic buffers.
            else if (updateType == NativeInputUpdateType.Dynamic &&
                     device.m_LastDynamicUpdate != m_CurrentDynamicUpdateCount)
            {
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_LastDynamicUpdate = m_CurrentDynamicUpdateCount;
            }
            // Same for fixed updates.
            else if (updateType == NativeInputUpdateType.Fixed &&
                     device.m_LastFixedUpdate != m_CurrentFixedUpdateCount)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_LastFixedUpdate = m_CurrentFixedUpdateCount;
            }

            // Don't flip.
        }

        // Domain reload survival logic.
#if UNITY_EDITOR
        [Serializable]
        internal struct DeviceState
        {
            // Preserving InputDevices is somewhat tricky business. Serializing
            // them in full would involve pretty nasty work. We have the restriction,
            // however, that everything needs to be created from templates (it partly
            // exists for the sake of reload survivability), so we should be able to
            // just go and recreate the device from the template. This also has the
            // advantage that if the template changes between reloads, the change
            // automatically takes effect.
            public string name;
            public string template;
            public int deviceId;
            public uint stateOffset;
            public InputDevice.Flags flags;
            public InputDeviceDescription description;
        }

        [Serializable]
        internal struct TemplateState
        {
            public string name;
            public string typeNameOrJson;
        }

        [Serializable]
        internal struct TypeRegistrationState
        {
            public string name;
            public string typeName;

            public static TypeRegistrationState[] SaveState(Dictionary<string, Type> table)
            {
                var count = table.Count;
                var array = new TypeRegistrationState[count];

                var i = 0;
                foreach (var entry in table)
                    array[i++] = new TypeRegistrationState
                    {
                        name = entry.Key,
                        typeName = entry.Value.AssemblyQualifiedName
                    };

                return array;
            }
        }

        [Serializable]
        internal struct SerializedState
        {
            public int templateSetupVersion;
            public TemplateState[] templateTypes;
            public TemplateState[] templateStrings;
            public KeyValuePair<string, string>[] baseTemplates;
            public TypeRegistrationState[] processors;
            public TypeRegistrationState[] modifiers;
            public SupportedDevice[] supportedDevices;
            public DeviceState[] devices;
            public AvailableDevice[] availableDevices;
            public InputStateBuffers buffers;
            public InputConfiguration.SerializedState configuration;

            // We want to preserve the event listeners across Save() and Restore() but not
            // across domain reloads. So we put them in here but don't serialize them (and
            // can't either except if we make them UnityEvents).
            [NonSerialized] public InlinedArray<DeviceChangeListener> deviceChangeListeners;
            [NonSerialized] public InlinedArray<EventListener> eventListeners;
        }

        internal SerializedState SaveState()
        {
            // Template types.
            var templateTypeCount = m_TemplateTypes.Count;
            var templateTypeArray = new TemplateState[templateTypeCount];

            var i = 0;
            foreach (var entry in m_TemplateTypes)
                templateTypeArray[i++] = new TemplateState
                {
                    name = entry.Key,
                    typeNameOrJson = entry.Value.AssemblyQualifiedName
                };

            // Template strings.
            var templateStringCount = m_TemplateStrings.Count;
            var templateStringArray = new TemplateState[templateStringCount];

            i = 0;
            foreach (var entry in m_TemplateStrings)
                templateStringArray[i++] = new TemplateState
                {
                    name = entry.Key,
                    typeNameOrJson = entry.Value
                };

            // Devices.
            var deviceCount = m_Devices?.Length ?? 0;
            var deviceArray = new DeviceState[deviceCount];
            for (i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                var deviceState = new DeviceState
                {
                    name = device.name,
                    template = device.template,
                    deviceId = device.id,
                    stateOffset = device.m_StateBlock.byteOffset,
                    description = device.m_Description,
                    flags = device.m_Flags
                };
                deviceArray[i] = deviceState;
            }

            return new SerializedState
            {
                templateSetupVersion = m_TemplateSetupVersion,
                templateTypes = templateTypeArray,
                templateStrings = templateStringArray,
                baseTemplates = m_BaseTemplateTable.ToArray(),
                processors = TypeRegistrationState.SaveState(m_Processors),
                modifiers = TypeRegistrationState.SaveState(m_Modifiers),
                supportedDevices = m_SupportedDevices.ToArray(),
                devices = deviceArray,
                availableDevices = m_AvailableDevices.ToArray(),
                buffers = m_StateBuffers,
                configuration = InputConfiguration.Save(),
                deviceChangeListeners = m_DeviceChangeListeners.Clone(),
                eventListeners = m_EventListeners.Clone()
            };

            // We don't bring monitors along. InputActions and related classes are equipped
            // with their own domain reload survival logic that will plug actions back into
            // the system after reloads -- *if* the user is serializing them as part of
            // MonoBehaviours/ScriptableObjects.
        }

        internal void RestoreState(SerializedState state)
        {
            m_TemplateTypes = new Dictionary<string, Type>();
            m_TemplateStrings = new Dictionary<string, string>();
            m_BaseTemplateTable = new Dictionary<string, string>();
            m_SupportedDevices = state.supportedDevices.ToList();
            m_Processors = new Dictionary<string, Type>();
            m_Modifiers = new Dictionary<string, Type>();
            m_StateBuffers = state.buffers;
            m_CurrentUpdate = InputUpdateType.Dynamic;
            m_DevicesById = new Dictionary<int, InputDevice>();
            m_AvailableDevices = state.availableDevices.ToList();
            m_Devices = null;
            m_TemplateSetupVersion = state.templateSetupVersion + 1;
            m_DeviceChangeListeners = state.deviceChangeListeners;
            m_EventListeners = state.eventListeners;

            // Configuration.
            InputConfiguration.Restore(state.configuration);

            ////TODO: protect against failing Type.GetType() calls by logging error and removing entry

            // Template types.
            foreach (var template in state.templateTypes)
                m_TemplateTypes[template.name] = Type.GetType(template.typeNameOrJson, true);
            InputTemplate.s_TemplateTypes = m_TemplateTypes;

            // Template strings.
            foreach (var template in state.templateStrings)
                m_TemplateStrings[template.name] = template.typeNameOrJson;
            InputTemplate.s_TemplateStrings = m_TemplateStrings;

            // Base templates.
            if (state.baseTemplates != null)
                foreach (var entry in state.baseTemplates)
                    m_BaseTemplateTable[entry.Key] = entry.Value;
            InputTemplate.s_BaseTemplateTable = m_BaseTemplateTable;

            // Processors.
            foreach (var processor in state.processors)
                m_Processors[processor.name] = Type.GetType(processor.typeName, true);
            InputProcessor.s_Processors = m_Processors;

            // Modifiers.
            foreach (var modifier in state.modifiers)
                m_Modifiers[modifier.name] = Type.GetType(modifier.typeName, true);

            // Refresh builtin templates.
            BuiltinDeviceTemplates.RegisterTemplates(this);

            // Re-create devices.
            var deviceCount = state.devices.Length;
            var devices = new InputDevice[deviceCount];
            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceState = state.devices[i];
                var setup = new InputControlSetup(deviceState.template);
                var device = setup.Finish();
                device.m_Name = deviceState.name;
                device.m_Id = deviceState.deviceId;
                device.m_DeviceIndex = i;
                device.m_Description = deviceState.description;
                device.m_Flags = deviceState.flags;
                device.BakeOffsetIntoStateBlockRecursive(deviceState.stateOffset);
                device.MakeCurrent();
                devices[i] = device;
                m_DevicesById[device.m_Id] = device;
            }
            m_Devices = devices;

            m_StateBuffers.FreeAll();
            ReallocateStateBuffers();
        }

        [SerializeField] private SerializedState m_SerializedState;

        // Stuff everything that we want to survive a domain reload into
        // a m_SerializedState.
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_SerializedState = SaveState();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RestoreState(m_SerializedState);
            m_SerializedState = default(SerializedState);
        }

#endif
    }
}
