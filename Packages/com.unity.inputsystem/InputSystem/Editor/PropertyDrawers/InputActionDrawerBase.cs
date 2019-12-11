#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Base class for property drawers that display input actions.
    /// </summary>
    internal abstract class InputActionDrawerBase : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitTreeIfNeeded(property);
            return m_TreeView.totalHeight;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitTreeIfNeeded(property);

            EditorGUI.BeginProperty(position, label, property);
            SetNameIfNotSet(property);

            m_TreeView.OnGUI(position);

            EditorGUI.EndProperty();
        }

        private void InitTreeIfNeeded(SerializedProperty property)
        {
            // NOTE: Unlike InputActionEditorWindow, we do not need to protect against the SerializedObject
            //       changing behind our backs by undo/redo here. Being a PropertyDrawer, we will automatically
            //       get recreated by Unity when it touches our serialized data.

            if (m_TreeView == null)
            {
                // Create tree and populate it.
                m_TreeView = new InputActionTreeView(property.serializedObject)
                {
                    onBuildTree = () => BuildTree(property),
                    onDoubleClick = OnItemDoubleClicked,
                    title = property.displayName,
                    // With the tree in the inspector, the foldouts are drawn too far to the left. I don't
                    // really know where this is coming from. This works around it by adding an arbitrary offset...
                    foldoutOffset = 14,
                    drawActionPropertiesButton = true
                };
                m_TreeView.Reload();
            }
        }

        private void SetNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            // Special case for InputActionProperty where we want to take the name not from
            // the m_Action property embedded in it but rather from the InputActionProperty field
            // itself.
            var name = actionProperty.displayName;
            var parent = actionProperty.GetParentProperty();
            if (parent != null && parent.type == "InputActionProperty")
                name = parent.displayName;

            var suffix = GetSuffixToRemoveFromPropertyDisplayName();
            if (name.EndsWith(suffix))
                name = name.Substring(0, name.Length - suffix.Length);

            nameProperty.stringValue = name;
            nameProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void OnItemDoubleClicked(ActionTreeItemBase item)
        {
            // Double-clicking on binding or action item opens property popup.
            PropertiesViewBase propertyView = null;
            if (item is BindingTreeItem)
            {
                if (m_ControlPickerState == null)
                    m_ControlPickerState = new InputControlPickerState();
                propertyView = new InputBindingPropertiesView(item.property,
                    controlPickerState: m_ControlPickerState,
                    expectedControlLayout: item.expectedControlLayout,
                    onChange:
                    change => m_TreeView.Reload());
            }
            else if (item is ActionTreeItem)
            {
                propertyView = new InputActionPropertiesView(item.property,
                    onChange: change => m_TreeView.Reload());
            }

            if (propertyView != null)
            {
                var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
                PropertiesViewPopup.Show(rect, propertyView);
            }
        }

        protected abstract TreeViewItem BuildTree(SerializedProperty property);
        protected abstract string GetSuffixToRemoveFromPropertyDisplayName();

        private InputActionTreeView m_TreeView;
        private InputControlPickerState m_ControlPickerState;

        internal class PropertiesViewPopup : EditorWindow
        {
            public static void Show(Rect btnRect, PropertiesViewBase view)
            {
                var window = CreateInstance<PropertiesViewPopup>();
                window.m_PropertyView = view;
                window.ShowPopup();
                window.ShowAsDropDown(btnRect, new Vector2(300, 350));
            }

            private void OnGUI()
            {
                m_PropertyView.OnGUI();
            }

            private PropertiesViewBase m_PropertyView;
        }
    }
}
#endif // UNITY_EDITOR
