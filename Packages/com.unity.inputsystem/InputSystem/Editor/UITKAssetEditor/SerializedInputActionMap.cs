#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class SerializedInputActionMapArrayExtensions
    {
        public static SerializedInputActionMap? FindByName(this SerializedArrayProperty<SerializedInputActionMap> arr, string name)
        {
            return arr.Find((p) => p.name == name);
        }
    }

    internal readonly struct SerializedInputActionMap
    {
        public SerializedInputActionMap(SerializedProperty serializedProperty)
        {
            wrappedProperty = serializedProperty ?? throw new ArgumentNullException(nameof(serializedProperty));
            nameProperty = serializedProperty.FindPropertyRelative(nameof(InputActionMap.m_Name));
            actionsProperty = serializedProperty.FindPropertyRelative(nameof(InputActionMap.m_Actions));
            actions = new SerializedArrayProperty<SerializedInputAction>(actionsProperty,
                (p) => new SerializedInputAction(p));
        }

        public string name => nameProperty.stringValue;
        public SerializedArrayProperty<SerializedInputAction> actions { get; }
        public SerializedProperty nameProperty { get; }
        public SerializedProperty actionsProperty { get;  }
        public SerializedProperty wrappedProperty { get; }
        public InputActionAsset asset => wrappedProperty.serializedObject.targetObject as InputActionAsset;

        public StringBuilder CopyToBuffer(StringBuilder buffer = null)
        {
            buffer ??= new StringBuilder();
            CopyPasteHelper.CopyItems(new List<SerializedProperty> { wrappedProperty }, buffer, typeof(InputActionMap), wrappedProperty.GetParentProperty());
            return buffer;
        }

        /*public void CopyTo(SerializedArrayProperty<SerializedInputActionMap> dst, StringBuilder buffer = null)
        {
            var buf = CopyToBuffer(buffer);
            dst.Paste(ref buffer);
        }*/

        public static SerializedArrayProperty<SerializedInputActionMap> ArrayFromAsset(SerializedObject asset)
        {
            return new SerializedArrayProperty<SerializedInputActionMap>(
                asset.FindProperty(nameof(InputActionAsset.m_ActionMaps)), (p) => new SerializedInputActionMap(p));
        }
    }
}

#endif
