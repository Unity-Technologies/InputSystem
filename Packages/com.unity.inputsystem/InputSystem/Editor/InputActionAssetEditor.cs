#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomEditor(typeof(InputActionAsset))]
    public class InputActionAssetEditor : UnityEditor.Editor
    {
        static List<InputActionAssetEditor> s_EnabledEditors;
        int m_InstanceId;

        internal static InputActionAssetEditor FindFor(InputActionAsset asset)
        {
            if (s_EnabledEditors != null)
            {
                foreach (var editor in s_EnabledEditors)
                    if (editor.target == asset)
                        return editor;
            }
            return null;
        }

        public void OnEnable()
        {
            // Need to access serializedObject so it gets recreated internally after the asset is modified 
            // outside of Unity. Otherwise exceptions are thrown
            m_InstanceId = serializedObject.targetObject.GetInstanceID();

            if (s_EnabledEditors == null)
                s_EnabledEditors = new List<InputActionAssetEditor>();
            s_EnabledEditors.Add(this);
        }

        public void OnDisable()
        {
            if (s_EnabledEditors != null)
                s_EnabledEditors.Remove(this);
        }

        public void Reload()
        {
            serializedObject.Update();
            Repaint();
        }

        protected override void OnHeaderGUI()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Edit asset"))
            {
                ActionInspectorWindow.OnOpenAsset(m_InstanceId, 0);
            }
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}
#endif // UNITY_EDITOR
