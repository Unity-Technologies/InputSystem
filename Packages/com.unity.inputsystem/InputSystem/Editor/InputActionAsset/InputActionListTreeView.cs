using System;
using System.Collections.Generic;
using System.Linq;
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
        string m_GroupFilter;

        protected InputActionListTreeView(Action applyAction,  InputActionAsset asset, SerializedObject serializedObject, TreeViewState state)
            : base(state)
        {
            
            m_ApplyAction = applyAction;
            m_Asset = asset;
            m_SerializedObject = serializedObject;
            Reload();
        }

        public void FilterResults(string filter)
        {
            m_GroupFilter = filter;
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
                var actionMap = m_Asset.actionMaps[i];
                var actionItem = new ActionSetItem(actionMap, actionMapsProperty, i);
                ParseActionMap(actionItem, actionMap, actionItem.elementProperty);
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
                
                var actionItem = new ActionItem(action, actionsArrayProperty, i);
                treeViewItem.AddChild(actionItem);

                var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(bindingsArrayProperty, actionName);

                CompositeGroupItem compositeGroupItem = null;
                for (var j = 0; j < bindingsCount; j++)
                {
                    var bindingProperty = InputActionSerializationHelpers.GetBinding(bindingsArrayProperty, actionName, j);
                    var binding = action.bindings[j];
                    if(!string.IsNullOrEmpty(m_GroupFilter) && !binding.groups.Split(';').Contains(m_GroupFilter))
                    {
                        continue;
                    }
                    if (binding.isComposite)
                    {
                        compositeGroupItem = new CompositeGroupItem(binding, bindingProperty, j);
                        actionItem.AddChild(compositeGroupItem);
                        continue;
                    }
                    if (binding.isPartOfComposite)
                    {
                        var compositeItem = new CompositeItem(binding, bindingProperty, j);
                        if(compositeGroupItem != null)
                            compositeGroupItem.AddChild(compositeItem);
                        continue;
                    }
                    compositeGroupItem = null;
                    var bindingsItem = new BindingItem(binding, bindingProperty, j);
                    actionItem.AddChild(bindingsItem);
                }
            }
        }

        internal class ActionSetItem : InputTreeViewLine
        {
            InputActionMap m_ActionMap;
            public ActionSetItem(InputActionMap actionMap, SerializedProperty setProperty, int index) : base(setProperty, index)
            {
                m_ActionMap = actionMap;
                displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
                id = displayName.GetHashCode();
            }
            
            protected override GUIStyle rectStyle
            {
                get { return Styles.yellowRect; }
            }

            public override string SerializeToString()
            {
                return JsonUtility.ToJson(m_ActionMap);
            }
        }


        internal class ActionItem : InputTreeViewLine
        {
            InputAction m_Action;

            public ActionItem(InputAction action, SerializedProperty setProperty, int index)
                : base(setProperty, index)
            {
                m_Action = action;
                displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
                id = displayName.GetHashCode();
                depth = 2;
            }

            protected override GUIStyle rectStyle
            {
                get { return Styles.orangeRect; }
            }

            public override string SerializeToString()
            {
                //TODO Need to add bindings as well    
                return JsonUtility.ToJson(m_Action);
            }
        }

        internal class CompositeGroupItem : BindingItem
        {
            public CompositeGroupItem(InputBinding binding, SerializedProperty bindingProperty, int index) : base(binding, bindingProperty, index)
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
            public CompositeItem(InputBinding binding, SerializedProperty bindingProperty, int index) : base(binding, bindingProperty, index)
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
            public BindingItem(InputBinding binding, SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
            {
                m_InputBinding = binding;
                m_BindingProperty = bindingProperty;
                var path = elementProperty.FindPropertyRelative("path").stringValue;
                var action = elementProperty.FindPropertyRelative("action").stringValue;
                displayName = ParseName(path);
                id = (action + " " + path + " " + index).GetHashCode();
                depth = 2;
            }

            InputBinding m_InputBinding;
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

            public override string SerializeToString()
            {
                return JsonUtility.ToJson(m_InputBinding);
            }
        }
    }
}
