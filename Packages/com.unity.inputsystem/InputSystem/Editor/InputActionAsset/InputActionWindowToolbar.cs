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
    internal class InputActionWindowToolbar
    {
        public Action<string> OnSearchChanged;
        public Action<string> OnSchemeChanged;
        public Action<string> OnDeviceChanged;

        [SerializeField] private int m_SelectedControlSchemeIndex = -1;
        [SerializeField] private int m_SelectedDeviceIndex;

        private string[] m_DeviceIdList;
        private string[] m_DeviceNamesList;
        private InputActionAssetManager m_ActionAssetManager;
        private SearchField m_SearchField;
        private string[] m_AllControlSchemeNames;
        internal string m_SearchText;
        private Action m_Apply;

        private static readonly GUIContent s_NoControlScheme = EditorGUIUtility.TrTextContent("No Control Scheme");
        private static readonly GUIContent s_AddSchemeGUI = new GUIContent("Add Control Scheme...");
        private static readonly GUIContent s_EditGUI = EditorGUIUtility.TrTextContent("Edit Control Scheme...");
        private static readonly GUIContent s_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate Control Scheme...");
        private static readonly GUIContent s_DeleteGUI = EditorGUIUtility.TrTextContent("Delete Control Scheme...");
        private static readonly GUIContent s_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save Asset");
        private static readonly GUIContent s_AutoSaveLabel = EditorGUIUtility.TrTextContent("Auto-Save");
        private static GUIStyle s_MiniToggleStyle;
        private static GUIStyle s_MiniLabelStyle;
        private static readonly float s_MininumButtonWidth = 110f;

        public string selectedControlSchemeName => m_SelectedControlSchemeIndex < 0 ? null : m_AllControlSchemeNames[m_SelectedControlSchemeIndex];

        public string selectedControlSchemeBindingGroup => m_SelectedControlSchemeIndex < 0 ? null : controlSchemes[m_SelectedControlSchemeIndex].bindingGroup;

        public string selectedDevice => m_SelectedDeviceIndex <= 0 ? null : m_DeviceIdList[m_SelectedDeviceIndex];

        public string[] allDevices => m_DeviceIdList.Skip(1).ToArray();

        public string nameFilter => m_SearchText;

        public ReadOnlyArray<InputControlScheme> controlSchemes => m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes;

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
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawSchemaSelection();
            DrawDeviceFilterSelection();
            if (!InputEditorUserSettings.autoSaveInputActionAssets)
                DrawSaveButton();
            GUILayout.FlexibleSpace();
            DrawAutoSaveToggle();
            GUILayout.Space(5);
            DrawSearchField();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSchemaSelection()
        {
            var selectedSchema = selectedControlSchemeName;
            if (selectedSchema == null)
                selectedSchema = "No Control Scheme";

            var buttonGUI = new GUIContent(selectedSchema);
            var buttonRect = GUILayoutUtility.GetRect(buttonGUI, EditorStyles.toolbarPopup, GUILayout.MinWidth(s_MininumButtonWidth));
            if (GUI.Button(buttonRect, buttonGUI, EditorStyles.toolbarPopup))
            {
                buttonRect = new Rect(EditorGUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y)), Vector2.zero);
                var menu = new GenericMenu();
                menu.AddItem(s_NoControlScheme, m_SelectedControlSchemeIndex == -1, OnControlSchemeSelected, -1);
                for (var i = 0; i < m_AllControlSchemeNames.Length; i++)
                {
                    menu.AddItem(new GUIContent(m_AllControlSchemeNames[i]), m_SelectedControlSchemeIndex == i, OnControlSchemeSelected, i);
                }
                menu.AddSeparator("");
                menu.AddItem(s_AddSchemeGUI, false, AddControlScheme, buttonRect);
                if (m_SelectedControlSchemeIndex >= 0)
                {
                    menu.AddItem(s_EditGUI, false, EditSelectedControlScheme, buttonRect);
                    menu.AddItem(s_DuplicateGUI, false, DuplicateControlScheme, buttonRect);
                    menu.AddItem(s_DeleteGUI, false, DeleteControlScheme);
                }
                else
                {
                    menu.AddDisabledItem(s_EditGUI, false);
                    menu.AddDisabledItem(s_DuplicateGUI, false);
                    menu.AddDisabledItem(s_DeleteGUI, false);
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
                GUILayout.Button("All devices", EditorStyles.toolbarPopup, GUILayout.MinWidth(s_MininumButtonWidth));
            }
            else if (GUILayout.Button(m_DeviceNamesList[m_SelectedDeviceIndex], EditorStyles.toolbarPopup, GUILayout.MinWidth(s_MininumButtonWidth)))
            {
                var menu = new GenericMenu();
                for (var i = 0; i < m_DeviceNamesList.Length; i++)
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
            OnDeviceChanged(m_SelectedDeviceIndex == 0 ? null : selectedDevice);
        }

        private void DrawSaveButton()
        {
            EditorGUI.BeginDisabledGroup(!m_ActionAssetManager.dirty);
            EditorGUILayout.Space();
            if (GUILayout.Button(s_SaveAssetGUI, EditorStyles.toolbarButton))
            {
                m_ActionAssetManager.SaveChangesToAsset();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawAutoSaveToggle()
        {
            ////FIXME: Using a normal Toggle style with a miniFont, I can't get the "Auto-Save" label to align properly on the vertical.
            ////       The workaround here splits it into a toggle with an empty label plus an extra label.
            ////       Not using EditorStyles.toolbarButton here as it makes it hard to tell that it's a toggle.
            if (s_MiniToggleStyle == null)
            {
                s_MiniToggleStyle = new GUIStyle("Toggle")
                {
                    font = EditorStyles.miniFont,
                    margin = new RectOffset(0, 0, 1, 0),
                    padding = new RectOffset(0, 16, 0, 0)
                };
                s_MiniLabelStyle = new GUIStyle("Label")
                {
                    font = EditorStyles.miniFont,
                    margin = new RectOffset(0, 0, 3, 0)
                };
            }

            var autoSaveNew = GUILayout.Toggle(InputEditorUserSettings.autoSaveInputActionAssets, "",
                s_MiniToggleStyle);
            GUILayout.Label(s_AutoSaveLabel, s_MiniLabelStyle);
            if (autoSaveNew != InputEditorUserSettings.autoSaveInputActionAssets && autoSaveNew && m_ActionAssetManager.dirty)
            {
                // If it changed from disabled to enabled, perform an initial save.
                m_ActionAssetManager.SaveChangesToAsset();
            }

            InputEditorUserSettings.autoSaveInputActionAssets = autoSaveNew;

            GUILayout.Space(5);
        }

        private void DrawSearchField()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            EditorGUI.BeginChangeCheck();
            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
                OnSearchChanged?.Invoke(m_SearchText);
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
