using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    public class ControlSchemesToolbar
    {
        [SerializeField]
        private InputActionAssetManager m_ActionAssetManager;
        [SerializeField]
        private int m_SelectedControlSchemeIndex = -1;
        [SerializeField]
        private int m_SelectedDeviceIndex;
        string[] m_AllControlSchemeNames;

        private readonly GUIContent m_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate");
        private readonly GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        private readonly GUIContent m_EditGUI = EditorGUIUtility.TrTextContent("edit");


        public ControlSchemesToolbar(InputActionAssetManager actionAssetManager)
        {
            m_ActionAssetManager = actionAssetManager;
            m_AllControlSchemeNames = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToArray();
        }

        public void OnGUI()
        {
            var controlSchemes = m_ActionAssetManager.m_AssetObjectForEditing.controlSchemes.Select(a => a.name).ToList();
            controlSchemes.Add("Add Control Scheme...");
            var newScheme = EditorGUILayout.Popup(m_SelectedControlSchemeIndex, controlSchemes.ToArray());
            if (newScheme == controlSchemes.Count - 1)
            {
                if (controlSchemes.Count == 1)
                    m_SelectedControlSchemeIndex = -1;
                var popup = new AddControlSchemePopup(m_ActionAssetManager.m_AssetObjectForEditing, () => m_ActionAssetManager.SetAssetDirty());
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
        }

        void DeleteControlScheme()
        {
            m_ActionAssetManager.m_AssetObjectForEditing.RemoveControlScheme(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
        }

        void DuplicateControlScheme(object rectObj)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager.m_AssetObjectForEditing, m_ActionAssetManager.SetAssetDirty);
            popup.SetSchemaParametersFrom(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            // TODO make sure name is unique
            PopupWindow.Show((Rect)rectObj, popup);
        }

        void EditSelectedControlScheme(object rectObj)
        {
            var popup = new AddControlSchemePopup(m_ActionAssetManager.m_AssetObjectForEditing, m_ActionAssetManager.SetAssetDirty);
            popup.SetSchemaForEditing(m_AllControlSchemeNames[m_SelectedControlSchemeIndex]);
            PopupWindow.Show((Rect)rectObj, popup);
        }
    }
}
