using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
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
        public static void Initialize()
        {
            InputSystem.s_Manager.m_Runtime.onUnityRemoteMessage = ProcessMessageFromUnityRemote;
        }

        private static unsafe bool ProcessMessageFromUnityRemote(IntPtr messageData)
        {
            var messageHeader = (MessageHeader*)messageData;

            switch (messageHeader->type)
            {
                case (byte)MessageType.Hello:
                    if (s_State.connected)
                        break;
                    s_State.touchscreen = InputSystem.AddDevice<Touchscreen>();
                    s_State.touchscreen.m_DeviceFlags |= InputDevice.DeviceFlags.Remote;
                    s_State.connected = true;
                    Debug.Log("Unity Remote connected to input!");
                    break;

                case (byte)MessageType.Goodbye:
                    if (!s_State.connected)
                        break;
                    InputSystem.RemoveDevice(s_State.touchscreen);
                    s_State = default;
                    Debug.Log("Unity Remote disconnected from input!");
                    break;

                case (byte)MessageType.TouchInput:
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
                        position = new Vector2(touchMessage->positionX, touchMessage->positionY),
                        radius = new Vector2(touchMessage->radius, touchMessage->radius)
                    });
                    break;
            }

            return false;
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

        private struct State
        {
            public bool connected;

            // Devices that we create for receiving input from the remote.
            public Touchscreen touchscreen;
            public Gyroscope gyroscope;
            public Accelerometer accelerometer;
            public Mouse mouse;
        }

        private static State s_State;

        ////TODO: hook this into Hakan's new cleanup mechanism
        internal static void ResetGlobalState()
        {
            s_State = default;
        }
    }
}
#endif
