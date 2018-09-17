using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: should this be made internal? things like control indices make no sense without having access to InputActionMapState

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A variable-size event that captures the triggering of an action.
    /// </summary>
    /// <remarks>
    /// Action events capture fully processed values only.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 16 + 1)]
    public unsafe struct ActionEvent : IInputEventTypeInfo
    {
        public const int Type = 0x4143544E; // 'ACTN'

        ////REVIEW: should we decouple this from InputEvent? we get deviceId which we don't really have a use for
        [FieldOffset(0)] public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize + 0)] public ushort controlIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 2)] public ushort bindingIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public ushort interactionIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 6)] public ushort valueSizeInBytes;
        [FieldOffset(InputEvent.kBaseEventSize + 8)] public double startTime;
        [FieldOffset(InputEvent.kBaseEventSize + 16)] public fixed byte valueData[1]; // Variable-sized.

        public IntPtr valuePtr
        {
            get
            {
                fixed(byte* data = valueData)
                {
                    return new IntPtr(data);
                }
            }
        }

        public InputEventPtr ToEventPtr()
        {
            fixed(ActionEvent* ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static ActionEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException("ptr");
            if (!ptr.IsA<ActionEvent>())
                throw new InvalidCastException(string.Format("Cannot cast event with type '{0}' into ActionEvent",
                    ptr.type));

            return (ActionEvent*)ptr.data;
        }
    }
}
