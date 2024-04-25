#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal readonly struct SerializedArrayProperty<T> where T : struct
    {
        public SerializedArrayProperty(SerializedProperty array, Func<SerializedProperty, T> factory)
        {
            Debug.Assert(array != null && array.isArray);
            Debug.Assert(factory != null);

            wrappedProperty = array;
            m_Factory = factory;
        }

        public int length => wrappedProperty.arraySize;

        public T this[int index] => m_Factory(wrappedProperty.GetArrayElementAtIndex(index));

        public int IndexOf(Predicate<T> predicate)
        {
            for (var i = 0; i < length; ++i)
            {
                if (predicate(this[i]))
                    return i;
            }
            return -1;
        }

        public T? Find(Predicate<T> predicate)
        {
            for (var i = 0; i < length; ++i)
            {
                var item = this[i];
                if (predicate(item))
                    return item;
            }
            return null;
        }

        public void Paste(StringBuilder buffer)
        {
            CopyPasteHelper.PasteItems(buffer.ToString(), new[] { length - 1 }, wrappedProperty);
        }

        public SerializedProperty wrappedProperty { get; }

        private readonly Func<SerializedProperty, T> m_Factory;
    }
}
#endif
