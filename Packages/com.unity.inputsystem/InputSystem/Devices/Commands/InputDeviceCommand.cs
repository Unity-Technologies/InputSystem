using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.LowLevel
{
    ////REVIEW: why is this passing the command by pointer instead of by ref?
    /// <summary>
    /// Delegate used by <see cref="InputSystem.onDeviceCommand"/>.
    /// </summary>
    public unsafe delegate long? InputDeviceCommandDelegate(InputDevice device, InputDeviceCommand* command);

    /// <summary>
    /// Delegate for executing <see cref="InputDeviceCommand"/>s inside <see cref="InputSystem.onFindLayoutForDevice"/>.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <seealso cref="InputSystem.onFindLayoutForDevice"/>
    /// <seealso cref="Layouts.InputDeviceFindControlLayoutDelegate"/>
    public delegate long InputDeviceExecuteCommandDelegate(ref InputDeviceCommand command);

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
        ////TODO: Remove kBaseCommandSize
        internal const int kBaseCommandSize = 8;
        public const int BaseCommandSize = 8;

        /// <summary>
        /// Generic failure code for <see cref="InputDevice.ExecuteCommand{TCommand}"/> calls.
        /// </summary>
        /// <remarks>
        /// Any negative return value for an <see cref="InputDevice.ExecuteCommand{TCommand}"/> call should be considered failure.
        /// </remarks>
        public const long GenericFailure = -1;

        public const long GenericSuccess = 1;

        [FieldOffset(0)] public FourCC type;
        [FieldOffset(4)] public int sizeInBytes;

        public int payloadSizeInBytes => sizeInBytes - kBaseCommandSize;

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

        public FourCC typeStatic
        {
            get { return new FourCC(); }
        }
    }
}
