using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    class ControlSchemesToolbar
    {
        [SerializeField]
        private InputActionAssetManager m_ActionAssetManager;
        [SerializeField]
        private int m_SelectedControlSchemeIndex = -1;
        [SerializeField]
        private int m_SelectedDeviceIndex;

        private string[] m_AllControlSchemeNames;
        private SearchField m_SearchField;
        private string m_SearchText;

        private static readonly GUIContent m_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate");
        private static readonly GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        private static readonly GUIContent m_EditGUI = EditorGUIUtility.TrTextContent("edit");
        private static readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save");

        public ControlSchemesToolbar(InputActionAssetManager actionAssetManager)
        {
            m_ActionAssetManager = actionAssetManager;
            m_AllControlSchemeNames = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToArray();
        }

        public bool searching
        {
            get
            {
                return !string.IsNullOrEmpty(m_SearchText);
            }
        }

        public void OnGUI()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            var controlSchemes = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToList();
            controlSchemes.Add("Add Control Scheme...");
            var newScheme = EditorGUILayout.Popup(m_SelectedControlSchemeIndex, controlSchemes.ToArray());
            if (controlSchemes.Count > 0 && newScheme == controlSchemes.Count - 1)
            {
                if (controlSchemes.Count == 1)
                    m_SelectedControlSchemeIndex = -1;
                var popup = new AddControlSchemePopup(m_ActionAssetManager);
                PopupWindow.Show(GUILayoutUtility.GetLastRect(), popup);
            }
            else if (newScheme != m_SelectedControlSchemeIndex)
            {
                m_SelectedControlSchemeIndex = newScheme;
            }

            EditorGUI.BeginDisabledGroup(m_SelectedControlSchemeIndex == -1);

            List<string> devices = new List<string>();
            if (m_SelectedControlSchemeIndex >= 0)
            {
                devices.Add("All devices");
                devices.AddRange(m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(controlSchemes[m_SelectedControlSchemeIndex]).devices.Select(a => a.devicePath).ToList());
            }
            m_SelectedDeviceIndex = EditorGUILayout.Popup(m_SelectedDeviceIndex, devices.ToArray());
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(m_EditGUI, EditorStyles.toolbarButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Edit \"" + controlSchemes[m_SelectedControlSchemeIndex] + "\""), false, EditSelectedControlScheme, GUILayoutUtility.GetLastRect());
                menu.AddSeparator("");
                menu.AddItem(m_DuplicateGUI, false, DuplicateControlScheme, GUILayoutUtility.GetLastRect());
                menu.AddItem(m_DeleteGUI, false, DeleteControlScheme);
                menu.ShowAsContext();
            }

            EditorGUI.BeginDisabledGroup(!m_ActionAssetManager.dirty);
            if (GUILayout.Button(m_SaveAssetGUI, EditorStyles.toolbarButton))
                m_ActionAssetManager.SaveChangesToAsset();
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();

            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
            {
//                m_TreeView.SetNameFilter(m_SearchText);
            }
        }

        private void DeleteControlScheme()
        {
            m_ActionAssetManager.m_AssetObjectForEditing.RemoveControlScheme(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
        }

        private void DuplicateControlScheme(object rectObj)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager);
            popup.SetSchemaParametersFrom(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            // TODO make sure name is unique
            PopupWindow.Show((Rect)rectObj, popup);
        }

        private void EditSelectedControlScheme(object rectObj)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager);
            popup.SetSchemaForEditing(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            PopupWindow.Show((Rect)rectObj, popup);
        }
    }
}
