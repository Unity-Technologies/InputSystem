using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + sizeof(int) + (sizeof(char) * kIMECharBufferSize))]
    public unsafe struct IMECompositionStringEvent : IInputEventTypeInfo
    {
        // These needs to match the native ImeCompositionStringInputEventData settings
        public const int kIMECharBufferSize = 16;
        public const int Type = 0x494D4553;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        public int size;

        [FieldOffset(InputEvent.kBaseEventSize + sizeof(int))]
        public fixed char buffer[kIMECharBufferSize];

        public unsafe string AsString()
        {
            if (size == 0)
                return "";

            string result;
            fixed (char* b = buffer)
            {
                result = new string(b, 0, size);
            }
            return result;
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }
    }
}
