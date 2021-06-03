#if UNITY_EDITOR
using System.Collections.Generic;
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
            return GetOrCreateViewData(property).TreeView.totalHeight;
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

            GetOrCreateViewData(property).TreeView.OnGUI(position);

            EditorGUI.EndProperty();
        }

        private void InitTreeIfNeeded(SerializedProperty property)
        {
            // NOTE: Unlike InputActionEditorWindow, we do not need to protect against the SerializedObject
            //       changing behind our backs by undo/redo here. Being a PropertyDrawer, we will automatically
            //       get recreated by Unity when it touches our serialized data.

            var viewData = GetOrCreateViewData(property);
            var propertyIsClone = IsPropertyAClone(property);

            if (viewData.TreeView != null && !propertyIsClone)
                return;

            if (propertyIsClone)
                ResetProperty(property);

            viewData.TreeView = new InputActionTreeView(property.serializedObject)
            {
                onBuildTree = () => BuildTree(property),
                onDoubleClick = item => OnItemDoubleClicked(item, property),
                // With the tree in the inspector, the foldouts are drawn too far to the left. I don't
                // really know where this is coming from. This works around it by adding an arbitrary offset...
                foldoutOffset = 14,
                drawActionPropertiesButton = true,
                title = (GetPropertyTitle(property), property.GetTooltip())
            };
            viewData.TreeView.Reload();
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

            // If it's a singleton action, we also need to adjust the InputBinding.action
            // property values in its binding list.
            var singleActionBindings = actionProperty.FindPropertyRelative("m_SingletonActionBindings");
            if (singleActionBindings != null)
            {
                var bindingCount = singleActionBindings.arraySize;
                for (var i = 0; i < bindingCount; ++i)
                {
                    var binding = singleActionBindings.GetArrayElementAtIndex(i);
                    var actionNameProperty = binding.FindPropertyRelative("m_Action");
                    actionNameProperty.stringValue = name;
                }
            }

            nameProperty.stringValue = name;

            actionProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(actionProperty.serializedObject.targetObject);
        }

        private static string GetPropertyTitle(SerializedProperty property)
        {
            var propertyTitleNumeral = string.Empty;

            if (property.GetParentProperty() != null && property.GetParentProperty().isArray)
                propertyTitleNumeral = $" {property.GetIndexOfArrayElement()}";

            return property.type == nameof(InputActionMap) ?
                $"Input Action Map{propertyTitleNumeral}" :
                $"Input Action{propertyTitleNumeral}";
        }

        private void OnItemDoubleClicked(ActionTreeItemBase item, SerializedProperty property)
        {
            var viewData = GetOrCreateViewData(property);

            // Double-clicking on binding or action item opens property popup.
            PropertiesViewBase propertyView = null;
            if (item is BindingTreeItem)
            {
                if (viewData.ControlPickerState == null)
                    viewData.ControlPickerState = new InputControlPickerState();
                propertyView = new InputBindingPropertiesView(item.property,
                    controlPickerState: viewData.ControlPickerState,
                    expectedControlLayout: item.expectedControlLayout,
                    onChange:
                    change => viewData.TreeView.Reload());
            }
            else if (item is ActionTreeItem)
            {
                propertyView = new InputActionPropertiesView(item.property,
                    onChange: change => viewData.TreeView.Reload());
            }

            if (propertyView != null)
            {
                var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
                PropertiesViewPopup.Show(rect, propertyView);
            }
        }

        private InputActionDrawerViewData GetOrCreateViewData(SerializedProperty property)
        {
            if (m_PerPropertyViewData == null)
                m_PerPropertyViewData = new Dictionary<string, InputActionDrawerViewData>();

            if (m_PerPropertyViewData.TryGetValue(property.propertyPath, out var data)) return data;

            data = new InputActionDrawerViewData();
            m_PerPropertyViewData.Add(property.propertyPath, data);

            return data;
        }

        protected abstract TreeViewItem BuildTree(SerializedProperty property);
        protected abstract string GetSuffixToRemoveFromPropertyDisplayName();
        protected abstract bool IsPropertyAClone(SerializedProperty property);
        protected abstract void ResetProperty(SerializedProperty property);

        // Unity creates a single instance of a property drawer to draw multiple instances of the property drawer type,
        // so we can't store state in the property drawer for each item. We do need that though, because each InputAction
        // needs to have it's own instance of the InputActionTreeView to correctly draw it's own bindings. So what we do
        // is keep this array around that stores a tree view instance for each unique property path that the property
        // drawer encounters. The tree view will be recreated if we detect that the property being drawn has changed.
        private Dictionary<string, InputActionDrawerViewData> m_PerPropertyViewData;

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

        private class InputActionDrawerViewData
        {
            public InputActionTreeView TreeView;
            public InputControlPickerState ControlPickerState;
        }
    }
}
#endif // UNITY_EDITOR
