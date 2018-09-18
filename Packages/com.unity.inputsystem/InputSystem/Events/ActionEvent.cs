using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: move this inside InputActionQueue?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A variable-size event that captures the triggering of an action.
    /// </summary>
    /// <remarks>
    /// Action events capture fully processed values only.
    ///
    /// This struct is internal as the data it stores requires having access to <see cref="InputActionMapState"/>.
    /// Public access is meant to go through <see cref="InputActionQueue"/> which provides a wrapper around
    /// action events in the form of <see cref="InputActionQueue.ActionEventPtr"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 16 + 1)]
    internal unsafe struct ActionEvent : IInputEventTypeInfo
    {
        public const int Type = 0x4143544E; // 'ACTN'

        ////REVIEW: should we decouple this from InputEvent? we get deviceId which we don't really have a use for
        [FieldOffset(0)] public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize + 0)] private ushort m_ControlIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 2)] private ushort m_BindingIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] private ushort m_InteractionIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 6)] private byte m_StateIndex;
        [FieldOffset(InputEvent.kBaseEventSize + 7)] private byte m_Phase;
        [FieldOffset(InputEvent.kBaseEventSize + 8)] private double m_StartTime;
        [FieldOffset(InputEvent.kBaseEventSize + 16)] public fixed byte m_ValueData[1]; // Variable-sized.

        public double startTime
        {
            get { return m_StartTime; }
            set { m_StartTime = value; }
        }

        public InputActionPhase phase
        {
            get { return (InputActionPhase)m_Phase; }
            set { m_Phase = (byte)value; }
        }

        public byte* valueData
        {
            get
            {
                fixed(byte* data = m_ValueData)
                {
                    return data;
                }
            }
        }

        public int valueSizeInBytes
        {
            get { return (int)baseEvent.sizeInBytes - InputEvent.kBaseEventSize - 16; }
        }

        public int stateIndex
        {
            get { return m_StateIndex; }
            set
            {
                Debug.Assert(value >= 0 && value <= byte.MaxValue);
                if (value < 0 || value > byte.MaxValue)
                    throw new NotSupportedException("State count cannot exceed byte.MaxValue");
                m_StateIndex = (byte)value;
            }
        }

        public int controlIndex
        {
            get { return m_ControlIndex; }
            set
            {
                Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                if (value < 0 || value > ushort.MaxValue)
                    throw new NotSupportedException("Control count cannot exceed ushort.MaxValue");
                m_ControlIndex = (ushort)value;
            }
        }

        public int bindingIndex
        {
            get { return m_BindingIndex; }
            set
            {
                Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                if (value < 0 || value > ushort.MaxValue)
                    throw new NotSupportedException("Binding count cannot exceed ushort.MaxValue");
                m_BindingIndex = (ushort)value;
            }
        }

        public int interactionIndex
        {
            get
            {
                if (m_InteractionIndex == ushort.MaxValue)
                    return InputActionMapState.kInvalidIndex;
                return m_InteractionIndex;
            }
            set
            {
                Debug.Assert(value == InputActionMapState.kInvalidIndex || (value >= 0 && value < ushort.MaxValue));
                if (value == InputActionMapState.kInvalidIndex)
                    m_InteractionIndex = ushort.MaxValue;
                else
                {
                    if (value < 0 || value >= ushort.MaxValue)
                        throw new NotSupportedException("Interaction count cannot exceed ushort.MaxValue-1");
                    m_InteractionIndex = (ushort)value;
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

        public static int GetEventSizeWithValueSize(int valueSizeInBytes)
        {
            return InputEvent.kBaseEventSize + 16 + valueSizeInBytes;
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
