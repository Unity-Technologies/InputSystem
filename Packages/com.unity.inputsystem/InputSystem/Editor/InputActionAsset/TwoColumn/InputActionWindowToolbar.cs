using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    class InputActionWindowToolbar
    {
        public Action<string> OnSearchChanged;
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
        private static readonly GUIContent m_EditGUI = EditorGUIUtility.IconContent("_Popup");
        private static readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save");
        Rect m_EditButtonRect;

        public InputActionWindowToolbar(InputActionAssetManager actionAssetManager)
        {
            SetReferences(actionAssetManager);
            RebuildData();
        }

        public void SetReferences(InputActionAssetManager actionAssetManager)
        {
            m_ActionAssetManager = actionAssetManager;
        }

        public void RebuildData()
        {
            m_AllControlSchemeNames = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToArray();
        }

        public bool searching
        {
            get
            {
                return !string.IsNullOrEmpty(m_SearchText);
            }
        }

        public string selectedControlSchemeName
        {
            get
            {
                return m_SelectedControlSchemeIndex == -1 ? null : m_AllControlSchemeNames[m_SelectedControlSchemeIndex];
            }
        }

        public void OnGUI()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            var controlSchemes = GetControlSchemesNames();
            var newScheme = EditorGUILayout.Popup(m_SelectedControlSchemeIndex, controlSchemes.ToArray());
            if (controlSchemes.Count > 0 && newScheme == (controlSchemes.Count - 1))
            {
                if (controlSchemes.Count == 1 || m_SelectedControlSchemeIndex == (controlSchemes.Count - 1))
                    m_SelectedControlSchemeIndex = -1;

                var popup = new AddControlSchemePopup(m_ActionAssetManager, this);
                PopupWindow.Show(GUILayoutUtility.GetLastRect(), popup);
            }
            else if (newScheme != m_SelectedControlSchemeIndex)
            {
                m_SelectedControlSchemeIndex = newScheme;
            }

            EditorGUI.BeginDisabledGroup(m_SelectedControlSchemeIndex == -1);
            var devices = GetDeviceList();
            m_SelectedDeviceIndex = EditorGUILayout.Popup(m_SelectedDeviceIndex, devices.ToArray());
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(selectedControlSchemeName == null);
            if (GUILayout.Button(m_EditGUI, EditorStyles.toolbarButton))
            {
                var position = new Rect(Event.current.mousePosition, Vector2.zero);
                position.x += EditorWindow.focusedWindow.position.x;
                position.y += EditorWindow.focusedWindow.position.y;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Edit \"" + selectedControlSchemeName + "\""), false, EditSelectedControlScheme, position);
                menu.AddSeparator("");
                menu.AddItem(m_DuplicateGUI, false, DuplicateControlScheme, position);
                menu.AddItem(m_DeleteGUI, false, DeleteControlScheme);
                menu.ShowAsContext();
            }

            if (Event.current.type == EventType.Repaint)
            {
                m_EditButtonRect = new Rect(Event.current.mousePosition, Vector2.zero);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!m_ActionAssetManager.dirty);
            EditorGUILayout.Space();
            if (GUILayout.Button(m_SaveAssetGUI, EditorStyles.toolbarButton))
                m_ActionAssetManager.SaveChangesToAsset();
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();

            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
            {
                if (OnSearchChanged != null)
                    OnSearchChanged(m_SearchText);
            }
        }

        List<string> GetControlSchemesNames()
        {
            var controlSchemes = m_AllControlSchemeNames.ToList();
            controlSchemes.Add("Add Control Scheme...");
            return controlSchemes;
        }

        List<string> GetDeviceList()
        {
            var devices = new List<string>();
            if (m_SelectedControlSchemeIndex >= 0)
            {
                devices.Add("All devices");
                devices.AddRange(m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(selectedControlSchemeName).devices.Select(a => a.devicePath).ToList());
            }
            return devices;
        }

        private void DeleteControlScheme()
        {
            m_ActionAssetManager.m_AssetObjectForEditing.RemoveControlScheme(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            m_SelectedControlSchemeIndex = -1;
            RebuildData();
        }

        private void DuplicateControlScheme(object position)
        {
            // TODO make sure name is unique
            var popup = new AddControlSchemePopup(m_ActionAssetManager, this);
            popup.SetSchemaParametersFrom(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            // Since it's a callback, we need to manually handle ExitGUIException
            try
            {
                PopupWindow.Show((Rect)position, popup);
            }
            catch (ExitGUIException) {}
        }

        private void EditSelectedControlScheme(object position)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager, this);
            popup.SetSchemaForEditing(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);

            // Since it's a callback, we need to manually handle ExitGUIException
            try
            {
                PopupWindow.Show((Rect)position, popup);
            }
            catch (ExitGUIException) {}
        }

        public void SelectControlScheme(string inputControlSchemeName)
        {
            m_SelectedControlSchemeIndex = Array.IndexOf(m_AllControlSchemeNames, inputControlSchemeName);
        }
    }
}
