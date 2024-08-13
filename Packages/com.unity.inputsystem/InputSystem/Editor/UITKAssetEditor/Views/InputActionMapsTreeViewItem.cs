// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A visual element that supports renaming of items.
    /// </summary>
    internal class InputActionMapsTreeViewItem : VisualElement
    {
        public EventCallback<string> EditTextFinishedCallback;

        private const string kRenameTextField = "rename-text-field";
        public event EventCallback<string> EditTextFinished;

        // for testing purposes to know if the item is focused to accept input
        internal bool IsFocused { get; private set; } = false;

        private bool m_IsEditing;
        private static InputActionMapsTreeViewItem s_EditingItem = null;

        public InputActionMapsTreeViewItem()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.InputActionMapsTreeViewItemUxml);
            template.CloneTree(this);

            focusable = true;
            delegatesFocus = false;

            renameTextfield.selectAllOnFocus = true;
            renameTextfield.selectAllOnMouseUp = false;

            RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            renameTextfield.RegisterCallback<FocusInEvent>(e => IsFocused = true);
            renameTextfield.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); IsFocused = false; });
        }

        public Label label => this.Q<Label>();
        private TextField renameTextfield => this.Q<TextField>(kRenameTextField);


        public void UnregisterInputField()
        {
            renameTextfield.SetEnabled(false);
            renameTextfield.selectAllOnFocus = false;
            UnregisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            renameTextfield.UnregisterCallback<FocusOutEvent>(e => OnEditTextFinished());
        }

        private double lastSingleClick;
        private static InputActionMapsTreeViewItem selected;

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
            if (m_IsEditing)
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
