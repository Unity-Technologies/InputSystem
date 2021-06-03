using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A complete state snapshot for an entire input device.
    /// </summary>
    /// <remarks>
    /// This is a variable-sized event.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 4 + kStateDataSizeToSubtract, Pack = 1)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154; // 'STAT'

        internal const int kStateDataSizeToSubtract = 1;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        /// <summary>
        /// Type code for the state stored in the event.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)]
        public FourCC stateFormat;

        [FieldOffset(InputEvent.kBaseEventSize + sizeof(int))]
        internal fixed byte stateData[kStateDataSizeToSubtract]; // Variable-sized.

        public uint stateSizeInBytes => baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + sizeof(int));

        public void* state
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return data;
                }
            }
        }

        public InputEventPtr ToEventPtr()
        {
            fixed(StateEvent * ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        public FourCC typeStatic => Type;

        /// <summary>
        /// Retrieve the state stored in the event.
        /// </summary>
        /// <typeparam name="TState">Type of state expected to be stored in the event. <see cref="IInputStateTypeInfo.format"/>
        /// must match <see cref="stateFormat"/>.</typeparam>
        /// <returns>Copy of the state stored in the event.</returns>
        /// <exception cref="InvalidOperationException"><see cref="stateFormat"/> does not match <see cref="IInputStateTypeInfo.format"/>
        /// of <typeparamref name="TState"/>.</exception>
        /// <remarks>
        /// The event may contain less or more data than what is found in the struct. Only the data found in the event
        /// is copied. The remainder of the struct is left at default values.
        /// </remarks>
        /// <seealso cref="GetState{T}(InputEventPtr)"/>
        public TState GetState<TState>()
            where TState : struct, IInputStateTypeInfo
        {
            var result = default(TState);
            if (stateFormat != result.format)
                throw new InvalidOperationException($"Expected state format '{result.format}' but got '{stateFormat}' instead");

            UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref result), state, Math.Min(stateSizeInBytes, UnsafeUtility.SizeOf<TState>()));

            return result;
        }

        /// <summary>
        /// Retrieve the state stored in the event.
        /// </summary>
        /// <typeparam name="TState">Type of state expected to be stored in the event. <see cref="IInputStateTypeInfo.format"/>
        /// must match <see cref="stateFormat"/>.</typeparam>
        /// <param name="ptr">A pointer to an input event. The pointer is checked for <c>null</c> and
        /// for whether the type of event it refers to is indeed a StateEvent.</param>
        /// <returns>Copy of the state stored in the event.</returns>
        /// <remarks>
        /// The event may contain less or more data than what is found in the struct. Only the data found in the event
        /// is copied. The remainder of the struct is left at default values.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="stateFormat"/> does not match <see cref="IInputStateTypeInfo.format"/>
        /// of <typeparamref name="TState"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="ptr"/> is <c>default(InputEventPtr)</c>.</exception>
        /// <exception cref="InvalidCastException"><paramref name="ptr"/> does not refer to a StateEvent.</exception>
        /// <seealso cref="GetState{T}()"/>
        public static TState GetState<TState>(InputEventPtr ptr)
            where TState : struct, IInputStateTypeInfo
        {
            return From(ptr)->GetState<TState>();
        }

        public static int GetEventSizeWithPayload<TState>()
            where TState : struct
        {
            return UnsafeUtility.SizeOf<TState>() + InputEvent.kBaseEventSize + sizeof(int);
        }

        /// <summary>
        /// Return the given <see cref="InputEventPtr"/> as a StateEvent pointer.
        /// </summary>
        /// <param name="ptr">A pointer to an input event. The pointer is checked for <c>null</c> and
        /// for whether the type of event it refers to is indeed a StateEvent.</param>
        /// <returns>Pointer <paramref name="ptr"/> converted to a StateEvent pointer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ptr"/> is <c>default(InputEventPtr)</c>.</exception>
        /// <exception cref="InvalidCastException"><paramref name="ptr"/> does not refer to a StateEvent.</exception>
        public static StateEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException(nameof(ptr));
            if (!ptr.IsA<StateEvent>())
                throw new InvalidCastException($"Cannot cast event with type '{ptr.type}' into StateEvent");

            return FromUnchecked(ptr);
        }

        internal static StateEvent* FromUnchecked(InputEventPtr ptr)
        {
            return (StateEvent*)ptr.data;
        }

        /// <summary>
        /// Read the current state of <paramref name="device"/> and create a state event from it.
        /// </summary>
        /// <param name="device">Device to grab the state from. Must be a device that has been added to the system.</param>
        /// <param name="eventPtr">Receives a pointer to the newly created state event.</param>
        /// <param name="allocator">Which native allocator to allocate memory for the event from. By default, the buffer is
        /// allocated as temporary memory (<see cref="Allocator.Temp"/>. Note that this means the buffer will not be valid
        /// past the current frame. Use <see cref="Allocator.Persistent"/> if the buffer for the state event is meant to
        /// persist for longer.</param>
        /// <returns>Buffer of unmanaged memory allocated for the event.</returns>
        /// <exception cref="ArgumentException"><paramref name="device"/> has not been added to the system.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static NativeArray<byte> From(InputDevice device, out InputEventPtr eventPtr,  Allocator allocator = Allocator.Temp)
        {
            return From(device, out eventPtr, allocator, useDefaultState: false);
        }

        /// <summary>
        /// Create a state event for the given <paramref name="device"/> and copy the default state of the device
        /// into the event.
        /// </summary>
        /// <param name="device">Device to create a state event for. Must be a device that has been added to the system.</param>
        /// <param name="eventPtr">Receives a pointer to the newly created state event.</param>
        /// <param name="allocator">Which native allocator to allocate memory for the event from. By default, the buffer is
        /// allocated as temporary memory (<see cref="Allocator.Temp"/>. Note that this means the buffer will not be valid
        /// past the current frame. Use <see cref="Allocator.Persistent"/> if the buffer for the state event is meant to
        /// persist for longer.</param>
        /// <returns>Buffer of unmanaged memory allocated for the event.</returns>
        /// <exception cref="ArgumentException"><paramref name="device"/> has not been added to the system.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static NativeArray<byte> FromDefaultStateFor(InputDevice device, out InputEventPtr eventPtr,  Allocator allocator = Allocator.Temp)
        {
            return From(device, out eventPtr, allocator, useDefaultState: true);
        }

        private static NativeArray<byte> From(InputDevice device, out InputEventPtr eventPtr,  Allocator allocator, bool useDefaultState)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (!device.added)
                throw new ArgumentException($"Device '{device}' has not been added to system",
                    nameof(device));

            var stateFormat = device.m_StateBlock.format;
            var stateSize = device.m_StateBlock.alignedSizeInBytes;
            var stateOffset = device.m_StateBlock.byteOffset;
            var statePtr = (byte*)(useDefaultState ? device.defaultStatePtr : device.currentStatePtr) + (int)stateOffset;
            var eventSize = InputEvent.kBaseEventSize + sizeof(int) + stateSize;

            var buffer = new NativeArray<byte>((int)eventSize, allocator);
            var stateEventPtr = (StateEvent*)buffer.GetUnsafePtr();

            stateEventPtr->baseEvent = new InputEvent(Type, (int)eventSize, device.deviceId, InputRuntime.s_Instance.currentTime);
            stateEventPtr->stateFormat = stateFormat;

            UnsafeUtility.MemCpy(stateEventPtr->state, statePtr, stateSize);

            eventPtr = stateEventPtr->ToEventPtr();
            return buffer;
        }
    }
}
