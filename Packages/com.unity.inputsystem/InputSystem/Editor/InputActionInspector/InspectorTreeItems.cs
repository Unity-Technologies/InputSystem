using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class BindingInspectorTreeItem : InputTreeViewLine
    {
        SerializedProperty m_BindingProperty;

        public BindingInspectorTreeItem(string actionMapName, SerializedProperty bindingProperty, int index) : base(bindingProperty, index)
        {
            m_BindingProperty = bindingProperty;
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            var action = elementProperty.FindPropertyRelative("action").stringValue;
            var name = elementProperty.FindPropertyRelative("name").stringValue;
            
            var flags = (InputBinding.Flags) elementProperty.FindPropertyRelative("flags").intValue;
            isComposite = (flags & InputBinding.Flags.Composite) == InputBinding.Flags.Composite;
            isPartOfComposite = (flags & InputBinding.Flags.PartOfComposite) == InputBinding.Flags.PartOfComposite;

            displayName = InputControlPath.ToHumanReadableString(path);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = "<empty>";
            }
            id = GetId(actionMapName, index, action, path, name);
            depth = 0;
            
        }

        public bool isComposite { get; private set; }
        
        public bool isPartOfComposite { get; private set; }

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
    }
    
    class CompositeGroupInspectorTreeItem : BindingTreeItem
    {
        public CompositeGroupInspectorTreeItem(SerializedProperty bindingProperty, int index)
            : base("", bindingProperty, index)
        {
            var name = elementProperty.FindPropertyRelative("name").stringValue;
            displayName = name;
            depth = 0;
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

    class CompositeInspectorTreeItem : BindingTreeItem
    {
        public CompositeInspectorTreeItem(SerializedProperty bindingProperty, int index)
            : base("", bindingProperty, index)
        {
            var path = elementProperty.FindPropertyRelative("path").stringValue;
            displayName = elementProperty.FindPropertyRelative("name").stringValue + ": " + InputControlPath.ToHumanReadableString(path);
            depth = 1;
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
}
