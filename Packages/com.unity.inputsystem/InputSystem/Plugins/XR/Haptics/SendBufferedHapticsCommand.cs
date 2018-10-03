using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct SendBufferedHapticCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'H', 'U', '0'); } }

        const int kMaxHapticBufferSize = 1024;
        const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(int) * 2) + (kMaxHapticBufferSize * sizeof(byte));

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public int channel;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int))]
        public int bufferSize;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + (sizeof(int) * 2))]
        public fixed byte buffer[kMaxHapticBufferSize];

        public static SendBufferedHapticCommand Create(int channel, byte[] rumbleBuffer)
        {
            int rumbleBufferSize = Mathf.Min(kMaxHapticBufferSize, rumbleBuffer.Length);
            SendBufferedHapticCommand newCommand = new SendBufferedHapticCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                bufferSize = rumbleBufferSize
            };

            //TODO TOMB: There must be a more effective, bulk copy operation for fixed buffers than this.
            //Replace if found.
            SendBufferedHapticCommand* commandPtr = &newCommand;
            fixed(byte* src = rumbleBuffer)
            {
                for (int cpyIndex = 0; cpyIndex < rumbleBufferSize; cpyIndex++)
                    commandPtr->buffer[cpyIndex] = src[cpyIndex];
            }

            return newCommand;
        }
    }
}
