using System;
using System.Runtime.CompilerServices;

namespace Unity.InputSystem.Runtime
{

    public partial struct InputGuid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe explicit operator InputGuid(Guid guid)
        {
            ulong r = 0;
            var ptr = (byte*) &r;
            var bytes = guid.ToByteArray();
            // TODO guids have first 8 bytes stored as uint ushort ushort and we need to do big-little endian conversion
            // instead we should massage native code to follow this
            ptr[0] = bytes[3];
            ptr[1] = bytes[2];
            ptr[2] = bytes[1];
            ptr[3] = bytes[0];
            ptr[4] = bytes[5];
            ptr[5] = bytes[4];
            ptr[6] = bytes[7];
            ptr[7] = bytes[6];
            var a = r;

            for (int i = 0; i < 8; ++i)
                ptr[i] = bytes[i + 8];
            var b = r;
            return new InputGuid { a = a , b = b };
        }

        public static unsafe implicit operator Guid(InputGuid guid)
        {
            ulong r = 0;
            var ptr = (byte*) &r;
            var bytes = new byte[16];
            // TODO guids have first 8 bytes stored as uint ushort ushort and we need to do big-little endian conversion
            // instead we should massage native code to follow this
            r = guid.a;
            bytes[3] = ptr[0];
            bytes[2] = ptr[1];
            bytes[1] = ptr[2];
            bytes[0] = ptr[3];
            bytes[5] = ptr[4];
            bytes[4] = ptr[5];
            bytes[7] = ptr[6];
            bytes[6] = ptr[7];

            r = guid.b;
            for (int i = 0; i < 8; ++i)
                bytes[i + 8] = ptr[i];

            return new Guid(bytes);
        }
    }
}