using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Contextual data made available when processing values of composite bindings.
    /// </summary>
    /// <seealso cref="IInputBindingComposite"/>
    /// <seealso cref="IInputBindingComposite{TValue}"/>
    /// <seealso cref="IInputBindingComposite{TValue}.ReadValue(ref InputBindingCompositeContext)"/>
    public struct InputBindingCompositeContext
    {
        internal InputActionMapState m_State;
        internal int m_BindingIndex;

        public TValue ReadValue<TValue>(int partNumber)
            where TValue : struct, IComparable<TValue>
        {
            if (m_State == null)
                return default(TValue);

            return m_State.ReadCompositePartValue<TValue>(m_BindingIndex, partNumber);
        }
    }
}
