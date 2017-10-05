using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
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

        public event UnityAction<InputDevice, InputDeviceChange> onDeviceChange
        {
            add
            {
                if (m_DeviceChangeEvent == null)
                    m_DeviceChangeEvent = new DeviceChangeEvent();
                m_DeviceChangeEvent.AddListener(value);
            }
            remove
            {
                if (m_DeviceChangeEvent != null)
                    m_DeviceChangeEvent.RemoveListener(value);
            }
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

            ReallocateStateBuffers();

            // Make it current.
            device.MakeCurrent();

            // Notify listeners.
            if (m_DeviceChangeEvent != null)
                m_DeviceChangeEvent.Invoke(device, InputDeviceChange.Added);
        }

        public InputDevice AddDevice(InputDeviceDescriptor descriptor)
        {
            throw new NotImplementedException();
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
                if (device.name.ToLower() == nameOrTemplateLowerCase || device.template.ToLower() == nameOrTemplateLowerCase)
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
            RegisterTemplate("Mouse", typeof(Pointer));
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
            //RegisterModifier("Hold", typeof(HoldModifier));

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

        private Dictionary<string, Type> m_TemplateTypes;
        private Dictionary<string, string> m_TemplateStrings;
        private Dictionary<string, Type> m_Processors;

        private InputDevice[] m_Devices;
        private Dictionary<int, InputDevice> m_DevicesById;

        private InputUpdateType m_CurrentUpdate;
        private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        private InputStateBuffers m_StateBuffers;

        private DeviceChangeEvent m_DeviceChangeEvent;

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
            public uint offset;
            public uint sizeInBytes; // Size of memory region to compare. We don't care about bitfields and
                                     // may trigger false positives for them. Actions have to sort that out
                                     // on their own.
        }
        private struct StateChangeMonitorListener
        {
            public InputControl control;
            public InputAction action;
        }

        // Indices correspond with those in m_Devices.
        private List<StateChangeMonitorMemoryRegion>[] m_StateChangeMonitorMemoryRegions;
        private List<StateChangeMonitorListener>[] m_StateChangeMonitorListeners;

        internal void AddStateChangeMonitor(InputControl control, InputAction action)
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
            }
            else if (m_StateChangeMonitorListeners.Length <= deviceIndex)
            {
                var deviceCount = m_Devices.Length;
                Array.Resize(ref m_StateChangeMonitorListeners, deviceCount);
                Array.Resize(ref m_StateChangeMonitorMemoryRegions, deviceCount);
            }

            // Allocate lists, if necessary.
            var listeners = m_StateChangeMonitorListeners[deviceIndex];
            var memoryRegions = m_StateChangeMonitorMemoryRegions[deviceIndex];
            if (listeners == null)
            {
                listeners = new List<StateChangeMonitorListener>();
                memoryRegions = new List<StateChangeMonitorMemoryRegion>();

                m_StateChangeMonitorListeners[deviceIndex] = listeners;
                m_StateChangeMonitorMemoryRegions[deviceIndex] = memoryRegions;
            }

            // Add monitor.
            listeners.Add(new StateChangeMonitorListener {action = action, control = control});
            memoryRegions.Add(new StateChangeMonitorMemoryRegion
            {
                offset = control.stateBlock.byteOffset,
                sizeInBytes = (uint)control.stateBlock.alignedSizeInBytes
            });
        }

        internal void RemoveStateChangeMonitor(InputControl control, InputAction action)
        {
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

            // Migrate state.
            newBuffers.MigrateAll(devices, newStateBlockOffsets, oldBuffers);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            m_StateBuffers.SwitchTo(m_CurrentUpdate);

            ////TODO: need to update state change monitors
        }

        // When we have the C# job system, this should be a job and NativeInputSystem should double
        // buffer input between frames. On top, the state change detection in here can be further
        // split off and put in its own job(s) (might not yield a gain; might be enough to just have
        // this thing in a job). The system can easily sync on a fence when some control goes
        // to the global state buffers so the user won't ever know that updates happen in the background.
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

                        // Update state on device.
                        var stateEventPtr = (StateEvent*)oldestEventPtr;
                        var stateType = stateEventPtr->stateType;
                        var stateBlock = device.m_StateBlock;
                        var stateSize = stateEventPtr->stateSizeInBytes;
                        if (stateBlock.typeCode == stateType &&
                            stateBlock.alignedSizeInBytes >= stateSize) // Allow device state to have unused control at end.
                        {
                            UnsafeUtility.MemCpy(stateBlock.currentStatePtr, stateEventPtr->state, stateSize);
                        }

                        // See if any actions are listening.
                        // This could be spun off into a job.
                        ProcessStateChangeMonitors(device.m_DeviceIndex, oldestEventTime);

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

                // Device received event so make it current.
                device.MakeCurrent();
            }

            ////TODO: fire event that allows code to update state *from* state we just updated
        }

        // This could easily be spun off into jobs.
        private void ProcessStateChangeMonitors(int deviceIndex, double time)
        {
            if (m_StateChangeMonitorListeners == null)
                return;

            ////REVIEW: multiple state events in the same update pose a problem to this logic

            // We resize the monitor arrays only when someone adds to them so they
            // may be out of sync with the size of m_Devices.
            if (deviceIndex >= m_StateChangeMonitorListeners.Length)
                return;

            var changeMonitors = m_StateChangeMonitorMemoryRegions[deviceIndex];
            if (changeMonitors == null)
                return; // No action cares about state changes on this device.

            var listeners = m_StateChangeMonitorListeners[deviceIndex];

            var current = InputStateBlock.s_CurrentStatePtr;
            var previous = InputStateBlock.s_PreviousStatePtr;

            var numMonitors = changeMonitors.Count;
            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = changeMonitors[i];
                var offset = (int)memoryRegion.offset;
                var sizeInBytes = memoryRegion.sizeInBytes;

                if (UnsafeUtility.MemCmp(current + offset, previous + offset, (int)sizeInBytes) != 0)
                {
                    // If this method ends up in a job, you do NOT want to call this right
                    // here in the job. Should be queued up and called later.
                    var listener = listeners[i];
                    listener.action.NotifyControlValueChanged(listener.control, time);
                }
            }
        }

        [Serializable]
        internal class DeviceChangeEvent : UnityEvent<InputDevice, InputDeviceChange>
        {
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
            public TemplateState[] templateTypes;
            public TemplateState[] templateStrings;
            public DeviceState[] devices;
            public InputStateBuffers buffers;
            public DeviceChangeEvent deviceChangeEvent;
        }

        [SerializeField] private SerializedState m_SerializedState;

        // Stuff everything that we want to survive a domain reload into
        // a m_SerializedState.
        void ISerializationCallbackReceiver.OnBeforeSerialize()
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
                    stateOffset = device.m_StateBufferOffset
                };
                deviceArray[i] = deviceState;
            }

            m_SerializedState = new SerializedState
            {
                templateTypes = templateTypeArray,
                templateStrings = templateStringArray,
                devices = deviceArray,
                buffers = m_StateBuffers,
                deviceChangeEvent = m_DeviceChangeEvent
            };

            // We don't bring monitors along. InputActions and related classes are equipped
            // with their own domain reload survival logic that will plug actions back into
            // the system after reloads -- *if* the user is serializing them as part of
            // MonoBehaviours/ScriptableObjects.
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_TemplateTypes = new Dictionary<string, Type>();
            m_TemplateStrings = new Dictionary<string, string>();
            m_Processors = new Dictionary<string, Type>();
            m_StateBuffers = m_SerializedState.buffers;
            m_CurrentUpdate = InputUpdateType.Dynamic;
            m_DeviceChangeEvent = m_SerializedState.deviceChangeEvent;

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
                device.m_StateBufferOffset = state.stateOffset;
                device.BakeOffsetIntoStateBlockRecursive(device.m_StateBufferOffset);
                devices[i] = device;
            }
            m_Devices = devices;
            ReallocateStateBuffers();

            m_SerializedState = default(SerializedState);
        }

#endif
    }
}
