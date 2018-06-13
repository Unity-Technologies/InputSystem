using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    abstract class InputTreeViewLine : TreeViewItem
    {
        protected static class Styles
        {
            public static GUIStyle actionItemRowStyle = new GUIStyle("Label");
            public static GUIStyle actionSetItemStyle = new GUIStyle("Label");
            public static GUIStyle actionItemLabelStyle = new GUIStyle("Label");
            
            public static GUIStyle yellowRect = new GUIStyle("Label");
            public static GUIStyle orangeRect = new GUIStyle("Label");
            public static GUIStyle greenRect = new GUIStyle("Label");
            public static GUIStyle blueRect = new GUIStyle("Label");
            public static GUIStyle cyanRect = new GUIStyle("Label");
            public static GUIStyle magentaRect = new GUIStyle("Label");
            
            static Styles()
            {
                var whiteBackgroundWithBorderTexture = CreateTextureWithBorder(Color.white);
                var blueBackgroundWithBorderTexture = CreateTextureWithBorder(new Color32(62, 125, 231, 255));
                
                actionItemRowStyle.normal.background = whiteBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onFocused.background = blueBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onNormal.background = blueBackgroundWithBorderTexture;
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionSetItemStyle.alignment = TextAnchor.MiddleLeft;
    
                actionItemLabelStyle.alignment = TextAnchor.MiddleLeft;
    
                yellowRect.normal.background = CreateTextureWithBorder(new Color(256f/256f, 230f/256f, 148f/256f));
                yellowRect.border = new RectOffset(2, 2, 2, 2);
                orangeRect.normal.background = CreateTextureWithBorder(new Color(246f/256f, 192f/256f, 129f/256f));
                orangeRect.border = new RectOffset(2, 2, 2, 2);
                greenRect.normal.background = CreateTextureWithBorder(new Color(168/256f, 208/256f, 152/256f));
                greenRect.border = new RectOffset(2, 2, 2, 2);
                blueRect.normal.background = CreateTextureWithBorder(new Color(162/256f, 196/256f, 200/256f));
                blueRect.border = new RectOffset(2, 2, 2, 2);
                cyanRect.normal.background = CreateTextureWithBorder(Color.cyan);
                cyanRect.border = new RectOffset(2, 2, 2, 2);
                magentaRect.normal.background = CreateTextureWithBorder(Color.magenta);
                magentaRect.border = new RectOffset(2, 2, 2, 2);
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
        
        public bool renaming;
            
        protected SerializedProperty m_SetProperty;
        protected SerializedProperty m_ElementProperty;
        protected int m_Index;
            
        public SerializedProperty setProperty
        {
            get { return m_SetProperty; }
        }
    
        public virtual SerializedProperty elementProperty
        {
            get { return m_SetProperty.GetArrayElementAtIndex(index); }
        }
        public int index
        {
            get { return m_Index; }
        }
    
        public InputTreeViewLine(SerializedProperty setProperty, int index)
        {
            m_SetProperty = setProperty;
            m_Index = index;
            depth = 1;
        }
    
        public void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
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
    
                DrawCustomRect(rowRect);
            }
        }
    
        protected abstract GUIStyle rectStyle { get; }
            
        public virtual void DrawCustomRect(Rect rowRect)
        {
            var boxRect = rowRect;
            boxRect.width = depth * 12;
            rectStyle.Draw(boxRect, "", false, false, false, false);
        }
    
        public abstract string SerializeToString();
    }
    
    class ActionMapTreeItem : InputTreeViewLine
    {
        InputActionMap m_ActionMap;
        public ActionMapTreeItem(InputActionMap actionMap, SerializedProperty setProperty, int index) : base(setProperty, index)
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
    
    class ActionTreeItem : InputTreeViewLine
    {
        InputAction m_Action;
        
        public int bindingsStartIndex
        {
            get { return m_Action.m_BindingsStartIndex; }
        }
        
        public int bindingsCount
        {
            get { return m_Action.m_BindingsCount; }
        }

        public ActionTreeItem(string actionMapName, InputAction action, SerializedProperty setProperty, int index)
            : base(setProperty, index)
        {
            m_Action = action;
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            id = (actionMapName + "/" + displayName).GetHashCode();
            depth = 2;
        }
        

        protected override GUIStyle rectStyle
        {
            get { return Styles.orangeRect; }
        }

        public IList<InputBinding> GetAllBindings()
        {
            return m_Action.bindings.ToList();
        }

        public override string SerializeToString()
        {
            //TODO Need to add bindings as well    
            return JsonUtility.ToJson(m_Action);
        }
    }
    
    class CompositeGroupTreeItem : BindingTreeItem
    {
        public CompositeGroupTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index) 
            : base(actionMapName, binding, bindingProperty, index)
        {
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            displayName = path;
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.greenRect; }
        }
    }
    
    class CompositeTreeItem : BindingTreeItem
    {
        public CompositeTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index) 
            : base(actionMapName, binding, bindingProperty, index)
        {
            depth++;
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.cyanRect; }
        }
    }
    
    class BindingTreeItem : InputTreeViewLine
    {
        public BindingTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
        {
            m_InputBinding = binding;
            m_BindingProperty = bindingProperty;
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            var action = elementProperty.FindPropertyRelative("action").stringValue;
            displayName = ParseName(path);
            id = (actionMapName + " " + action + " " + path + " " + index).GetHashCode();
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
