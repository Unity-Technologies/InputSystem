using System;

namespace UnityEngine.Experimental.Input
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

            return m_State.ReadCompositePartValue<TValue>(m_BindingIndex, partNumber);
        }
    }
}
