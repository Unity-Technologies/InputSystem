using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A complete state snapshot for an entire input device.
    /// </summary>
    /// <remarks>
    /// This is a variable-sized event.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 5)]
    public unsafe struct StateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53544154; // 'STAT'

        [FieldOffset(0)] public InputEvent baseEvent;

        /// <summary>
        /// Type code for the state stored in the event.
        /// </summary>
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;

        [FieldOffset(InputEvent.kBaseEventSize + sizeof(int))] public fixed byte stateData[1]; // Variable-sized.

        public uint stateSizeInBytes
        {
            get { return baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + sizeof(int)); }
        }

        public IntPtr state
        {
            get
            {
                fixed(byte* data = stateData)
                {
                    return new IntPtr((void*)data);
                }
            }
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static int GetEventSizeWithPayload<TState>()
            where TState : struct
        {
            return UnsafeUtility.SizeOf<TState>() + InputEvent.kBaseEventSize + sizeof(int);
        }

        public static StateEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException("ptr");
            if (!ptr.IsA<StateEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into StateEvent",
                        ptr.type));

            return (StateEvent*)ptr.data;
        }
    }
}
