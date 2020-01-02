using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

////REVIEW: move this inside InputActionTrace?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A variable-size event that captures the triggering of an action.
    /// </summary>
    /// <remarks>
    /// Action events capture fully processed values only.
    ///
    /// This struct is internal as the data it stores requires having access to <see cref="InputActionState"/>.
    /// Public access is meant to go through <see cref="InputActionTrace"/> which provides a wrapper around
    /// action events in the form of <see cref="InputActionTrace.ActionEventPtr"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 16 + 1)]
    internal unsafe struct ActionEvent : IInputEventTypeInfo
    {
        public static FourCC Type => new FourCC('A', 'C', 'T', 'N');

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
            get => m_StartTime;
            set => m_StartTime = value;
        }

        public InputActionPhase phase
        {
            get => (InputActionPhase)m_Phase;
            set => m_Phase = (byte)value;
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

        public int valueSizeInBytes => (int)baseEvent.sizeInBytes - InputEvent.kBaseEventSize - 16;

        public int stateIndex
        {
            get => m_StateIndex;
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
            get => m_ControlIndex;
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
            get => m_BindingIndex;
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
                    return InputActionState.kInvalidIndex;
                return m_InteractionIndex;
            }
            set
            {
                Debug.Assert(value == InputActionState.kInvalidIndex || (value >= 0 && value < ushort.MaxValue));
                if (value == InputActionState.kInvalidIndex)
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

        public FourCC typeStatic => Type;

        public static int GetEventSizeWithValueSize(int valueSizeInBytes)
        {
            return InputEvent.kBaseEventSize + 16 + valueSizeInBytes;
        }

        public static ActionEvent* From(InputEventPtr ptr)
        {
            if (!ptr.valid)
                throw new ArgumentNullException(nameof(ptr));
            if (!ptr.IsA<ActionEvent>())
                throw new InvalidCastException($"Cannot cast event with type '{ptr.type}' into ActionEvent");

            return (ActionEvent*)ptr.data;
        }
    }
}
