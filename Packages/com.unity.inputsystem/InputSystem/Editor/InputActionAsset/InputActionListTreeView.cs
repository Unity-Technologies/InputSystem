using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal partial class InputActionListTreeView
    {
        public static InputActionListTreeView Create(Action applyAction, InputActionAsset asset, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();
            
            var treeView = new InputActionListTreeView(applyAction, asset, serializedObject, treeViewState);
            treeView.ExpandAll();
            return treeView;
        }

        private InputActionAsset m_Asset;
        private SerializedObject m_SerializedObject;

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
                
                ParseActionMap(actionItem, m_Asset.actionMaps[i], actionMapsProperty.GetArrayElementAtIndex(i));
            
                root.AddChild(actionItem);
            }
            return root;
        }

        private void ParseActionMap(TreeViewItem treeViewItem, InputActionMap actionMap, SerializedProperty actionMapProperty)
        {
            var bindingsProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            var actionsProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            
            for (var i = 0; i < actionMap.actions.Count; i++)
            {
                var action = actionMap.actions[i];
                var actionItem = new ActionItem(actionsProperty, i);
                treeViewItem.AddChild(actionItem);
                
                //need to access actions.binding to get m_BindingsStartIndex populated?
                if(!action.bindings.Any())
                    continue;
                
                for (var j = action.m_BindingsStartIndex; j < action.m_BindingsStartIndex + action.m_BindingsCount; j++)
                {
                    var bindingsItem = new BindingItem(bindingsProperty, j);
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

            public override void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
            {
                var rect = rowRect;
                if (Event.current.type == EventType.Repaint)
                {
                    rowRect.height += 1;
                    Styles.actionItemRowStyle.Draw(rowRect, "", false, false, selected, focused);

                    rect.x += indent;
                    rect.width -= indent + 2;
                    rect.y += 1;
                    rect.height -= 2;
                    
                    if(!renaming)
                        Styles.actionSetItemStyle.Draw(rect, displayName, false, false, selected, focused);

                    var orangeBoxRect = rowRect;
                    orangeBoxRect.width = 12;
                    Styles.yellowRect.Draw(orangeBoxRect, "", false, false, false, false);
                }
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

            public override void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
            {
                var rect = rowRect;
                if (Event.current.type == EventType.Repaint)
                {
                    rowRect.height += 1;
                    Styles.actionItemRowStyle.Draw(rowRect, "", false, false, selected, focused);

                    rect.x += indent;
                    rect.width -= indent + 2;
                    rect.y += 1;
                    rect.height -= 2;
                    
                    if(!renaming)
                        Styles.actionSetItemStyle.Draw(rect, displayName, false, false, selected, focused);

                    var orangeBoxRect = rowRect;
                    orangeBoxRect.width = 24;
                    Styles.orangeRect.Draw(orangeBoxRect, "", false, false, false, false);
                }
            }
        }
        
        internal class BindingItem : InputTreeViewLine
        {
            public BindingItem(SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
            {
                var path = elementProperty.FindPropertyRelative("path").stringValue;
                var action = elementProperty.FindPropertyRelative("action").stringValue;
                displayName = ParseName(path);
                id = (action + " " + path + " " + index).GetHashCode();
                depth = 2;
            }
            
            private static Regex s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            private static Regex s_ControlRegex = new Regex("<([A-Za-z0-9:\\-]+)>({([A-Za-z0-9]+)})?/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");
            
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

            public override void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
            {
                var rect = rowRect;
                if (Event.current.type == EventType.Repaint)
                {
                    rowRect.height += 1;
                    Styles.actionItemRowStyle.Draw(rowRect, "", false, false, selected, focused);

                    rect.x += indent;
                    rect.width -= indent + 2;
                    rect.y += 1;
                    rect.height -= 2;

                    if (!renaming)
                        Styles.actionSetItemStyle.Draw(rect, displayName, false, false, selected, focused);

                    var boxRect = rowRect;
                    boxRect.width = 36;
                    Styles.greenRect.Draw(boxRect, "", false, false, false, false);
                }
            }
        }

        public void DeleteSelected()
        {
            throw new NotImplementedException();
        }
    }
}
