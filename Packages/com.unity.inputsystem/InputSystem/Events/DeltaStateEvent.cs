using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Partial state update for an input device.
    /// </summary>
    /// <remarks>
    /// Avoids having to send a full state memory snapshot when only a small
    /// part of the state has changed.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = InputEvent.kBaseEventSize + 9)]
    public unsafe struct DeltaStateEvent : IInputEventTypeInfo
    {
        public const int Type = 0x444C5441; // 'DLTA'

        [FieldOffset(0)] public InputEvent baseEvent;
        [FieldOffset(InputEvent.kBaseEventSize)] public FourCC stateFormat;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public uint stateOffset;
        [FieldOffset(InputEvent.kBaseEventSize + 8)] public fixed byte stateData[1]; // Variable-sized.

        public uint deltaStateSizeInBytes
        {
            get { return baseEvent.sizeInBytes - (InputEvent.kBaseEventSize + 8); }
        }

        public IntPtr deltaState
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

        public static DeltaStateEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException("ptr");
            if (!ptr.IsA<DeltaStateEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into DeltaStateEvent",
                    ptr.type));

            return (DeltaStateEvent*)ptr.data;
        }
    }
}
