using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    public class ActionInspectorWindow : EditorWindow
    {
        static class Styles
        {
            public static GUIStyle darkGreyBackgroundWithBorder = new GUIStyle("Label");
            public static GUIStyle whiteBackgroundWithBorder = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle("Label");

            static Styles()
            {
                var darkGreyBackgroundWithBorderTexture = CreateTextureWithBorder(new Color32(114, 114, 114, 255));
                darkGreyBackgroundWithBorder.normal.background = darkGreyBackgroundWithBorderTexture;
                darkGreyBackgroundWithBorder.border = new RectOffset(3, 3, 3, 3);

                var whiteBackgroundWithBorderTexture = CreateTextureWithBorder(Color.white);
                whiteBackgroundWithBorder.normal.background = whiteBackgroundWithBorderTexture;
                whiteBackgroundWithBorder.border = new RectOffset(3, 3, 3, 3);
                
                columnHeaderLabel.normal.background = whiteBackgroundWithBorderTexture;
                columnHeaderLabel.border = new RectOffset(3, 3, 3, 3);
                columnHeaderLabel.alignment = TextAnchor.MiddleLeft;
                columnHeaderLabel.fontStyle = FontStyle.Bold;
                columnHeaderLabel.padding.left = 10;
            }

            static Texture2D CreateTextureWithBorder(Color innerColor)
            {
                var texture = new Texture2D(5, 5);
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        texture.SetPixel(i, j, Color.black);
                    }
                }

                for (int i = 1; i < 4; i++)
                {
                    for (int j = 1; j < 4; j++)
                    {
                        texture.SetPixel(i, j, innerColor);
                    }
                }

                texture.filterMode = FilterMode.Point;
                texture.Apply();
                return texture;
            }
        }
        
        [MenuItem("Input System/Show Input Manager")]
        public static void ShowActionInspectorWindow()
        {
            var w = GetWindow<ActionInspectorWindow>("Input Manager");
            w.Show();
        }
        
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (path.EndsWith(".inputactions"))
            {

                var obj = EditorUtility.InstanceIDToObject(instanceID);
                var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
                var window = inputManagers.FirstOrDefault(w => w.m_ReferencedObject.Equals(obj));
                if (window != null)
                {
                    window.Show();
                    window.Focus();
                    return true;
                }
                window = CreateInstance<ActionInspectorWindow>();
                window.title = "Input Manager - " + obj.name;
                window.m_ReferencedObject = obj;
                window.Show();
                return true;
            }
            return false;
        }

        [SerializeField]
        Object m_ReferencedObject;
        SerializedObject m_SerializedObject;

        InputActionListTreeView m_TreeView;
        [SerializeField]
        TreeViewState m_TreeViewState;

        PropertiesView m_PropertyView;
        int m_GroupIndex;
        List<string> m_GroupPopupList;

        public void OnEnable()
        {
            InitiateTrees();
            Undo.undoRedoPerformed += OnUndoCallback;
        }

        void OnUndoCallback()
        {
            m_TreeView.Reload();
            m_TreeView.Repaint();
            if (m_TreeView.GetSelectedProperty() != null)
            {
                var p = m_TreeView.GetSelectedRow();
                if (p is BindingItem)
                {
                    m_PropertyView = new PropertiesView(p.elementProperty, Apply);
                }
            }
        }

        void InitiateTrees()
        {
            if (m_SerializedObject != null)
            {
                ParseGroups(m_ReferencedObject as InputActionAsset);
                m_TreeView = InputActionListTreeView.Create(Apply, m_ReferencedObject as InputActionAsset, m_SerializedObject, ref m_TreeViewState);
                
                m_TreeView.OnSelectionChanged = p =>
                {
                    if (p is BindingItem)
                    {
                        m_PropertyView = new PropertiesView(p.elementProperty, Apply);
                    }
                    else
                    {
                        m_PropertyView = null;
                    }
                };
                
                if (m_PropertyView == null && m_TreeView.GetSelectedProperty() != null)
                {
                    var p = m_TreeView.GetSelectedRow();
                    if (p is BindingItem)
                    {
                        m_PropertyView = new PropertiesView(p.elementProperty, Apply);
                    }
                }
            }
        }
        
        void ParseGroups(InputActionAsset actionMapAsset)
        {
            HashSet<string> allGroups = new HashSet<string>();
            allGroups.Clear();
            foreach (var actionMap in actionMapAsset.actionMaps)
            {
                foreach (var binding in actionMap.bindings)
                {
                    foreach (var group in binding.groups.Split(';'))
                    {
                        if (!string.IsNullOrEmpty(@group))
                            allGroups.Add(@group);
                    }
                }
            }
            m_GroupPopupList = new List<string>() { "<no group>" };
            m_GroupPopupList.AddRange(allGroups);
        }

        void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUI.BeginChangeCheck();
            m_ReferencedObject = EditorGUILayout.ObjectField("Input Actions Asset", m_ReferencedObject, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_ReferencedObject == null)
                {
                    m_SerializedObject = null;
                }
            }

            if (m_SerializedObject == null && m_ReferencedObject != null)
            {
                m_SerializedObject = new SerializedObject(m_ReferencedObject);
                var pr = m_SerializedObject.FindProperty("m_ActionMaps");
                if (pr == null)
                {
                    m_ReferencedObject = null;
                    m_SerializedObject = null;
                    return;
                }
                if(m_TreeView == null)
                {
                    InitiateTrees();
                }
            }

            if (m_ReferencedObject == null)
                return;

            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            m_GroupIndex = EditorGUILayout.Popup(m_GroupIndex, m_GroupPopupList.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                var filter = m_GroupIndex > 0 ? m_GroupPopupList[m_GroupIndex] : null;
                m_TreeView.FilterResults(filter);
            }
            EditorGUILayout.TextField("Search box (not implemeneted)", GUILayout.MaxWidth(200));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            DrawMainTree();
            DrawProperties();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace)
                {
                    if (m_TreeView.HasFocus())
                    {
                        DeleteSelectedRow();
                        Apply();
                        m_PropertyView = null;
                    }
                }
                
                if (Event.current.keyCode == KeyCode.C 
                    && ((Event.current.modifiers & EventModifiers.Control) != 0 
                        || (Event.current.modifiers & EventModifiers.Command) != 0))
                {
                    HandleCopyEvent();
                }
                
                if (Event.current.keyCode == KeyCode.V 
                    && ((Event.current.modifiers & EventModifiers.Control) != 0 
                        || (Event.current.modifiers & EventModifiers.Command) != 0))
                {
                    HandlePasteEvent();
                }
            }
        }

        void HandleCopyEvent()
        {
            var row = m_TreeView.GetSelectedRow().SerializeToString();
            Debug.Log(row);
            EditorGUIUtility.systemCopyBuffer = row;
        }

        void DeleteSelectedRow()
        {
            var row = m_TreeView.GetSelectedRow();
            if (row is BindingItem)
            {
                var actionMapProperty = (row.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (row.parent as InputTreeViewLine).elementProperty;
                InputActionSerializationHelpers.RemoveBinding(actionProperty, (row as BindingItem).index, actionMapProperty);
                Apply();
            }
            else if (row is ActionItem)
            {
                var actionProperty = (row.parent as InputTreeViewLine).elementProperty;
                InputActionSerializationHelpers.DeleteAction(actionProperty, (row as ActionItem).index);
            }
            else if (row is ActionSetItem)
            {
                InputActionSerializationHelpers.DeleteActionMap(m_SerializedObject, (row as InputTreeViewLine).index);
            }
        }

        void HandlePasteEvent()
        {
            var json = EditorGUIUtility.systemCopyBuffer;
            try
            {
                var map = JsonUtility.FromJson<InputActionMap>(json);
                InputActionSerializationHelpers.AddActionMapFromObject(m_SerializedObject, map);
                return;
            }
            catch (ArgumentException)
            {
            }
            EditorApplication.Beep();
        }

        void DrawMainTree()
        {
            EditorGUILayout.BeginVertical(Styles.darkGreyBackgroundWithBorder);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            var treeViewRect = GUILayoutUtility.GetLastRect();
            var labelRect = new Rect(treeViewRect);
            labelRect.height = 20;
            treeViewRect.y += 20 - 1;
            treeViewRect.height -= 20;
            treeViewRect.x += 2;
            treeViewRect.width -= 4;
            
            EditorGUI.LabelField(labelRect, "Action maps", Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - 18;
            labelRect.width = 18;
            var plusIconContext = EditorGUIUtility.IconContent("Toolbar Plus");
            if (GUI.Button(labelRect, plusIconContext, GUIStyle.none))
            {
                ShowAddMenu();
            }
            
            m_TreeView.OnGUI(treeViewRect);
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add action map"), false, OnAddActionMap);
            menu.AddItem(new GUIContent("Add action"), false, OnAddAction);
            menu.AddItem(new GUIContent("Add binding"), false, OnAddBinding);
            menu.ShowAsContext();
        }

        void OnAddBinding()
        {
            var actionMapLine = GetSelectedActionMapLine();
            var actionLine = GetSelectedActionLine();
            InputActionSerializationHelpers.AppendBinding(actionLine.elementProperty, actionMapLine.elementProperty);
            Apply();
        }

        void OnAddAction()
        {
            var actionLine = GetSelectedActionMapLine();
            InputActionSerializationHelpers.AddAction(actionLine.elementProperty);
            Apply();
        }

        void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_SerializedObject);
            Apply();
        }

        ActionItem GetSelectedActionLine()
        {
            TreeViewItem selectedRow = m_TreeView.GetSelectedRow();
            do
            {
                if (selectedRow is ActionItem)
                    return (ActionItem) selectedRow;
                selectedRow = selectedRow.parent;
            } while (selectedRow.parent != null);

            return null;
        }

        ActionSetItem GetSelectedActionMapLine()
        {
            TreeViewItem selectedRow = m_TreeView.GetSelectedRow();
            do
            {
                if (selectedRow is ActionSetItem)
                    return (ActionSetItem) selectedRow;
                selectedRow = selectedRow.parent;
            } while (selectedRow.parent != null);

            return null;
        }

        void DrawProperties()
        {
            EditorGUILayout.BeginVertical(Styles.whiteBackgroundWithBorder,GUILayout.MaxWidth(250));

            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            rect.x -= 2;
            rect.y -= 1;
            rect.width += 4;
            
            EditorGUI.LabelField(rect, "Properties", Styles.columnHeaderLabel);
            
            if (m_PropertyView != null)
                m_PropertyView.OnGUI();
            
            EditorGUILayout.EndVertical();
        }
    }
}