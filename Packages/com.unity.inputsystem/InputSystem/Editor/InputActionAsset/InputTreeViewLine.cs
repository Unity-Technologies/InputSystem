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
            public static GUIStyle borderStyle;

            public static GUIStyle actionMapItemStyle;
            public static GUIStyle actionItemStyle;
            public static GUIStyle bindingItemStyle;

            static Styles()
            {
                borderStyle = new GUIStyle("Label")
                {
                    normal =
                    {
                        background = AssetDatabase.LoadAssetAtPath<Texture2D>(
                            ActionInspectorWindow.Styles.ResourcesPath + "actionTreeBackground.png")
                    }
                };

                actionItemStyle = new GUIStyle("Label")
                {
                    normal =
                    {
                        background = AssetDatabase.LoadAssetAtPath<Texture2D>(
                            ActionInspectorWindow.Styles.ResourcesPath + "actionTreeBackgroundWithoutBorder.png")
                    },
                    border = new RectOffset(3, 3, 3, 3),
                    onFocused =
                    {
                        background = AssetDatabase.LoadAssetAtPath<Texture2D>(
                            ActionInspectorWindow.Styles.ResourcesPath + "rowSelected.png"),
                        textColor = Color.white,
                    },
                    onNormal =
                    {
                        background = AssetDatabase.LoadAssetAtPath<Texture2D>(
                            ActionInspectorWindow.Styles.ResourcesPath + "rowSelected.png"),
                        textColor = Color.white,
                    },
                    alignment = TextAnchor.MiddleLeft
                };

                actionMapItemStyle = new GUIStyle(actionItemStyle);
                bindingItemStyle = new GUIStyle(actionItemStyle);

                var isProSkin = EditorGUIUtility.isProSkin;
                var actionMapBackground = isProSkin ? "orange.png" : "yellow.png";
                var actionBackground = isProSkin ? "blue.png" : "green.png";

                // Assign colors.
                actionMapItemStyle.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        ActionInspectorWindow.Styles.SharedResourcesPath + actionMapBackground);
                actionItemStyle.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        ActionInspectorWindow.Styles.SharedResourcesPath + actionBackground);

                if (isProSkin)
                {
                    actionItemStyle.normal.textColor = Color.white;
                    actionMapItemStyle.normal.textColor = Color.white;
                }
            }
        }

        protected SerializedProperty m_ElementProperty;
        protected int m_Index;

        public virtual bool isDraggable
        {
            get { return false; }
        }

        public SerializedProperty elementProperty
        {
            get { return m_ElementProperty; }
        }

        public int index
        {
            get { return m_Index; }
        }

        protected abstract GUIStyle style
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

        public void OnGUI(Rect rowRect, bool selected, bool focused)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Border.
            Styles.borderStyle.Draw(rowRect, "", false, false, selected, focused);

            // Background.
            var rect = rowRect;
            rect.y += 1;
            rect.height -= 1;

            style.Draw(rect, "", false, false, selected, focused);

            // Label.
            var indent = (depth + 2) * 6 + 10;
            rect.x += indent;
            rect.width -= indent + 2;
            rect.y += 1;
            rect.height -= 2;

            style.Draw(rect, displayName, false, false, selected, focused);
        }

        public abstract string SerializeToString();

        public virtual InputBindingPropertiesView GetPropertiesView(Action apply, TreeViewState state)
        {
            return new InputBindingPropertiesView(elementProperty, apply, state);
        }
    }

    internal class ActionMapTreeItem : ActionTreeViewItem
    {
        public ActionMapTreeItem(SerializedProperty actionMapProperty, int index)
            : base(actionMapProperty, index)
        {
            displayName = elementProperty.FindPropertyRelative("m_Name").stringValue;
            id = displayName.GetHashCode();
        }

        protected override GUIStyle style
        {
            get { return Styles.actionMapItemStyle; }
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
            var actionMapName = "";
            if (m_ActionMapProperty != null)
                actionMapName = m_ActionMapProperty.FindPropertyRelative("m_Name").stringValue;
            id = (actionMapName + "/" + displayName).GetHashCode();
        }

        protected override GUIStyle style
        {
            get { return Styles.actionItemStyle; }
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
            InputActionSerializationHelpers.AppendBindingFromSavedProperties(values, elementProperty, m_ActionMapProperty);
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

    internal class CompositeGroupTreeItem : BindingTreeItem
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

        protected override GUIStyle style
        {
            get { return Styles.bindingItemStyle; }
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
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            displayName = elementProperty.FindPropertyRelative("name").stringValue + ": " + InputControlPath.ToHumanReadableString(path);
        }

        protected override GUIStyle style
        {
            get { return Styles.bindingItemStyle; }
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

        protected virtual int GetId(string actionMapName, int index, string action, string path, string name)
        {
            return (actionMapName + " " + action + " " + path + " " + index).GetHashCode();
        }

        protected override GUIStyle style
        {
            get { return Styles.bindingItemStyle; }
        }

        public override bool hasProperties
        {
            get { return true; }
        }

        public override string SerializeToString()
        {
            var builder = new StringBuilder();
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
