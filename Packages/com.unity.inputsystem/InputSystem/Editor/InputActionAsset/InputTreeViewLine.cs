#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal abstract class ActionTreeViewItem : TreeViewItem
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

            static Styles()
            {
                backgroundStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    InputActionTreeBase.ResourcesPath + "actionTreeBackgroundWithoutBorder.png");

                actionItemRowStyle.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(InputActionTreeBase.ResourcesPath + "row.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onFocused.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "rowSelected.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);
                actionItemRowStyle.onNormal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "rowSelected.png");
                actionItemRowStyle.border = new RectOffset(3, 3, 3, 3);

                actionSetItemStyle.alignment = TextAnchor.MiddleLeft;
                actionItemLabelStyle.alignment = TextAnchor.MiddleLeft;

                yellowRect.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.SharedResourcesPath + "yellow.png");
                orangeRect.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.SharedResourcesPath + "orange.png");
                greenRect.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.SharedResourcesPath + "green.png");
                blueRect.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.SharedResourcesPath + "blue.png");
                pinkRect.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.SharedResourcesPath + "pink.png");
            }
        }

        public bool renaming;
        protected SerializedProperty m_ElementProperty;
        protected int m_Index;

        public virtual bool isDraggable
        {
            get { return false; }
        }

        public virtual SerializedProperty elementProperty
        {
            get { return m_ElementProperty; }
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

        public ActionTreeViewItem(SerializedProperty elementProperty, int index)
        {
            m_ElementProperty = elementProperty;
            m_Index = index;
            depth = 0;
        }

        public void OnGUI(Rect rowRect, bool selected, bool focused, float indent)
        {
            var rect = rowRect;
            if (Event.current.type != EventType.Repaint)
                return;

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

        public virtual void DrawCustomRect(Rect rowRect)
        {
            var boxRect = rowRect;
            boxRect.width = (depth + 1) * 6;
            boxRect.height -= 2;
            boxRect.y += 1;
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

        public abstract int GetIdForName(string argsNewName);
    }

    internal class ActionMapTreeItem : ActionTreeViewItem
    {
        public ActionMapTreeItem(SerializedProperty actionMapProperty, int index)
            : base(actionMapProperty, index)
        {
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            id = GetIdForName(displayName);
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
            return InputActionSerializationHelpers.AddActionFromSavedProperties(parameters, elementProperty);
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
            var builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "m_Name", elementProperty.FindPropertyRelative("m_Name").stringValue);
            return builder.ToString();
        }

        public override int GetIdForName(string name)
        {
            return name.GetHashCode();
        }

        public override bool isDraggable
        {
            get { return true; }
        }
    }

    internal class ActionTreeItem : ActionTreeViewItem
    {
        private SerializedProperty m_ActionMapProperty;

        public int bindingsStartIndex { get; private set; }
        public int bindingsCount { get; private set; }
        public string actionName { get; private set; }

        public ActionTreeItem(SerializedProperty actionMapProperty, SerializedProperty actionProperty, int index)
            : base(actionProperty, index)
        {
            m_ActionMapProperty = actionMapProperty;
            actionName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            if (m_ActionMapProperty != null)
            {
                bindingsStartIndex = InputActionSerializationHelpers.GetBindingsStartIndex(m_ActionMapProperty.FindPropertyRelative("m_Bindings"), actionName);
                bindingsCount = InputActionSerializationHelpers.GetBindingCount(m_ActionMapProperty.FindPropertyRelative("m_Bindings"), actionName);
            }
            else
            {
                bindingsStartIndex = 0;
                bindingsCount = InputActionSerializationHelpers.GetBindingCount(elementProperty.FindPropertyRelative("m_SingletonActionBindings"), actionName);
            }
            displayName = actionName;
            id = GetIdForName(displayName);
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.greenRect; }
        }

        public override bool isDraggable
        {
            get { return true; }
        }

        public void AddCompositeBinding(string compositeName)
        {
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            InputActionSerializationHelpers.AddCompositeBinding(elementProperty, m_ActionMapProperty, compositeName, compositeType);
        }

        public void AddBinding()
        {
            InputActionSerializationHelpers.AddBinding(elementProperty, m_ActionMapProperty);
        }

        public void AddBindingFromSavedProperties(Dictionary<string, string> values)
        {
            InputActionSerializationHelpers.AddBindingFromSavedProperties(values, elementProperty, m_ActionMapProperty);
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
            var builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "m_Name", elementProperty.FindPropertyRelative("m_Name").stringValue);
            return builder.ToString();
        }

        public override int GetIdForName(string name)
        {
            var actionMapName = "";
            if (m_ActionMapProperty != null)
                actionMapName = m_ActionMapProperty.FindPropertyRelative("m_Name").stringValue;
            return (actionMapName + "/" + name).GetHashCode();
        }
    }

    internal class CompositeGroupTreeItem : BindingTreeItem
    {
        public CompositeGroupTreeItem(string actionMapName, SerializedProperty bindingProperty, int index)
            : base(actionMapName, bindingProperty, index)
        {
            var name = elementProperty.FindPropertyRelative("m_Name").stringValue;
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

    internal class CompositeTreeItem : BindingTreeItem
    {
        public CompositeTreeItem(string actionMapName, SerializedProperty bindingProperty, int index)
            : base(actionMapName, bindingProperty, index)
        {
            var path = elementProperty.FindPropertyRelative("m_Path").stringValue;
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue + ": " + InputControlPath.ToHumanReadableString(path);
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

    internal class BindingTreeItem : ActionTreeViewItem
    {
        public BindingTreeItem(string actionMapName, SerializedProperty bindingProperty, int index)
            : base(bindingProperty, index)
        {
            path = elementProperty.FindPropertyRelative("m_Path").stringValue;
            groups = elementProperty.FindPropertyRelative("m_Groups").stringValue;
            action = elementProperty.FindPropertyRelative("m_Action").stringValue;
            name = elementProperty.FindPropertyRelative("m_Name").stringValue;

            var flags = (InputBinding.Flags)elementProperty.FindPropertyRelative("m_Flags").intValue;
            isComposite = (flags & InputBinding.Flags.Composite) == InputBinding.Flags.Composite;
            isPartOfComposite = (flags & InputBinding.Flags.PartOfComposite) == InputBinding.Flags.PartOfComposite;

            displayName = InputControlPath.ToHumanReadableString(path);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "Empty Binding";
            }

            m_ActionMapName = actionMapName;
            id = GetIdForName(name);
        }

        private string m_ActionMapName;
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

        protected virtual int GetId(string actionMapName, int index, string action, string path, string name)
        {
            return (actionMapName + " " + action + " " + index).GetHashCode();
        }

        protected override GUIStyle rectStyle
        {
            get { return Styles.blueRect; }
        }

        public override void DrawCustomRect(Rect rowRect)
        {
            var boxRect = rowRect;
            boxRect.width = (depth + 1) * 6;
            boxRect.y += 1;
            boxRect.height -= 2;
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
            var builder = new StringBuilder();
            builder.AppendFormat("{0}={1}\n", "name", elementProperty.FindPropertyRelative("m_Name").stringValue);
            builder.AppendFormat("{0}={1}\n", "path", elementProperty.FindPropertyRelative("m_Path").stringValue);
            builder.AppendFormat("{0}={1}\n", "groups", elementProperty.FindPropertyRelative("m_Groups").stringValue);
            builder.AppendFormat("{0}={1}\n", "interactions", elementProperty.FindPropertyRelative("m_Interactions").stringValue);
            builder.AppendFormat("{0}={1}\n", "flags", elementProperty.FindPropertyRelative("m_Flags").intValue);
            builder.AppendFormat("{0}={1}\n", "action", elementProperty.FindPropertyRelative("m_Action").stringValue);
            return builder.ToString();
        }

        public override int GetIdForName(string name)
        {
            return GetId(m_ActionMapName, index, action, path, name);
        }
    }
}
#endif // UNITY_EDITOR
