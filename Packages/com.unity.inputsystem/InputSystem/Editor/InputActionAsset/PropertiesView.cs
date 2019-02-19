#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.Experimental.Input.Editor.Lists;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    internal abstract class PropertiesView
    {
        public PropertiesView(string label, SerializedProperty bindingOrAction, Action<FourCC> onChange, string expectedControlLayout = null)
        {
            m_InteractionsProperty = bindingOrAction.FindPropertyRelative("m_Interactions");
            m_ProcessorsProperty = bindingOrAction.FindPropertyRelative("m_Processors");

            m_InteractionsList = new InteractionsListView(m_InteractionsProperty, OnInteractionsModified, expectedControlLayout);
            m_ProcessorsList = new ProcessorsListView(m_ProcessorsProperty, OnProcessorsModified, expectedControlLayout);

            m_OnChange = onChange;
            m_GeneralFoldoutLabel = EditorGUIUtility.TrTextContent(label);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawGeneralGroup();
            EditorGUILayout.Space();
            DrawInteractionsGroup();
            EditorGUILayout.Space();
            DrawProcessorsGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        protected abstract void DrawGeneralProperties();

        private void DrawGeneralGroup()
        {
            m_GeneralFoldout = DrawFoldout(m_GeneralFoldoutLabel, m_GeneralFoldout);
            if (m_GeneralFoldout)
            {
                EditorGUI.indentLevel++;
                DrawGeneralProperties();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawProcessorsGroup()
        {
            m_ProcessorsFoldout = DrawFoldout(s_ProcessorsFoldoutLabel, m_ProcessorsFoldout);
            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                m_ProcessorsList.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawInteractionsGroup()
        {
            m_InteractionsFoldout = DrawFoldout(s_InteractionsFoldoutLabel, m_InteractionsFoldout);
            if (m_InteractionsFoldout)
            {
                EditorGUI.indentLevel++;
                m_InteractionsList.OnGUI();
                EditorGUI.indentLevel--;
            }
        }

        private static bool DrawFoldout(GUIContent content, bool folded)
        {
            var bgRect = GUILayoutUtility.GetRect(s_ProcessorsFoldoutLabel, Styles.s_FoldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.s_FoldoutBackgroundStyle);
            return EditorGUI.Foldout(bgRect, folded, content, Styles.s_FoldoutStyle);
        }

        private void OnProcessorsModified()
        {
            m_ProcessorsProperty.stringValue = m_ProcessorsList.ToSerializableString();
            m_OnChange(k_ProcessorsChanged);
        }

        private void OnInteractionsModified()
        {
            m_InteractionsProperty.stringValue = m_InteractionsList.ToSerializableString();
            m_OnChange(k_InteractionsChanged);
        }

        public Action<FourCC> onChange => m_OnChange;

        private bool m_GeneralFoldout = true;
        private bool m_InteractionsFoldout = true;
        private bool m_ProcessorsFoldout = true;

        private Action<FourCC> m_OnChange;

        private readonly InteractionsListView m_InteractionsList;
        private readonly ProcessorsListView m_ProcessorsList;

        private readonly SerializedProperty m_InteractionsProperty;
        private readonly SerializedProperty m_ProcessorsProperty;

        private readonly GUIContent m_GeneralFoldoutLabel;

        private static readonly GUIContent s_ProcessorsFoldoutLabel = EditorGUIUtility.TrTextContent("Processors");
        private static readonly GUIContent s_InteractionsFoldoutLabel = EditorGUIUtility.TrTextContent("Interactions");

        public static FourCC k_InteractionsChanged => new FourCC("IACT");
        public static FourCC k_ProcessorsChanged => new FourCC("PROC");

        private static class Styles
        {
            public static readonly GUIStyle s_FoldoutBackgroundStyle = new GUIStyle("Label");
            public static readonly GUIStyle s_FoldoutStyle = new GUIStyle("foldout");

            static Styles()
            {
                var darkGreyBackgroundWithBorderTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "foldoutBackground.png");

                s_FoldoutBackgroundStyle.normal.background = darkGreyBackgroundWithBorderTexture;
                s_FoldoutBackgroundStyle.border = new RectOffset(3, 3, 3, 3);
                s_FoldoutBackgroundStyle.margin = new RectOffset(1, 1, 3, 3);
            }
        }
    }
}
#endif // UNITY_EDITOR
