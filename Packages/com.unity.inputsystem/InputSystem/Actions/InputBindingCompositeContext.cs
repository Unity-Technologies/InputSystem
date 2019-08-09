using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Contextual data made available when processing values of composite bindings.
    /// </summary>
    /// <seealso cref="InputBindingComposite"/>
    /// <seealso cref="InputBindingComposite{TValue}"/>
    /// <seealso cref="InputBindingComposite{TValue}.ReadValue(ref InputBindingCompositeContext)"/>
    public struct InputBindingCompositeContext
    {
        internal InputActionState m_State;
        internal int m_BindingIndex;

        public TValue ReadValue<TValue>(int partNumber)
            where TValue : struct, IComparable<TValue>
        {
            if (m_State == null)
                return default;

            return m_State.ReadCompositePartValue<TValue>(m_BindingIndex, partNumber, out _, out _);
        }

        public TValue ReadValue<TValue>(int partNumber, out InputControl sourceControl)
            where TValue : struct, IComparable<TValue>
        {
            if (m_State == null)
            {
                sourceControl = null;
                return default;
            }

            var value = m_State.ReadCompositePartValue<TValue>(m_BindingIndex, partNumber, out _, out var controlIndex);
            sourceControl = m_State.controls[controlIndex];
            return value;
        }

        public TValue ReadValue<TValue, TComparer>(int partNumber, TComparer comparer = default)
            where TValue : struct
            where TComparer : IComparer<TValue>
        {
            if (m_State == null)
                return default;

            return m_State.ReadCompositePartValue<TValue, TComparer>(m_BindingIndex, partNumber, comparer, out _);
        }

        public TValue ReadValue<TValue, TComparer>(int partNumber, out InputControl sourceControl, TComparer comparer = default)
            where TValue : struct
            where TComparer : IComparer<TValue>
        {
            if (m_State == null)
            {
                sourceControl = null;
                return default;
            }

            var value = m_State.ReadCompositePartValue<TValue, TComparer>(m_BindingIndex, partNumber, comparer, out var controlIndex);
            sourceControl = m_State.controls[controlIndex];
            return value;
        }

        public bool ReadValueAsButton(int partNumber)
        {
            if (m_State == null)
                return default;

            m_State.ReadCompositePartValue<float>(m_BindingIndex, partNumber, out var buttonValue, out _);
            return buttonValue;
        }
    }
}
