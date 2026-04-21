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
    /// <param name="device">The device to execute the command on.</param>
    /// <param name="command">Pointer to the command to execute.</param>
    /// <returns>A non-negative value on success, or a negative value on failure.</returns>
    public unsafe delegate long? InputDeviceCommandDelegate(InputDevice device, InputDeviceCommand* command);

    /// <summary>
    /// Delegate for executing <see cref="InputDeviceCommand"/>s inside <see cref="InputSystem.onFindLayoutForDevice"/>.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <returns>A non-negative value on success, or a negative value on failure.</returns>
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
        /// <summary>The size in bytes of the base <see cref="InputDeviceCommand"/> header.</summary>
        public const int BaseCommandSize = 8;

        /// <summary>
        /// Generic failure code for <see cref="InputDevice.ExecuteCommand{TCommand}"/> calls.
        /// </summary>
        /// <remarks>
        /// Any negative return value for an <see cref="InputDevice.ExecuteCommand{TCommand}"/> call should be considered failure.
        /// </remarks>
        public const long GenericFailure = -1;

        /// <summary>Return value indicating a generic success result.</summary>
        public const long GenericSuccess = 1;

        /// <summary>The FourCC type identifier of this command.</summary>
        [FieldOffset(0)] public FourCC type;
        /// <summary>The total size of this command in bytes.</summary>
        [FieldOffset(4)] public int sizeInBytes;

        /// <summary>The size in bytes of the command payload beyond the base header.</summary>
        public int payloadSizeInBytes => sizeInBytes - kBaseCommandSize;

        /// <summary>Pointer to the first byte of the command payload.</summary>
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

        /// <summary>Initializes a command with the given type code and total size.</summary>
        public InputDeviceCommand(FourCC type, int sizeInBytes = kBaseCommandSize)
        {
            this.type = type;
            this.sizeInBytes = sizeInBytes;
        }

        /// <summary>Allocates a native buffer for a command of the given type and payload size.</summary>
        public static unsafe NativeArray<byte> AllocateNative(FourCC type, int payloadSize)
        {
            var sizeInBytes = payloadSize + kBaseCommandSize;
            var buffer = new NativeArray<byte>(sizeInBytes, Allocator.Temp);

            var commandPtr = (InputDeviceCommand*)NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
            commandPtr->type = type;
            commandPtr->sizeInBytes = sizeInBytes;

            return buffer;
        }

        /// <summary>Static FourCC type code used to identify this command type.</summary>
        public FourCC typeStatic
        {
            get { return new FourCC(); }
        }
    }
}
