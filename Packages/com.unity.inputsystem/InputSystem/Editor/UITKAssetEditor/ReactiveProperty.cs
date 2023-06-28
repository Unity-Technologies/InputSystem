#if UNITY_EDITOR
using System;

namespace UnityEngine.InputSystem.Editor
{
    internal class ReactiveProperty<T>
    {
        private T m_Value;
        public event Action<T> Changed;

        public T value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                Changed?.Invoke(m_Value);
            }
        }

        public void SetValueWithoutChangeNotification(T value)
        {
            m_Value = value;
        }
    }
}

#endif
