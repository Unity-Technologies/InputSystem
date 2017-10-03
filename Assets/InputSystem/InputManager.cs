using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal.Input;

//native sends (full/partial) input templates for any new device

namespace ISX
{
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
        public ReadOnlyArray<InputDevice> devices
        {
            get { return new ReadOnlyArray<InputDevice>(m_Devices); }
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

            // All we do is enter the type into a map. We don't construct an InputTemplate
            // from it until we actually need it in an InputControlSetup to create a device.
            // This not only avoids us creating a bunch of objects on the managed heap but
            // also avoids us laboriously constructing a VRController template, for example,
            // in a game that never uses VR.
            m_TemplateTypes[name.ToLower()] = type;

            ////TODO: see if we need to reconstruct any input device
        }

        // Add a template constructed from a JSON string.
        public void RegisterTemplate(string json, string name = null)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException(nameof(json));

            if (string.IsNullOrEmpty(name))
            {
                name = InputTemplate.ParseNameFromJson(json);
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException($"Template name has not been given and is not set in JSON template", nameof(name));
            }

            m_TemplateStrings[name.ToLower()] = json;
        }

        public void RegisterProcessor(string name, Type type)
        {
        }

        public void RegisterUsage(string name, string type, params string[] processors)
        {
        }

        // Processes a path specification that may match more than a single control.
        // Adds all controls that match to the given list.
        // Returns true if at least one control was matched.
        // Must not generate garbage!
        public bool TryGetControls(string path, List<InputControl> controls)
        {
            throw new NotImplementedException();
        }

        // Must not generate garbage!
        public InputControl TryGetControl(string path)
        {
            throw new NotImplementedException();
        }

        public InputControl GetControl(string path)
        {
            throw new NotImplementedException();
        }

        // Creates a device from the given template and adds it to the system.
        // NOTE: Creates garbage.
        public InputDevice AddDevice(string template)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentException(nameof(template));

            var setup = new InputControlSetup(template);
            var device = setup.Finish();

            AddDevice(device);

            return device;
        }

        public void AddDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.template == null)
                throw new ArgumentException("Device has no associated template", nameof(device));

            // Ignore if the same device gets added multiple times.
            if (ArrayHelpers.Contains(m_Devices, device))
                return;

            MakeDeviceNameUnique(device);
            AssignUniqueDeviceId(device);

            ArrayHelpers.Append(ref m_Devices, device);

            ////REVIEW: Not sure a full-blown dictionary is the right way here. Alternatives are to keep
            ////        a sparse array that directly indices using the linearly increasing IDs (though that
            ////        may get large over time). Or to just do a linear search through m_Devices (but
            ////        that may end up tapping a bunch of memory locations in the heap to find the right
            ////        device; could be improved by sorting m_Devices by ID and picking a good starting
            ////        point based on the ID we have instead of searching from [0] always).
            m_DevicesById[device.id] = device;

            ReallocateStateBuffers();
        }

        public InputDevice AddDevice(InputDeviceDescriptor descriptor)
        {
            throw new NotImplementedException();
        }

        public InputDevice TryGetDeviceById(int id)
        {
            InputDevice result;
            if (m_DevicesById.TryGetValue(id, out result))
                return result;
            return null;
        }

        public void QueueEvent<TEvent>(TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            // Don't bother keeping the data on the managed side. Just stuff the raw data directly
            // into the native buffers. This also means this method is thread-safe.
            NativeInputSystem.SendInput(inputEvent);
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
            m_Usages = new Dictionary<string, InputUsage>();
            m_TemplateTypes = new Dictionary<string, Type>();
            m_TemplateStrings = new Dictionary<string, string>();
            m_Processors = new Dictionary<string, Type>();
            m_DevicesById = new Dictionary<int, InputDevice>();

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

            ////REVIEW: #if templates to the platforms they make sense on?

            // Register processors.
            RegisterProcessor("Invert", typeof(InvertProcessor));
            RegisterProcessor("Clamp", typeof(ClampProcessor));
            RegisterProcessor("Normalize", typeof(NormalizeProcessor));
            RegisterProcessor("Deadzone", typeof(DeadzoneProcessor));
            RegisterProcessor("Curve", typeof(CurveProcessor));

            // Register usages.
            RegisterUsage("PrimaryStick", "Stick");
            RegisterUsage("SecondaryStick", "Stick");
            RegisterUsage("PrimaryAction", "Button");
            RegisterUsage("SecondaryAction", "Button");
            RegisterUsage("PrimaryTrigger", "Axis", "Normalized(0,1)");
            RegisterUsage("SecondaryTrigger", "Axis", "Normalized(0,1)");
            RegisterUsage("Back", "Button");
            RegisterUsage("Forward", "Button");
            RegisterUsage("Menu", "Button");
            RegisterUsage("Enter", "Button"); // Commit/confirm.
            RegisterUsage("Previous", "Button");
            RegisterUsage("Next", "Button");
            RegisterUsage("ScrollHorizontal", "Axis");
            RegisterUsage("ScrollVertical", "Axis");
            RegisterUsage("Pressure", "Axis", "Normalized(0,1)");
            RegisterUsage("Position", "Vector3");
            RegisterUsage("Orientation", "Quaternion");
            RegisterUsage("Point", "Vector2");

            RegisterUsage("LowFreqMotor", "Axis", "Normalized(0,1)");
            RegisterUsage("HighFreqMotor", "Axis", "Normalized(0,1)");

            RegisterUsage("LeftHand", "XRController");
            RegisterUsage("RightHand", "XRController");

            //InputUsage.s_Usages = m_Usages;
            InputTemplate.s_TemplateTypes = m_TemplateTypes;
            InputTemplate.s_TemplateStrings = m_TemplateStrings;

            NativeInputSystem.onUpdate += OnNativeUpdate;
        }

        internal void Destroy()
        {
            InputTemplate.s_TemplateTypes = null;
            InputTemplate.s_TemplateStrings = null;

            NativeInputSystem.onUpdate -= OnNativeUpdate;
        }

        private Dictionary<string, InputUsage> m_Usages; ////REVIEW: Array or dictionary?
        private Dictionary<string, Type> m_TemplateTypes;
        private Dictionary<string, string> m_TemplateStrings;
        private Dictionary<string, Type> m_Processors;

        private InputDevice[] m_Devices;
        private Dictionary<int, InputDevice> m_DevicesById;

        private InputUpdateType m_CurrentUpdate;
        private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        private InputStateBuffers m_StateBuffers;


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
                if (m_Devices != null)
                {
                    // Safety check to make sure out IDs are really unique.
                    // Given they are assigned by the native system they should be fine
                    // but let's make sure.
                    var deviceId = device.id;
                    for (var i = 0; i < m_Devices.Length; ++i)
                        if (m_Devices[i].id == deviceId)
                            throw new Exception(
                                $"Duplicate device ID {deviceId} detected for devices '{device.name}' and '{m_Devices[i].name}'");
                }
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

            // Migrate state.
            newBuffers.MigrateAll(devices, newStateBlockOffsets, oldBuffers);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            m_StateBuffers.SwitchTo(m_CurrentUpdate);
        }

        private unsafe void OnNativeUpdate(NativeInputUpdateType updateType, int eventCount, IntPtr eventData)
        {
            // We *always* have to process events into the current state even if the given update isn't enabled.
            // This is because the current state is for all updates and reflects the most up-to-date device states.
            // Where enabled-or-not comes is in terms of previous state allocation and processing.

            m_StateBuffers.SwapAndSwitchTo((InputUpdateType)updateType);

            if (eventCount <= 0)
                return;

            for (var i = 0; i < eventCount; ++i)
            {
                // Find next oldest event.
                var currentEventPtr = (InputEvent*)eventData;
                var oldestEventPtr = currentEventPtr;
                var oldestEventTime = oldestEventPtr->time;
                for (var n = 1; n < eventCount; ++n)
                {
                    var nextEventPtr = (InputEvent*)((byte*)currentEventPtr + currentEventPtr->sizeInBytes);

                    if (oldestEventTime < 0 || nextEventPtr->time < oldestEventTime)
                    {
                        oldestEventPtr = nextEventPtr;
                        oldestEventTime = oldestEventPtr->time;
                    }

                    currentEventPtr = nextEventPtr;
                }

                // Notify listeners.
                //for (var n = 0; n < m_EventListeners.Count; ++n)
                //m_EventListeners[n](new InputEventPtr(oldestEventPtr));

                if (oldestEventPtr->type == new FourCC())
                    continue;

                // Grab device for event.
                var device = TryGetDeviceById(oldestEventPtr->deviceId);
                if (device == null)
                    continue;

                // Process.
                switch (oldestEventPtr->type)
                {
                    case StateEvent.Type:
                        var stateEventPtr = (StateEvent*)oldestEventPtr;
                        var stateType = stateEventPtr->stateType;
                        var stateBlock = device.m_StateBlock;
                        var stateSize = stateEventPtr->stateSizeInBytes;
                        if (stateBlock.typeCode == stateType &&
                            stateBlock.alignedSizeInBytes == stateSize)
                        {
                            GamepadState gpad = new GamepadState();
                            IntPtr gpadPtr = UnsafeUtility.AddressOf(ref gpad);
                            UnsafeUtility.MemCpy(gpadPtr, stateEventPtr->state, stateSize);

                            UnsafeUtility.MemCpy(stateBlock.currentStatePtr, stateEventPtr->state, stateSize);
                        }
                        ////TODO: queue up state change notification (only if anyone is listening)
                        break;

                        /*
                    case ConnectEvent.Type:
                        if (device.connected)
                        {
                            device.connected = true;
                            NotifyListenersOfDeviceChange(device, InputDeviceChange.Connected);
                        }
                        break;
                        */
                }

                // Mark as processed by setting time to negative.
                oldestEventPtr->time = -1;
            }
        }

        // Domain reload survival logic.
#if UNITY_EDITOR
        [Serializable]
        internal struct DeviceState
        {
            // Preserving InputDevice is somewhat tricky business. Serializing
            // them in full would involve pretty nasty work. We have the restriction,
            // however, that everything needs to be created from templates (it partly
            // exists for the sake of reload survivability), so we should be able to
            // just go and recreate the device from the template. This also has the
            // advantage that if the template changes between reloads, the change
            // automatically takes effect.
            public string name;
            public string template;
            public int deviceId;
        }

        [Serializable]
        internal struct TemplateState
        {
            public string name;
            public string typeNameOrJson;
        }

        [Serializable]
        internal struct SerializedState
        {
            public InputUsage[] usages;
            public TemplateState[] templateTypes;
            public TemplateState[] templateStrings;
            public DeviceState[] devices;
            public InputStateBuffers buffers;
        }

        [SerializeField] private SerializedState m_SerializedState;

        // Stuff everything that we want to survive a domain reload into
        // a m_SerializedState.
        public void OnBeforeSerialize()
        {
            // Usages.
            var usageCount = m_Usages.Count;
            var usageArray = new InputUsage[usageCount];

            var i = 0;
            foreach (var usage in m_Usages.Values)
                usageArray[i++] = usage;

            // Template types.
            var templateTypeCount = m_TemplateTypes.Count;
            var templateTypeArray = new TemplateState[templateTypeCount];

            i = 0;
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
                templateTypeArray[i++] = new TemplateState
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
                    deviceId = device.id
                };
                deviceArray[i] = deviceState;
            }

            m_SerializedState = new SerializedState
            {
                usages = usageArray,
                templateTypes = templateTypeArray,
                templateStrings = templateStringArray,
                devices = deviceArray,
                buffers = m_StateBuffers
            };
        }

        public void OnAfterDeserialize()
        {
            m_Usages = new Dictionary<string, InputUsage>();
            m_TemplateTypes = new Dictionary<string, Type>();
            m_TemplateStrings = new Dictionary<string, string>();
            m_Processors = new Dictionary<string, Type>();
            m_StateBuffers = m_SerializedState.buffers;
            m_CurrentUpdate = InputUpdateType.Dynamic;

            // Usages.
            foreach (var usage in m_SerializedState.usages)
                m_Usages[usage.name.ToLower()] = usage;
            //InputUsage.s_Usages = m_Usages;

            // Template types.
            foreach (var template in m_SerializedState.templateTypes)
                m_TemplateTypes[template.name.ToLower()] = Type.GetType(template.typeNameOrJson, true);
            InputTemplate.s_TemplateTypes = m_TemplateTypes;

            // Template strings.
            foreach (var template in m_SerializedState.templateStrings)
                m_TemplateStrings[template.name.ToLower()] = template.typeNameOrJson;
            InputTemplate.s_TemplateStrings = m_TemplateStrings;

            // Re-create devices.
            var deviceCount = m_SerializedState.devices.Length;
            var devices = new InputDevice[deviceCount];
            for (var i = 0; i < deviceCount; ++i)
            {
                var state = m_SerializedState.devices[i];
                var setup = new InputControlSetup(state.template);
                var device = setup.Finish();
                device.m_Name = state.name;
                device.m_Id = state.deviceId;
                devices[i] = device;
            }
            m_Devices = devices;
            ReallocateStateBuffers();

            m_SerializedState = default(SerializedState);
        }

#endif
    }
}
