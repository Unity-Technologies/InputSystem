using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    class AddControlSchemePopup : PopupWindowContent
    {
        public static class Styles
        {
            public static GUIStyle headerLabel = new GUIStyle(EditorStyles.toolbar);
            static Styles()
            {
                headerLabel.alignment = TextAnchor.MiddleCenter;
                headerLabel.fontStyle = FontStyle.Bold;
                headerLabel.padding.left = 10;
            }
        }
        
        int m_ControlSchemeIndex = -1;
        ReorderableList m_DevicesReorderableList;
        List<DeviceEntryForList> m_Devices = new List<DeviceEntryForList>();
        string m_InputControlSchemeName = "New control schema";
        int m_RequirementsOptionsChoice;

        static Vector2 s_Size = new Vector2(400, 250);
        static string[] choices = { "Optional", "Required" };

        InputActionAssetManager m_AssetManager;
        InputActionWindowToolbar m_Toolbar;

        bool m_SetFocus;

        public AddControlSchemePopup(InputActionAssetManager assetManager, InputActionWindowToolbar toolbar)
        {
            m_AssetManager = assetManager;
            m_Toolbar = toolbar;
            m_SetFocus = true;
        }

        public void SetSchemaForEditing(string schemaName)
        {
            for (int i = 0; i < m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes.Length; i++)
            {
                if (m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes[i].name == schemaName)
                {
                    m_ControlSchemeIndex = i;
                    break;
                }
            }
            SetSchemaParametersFrom(schemaName);
        }

        public void SetSchemaParametersFrom(string schemaName)
        {
            m_InputControlSchemeName = m_AssetManager.m_AssetObjectForEditing.GetControlScheme(schemaName).name;
            m_Devices = m_AssetManager.m_AssetObjectForEditing.GetControlScheme(schemaName).devices.Select(a => new DeviceEntryForList() { name = a.devicePath, deviceEntry = a }).ToList();
        }

        public override Vector2 GetWindowSize()
        {
            return s_Size;
        }

        public override void OnOpen()
        {
            m_DevicesReorderableList = new ReorderableList(m_Devices, typeof(InputControlScheme.DeviceEntry));
            m_DevicesReorderableList.headerHeight = 2;
            m_DevicesReorderableList.onAddCallback += OnDeviceAdd;
            m_DevicesReorderableList.onRemoveCallback += OnDeviceRemove;
        }

        void OnDeviceRemove(ReorderableList list)
        {
            list.list.RemoveAt(list.index);
            list.index = -1;
        }

        void OnDeviceAdd(ReorderableList list)
        {
            var menu = new GenericMenu();
            var deviceList = GetDeviceOptions();
            deviceList.Sort();
            foreach (var device in deviceList)
            {
                menu.AddItem(new GUIContent(device), false, AddElement, device);
            }
            menu.ShowAsContext();
        }

        List<string> GetDeviceOptions()
        {
            List<string> devices = new List<string>();
            BuildTreeForAbstractDevices(devices);
            BuildTreeForSpecificDevices(devices);
            return devices;
        }

        private void BuildTreeForAbstractDevices(List<string> deviceList)
        {
            foreach (var deviceLayout in EditorInputControlLayoutCache.allDeviceLayouts.OrderBy(a => a.name))
                AddDeviceTreeItem(deviceLayout, deviceList);
        }

        private void BuildTreeForSpecificDevices(List<string> deviceList)
        {
            foreach (var layout in EditorInputControlLayoutCache.allProductLayouts.OrderBy(a => a.name))
            {
                var rootLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                if (string.IsNullOrEmpty(rootLayoutName))
                    rootLayoutName = "Other";
                else
                    rootLayoutName = rootLayoutName.GetPlural();

                AddDeviceTreeItem(layout, deviceList);
            }
        }
        
        private void AddDeviceTreeItem(InputControlLayout layout, List<string> deviceList)
        {
            deviceList.Add(layout.name);
            foreach (var commonUsage in layout.commonUsages)
            {
                deviceList.Add(layout.name + " " + commonUsage);
            }
        }

        void AddElement(object nameObject)
        {
            var name = nameObject.ToString();
            if (!m_DevicesReorderableList.list.Cast<DeviceEntryForList>().Any(a => a.name == name))
            {
                var device = new InputControlScheme.DeviceEntry();
                device.devicePath = name;
                m_Devices.Add(new DeviceEntryForList(){name = name, deviceEntry = device});
                m_DevicesReorderableList.index = m_DevicesReorderableList.list.Count - 1;
            }
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    editorWindow.Close();
                    Event.current.Use();
                }
            }

            GUILayout.BeginArea(rect);

            EditorGUILayout.LabelField("Add control scheme", Styles.headerLabel);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            GUI.SetNextControlName("SchemaName");
            m_InputControlSchemeName = EditorGUILayout.TextField("Scheme Name", m_InputControlSchemeName);
            EditorGUILayout.BeginHorizontal();

            if (m_SetFocus)
            {
                EditorGUI.FocusTextInControl("SchemaName");
                m_SetFocus = false;
            }

            var r = GUILayoutUtility.GetRect(0, GetWindowSize().x / 2, 0, GetWindowSize().x / 2, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Requirements:");

            EditorGUI.BeginDisabledGroup(m_DevicesReorderableList.index == -1);

            var requirementsOption = -1;
            if (m_DevicesReorderableList.index >= 0)
            {
                var deviceEntryForList = (DeviceEntryForList)m_DevicesReorderableList.list[m_DevicesReorderableList.index];
                requirementsOption = deviceEntryForList.deviceEntry.isOptional ? 0 : 1;
            }
            EditorGUI.BeginChangeCheck();
            requirementsOption = GUILayout.SelectionGrid(requirementsOption, choices, 1, EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck())
            {
                m_Devices[m_DevicesReorderableList.index].deviceEntry.isOptional = requirementsOption == 0;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            m_DevicesReorderableList.DoList(r);
            EditorGUILayout.EndVertical();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_InputControlSchemeName));
            if (m_ControlSchemeIndex == -1)
            {
                if (GUILayout.Button("Add", GUILayout.ExpandWidth(true)))
                {
                    Add();
                }
            }
            else
            {
                if (GUILayout.Button("Save", GUILayout.ExpandWidth(true)))
                {
                    Save();
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();
        }

        void Save()
        {
            m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes[m_ControlSchemeIndex].m_Name = m_InputControlSchemeName;
            m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes[m_ControlSchemeIndex].m_Devices = m_Devices.Select(a => a.deviceEntry).ToArray();
            m_AssetManager.SetAssetDirty();
            m_Toolbar.RebuildData();
            m_Toolbar.SelectControlScheme(m_InputControlSchemeName);
            editorWindow.Close();
        }

        void Add()
        {
            var controlScheme = new InputControlScheme(m_InputControlSchemeName);
            controlScheme.m_Devices = m_Devices.Select(a => a.deviceEntry).ToArray();
            m_AssetManager.m_AssetObjectForEditing.AddControlScheme(controlScheme);
            m_AssetManager.SetAssetDirty();
            m_Toolbar.RebuildData();
            m_Toolbar.SelectControlScheme(m_InputControlSchemeName);
            editorWindow.Close();
        }

        class DeviceEntryForList
        {
            public string name;
            public InputControlScheme.DeviceEntry deviceEntry;
            public override string ToString()
            {
                return name;
            }
        }
    }
}
