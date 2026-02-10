#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A visual element that supports renaming of items.
    /// </summary>
    internal class InputActionsTreeViewItem : VisualElement
    {
        public EventCallback<string> EditTextFinishedCallback;

        private const string kRenameTextField = "rename-text-field";
        public event EventCallback<string> EditTextFinished;
        public Action<ContextualMenuPopulateEvent> OnContextualMenuPopulateEvent;

        // for testing purposes to know if the item is focused to accept input
        internal bool IsFocused { get; private set; } = false;

        private bool m_IsEditing;
        private static InputActionsTreeViewItem s_EditingItem = null;

        internal bool isCut { get; set; }

        public InputActionsTreeViewItem()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.InputActionsTreeViewItemUxml);
            template.CloneTree(this);

            focusable = true;
            delegatesFocus = false;

            renameTextfield.selectAllOnMouseUp = false;

            RegisterInputField();
            _ = new ContextualMenuManipulator(menuBuilder =>
            {
                OnContextualMenuPopulateEvent?.Invoke(menuBuilder);
            })
            { target = this };
        }

        public Label label => this.Q<Label>();
        private TextField renameTextfield => this.Q<TextField>(kRenameTextField);

        public void RegisterInputField()
        {
            renameTextfield.SetEnabled(true);
            renameTextfield.selectAllOnFocus = true;
            RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            renameTextfield.RegisterCallback<FocusInEvent>(e => IsFocused = true);
            renameTextfield.RegisterCallback<FocusOutEvent>(e =>
            {
                OnEditTextFinished();
                IsFocused = false;
            });
        }

        public void UnregisterInputField()
        {
            renameTextfield.SetEnabled(false);
            renameTextfield.selectAllOnFocus = false;
            UnregisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            renameTextfield.UnregisterCallback<FocusOutEvent>(e => OnEditTextFinished());
        }

        private double lastSingleClick;
        private static InputActionsTreeViewItem selected;

        private void OnMouseDownEventForRename(MouseDownEvent e)
        {
            if (e.clickCount != 1 || e.button != (int)MouseButton.LeftMouse || e.target == null)
                return;
            var now = EditorApplication.timeSinceStartup;
            if (selected == this && now - lastSingleClick < 3)
            {
                FocusOnRenameTextField();
                e.StopImmediatePropagation();
                lastSingleClick = 0;
                return;
            }
            lastSingleClick = now;
            selected = this;
        }

        public void Reset()
        {
            if (m_IsEditing)
            {
                lastSingleClick = 0;
                delegatesFocus = false;

                renameTextfield.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
                label.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
                s_EditingItem = null;
                m_IsEditing = false;
            }
            EditTextFinished = null;
        }

        public void FocusOnRenameTextField()
        {
            if (m_IsEditing || isCut)
                return;
            delegatesFocus = true;

            renameTextfield.SetValueWithoutNotify(label.text);
            renameTextfield.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label?.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);

            //a bit hacky - e.StopImmediatePropagation() for events does not work like expected on ListViewItems or TreeViewItems because
            //the listView/treeView reclaims the focus - this is a workaround with less overhead than rewriting the events
            schedule.Execute(() => renameTextfield.Q<TextField>().Focus()).StartingIn(120);
            renameTextfield.SelectAll();

            s_EditingItem = this;
            m_IsEditing = true;
        }

        public static void CancelRename()
        {
            s_EditingItem?.OnEditTextFinished();
        }

        private void OnEditTextFinished()
        {
            if (!m_IsEditing)
                return;
            lastSingleClick = 0;
            delegatesFocus = false;

            renameTextfield.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            s_EditingItem = null;
            m_IsEditing = false;

            var text = renameTextfield.text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                renameTextfield.schedule.Execute(() => renameTextfield.SetValueWithoutNotify(text));
                return;
            }

            EditTextFinished?.Invoke(text);
        }
    }
}
#endif
