#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    class InputActionWindowToolbar
    {
        public Action<string> OnSearchChanged;
        public Action<string> OnSchemeChanged;
        public Action<string> OnDeviceChanged;

        [SerializeField]
        private int m_SelectedControlSchemeIndex = -1;
        [SerializeField]
        private int m_SelectedDeviceIndex;

        private string[] m_DeviceIdList;
        private string[] m_DeviceNamesList;
        private InputActionAssetManager m_ActionAssetManager;
        private SearchField m_SearchField;
        private string[] m_AllControlSchemeNames;
        internal string m_SearchText;
        private Action m_Apply;

        private static readonly GUIContent m_NoControlScheme = EditorGUIUtility.TrTextContent("No Control Scheme");
        private static readonly GUIContent m_AddSchemeGUI = new GUIContent("Add Control Scheme...");
        private static readonly GUIContent m_EditGUI = EditorGUIUtility.TrTextContent("Edit Control Scheme...");
        private static readonly GUIContent m_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate Control Scheme...");
        private static readonly GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete Control Scheme...");
        private static readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save Asset");
        private static readonly float m_MininumButtonWidth = 110f;

        public string selectedControlSchemeName
        {
            get
            {
                return m_SelectedControlSchemeIndex < 0 ? null : m_AllControlSchemeNames[m_SelectedControlSchemeIndex];
            }
        }

        public string selectedControlSchemeBindingGroup
        {
            get
            {
                return m_SelectedControlSchemeIndex < 0 ? null : controlSchemes[m_SelectedControlSchemeIndex].bindingGroup;
            }
        }

        public string selectedDevice
        {
            get
            {
                return m_SelectedDeviceIndex <= 0 ? null : m_DeviceIdList[m_SelectedDeviceIndex];
            }
        }

        public string[] allDevices
        {
            get
            {
                return m_DeviceIdList.Skip(1).ToArray();
            }
        }

        public string nameFilter
        {
            get { return m_SearchText; }
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get { return m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes; }
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

        public void SelectControlScheme(string inputControlSchemeName)
        {
            m_SelectedControlSchemeIndex = Array.IndexOf(m_AllControlSchemeNames, inputControlSchemeName);
            BuildDeviceList();
        }

        public void RebuildData()
        {
            m_AllControlSchemeNames = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToArray();
        }

        public void OnGUI()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            DrawSchemaSelection();
            DrawDeviceFilterSelection();
            DrawSaveButton();
        }

        private void DrawSchemaSelection()
        {
            var selectedSchema = selectedControlSchemeName;
            if (selectedSchema == null)
                selectedSchema = "No Control Scheme";

            var buttonGUI = new GUIContent(selectedSchema);
            var buttonRect = GUILayoutUtility.GetRect(buttonGUI, EditorStyles.toolbarPopup, GUILayout.MinWidth(m_MininumButtonWidth));
            if (GUI.Button(buttonRect, buttonGUI, EditorStyles.toolbarPopup))
            {
                buttonRect = new Rect(EditorGUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y)), Vector2.zero);
                var menu = new GenericMenu();
                menu.AddItem(m_NoControlScheme, m_SelectedControlSchemeIndex == -1, OnControlSchemeSelected, -1);
                for (int i = 0; i < m_AllControlSchemeNames.Length; i++)
                {
                    menu.AddItem(new GUIContent(m_AllControlSchemeNames[i]), m_SelectedControlSchemeIndex == i, OnControlSchemeSelected, i);
                }
                menu.AddSeparator("");
                menu.AddItem(m_AddSchemeGUI, false, AddControlScheme, buttonRect);
                if (m_SelectedControlSchemeIndex >= 0)
                {
                    menu.AddItem(m_EditGUI, false, EditSelectedControlScheme, buttonRect);
                    menu.AddItem(m_DuplicateGUI, false, DuplicateControlScheme, buttonRect);
                    menu.AddItem(m_DeleteGUI, false, DeleteControlScheme);
                }
                else
                {
                    menu.AddDisabledItem(m_EditGUI, false);
                    menu.AddDisabledItem(m_DuplicateGUI, false);
                    menu.AddDisabledItem(m_DeleteGUI, false);
                }
                menu.ShowAsContext();
            }
        }

        internal void OnControlSchemeSelected(object indexObj)
        {
            var index = (int)indexObj;
            if (m_SelectedControlSchemeIndex == index)
                return;
            m_SelectedControlSchemeIndex = index;
            m_SelectedDeviceIndex = 0;
            BuildDeviceList();
            OnSchemeChanged(selectedControlSchemeName);
        }

        private void DrawDeviceFilterSelection()
        {
            EditorGUI.BeginDisabledGroup(m_SelectedControlSchemeIndex < 0);
            if (m_DeviceNamesList.Length == 0)
            {
                GUILayout.Button("All devices", EditorStyles.toolbarPopup, GUILayout.MinWidth(m_MininumButtonWidth));
            }
            else if (GUILayout.Button(m_DeviceNamesList[m_SelectedDeviceIndex], EditorStyles.toolbarPopup, GUILayout.MinWidth(m_MininumButtonWidth)))
            {
                var menu = new GenericMenu();
                for (int i = 0; i < m_DeviceNamesList.Length; i++)
                {
                    menu.AddItem(new GUIContent(m_DeviceNamesList[i]), m_SelectedDeviceIndex == i, OnDeviceSelected, i);
                }
                menu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();
        }

        internal void OnDeviceSelected(object indexObj)
        {
            m_SelectedDeviceIndex = (int)indexObj;
            if (m_SelectedDeviceIndex == 0)
                OnDeviceChanged(null);
            else
                OnDeviceChanged(selectedDevice);
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

        private void BuildDeviceList()
        {
            var devices = new List<string>();
            if (m_SelectedControlSchemeIndex >= 0)
            {
                devices.Add("[All devices]");
                var controlScheme = m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(selectedControlSchemeName);
                devices.AddRange(controlScheme.deviceRequirements.Select(a => a.controlPath).ToList());
            }
            m_DeviceIdList = devices.ToArray();
            m_DeviceNamesList = devices.Select(a => a.Substring(1, a.Length - 2)).ToArray();
        }

        private void AddControlScheme(object position)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager, this, m_Apply);
            popup.SetUniqueName();
            // Since it's a callback, we need to manually handle ExitGUIException
            try
            {
                PopupWindow.Show((Rect)position, popup);
            }
            catch (ExitGUIException) {}
        }

        private void DeleteControlScheme()
        {
            if (!EditorUtility.DisplayDialog("Delete scheme", "Confirm scheme deletion", "Delete", "Cancel"))
            {
                return;
            }
            m_ActionAssetManager.m_AssetObjectForEditing.RemoveControlScheme(selectedControlSchemeName);
            m_SelectedControlSchemeIndex = -1;
            m_SelectedDeviceIndex = 0;
            m_Apply();
            RebuildData();
            OnSchemeChanged(selectedControlSchemeName);
        }

        private void DuplicateControlScheme(object position)
        {
            if (m_SelectedControlSchemeIndex == -1)
                return;
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
