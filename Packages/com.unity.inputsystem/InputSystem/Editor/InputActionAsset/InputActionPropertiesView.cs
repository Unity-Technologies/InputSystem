#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputActionPropertiesView
    {
        private static class Styles
        {
            public static GUIStyle foldoutBackgroundStyle = new GUIStyle("Label");
            public static GUIStyle foldoutStyle = new GUIStyle("foldout");

            static Styles()
            {
                var darkGreyBackgroundWithBorderTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "foldoutBackground.png");
                foldoutBackgroundStyle.normal.background = darkGreyBackgroundWithBorderTexture;
                foldoutBackgroundStyle.border = new RectOffset(3, 3, 3, 3);
                foldoutBackgroundStyle.margin = new RectOffset(1, 1, 3, 3);
            }
        }

        private SerializedProperty m_ActionProperty;
        private SerializedProperty m_ExpectedControlLayoutProperty;

        private Action m_ReloadTree;
        private bool m_GeneralFoldout = true;

        private static readonly GUIContent s_GeneralContent = EditorGUIUtility.TrTextContent("General");

        private string m_ExpectedControlLayout;
        private string[] m_ControlTypeList;
        private int m_SelectedControlType;

        public InputActionPropertiesView(SerializedProperty actionProperty, Action reloadTree)
        {
            m_ActionProperty = actionProperty;
            m_ExpectedControlLayoutProperty = actionProperty.FindPropertyRelative("m_ExpectedControlLayout");
            m_ReloadTree = reloadTree;

            m_ControlTypeList = BuildControlTypeList();

            m_SelectedControlType = Array.IndexOf(m_ControlTypeList, m_ExpectedControlLayoutProperty.stringValue);
            if (m_SelectedControlType == -1)
                m_SelectedControlType = 0;
        }

        private string[] BuildControlTypeList()
        {
            var types = new List<string>();
            foreach (var layoutName in InputSystem.s_Manager.m_Layouts.layoutTypes.Keys)
            {
                if (typeof(InputControl).IsAssignableFrom(InputSystem.s_Manager.m_Layouts.layoutTypes[layoutName]) &&
                    !typeof(InputDevice).IsAssignableFrom(InputSystem.s_Manager.m_Layouts.layoutTypes[layoutName]))
                {
                    types.Add(layoutName);
                }
            }
            // Sort alphabetically.
            types.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
            // Make sure "Any" is always topmost entry.
            types.Insert(0, "Any");
            return types.ToArray();
        }

        private void ApplyModifiers()
        {
            m_ExpectedControlLayoutProperty.serializedObject.ApplyModifiedProperties();
            m_ReloadTree();
        }

        public void OnGUI()
        {
            if (m_ActionProperty == null)
                return;

            EditorGUILayout.BeginVertical();

            m_GeneralFoldout = DrawFoldout(s_GeneralContent, m_GeneralFoldout);
            EditorGUI.indentLevel++;
            if (m_GeneralFoldout)
            {
                EditorGUI.BeginChangeCheck();
                m_SelectedControlType = EditorGUILayout.Popup("Type", m_SelectedControlType, m_ControlTypeList);
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_SelectedControlType == 0)
                        m_ExpectedControlLayoutProperty.stringValue = "";
                    else
                        m_ExpectedControlLayoutProperty.stringValue = m_ControlTypeList[m_SelectedControlType];
                    ApplyModifiers();
                }
            }
            EditorGUI.indentLevel--;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private static bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(content, Styles.foldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.foldoutBackgroundStyle);
            return EditorGUI.Foldout(bgRect, folded, content, Styles.foldoutStyle);
        }
    }
}
#endif // UNITY_EDITOR
