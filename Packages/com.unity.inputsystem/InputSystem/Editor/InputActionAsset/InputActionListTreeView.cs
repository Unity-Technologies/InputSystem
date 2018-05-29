using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    partial class InputActionListTreeView
    {
        public static InputActionListTreeView Create(Action applyAction, InputActionAsset asset, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();
            
            var treeView = new InputActionListTreeView(applyAction, asset, serializedObject, treeViewState);
            treeView.ExpandAll();
            return treeView;
        }

        InputActionAsset m_Asset;
        SerializedObject m_SerializedObject;

        protected InputActionListTreeView(Action applyAction,  InputActionAsset asset, SerializedObject serializedObject, TreeViewState state)
            : base(state)
        {
            
            m_ApplyAction = applyAction;
            m_Asset = asset;
            m_SerializedObject = serializedObject;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };

            root.children = new List<TreeViewItem>();
            m_SerializedObject.Update();
            var actionMapsProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < m_Asset.actionMaps.Count; i++)
            {
                var actionItem = new ActionSetItem(actionMapsProperty, i);
                ParseActionMap(actionItem, m_Asset.actionMaps[i], actionItem.elementProperty);
                root.AddChild(actionItem);
            }
            return root;
        }

        void ParseActionMap(TreeViewItem treeViewItem, InputActionMap actionMap, SerializedProperty actionMapProperty)
        {
            var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            
            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(i);

                var action = actionMap.actions[i];
                
                var actionItem = new ActionItem(actionsArrayProperty, i);
                treeViewItem.AddChild(actionItem);

                var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);

                CompositeGroupItem compositeGroupItem = null;
                for (var j = 0; j < bindingsCount; j++)
                {
                    var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                    var binding = action.bindings[j];
                    if (binding.isComposite)
                    {
                        compositeGroupItem = new CompositeGroupItem(bindingProperty, j);
                        actionItem.AddChild(compositeGroupItem);
                        continue;
                    }
                    if (binding.isPartOfComposite)
                    {
                        var compositeItem = new CompositeItem(bindingProperty, j);
                        if(compositeGroupItem != null)
                            compositeGroupItem.AddChild(compositeItem);
                        continue;
                    }
                    compositeGroupItem = null;
                    var bindingsItem = new BindingItem(bindingProperty, j);
                    actionItem.AddChild(bindingsItem);
                }
            }
        }

        internal class ActionSetItem : InputTreeViewLine
        {
            public ActionSetItem(SerializedProperty setProperty, int index) : base(setProperty, index)
            {
                displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
                id = displayName.GetHashCode();
            }
            
            protected override GUIStyle rectStyle
            {
                get { return Styles.yellowRect; }
            }
        }
        

        internal class ActionItem : InputTreeViewLine
        {
            public ActionItem(SerializedProperty setProperty, int index) : base(setProperty, index)
            {
                displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
                id = displayName.GetHashCode();
                depth = 2;
            }

            protected override GUIStyle rectStyle
            {
                get { return Styles.orangeRect; }
            }
        }

        internal class CompositeGroupItem : BindingItem
        {
            public CompositeGroupItem(SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
            {
                var path = elementProperty.FindPropertyRelative("path").stringValue;
                displayName = path;
                depth++;
            }

            protected override GUIStyle rectStyle
            {
                get { return Styles.cyanRect; }
            }
        }

        internal class CompositeItem : BindingItem
        {
            public CompositeItem(SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
            {
                depth++;
            }

            protected override GUIStyle rectStyle
            {
                get { return Styles.cyanRect; }
            }
        }

        internal class BindingItem : InputTreeViewLine
        {
            public BindingItem(SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
            {
                m_BindingProperty = bindingProperty;
                var path = elementProperty.FindPropertyRelative("path").stringValue;
                var action = elementProperty.FindPropertyRelative("action").stringValue;
                displayName = ParseName(path);
                id = (action + " " + path + " " + index).GetHashCode();
                depth = 2;
            }

            SerializedProperty m_BindingProperty;
            public override SerializedProperty elementProperty
            {
                get { return m_BindingProperty; }
            }

            static Regex s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            static Regex s_ControlRegex = new Regex("<([A-Za-z0-9:\\-]+)>({([A-Za-z0-9]+)})?/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

            const int kUsageNameGroup = 1;
            const int kDeviceNameGroup = 1;
            const int kDeviceUsageGroup = 3;
            const int kControlPathGroup = 4;
            
            internal static string ParseName(string path)
            {
                string text = "";
                var usageMatch = s_UsageRegex.Match(path);
                if (usageMatch.Success)
                {
                    text = usageMatch.Groups[kUsageNameGroup].Value;
                }
                else
                {
                    var controlMatch = s_ControlRegex.Match(path);
                    if (controlMatch.Success)
                    {
                        var device = controlMatch.Groups[kDeviceNameGroup].Value;
                        var deviceUsage = controlMatch.Groups[kDeviceUsageGroup].Value;
                        var control = controlMatch.Groups[kControlPathGroup].Value;

                        if (!string.IsNullOrEmpty(deviceUsage))
                            text = string.Format("{0} {1} {2}", deviceUsage, device, control);
                        else
                            text = string.Format("{0} {1}", device, control);
                    }
                }

                return text;
            }

            protected override GUIStyle rectStyle
            {
                get { return Styles.greenRect; }
            }
            
            public override void DrawCustomRect(Rect rowRect)
            {
                var boxRect = rowRect;
                boxRect.width = (1 + depth) * 10;
                rectStyle.Draw(boxRect, "", false, false, false, false);
            }
        }

        public void DeleteSelected()
        {
            var row = GetSelectedRow();
            if (row is BindingItem)
            {
                var actionMapProperty = (row.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (row.parent as InputTreeViewLine).elementProperty;
                InputActionSerializationHelpers.RemoveBinding(actionProperty, (row as BindingItem).index, actionMapProperty);
                m_ApplyAction();
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
    }
}
