#if UNITY_EDITOR
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

        [SerializeField]
        private int m_SelectedControlSchemeIndex;
        [SerializeField]
        private int m_SelectedDeviceIndex;

        private string[] m_DeviceIdList;
        private string[] m_DeviceNamesList;
        private InputActionAssetManager m_ActionAssetManager;
        private SearchField m_SearchField;
        private string[] m_AllControlSchemeNames;
        private string m_SearchText;
        private Action m_Apply;

        private static readonly GUIContent m_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate");
        private static readonly GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        private static readonly GUIContent m_EditGUI = EditorGUIUtility.IconContent("_Popup");
        private static readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save Asset");

        string selectedControlSchemeName
        {
            get
            {
                return m_SelectedControlSchemeIndex <= 0 ? null : m_AllControlSchemeNames[m_SelectedControlSchemeIndex - 1];
            }
        }

        public bool searching
        {
            get
            {
                return !string.IsNullOrEmpty(m_SearchText);
            }
        }

        public string[] deviceFilter
        {
            get
            {
                if (m_SelectedDeviceIndex < 0)
                {
                    return null;
                }
                if (m_SelectedDeviceIndex == 0)
                {
                    // All devices
                    return m_DeviceIdList.Skip(1).ToArray();
                }
                return m_DeviceIdList.Skip(m_SelectedDeviceIndex).Take(1).ToArray();
            }
        }

        public InputActionWindowToolbar(InputActionAssetManager actionAssetManager, Action apply)
        {
            SetReferences(actionAssetManager, apply);
            RebuildData();
        }

        public void SetReferences(InputActionAssetManager actionAssetManager, Action apply)
        {
            m_ActionAssetManager = actionAssetManager;
            m_Apply = apply;
            RebuildData();
            BuildDeviceList();
        }

        public void RebuildData()
        {
            m_AllControlSchemeNames = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToArray();
        }

        public void SelectControlScheme(string inputControlSchemeName)
        {
            m_SelectedControlSchemeIndex = Array.IndexOf(m_AllControlSchemeNames, inputControlSchemeName) + 1;
            BuildDeviceList();
        }

        public void OnGUI()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            DrawSchemaSelection();
            DrawDeviceFilterSelection();
            DrawSchemaEditButton();
            DrawSaveButton();
        }

        private void DrawSchemaSelection()
        {
            var controlSchemes = GetControlSchemesNames();
            var newScheme = EditorGUILayout.Popup(m_SelectedControlSchemeIndex, controlSchemes);
            if (controlSchemes.Length > 0 && newScheme == (controlSchemes.Length - 1))
            {
                if (controlSchemes.Length == 1 || m_SelectedControlSchemeIndex == (controlSchemes.Length - 1))
                    m_SelectedControlSchemeIndex = 0;

                var popup = new AddControlSchemePopup(m_ActionAssetManager, this, m_Apply);
                popup.SetUniqueName();
                PopupWindow.Show(GUILayoutUtility.GetLastRect(), popup);
            }
            else if (newScheme != m_SelectedControlSchemeIndex)
            {
                m_SelectedControlSchemeIndex = newScheme;
                m_SelectedDeviceIndex = 0;
                BuildDeviceList();
            }
        }

        private void DrawDeviceFilterSelection()
        {
            EditorGUI.BeginDisabledGroup(m_SelectedControlSchemeIndex <= 0);
            m_SelectedDeviceIndex = EditorGUILayout.Popup(m_SelectedDeviceIndex, m_DeviceNamesList);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawSchemaEditButton()
        {
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
            EditorGUI.EndDisabledGroup();
        }

        private void DrawSaveButton()
        {
            EditorGUI.BeginDisabledGroup(!m_ActionAssetManager.dirty);
            EditorGUILayout.Space();
            if (GUILayout.Button(m_SaveAssetGUI, EditorStyles.toolbarButton))
            {
                m_ActionAssetManager.SaveChangesToAsset();
            }
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

        private string[] GetControlSchemesNames()
        {
            var controlSchemes = new string[m_AllControlSchemeNames.Length + 2];
            controlSchemes[0] = "No Control Scheme";
            Array.Copy(m_AllControlSchemeNames, 0, controlSchemes, 1, m_AllControlSchemeNames.Length);
            controlSchemes[m_AllControlSchemeNames.Length + 1] = "Add Control Scheme...";
            return controlSchemes;
        }

        private void BuildDeviceList()
        {
            var devices = new List<string>();
            if (m_SelectedControlSchemeIndex >= 1)
            {
                devices.Add("All devices");
                var controlScheme = m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(selectedControlSchemeName);
                devices.AddRange(controlScheme.deviceRequirements.Select(a => a.controlPath).ToList());
            }
            m_DeviceIdList = devices.ToArray();
            m_DeviceNamesList = devices.Select(InputControlPath.ToHumanReadableString).ToArray();
        }

        private void DeleteControlScheme()
        {
            m_ActionAssetManager.m_AssetObjectForEditing.RemoveControlScheme(selectedControlSchemeName);
            m_SelectedControlSchemeIndex = 0;
            m_SelectedDeviceIndex = -1;
            m_Apply();
            RebuildData();
        }

        private void DuplicateControlScheme(object position)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager, this, m_Apply);
            popup.DuplicateParametersFrom(selectedControlSchemeName);
            // Since it's a callback, we need to manually handle ExitGUIException
            try
            {
                PopupWindow.Show((Rect)position, popup);
            }
            catch (ExitGUIException) {}
        }

        private void EditSelectedControlScheme(object position)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager, this, m_Apply);
            popup.SetSchemaForEditing(selectedControlSchemeName);

            // Since it's a callback, we need to manually handle ExitGUIException
            try
            {
                PopupWindow.Show((Rect)position, popup);
            }
            catch (ExitGUIException) {}
        }
    }
}
#endif // UNITY_EDITOR
