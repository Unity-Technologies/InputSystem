using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Data header for a command send to an <see cref="InputDevice"/>.
    /// </summary>
    /// <remarks>
    /// Commands are essentially synchronously processed events send directly
    /// to a specific device. Their primary use is to expose device-specific
    /// functions without having to extend the C# API used to communicate
    /// between input code and backend device implementations (which may sit
    /// in native code).
    ///
    /// Like input events, device commands use <see cref="FourCC"/> codes
    /// to indicate their type.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = kBaseCommandSize)]
    public struct InputDeviceCommand : IInputDeviceCommandInfo
    {
        public const int kBaseCommandSize = 8;

        /// <summary>
        /// Generic failure code for <see cref="IOCTL"/> calls.
        /// </summary>
        /// <remarks>
        /// Any negative return value for an <see cref="IOCTL"/> call should be considered failure.
        /// </remarks>
        public const long kGenericFailure = -1;

        public const long kGenericSuccess = 1;

        [FieldOffset(0)] public FourCC type;
        [FieldOffset(4)] public int sizeInBytes;

        public int payloadSizeInBytes
        {
            get { return sizeInBytes - kBaseCommandSize; }
        }

        public unsafe void* payloadPtr
        {
            get
            {
                fixed(void* thisPtr = &this)
                {
                    return ((byte*)thisPtr) + kBaseCommandSize;
                }
            }
        }

        public InputDeviceCommand(FourCC type, int sizeInBytes = kBaseCommandSize)
        {
            this.type = type;
            this.sizeInBytes = sizeInBytes;
        }

        public static unsafe NativeArray<byte> AllocateNative(FourCC type, int payloadSize)
        {
            var sizeInBytes = payloadSize + kBaseCommandSize;
            var buffer = new NativeArray<byte>(sizeInBytes, Allocator.Temp);

            var commandPtr = (InputDeviceCommand*)NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
            commandPtr->type = type;
            commandPtr->sizeInBytes = sizeInBytes;

            return buffer;
        }

        public FourCC GetTypeStatic()
        {
            return new FourCC();
        }
    }
}
