#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;

////TODO: show parameters for selected interaction or processor inline in list rather than separately underneath list

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Base class for views that show the properties of actions or bindings.
    /// </summary>
    internal abstract class PropertiesViewBase
    {
        protected PropertiesViewBase(string label, SerializedProperty bindingOrAction, Action<FourCC> onChange, string expectedControlLayout = null)
        {
            if (bindingOrAction == null)
                throw new ArgumentNullException(nameof(bindingOrAction));

            m_InteractionsProperty = bindingOrAction.FindPropertyRelative("m_Interactions");
            m_ProcessorsProperty = bindingOrAction.FindPropertyRelative("m_Processors");

            m_InteractionsList = new InteractionsListView(m_InteractionsProperty, OnInteractionsModified, null);
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
            m_ProcessorsFoldout = DrawFoldout(s_ProcessorsFoldoutLabel, m_ProcessorsFoldout, s_ProcessorsAddButton, m_ProcessorsList.OnAddDropdown);
            if (m_ProcessorsFoldout)
                m_ProcessorsList.OnGUI();
        }

        private void DrawInteractionsGroup()
        {
            m_InteractionsFoldout = DrawFoldout(s_InteractionsFoldoutLabel, m_InteractionsFoldout, s_InteractionsAddButton, m_InteractionsList.OnAddDropdown);
            if (m_InteractionsFoldout)
                m_InteractionsList.OnGUI();
        }

        private static bool DrawFoldout(GUIContent content, bool folded, GUIContent addButton = null, Action<Rect> addDropDown = null)
        {
            const int k_PopupSize = 20;
            var bgRect = GUILayoutUtility.GetRect(content, Styles.s_FoldoutBackgroundStyle);
            EditorGUI.LabelField(bgRect, GUIContent.none, Styles.s_FoldoutBackgroundStyle);
            var foldoutRect = bgRect;
            foldoutRect.xMax -= k_PopupSize;
            var retval = EditorGUI.Foldout(foldoutRect, folded, content, Styles.s_FoldoutStyle);
            if (addButton != null)
            {
                var popupRect = bgRect;
                popupRect.xMin = popupRect.xMax - k_PopupSize;
                if (GUI.Button(popupRect, addButton, EditorStyles.label))
                    addDropDown(popupRect);
            }
            return retval;
        }

        private void OnProcessorsModified()
        {
            m_ProcessorsProperty.stringValue = m_ProcessorsList.ToSerializableString();
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
            m_OnChange(k_ProcessorsChanged);
        }

        private void OnInteractionsModified()
        {
            m_InteractionsProperty.stringValue = m_InteractionsList.ToSerializableString();
            m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
            m_OnChange(k_InteractionsChanged);
        }

        public Action<FourCC> onChange => m_OnChange;

        private bool m_GeneralFoldout = true;
        private bool m_InteractionsFoldout = true;
        private bool m_ProcessorsFoldout = true;

        private readonly Action<FourCC> m_OnChange;

        private readonly InteractionsListView m_InteractionsList;
        private readonly ProcessorsListView m_ProcessorsList;

        private readonly SerializedProperty m_InteractionsProperty;
        private readonly SerializedProperty m_ProcessorsProperty;

        private readonly GUIContent m_GeneralFoldoutLabel;

        ////TODO: tooltips
        private static readonly GUIContent s_ProcessorsFoldoutLabel = EditorGUIUtility.TrTextContent("Processors");
        public static readonly GUIContent s_ProcessorsAddButton = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Add Processor");
        private static readonly GUIContent s_InteractionsFoldoutLabel = EditorGUIUtility.TrTextContent("Interactions");
        public static readonly GUIContent s_InteractionsAddButton = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Add Interaction");

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
                        InputActionTreeView.ResourcesPath + "foldoutBackground.png");

                s_FoldoutBackgroundStyle.normal.background = darkGreyBackgroundWithBorderTexture;
                s_FoldoutBackgroundStyle.border = new RectOffset(3, 3, 3, 3);
                s_FoldoutBackgroundStyle.margin = new RectOffset(1, 1, 3, 3);
            }
        }
    }
}
#endif // UNITY_EDITOR
