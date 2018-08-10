#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
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

        protected abstract GUIStyle rectStyle
        {
            get;
        }

        public virtual bool hasProperties
        {
            get { return false; }
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

        public virtual InputBindingPropertiesView GetPropertiesView(Action apply, TreeViewState state)
        {
            return new InputBindingPropertiesView(elementProperty, apply, state);
        }
    }

    class ActionMapTreeItem : InputTreeViewLine
    {
        public ActionMapTreeItem(SerializedProperty actionMapProperty, int index) : base(actionMapProperty, index)
        {
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

        public void AddAction()
        {
            InputActionSerializationHelpers.AddAction(elementProperty);
        }

        public SerializedProperty AddActionFromObject(Dictionary<string, string> parameters)
        {
            return InputActionSerializationHelpers.AddActionFromObject(parameters, elementProperty);
        }

        public void DeleteAction(int actionRowIndex)
        {
            InputActionSerializationHelpers.DeleteAction(elementProperty, actionRowIndex);
        }

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameActionMap(elementProperty, newName);
        }

        public override string SerializeToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "m_Name", elementProperty.FindPropertyRelative("m_Name").stringValue);
            return builder.ToString();
        }
    }

    class ActionTreeItem : InputTreeViewLine
    {
        SerializedProperty m_ActionMapProperty;

        public int bindingsStartIndex { get; private set; }
        public int bindingsCount { get; private set; }
        public string actionName { get; private set; }

        public ActionTreeItem(SerializedProperty actionMapProperty, SerializedProperty setProperty, int index)
            : base(setProperty, index)
        {
            m_ActionMapProperty = actionMapProperty;
            actionName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            bindingsStartIndex = InputActionSerializationHelpers.GetBindingsStartIndex(m_ActionMapProperty.FindPropertyRelative("m_Bindings"), actionName);
            bindingsCount = InputActionSerializationHelpers.GetBindingCount(m_ActionMapProperty.FindPropertyRelative("m_Bindings"), actionName);
            displayName = actionName;
            var actionMapName = m_ActionMapProperty.FindPropertyRelative("m_Name").stringValue;
            id = (actionMapName + "/" + displayName).GetHashCode();
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.greenRect; }
        }

        public void AppendCompositeBinding(string compositeName)
        {
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            InputActionSerializationHelpers.AppendCompositeBinding(elementProperty, m_ActionMapProperty, compositeName, compositeType);
        }

        public void AppendBinding()
        {
            InputActionSerializationHelpers.AppendBinding(elementProperty, m_ActionMapProperty);
        }

        public void AppendBindingFromObject(Dictionary<string, string> values)
        {
            InputActionSerializationHelpers.AppendBindingFromObject(values, elementProperty, m_ActionMapProperty);
        }

        public void RemoveBinding(int compositeIndex)
        {
            InputActionSerializationHelpers.RemoveBinding(elementProperty, compositeIndex, m_ActionMapProperty);
        }

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameAction(elementProperty, m_ActionMapProperty, newName);
        }

        public override string SerializeToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "m_Name", elementProperty.FindPropertyRelative("m_Name").stringValue);
            return builder.ToString();
        }
    }

    class CompositeGroupTreeItem : BindingTreeItem
    {
        public CompositeGroupTreeItem(string actionMapName, SerializedProperty bindingProperty, int index)
            : base(actionMapName, bindingProperty, index)
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

        public void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameComposite(elementProperty, newName);
        }

        public override InputBindingPropertiesView GetPropertiesView(Action apply, TreeViewState state)
        {
            return new CompositeGroupPropertiesView(elementProperty, apply, state);
        }
    }

    class CompositeTreeItem : BindingTreeItem
    {
        public CompositeTreeItem(string actionMapName, SerializedProperty bindingProperty, int index)
            : base(actionMapName, bindingProperty, index)
        {
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            displayName = elementProperty.FindPropertyRelative("name").stringValue + ": " + InputControlPath.ToHumanReadableString(path);
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
        SerializedProperty m_BindingProperty;

        public BindingTreeItem(string actionMapName, SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
        {
            m_BindingProperty = bindingProperty;
            path = elementProperty.FindPropertyRelative("path").stringValue;
            groups = elementProperty.FindPropertyRelative("groups").stringValue;
            action = elementProperty.FindPropertyRelative("action").stringValue;
            name = elementProperty.FindPropertyRelative("name").stringValue;

            var flags = (InputBinding.Flags)elementProperty.FindPropertyRelative("flags").intValue;
            isComposite = (flags & InputBinding.Flags.Composite) == InputBinding.Flags.Composite;
            isPartOfComposite = (flags & InputBinding.Flags.PartOfComposite) == InputBinding.Flags.PartOfComposite;

            displayName = InputControlPath.ToHumanReadableString(path);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "<empty>";
            }
            id = GetId(actionMapName, index, action, path, name);
        }

        public bool isComposite { get; private set; }
        public bool isPartOfComposite { get; private set; }
        public string path { get; private set; }
        public string groups { get; private set; }
        public string action { get; private set; }
        public string name { get; private set; }

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

        public override bool hasProperties
        {
            get { return true; }
        }

        public override string SerializeToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "name", elementProperty.FindPropertyRelative("name").stringValue);
            builder.AppendFormat("{0}={1}\n", "path", elementProperty.FindPropertyRelative("path").stringValue);
            builder.AppendFormat("{0}={1}\n", "groups", elementProperty.FindPropertyRelative("groups").stringValue);
            builder.AppendFormat("{0}={1}\n", "interactions", elementProperty.FindPropertyRelative("interactions").stringValue);
            builder.AppendFormat("{0}={1}\n", "flags", elementProperty.FindPropertyRelative("flags").intValue);
            builder.AppendFormat("{0}={1}\n", "action", elementProperty.FindPropertyRelative("action").stringValue);
            return builder.ToString();
        }
    }
}
#endif // UNITY_EDITOR
