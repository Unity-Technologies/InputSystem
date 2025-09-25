#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal readonly struct SerializedInputActionMap
    {
        public SerializedInputActionMap(SerializedProperty serializedProperty)
        {
            wrappedProperty = serializedProperty ?? throw new ArgumentNullException(nameof(serializedProperty));
            name = serializedProperty.FindPropertyRelative(nameof(InputActionMap.m_Name)).stringValue;
        }

        public string name { get; }
        public SerializedProperty wrappedProperty { get; }
    }
}

#endif
