#if UNITY_EDITOR

using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal readonly struct SerializedInputActionAsset
    {
        private readonly SerializedObject m_Obj;

        public SerializedInputActionAsset(SerializedObject serializedObject)
        {
            Debug.Assert(serializedObject != null && serializedObject.targetObject is InputActionAsset);

            m_Obj = serializedObject;
        }

        public SerializedArrayProperty<SerializedInputActionMap> actionMaps =>
            new SerializedArrayProperty<SerializedInputActionMap>(
                m_Obj.FindProperty(nameof(InputActionAsset.m_ActionMaps)),
                factory: (p) => new SerializedInputActionMap(p));
    }
}

#endif
