// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
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

        private bool m_IsEditing;
        private static InputActionsTreeViewItem s_EditingItem = null;

        public InputActionsTreeViewItem()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.InputActionsTreeViewItemUxml);
            template.CloneTree(this);

            focusable = true;
            delegatesFocus = false;

            renameTextfield.selectAllOnFocus = true;
            renameTextfield.selectAllOnMouseUp = false;


            RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            renameTextfield.RegisterCallback<FocusOutEvent>(e => OnEditTextFinished());
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

        private float lastSingleClick;
        private static InputActionsTreeViewItem selected;

        private void OnMouseDownEventForRename(MouseDownEvent e)
        {
            if (e.clickCount != 1 || e.button != (int)MouseButton.LeftMouse || e.target == null)
                return;

            if (selected == this && Time.time - lastSingleClick < 3f)
            {
                FocusOnRenameTextField();
                e.StopImmediatePropagation();
                lastSingleClick = 0;
            }
            lastSingleClick = Time.time;
            selected = this;
        }

        public void Reset()
        {
            EditTextFinished = null;
            m_IsEditing = false;
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
            DelayCall();
            renameTextfield.SelectAll();

            s_EditingItem = this;
            m_IsEditing = true;
        }

        public static void CancelRename()
        {
            s_EditingItem?.OnEditTextFinished();
        }

        async void DelayCall()
        {
            await Task.Delay(120);
            renameTextfield.Q<TextField>().Focus();
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
