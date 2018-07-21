#if UNITY_EDITOR
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
            public static GUIStyle backgroundStyle = new GUIStyle("Label");
            public static GUIStyle yellowRect = new GUIStyle("Label");
            public static GUIStyle orangeRect = new GUIStyle("Label");
            public static GUIStyle greenRect = new GUIStyle("Label");
            public static GUIStyle blueRect = new GUIStyle("Label");
            public static GUIStyle pinkRect = new GUIStyle("Label");

            static string SharedResourcesPath = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
            static string ResourcesPath
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                        return SharedResourcesPath + "pro/";
                    return SharedResourcesPath + "personal/";
                }
            }

            static Styles()
            {
                backgroundStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "actionTreeBackgroundWithoutBorder.png");

                actionItemRowStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "row.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onFocused.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "rowSelected.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onNormal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "rowSelected.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);

                actionSetItemStyle.alignment = TextAnchor.MiddleLeft;
                actionItemLabelStyle.alignment = TextAnchor.MiddleLeft;

                yellowRect.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedResourcesPath + "yellow.png");
                orangeRect.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedResourcesPath + "orange.png");
                greenRect.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedResourcesPath + "green.png");
                blueRect.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedResourcesPath + "blue.png");
                pinkRect.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedResourcesPath + "pink.png");
            }
        }

        public bool renaming;
        protected SerializedProperty m_SetProperty;
        protected int m_Index;

        public virtual bool isDraggable
        {
            get { return false; }
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
            depth = 0;
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
        public virtual bool hasProperties
        {
            get { return false; }
        }

        public virtual void DrawCustomRect(Rect rowRect)
        {
            var boxRect = rowRect;
            boxRect.width = (depth + 1) * 6;
            rectStyle.Draw(boxRect, GUIContent.none, false, false, false, false);
            if (depth == 0)
                return;
            boxRect.width = 6 * depth;
            Styles.backgroundStyle.Draw(boxRect, GUIContent.none, false, false, false, false);
        }

        public abstract string SerializeToString();
    }

    class ActionMapTreeItem : InputTreeViewLine
    {
        InputActionMap m_ActionMap;

        public ActionMapTreeItem(InputActionMap actionMap, SerializedProperty actionMapProperty, int index) : base(actionMapProperty, index)
        {
            m_ActionMap = actionMap;
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            id = displayName.GetHashCode();
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.yellowRect; }
        }

        public SerializedProperty bindingsProperty
        {
            get
            {
                return elementProperty.FindPropertyRelative("m_Bindings");
            }
        }

        public SerializedProperty actionsProperty
        {
            get
            {
                return elementProperty.FindPropertyRelative("m_Actions");
            }
        }

        public override string SerializeToString()
        {
            return JsonUtility.ToJson(m_ActionMap);
        }

        public void AddAction()
        {
            InputActionSerializationHelpers.AddAction(elementProperty);
        }

        public SerializedProperty AddActionFromObject(InputAction action)
        {
            return InputActionSerializationHelpers.AddActionFromObject(action, elementProperty);
        }

        public void DeleteAction(int actionRowIndex)
        {
            InputActionSerializationHelpers.DeleteAction(elementProperty, actionRowIndex);
        }

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameActionMap(elementProperty, newName);
        }
    }

    class ActionTreeItem : InputTreeViewLine
    {
        InputAction m_Action;
        SerializedProperty m_ActionMapProperty;

        public int bindingsStartIndex
        {
            get { return m_Action.m_BindingsStartIndex; }
        }

        public int bindingsCount
        {
            get { return m_Action.m_BindingsCount; }
        }

        public ActionTreeItem(SerializedProperty actionMapProperty, InputAction action, SerializedProperty setProperty, int index)
            : base(setProperty, index)
        {
            m_Action = action;
            m_ActionMapProperty = actionMapProperty;
            var actionMapName = m_ActionMapProperty.FindPropertyRelative("m_Name").stringValue;
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            id = (actionMapName + "/" + displayName).GetHashCode();
            depth = 1;
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.greenRect; }
        }

        public override string SerializeToString()
        {
            return JsonUtility.ToJson(m_Action);
        }

        public void AppendCompositeBinding(string compositeName)
        {
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            InputActionSerializationHelpers.AppendCompositeBinding(elementProperty, m_ActionMapProperty, compositeType);
        }

        public void AppendBinding()
        {
            InputActionSerializationHelpers.AppendBinding(elementProperty, m_ActionMapProperty);
        }

        public void AppendBindingFromObject(InputBinding binding)
        {
            InputActionSerializationHelpers.AppendBindingFromObject(binding, elementProperty, m_ActionMapProperty);
        }

        public void RemoveBinding(int compositeIndex)
        {
            InputActionSerializationHelpers.RemoveBinding(elementProperty, compositeIndex, m_ActionMapProperty);
        }

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameAction(elementProperty, m_ActionMapProperty, newName);
        }
    }

    class CompositeGroupTreeItem : BindingTreeItem
    {
        public CompositeGroupTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index)
            : base(actionMapName, binding, bindingProperty, index)
        {
            var name = elementProperty.FindPropertyRelative("name").stringValue;
            displayName = name;
        }

        protected override int GetId(string actionMapName, int index, string action, string path, string name)
        {
            return (actionMapName + " " + action + " " + name + " " + index).GetHashCode();
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.blueRect; }
        }

        public override bool hasProperties
        {
            get { return false; }
        }

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameComposite(elementProperty, newName);
        }
    }

    class CompositeTreeItem : BindingTreeItem
    {
        public CompositeTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index)
            : base(actionMapName, binding, bindingProperty, index)
        {
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            displayName = elementProperty.FindPropertyRelative("name").stringValue + ": " + ParseName(path);
            depth++;
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.pinkRect; }
        }

        public override bool isDraggable
        {
            get { return false; }
        }
    }

    class BindingTreeItem : InputTreeViewLine
    {
        InputBinding m_InputBinding;
        SerializedProperty m_BindingProperty;

        public BindingTreeItem(string actionMapName, InputBinding binding, SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
        {
            m_InputBinding = binding;
            m_BindingProperty = bindingProperty;
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            var action = elementProperty.FindPropertyRelative("action").stringValue;
            var name = elementProperty.FindPropertyRelative("name").stringValue;
            displayName = ParseName(path);
            id = GetId(actionMapName, index, action, path, name);
            depth = 2;
        }

        public override bool isDraggable
        {
            get { return true; }
        }

        public override SerializedProperty elementProperty
        {
            get { return m_BindingProperty; }
        }

        protected virtual int GetId(string actionMapName, int index, string action, string path, string name)
        {
            return (actionMapName + " " + action + " " + path + " " + index).GetHashCode();
        }

        static Regex s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
        static Regex s_ControlRegex = new Regex("<([A-Za-z0-9:\\-]+)>({([A-Za-z0-9]+)})?/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

        const int kUsageNameGroup = 1;
        const int kDeviceNameGroup = 1;
        const int kDeviceUsageGroup = 3;
        const int kControlPathGroup = 4;

        internal static string ParseName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "<empty>";
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
            get { return Styles.blueRect; }
        }

        public override void DrawCustomRect(Rect rowRect)
        {
            var boxRect = rowRect;
            boxRect.width = (depth + 1) * 6;
            rectStyle.Draw(boxRect, GUIContent.none, false, false, false, false);
            boxRect.width = 6 * depth;
            Styles.backgroundStyle.Draw(boxRect, GUIContent.none, false, false, false, false);
        }

        public override string SerializeToString()
        {
            return JsonUtility.ToJson(m_InputBinding);
        }

        public override bool hasProperties
        {
            get { return true; }
        }
    }
}
#endif // UNITY_EDITOR
