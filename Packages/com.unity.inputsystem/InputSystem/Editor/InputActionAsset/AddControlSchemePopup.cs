#if UNITY_EDITOR
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
        string m_InputControlSchemeName = "New control scheme";
        int m_RequirementsOptionsChoice;

        InputActionAssetManager m_AssetManager;
        InputActionWindowToolbar m_Toolbar;

        bool m_SetFocus;
        Vector2 m_SchemaNameLabelSize;
        float m_RequirementHeights;
        float m_ButtonsAndLabelsHeights;

        static readonly GUIContent m_RequirementGUI = new GUIContent("Requirements:");
        static readonly GUIContent m_AddControlSchemeGUI = new GUIContent("Add control scheme");
        static readonly GUIContent m_SchemaNameGUI = new GUIContent("Scheme Name");
        static readonly Vector2 s_DefaultSize = new Vector2(300, 200);
        static readonly string[] choices = { "Optional", "Required" };
        string m_OriginalName;
        Action m_Apply;

        public AddControlSchemePopup(InputActionAssetManager assetManager, InputActionWindowToolbar toolbar, Action apply)
        {
            m_AssetManager = assetManager;
            m_Toolbar = toolbar;
            m_SetFocus = true;
            m_Apply = apply;
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
            m_OriginalName = schemaName;
        }

        public void DuplicateParametersFrom(string schemaName)
        {
            SetSchemaParametersFrom(schemaName);
            SetUniqueName();
            m_OriginalName = m_InputControlSchemeName;
        }

        private void SetSchemaParametersFrom(string schemaName)
        {
            m_InputControlSchemeName = m_AssetManager.m_AssetObjectForEditing.GetControlScheme(schemaName).name;
            var schema = m_AssetManager.m_AssetObjectForEditing.GetControlScheme(schemaName);
            m_Devices = schema.deviceRequirements.Select(a => new DeviceEntryForList() { name = a.controlPath, deviceRequirement = a }).ToList();
        }

        public override Vector2 GetWindowSize()
        {
            return m_ButtonsAndLabelsHeights > 0 ? new Vector2(300, m_ButtonsAndLabelsHeights) : s_DefaultSize;
        }

        public override void OnOpen()
        {
            m_DevicesReorderableList = new ReorderableList(m_Devices, typeof(InputControlScheme.DeviceRequirement));
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
                menu.AddItem(new GUIContent(device.ToString()), false, AddElement, device.id);
            }
            menu.ShowAsContext();
        }

        List<DeviceEntryForList> GetDeviceOptions()
        {
            List<DeviceEntryForList> devices = new List<DeviceEntryForList>();
            BuildTreeForAbstractDevices(devices);
            BuildTreeForSpecificDevices(devices);
            return devices;
        }

        void BuildTreeForAbstractDevices(List<DeviceEntryForList> deviceList)
        {
            foreach (var deviceLayout in EditorInputControlLayoutCache.allDeviceLayouts)
                AddDeviceTreeItem(deviceLayout, deviceList);
        }

        void BuildTreeForSpecificDevices(List<DeviceEntryForList> deviceList)
        {
            foreach (var layout in EditorInputControlLayoutCache.allProductLayouts)
            {
                var rootLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                if (string.IsNullOrEmpty(rootLayoutName))
                    rootLayoutName = "Other";
                else
                    rootLayoutName = rootLayoutName.GetPlural();

                AddDeviceTreeItem(layout, deviceList);
            }
        }

        void AddDeviceTreeItem(InputControlLayout layout, List<DeviceEntryForList> deviceList)
        {
            var entry = new DeviceEntryForList();
            entry.name = layout.name;
            deviceList.Add(entry);
            foreach (var commonUsage in layout.commonUsages)
            {
                var entryWithUsage = new DeviceEntryForList();
                entryWithUsage.name = layout.name;
                entryWithUsage.commonUsage = commonUsage;
                deviceList.Add(entryWithUsage);
            }
        }

        void AddElement(object nameObject)
        {
            var name = nameObject.ToString();
            if (!m_DevicesReorderableList.list.Cast<DeviceEntryForList>().Any(a => a.name == name))
            {
                var device = new InputControlScheme.DeviceRequirement();
                device.controlPath = name;
                m_Devices.Add(new DeviceEntryForList(){name = name, deviceRequirement = device});
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

            if (Event.current.type == EventType.Repaint)
                m_ButtonsAndLabelsHeights = 0;

            GUILayout.BeginArea(rect);
            DrawTopBar();
            EditorGUILayout.BeginVertical(EditorStyles.label);
            DrawSpace();
            DrawNameEditTextField();
            DrawSpace();
            DrawDeviceList();
            DrawConfirmationButton();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void DrawConfirmationButton()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
            {
                editorWindow.Close();
            }
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_InputControlSchemeName) || m_DevicesReorderableList.list.Count <= 0);
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
            if (Event.current.type == EventType.Repaint)
                m_ButtonsAndLabelsHeights += GUILayoutUtility.GetLastRect().height;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void DrawDeviceList()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.label);
            var requirementsLabelSize = EditorStyles.label.CalcSize(m_RequirementGUI);
            var deviceListRect = GUILayoutUtility.GetRect(GetWindowSize().x - requirementsLabelSize.x - 20, m_DevicesReorderableList.GetHeight());
            m_DevicesReorderableList.DoList(deviceListRect);
            var requirementsHeight = DrawRequirementsCheckboxes();
            var listHeight = m_DevicesReorderableList.GetHeight() + EditorGUIUtility.singleLineHeight * 3;
            if (Event.current.type == EventType.Repaint)
            {
                if (listHeight < requirementsHeight)
                {
                    m_ButtonsAndLabelsHeights += requirementsHeight;
                }
                else
                {
                    m_ButtonsAndLabelsHeights += listHeight;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawSpace()
        {
            GUILayout.Space(6f);
            if (Event.current.type == EventType.Repaint)
                m_ButtonsAndLabelsHeights += 6f;
        }

        void DrawTopBar()
        {
            EditorGUILayout.LabelField(m_AddControlSchemeGUI, Styles.headerLabel);

            if (Event.current.type == EventType.Repaint)
                m_ButtonsAndLabelsHeights += GUILayoutUtility.GetLastRect().height;
        }

        void DrawNameEditTextField()
        {
            EditorGUILayout.BeginHorizontal();
            m_SchemaNameLabelSize = EditorStyles.label.CalcSize(m_RequirementGUI);
            EditorGUILayout.LabelField(m_SchemaNameGUI, GUILayout.Width(m_SchemaNameLabelSize.x));
            GUI.SetNextControlName("SchemaName");

            EditorGUI.BeginChangeCheck();
            m_InputControlSchemeName = EditorGUILayout.TextField(m_InputControlSchemeName);
            if (EditorGUI.EndChangeCheck())
            {
                SetUniqueName();
            }

            if (m_SetFocus)
            {
                EditorGUI.FocusTextInControl("SchemaName");
                m_SetFocus = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        float DrawRequirementsCheckboxes()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(m_RequirementGUI, GUILayout.Width(200));
            var requirementHeights = GUILayoutUtility.GetLastRect().y;
            EditorGUI.BeginDisabledGroup(m_DevicesReorderableList.index == -1);
            var requirementsOption = -1;
            if (m_DevicesReorderableList.index >= 0)
            {
                var deviceEntryForList = (DeviceEntryForList)m_DevicesReorderableList.list[m_DevicesReorderableList.index];
                requirementsOption = deviceEntryForList.deviceRequirement.isOptional ? 0 : 1;
            }
            EditorGUI.BeginChangeCheck();
            requirementsOption = GUILayout.SelectionGrid(requirementsOption, choices, 1, EditorStyles.radioButton);
            requirementHeights += GUILayoutUtility.GetLastRect().y;
            if (EditorGUI.EndChangeCheck())
            {
                m_Devices[m_DevicesReorderableList.index].deviceRequirement.isOptional = requirementsOption == 0;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            return requirementHeights;
        }

        void Save()
        {
            m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes[m_ControlSchemeIndex].m_DeviceRequirements = m_Devices.Select(a => a.deviceRequirement).ToArray();
            m_AssetManager.m_AssetObjectForEditing.m_ControlSchemes[m_ControlSchemeIndex].m_Name = m_InputControlSchemeName;
            m_Apply();
            m_Toolbar.SelectControlScheme(m_InputControlSchemeName);
            editorWindow.Close();
        }

        void Add()
        {
            var controlScheme = new InputControlScheme(m_InputControlSchemeName);
            controlScheme.m_DeviceRequirements = m_Devices.Select(a => a.deviceRequirement).ToArray();
            m_AssetManager.m_AssetObjectForEditing.AddControlScheme(controlScheme);
            m_Apply();
            m_Toolbar.SelectControlScheme(m_InputControlSchemeName);
            editorWindow.Close();
        }

        class DeviceEntryForList : IComparable
        {
            public string name;
            public InputControlScheme.DeviceRequirement deviceRequirement;
            public InternedString commonUsage;
            public object id
            {
                get
                {
                    if (string.IsNullOrEmpty(commonUsage))
                    {
                        return string.Format("<{0}>", name);
                    }
                    else
                    {
                        return string.Format("<{0}>{{{1}}}", name, commonUsage);
                    }
                }
            }

            public override string ToString()
            {
                return string.Format("{0} {1}", InputControlPath.ToHumanReadableString(name), commonUsage);
            }

            public int CompareTo(object obj)
            {
                var c = (DeviceEntryForList)obj;
                return String.Compare(name, c.name);
            }
        }

        public void SetUniqueName()
        {
            if (m_InputControlSchemeName == m_OriginalName)
                return;
            m_AssetManager.serializedObject.Update();
            var controlSchemeArrayProperty = m_AssetManager.serializedObject.FindProperty("m_ControlSchemes");
            m_InputControlSchemeName = InputActionSerializationHelpers.FindUniqueName(controlSchemeArrayProperty, m_InputControlSchemeName);
        }
    }
}
#endif // UNITY_EDITOR
