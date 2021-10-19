#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Adds support for processing input-related messages sent from the <c>Unite Remote</c> app.
    /// </summary>
    /// <remarks>
    /// A hook in the Unity runtime allows us to observe messages received from the remote (see Modules/GenericRemoteEditor).
    /// We get the binary blob of each message and a shot at processing the message instead of
    /// the native code doing it.
    /// </remarks>
    internal static class UnityRemoteSupport
    {
        public static bool isConnected => s_State.connected;

        public static void Initialize()
        {
            InputRuntime.s_Instance.onUnityRemoteMessage = ProcessMessageFromUnityRemote;

            InputSystem.onSettingsChange += () =>
            {
                if (InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kDisableUnityRemoteSupport))
                {
                    InputRuntime.s_Instance.onUnityRemoteMessage = null;
                    if (s_State.connected)
                        Disconnect();
                }
                else
                    InputRuntime.s_Instance.onUnityRemoteMessage = ProcessMessageFromUnityRemote;
            };
        }

        private static unsafe bool ProcessMessageFromUnityRemote(IntPtr messageData)
        {
            var messageHeader = (MessageHeader*)messageData;

            switch (messageHeader->type)
            {
                case (byte)MessageType.Hello:
                    if (s_State.connected)
                        break;

                    // Install handlers.
                    s_State.deviceChangeHandler = OnDeviceChange;
                    s_State.deviceCommandHandler = OnDeviceCommand; ////REVIEW: We really should have a way of installing a handler just for a specific device.
                    InputSystem.onDeviceChange += s_State.deviceChangeHandler;
                    InputSystem.onDeviceCommand += s_State.deviceCommandHandler;

                    // Add devices.
                    s_State.touchscreen = InputSystem.AddDevice<Touchscreen>();
                    s_State.touchscreen.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                    s_State.accelerometer = InputSystem.AddDevice<Accelerometer>();
                    s_State.accelerometer.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                    // Gryo etc. added only when we receive GyroSettingsMessage.

                    s_State.connected = true;
                    Debug.Log("Unity Remote connected to input!");
                    break;

                case (byte)MessageType.Goodbye:
                    if (!s_State.connected)
                        break;
                    Disconnect();
                    Debug.Log("Unity Remote disconnected from input!");
                    break;

                case (byte)MessageType.Options:
                    var optionsMessage = (OptionsMessage*)messageData;
                    s_State.screenSize = DetermineScreenSize(optionsMessage->dimension1, optionsMessage->dimension2);
                    break;

                case (byte)MessageType.TouchInput:
                    if (s_State.touchscreen == null)
                        break;
                    // Android Remote seems to not be sending the last two fields (azimuthAngle and attitudeAngle).
                    if (messageHeader->length < 56)
                        break;
                    var touchMessage = (TouchInputMessage*)messageData;
                    var phase = TouchPhase.None;
                    switch (touchMessage->phase)
                    {
                        case (int)UnityEngine.TouchPhase.Began: phase = TouchPhase.Began; break;
                        case (int)UnityEngine.TouchPhase.Canceled: phase = TouchPhase.Canceled; break;
                        case (int)UnityEngine.TouchPhase.Ended: phase = TouchPhase.Ended; break;
                        case (int)UnityEngine.TouchPhase.Moved: phase = TouchPhase.Moved; break;
                            // Ignore stationary.
                    }
                    if (phase == default)
                        break;
                    InputSystem.QueueStateEvent(s_State.touchscreen, new TouchState
                    {
                        touchId = touchMessage->id + 1,
                        phase = phase,
                        position = MapRemoteTouchCoordinatesToLocal(new Vector2(touchMessage->positionX, touchMessage->positionY)),
                        radius = new Vector2(touchMessage->radius, touchMessage->radius),
                        pressure = touchMessage->pressure
                    });
                    break;

                case (byte)MessageType.GyroSettings:
                    var gyroSettingsMessage = (GyroSettingsMessage*)messageData;
                    if (!s_State.gyroInitialized)
                    {
                        // Message itself indicates presence of a gyro. Add the devices.
                        s_State.gyroscope = InputSystem.AddDevice<Gyroscope>();
                        s_State.attitude = InputSystem.AddDevice<AttitudeSensor>();
                        s_State.gravity = InputSystem.AddDevice<GravitySensor>();
                        s_State.linearAcceleration = InputSystem.AddDevice<LinearAccelerationSensor>();
                        s_State.gyroscope.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                        s_State.attitude.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                        s_State.gravity.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                        s_State.linearAcceleration.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;

                        s_State.gyroInitialized = true;
                    }
                    // Disable them if they are not currently enabled.
                    if (gyroSettingsMessage->enabled == 0)
                    {
                        InputSystem.DisableDevice(s_State.gyroscope);
                        InputSystem.DisableDevice(s_State.attitude);
                        InputSystem.DisableDevice(s_State.gravity);
                        InputSystem.DisableDevice(s_State.linearAcceleration);
                    }
                    else
                    {
                        s_State.gyroEnabled = true;
                    }
                    s_State.gyroUpdateInterval = gyroSettingsMessage->receivedGyroUpdateInternal;
                    break;

                case (byte)MessageType.GyroInput:
                    var gyroInputMessage = (GyroInputMessage*)messageData;
                    if (s_State.attitude != null && s_State.attitude.enabled)
                    {
                        InputSystem.QueueStateEvent(s_State.attitude, new AttitudeState
                        {
                            attitude = new Quaternion(gyroInputMessage->attitudeX, gyroInputMessage->attitudeY, gyroInputMessage->attitudeZ,
                                gyroInputMessage->attitudeW)
                        });
                    }
                    if (s_State.gyroscope != null && s_State.gyroscope.enabled)
                    {
                        InputSystem.QueueStateEvent(s_State.gyroscope, new GyroscopeState
                        {
                            angularVelocity = new Vector3(gyroInputMessage->rotationRateX, gyroInputMessage->rotationRateY,
                                gyroInputMessage->rotationRateZ)
                        });
                    }
                    if (s_State.gravity != null && s_State.gravity.enabled)
                    {
                        InputSystem.QueueStateEvent(s_State.gravity, new GravityState
                        {
                            gravity = new Vector3(gyroInputMessage->gravityX, gyroInputMessage->gravityY,
                                gyroInputMessage->gravityZ)
                        });
                    }
                    if (s_State.linearAcceleration != null && s_State.linearAcceleration.enabled)
                    {
                        InputSystem.QueueStateEvent(s_State.linearAcceleration, new LinearAccelerationState
                        {
                            acceleration = new Vector3(gyroInputMessage->userAccelerationX, gyroInputMessage->userAccelerationY,
                                gyroInputMessage->userAccelerationZ)
                        });
                    }
                    break;

                case (byte)MessageType.AccelerometerInput:
                    if (s_State.accelerometer == null)
                        break;
                    var accelerometerMessage = (AccelerometerInputMessage*)messageData;
                    InputSystem.QueueStateEvent(s_State.accelerometer, new AccelerometerState
                    {
                        acceleration = new Vector3(accelerometerMessage->accelerationX, accelerometerMessage->accelerationY,
                            accelerometerMessage->accelerationZ)
                    });
                    break;
            }

            return false;
        }

        private static void Disconnect()
        {
            InputSystem.RemoveDevice(s_State.touchscreen);
            InputSystem.RemoveDevice(s_State.accelerometer);
            if (s_State.gyroscope != null)
                InputSystem.RemoveDevice(s_State.gyroscope);
            if (s_State.attitude != null)
                InputSystem.RemoveDevice(s_State.attitude);
            if (s_State.gravity != null)
                InputSystem.RemoveDevice(s_State.gravity);
            if (s_State.linearAcceleration != null)
                InputSystem.RemoveDevice(s_State.linearAcceleration);

            ResetGlobalState();
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Removed:
                    // Deal with someone manually removing one of our devices.
                    if (device == s_State.accelerometer)
                        s_State.accelerometer = null;
                    else if (device == s_State.attitude)
                        s_State.accelerometer = null;
                    else if (device == s_State.gravity)
                        s_State.gravity = null;
                    else if (device == s_State.gyroscope)
                        s_State.gyroscope = null;
                    else if (device == s_State.touchscreen)
                        s_State.touchscreen = null;
                    else if (device == s_State.linearAcceleration)
                        s_State.linearAcceleration = null;
                    break;

                case InputDeviceChange.Enabled:
                case InputDeviceChange.Disabled:
                    // If it's any of our devices that make up the remote gyro,
                    // send a message to the remote.
                    if (device == s_State.attitude || device == s_State.gravity || device == s_State.gyroscope ||
                        device == s_State.linearAcceleration)
                    {
                        SyncGyroEnabledInRemote();
                    }
                    break;
            }
        }

        private static unsafe long? OnDeviceCommand(InputDevice device, InputDeviceCommand* command)
        {
            if (device != s_State.attitude && device != s_State.gyroscope && device != s_State.gravity &&
                device != s_State.linearAcceleration)
                return null;

            if (command->type == SetSamplingFrequencyCommand.Type)
            {
                s_State.gyroUpdateInterval = ((SetSamplingFrequencyCommand*)command)->frequency;
                InputRuntime.s_Instance.SetUnityRemoteGyroUpdateInterval(s_State.gyroUpdateInterval);
                return InputDeviceCommand.GenericSuccess;
            }

            if (command->type == QuerySamplingFrequencyCommand.Type)
            {
                ((QuerySamplingFrequencyCommand*)command)->frequency = s_State.gyroUpdateInterval;
                return InputDeviceCommand.GenericSuccess;
            }

            return InputDeviceCommand.GenericFailure;
        }

        private static void SyncGyroEnabledInRemote()
        {
            var enabled = (s_State.attitude?.enabled ?? false) || (s_State.gravity?.enabled ?? false) ||
                (s_State.gyroscope?.enabled ?? false) || (s_State.linearAcceleration?.enabled ?? false);
            if (enabled != s_State.gyroEnabled)
            {
                s_State.gyroEnabled = enabled;
                InputRuntime.s_Instance.SetUnityRemoteGyroEnabled(enabled);
            }
        }

        // This is taken from HandleOptionsMessage() in GenericRemote.cpp.
        private static Vector2 DetermineScreenSize(int dimension1, int dimension2)
        {
            const float kMaxPixels = 640 * 480;  // limit the resolution to VGA
            float screenPixels = dimension1 * dimension2;
            var divider = (int)Mathf.Ceil(Mathf.Sqrt(screenPixels / kMaxPixels));

            if (divider == 0)
                return default;

            var hdim1 = dimension1 / divider;
            var hdim2 = dimension2 / divider;

            // GetConfigValue is private. Reflect around it.
            var getConfigValueMethod = typeof(EditorSettings).GetMethod("GetConfigValue");
            if (getConfigValueMethod != null && getConfigValueMethod.Invoke(null, new[] { "UnityRemoteResolution" }) == "Normal")
            {
                hdim1 = dimension1;
                hdim2 = dimension2;
            }

            if (hdim1 >= 1 && hdim2 >= 1)
                return new Vector2(dimension1, dimension2);

            return default;
        }

        private static Vector2 MapRemoteTouchCoordinatesToLocal(Vector2 position)
        {
            var screenSizeRemote = s_State.screenSize;
            var screenSizeLocal = InputRuntime.s_Instance.screenSize;

            return new Vector2(
                position.x / screenSizeRemote.x * screenSizeLocal.x,
                position.y = position.y / screenSizeRemote.y * screenSizeLocal.y);
        }

        // See Editor/Src/RemoteInput/GenericRemote.cpp

        internal enum MessageType : byte
        {
            Invalid = 0,

            Hello = 1,
            Options = 2,
            GyroSettings = 3,
            DeviceOrientation = 4,
            DeviceFeatures = 5,

            TouchInput = 10,
            AccelerometerInput = 11,
            TrackBallInput = 12,
            Key = 13,
            GyroInput = 14,
            MousePresence = 15,
            JoystickInput = 16,
            JoystickNames = 17,

            WebCamDeviceList = 20,
            WebCamStream = 21,

            LocationServiceData = 30,
            CompassData = 31,

            Goodbye = 32,

            Reserved = 255,
        }

        internal interface IUnityRemoteMessage
        {
            byte staticType { get; }
        }

        [StructLayout(LayoutKind.Explicit, Size = 5)]
        internal struct MessageHeader
        {
            // Unfortunately, the header has an odd 5 byte length and everything
            // coming after it is misaligned. Reason is that native reads is as a stream
            // and wants to pack tightly.
            [FieldOffset(0)] public byte type;
            [FieldOffset(1)] public int length;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct HelloMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public uint protocolIdLength;
            [FieldOffset(9)] public fixed char protocolId[11];
            [FieldOffset(20)] public int protocolVersion;

            public byte staticType => (byte)MessageType.Hello;

            public static HelloMessage Create()
            {
                var msg = default(HelloMessage);
                msg.protocolIdLength = 11;
                msg.protocolId[0] = 'U';
                msg.protocolId[1] = 'n';
                msg.protocolId[2] = 'i';
                msg.protocolId[3] = 't';
                msg.protocolId[4] = 'y';
                msg.protocolId[5] = 'R';
                msg.protocolId[6] = 'e';
                msg.protocolId[7] = 'm';
                msg.protocolId[8] = 'o';
                msg.protocolId[9] = 't';
                msg.protocolId[10] = 'e';
                msg.protocolVersion = 0;
                return msg;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct OptionsMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public int dimension1;
            [FieldOffset(9)] public int dimension2;

            public byte staticType => (byte)MessageType.Options;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct GoodbyeMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;

            public byte staticType => (byte)MessageType.Goodbye;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct TouchInputMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public float positionX;
            [FieldOffset(9)] public float positionY;
            [FieldOffset(13)] public ulong frame;
            [FieldOffset(21)] public int id;
            [FieldOffset(25)] public int phase;
            [FieldOffset(29)] public int tapCount;
            [FieldOffset(33)] public float radius;
            [FieldOffset(37)] public float radiusVariance;
            [FieldOffset(41)] public int type;
            [FieldOffset(45)] public float pressure;
            [FieldOffset(49)] public float maximumPossiblePressure;
            [FieldOffset(53)] public float azimuthAngle;
            [FieldOffset(57)] public float altitudeAngle;

            public byte staticType => (byte)MessageType.TouchInput;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct GyroSettingsMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public int enabled;
            [FieldOffset(9)] public float receivedGyroUpdateInternal;

            public byte staticType => (byte)MessageType.GyroSettings;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct GyroInputMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public float rotationRateX;
            [FieldOffset(9)] public float rotationRateY;
            [FieldOffset(13)] public float rotationRateZ;
            [FieldOffset(17)] public float rotationRateUnbiasedX;
            [FieldOffset(21)] public float rotationRateUnbiasedY;
            [FieldOffset(25)] public float rotationRateUnbiasedZ;
            [FieldOffset(29)] public float gravityX;
            [FieldOffset(33)] public float gravityY;
            [FieldOffset(37)] public float gravityZ;
            [FieldOffset(41)] public float userAccelerationX;
            [FieldOffset(45)] public float userAccelerationY;
            [FieldOffset(49)] public float userAccelerationZ;
            [FieldOffset(53)] public float attitudeX;
            [FieldOffset(57)] public float attitudeY;
            [FieldOffset(61)] public float attitudeZ;
            [FieldOffset(65)] public float attitudeW;

            public byte staticType => (byte)MessageType.GyroInput;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct AccelerometerInputMessage : IUnityRemoteMessage
        {
            [FieldOffset(0)] public MessageHeader header;
            [FieldOffset(5)] public float accelerationX;
            [FieldOffset(9)] public float accelerationY;
            [FieldOffset(13)] public float accelerationZ;
            [FieldOffset(17)] public float deltaTime;

            public byte staticType => (byte)MessageType.AccelerometerInput;
        }

        private struct State
        {
            public bool connected;
            public bool gyroInitialized;
            public bool gyroEnabled;
            public float gyroUpdateInterval;
            public Vector2 screenSize;

            public Action<InputDevice, InputDeviceChange> deviceChangeHandler;
            public InputDeviceCommandDelegate deviceCommandHandler;

            // Devices that we create for receiving input from the remote.
            public Touchscreen touchscreen;
            public Accelerometer accelerometer;
            public Gyroscope gyroscope;
            public AttitudeSensor attitude;
            public GravitySensor gravity;
            public LinearAccelerationSensor linearAcceleration;
        }

        private static State s_State;

        ////TODO: hook this into Hakan's new cleanup mechanism
        internal static void ResetGlobalState()
        {
            if (s_State.deviceChangeHandler != null)
                InputSystem.onDeviceChange -= s_State.deviceChangeHandler;
            if (s_State.deviceCommandHandler != null)
                InputSystem.onDeviceCommand -= s_State.deviceCommandHandler;
            s_State = default;
        }
    }
}
#endif
