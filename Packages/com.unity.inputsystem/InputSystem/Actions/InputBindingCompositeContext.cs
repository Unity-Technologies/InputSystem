using System;

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

            bool buttonValue;
            return m_State.ReadCompositePartValue<TValue>(m_BindingIndex, partNumber, out buttonValue);
        }

        public bool ReadValueAsButton(int partNumber)
        {
            if (m_State == null)
                return default;

            bool buttonValue;
            m_State.ReadCompositePartValue<float>(m_BindingIndex, partNumber, out buttonValue);
            return buttonValue;
        }
    }
}
