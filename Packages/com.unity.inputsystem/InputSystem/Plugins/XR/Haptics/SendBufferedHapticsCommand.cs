// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.XR.Haptics
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct SendBufferedHapticCommand : IInputDeviceCommandInfo
    {
        static FourCC Type => new FourCC('X', 'H', 'U', '0');

        private const int kMaxHapticBufferSize = 1024;
        private const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(int) * 2) + (kMaxHapticBufferSize * sizeof(byte));

        public FourCC typeStatic => Type;

        [FieldOffset(0)]
        private InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        private int channel;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int))]
        private int bufferSize;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + (sizeof(int) * 2))]
        private fixed byte buffer[kMaxHapticBufferSize];

        public static SendBufferedHapticCommand Create(byte[] rumbleBuffer)
        {
            if (rumbleBuffer == null)
                throw new System.ArgumentNullException(nameof(rumbleBuffer));

            var rumbleBufferSize = Mathf.Min(kMaxHapticBufferSize, rumbleBuffer.Length);
            var newCommand = new SendBufferedHapticCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                bufferSize = rumbleBufferSize
            };

            //TODO TOMB: There must be a more effective, bulk copy operation for fixed buffers than this.
            //Replace if found.
            var commandPtr = &newCommand;
            fixed(byte* src = rumbleBuffer)
            {
                for (int cpyIndex = 0; cpyIndex < rumbleBufferSize; cpyIndex++)
                    commandPtr->buffer[cpyIndex] = src[cpyIndex];
            }

            return newCommand;
        }
    }
}
#endif
