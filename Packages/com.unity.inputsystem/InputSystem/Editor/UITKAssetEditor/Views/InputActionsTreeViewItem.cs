// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A visual element that supports renaming of items.
    /// </summary>
    internal class InputActionsTreeViewItem : VisualElement
    {
        private const string kRenameTextField = "rename-text-field";
        public event EventCallback<string> EditTextFinished;

        private bool isEditing;

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

            // TODO: The rename functionality is currently disabled due to focus issues. The text field doesn't
            // focus correctly when calling Focus() on it (it needs to be subsequently clicked) and on losing focus,
            // we're getting two focus out events, one for the TextField and one for the contained TextElement.

            RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            RegisterCallback<KeyDownEvent>(OnKeyDownEventForRename);

            renameTextfield.RegisterCallback<BlurEvent>(e =>
            {
                OnEditTextFinished(renameTextfield);
            });
        }

        public Label label => this.Q<Label>();
        private TextField renameTextfield => this.Q<TextField>(kRenameTextField);

        private void OnKeyDownEventForRename(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.F2)
                return;

            FocusOnRenameTextField();
            e.StopImmediatePropagation();
        }

        private void OnMouseDownEventForRename(MouseDownEvent e)
        {
            if (e.clickCount != 2 || e.button != (int)MouseButton.LeftMouse || e.target == null)
                return;

            FocusOnRenameTextField();
            e.StopImmediatePropagation();
        }

        public void FocusOnRenameTextField()
        {
            delegatesFocus = true;

            renameTextfield.SetValueWithoutNotify(label.text);
            renameTextfield.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label?.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);

            //a bit hacky - e.StopImmediatePropagation() for events does not work like expected on ListViewItems or TreeViewItems because
            //the listView/treeView reclaims the focus - this is a workaround with less overhead than rewriting the events
            DelayCall();
            renameTextfield.SelectAll();

            isEditing = true;
        }

        async void DelayCall()
        {
            await Task.Delay(120);
            renameTextfield.Q<TextField>().Focus();
        }

        private void OnEditTextFinished(TextField renameTextField)
        {
            if (!isEditing)
                return;
            delegatesFocus = false;

            var text = renameTextField.value?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                renameTextField.schedule.Execute(() =>
                {
                    FocusOnRenameTextField();
                    renameTextField.SetValueWithoutNotify(text);
                });
                return;
            }

            renameTextField.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label.text = renameTextField.text;

            EditTextFinished?.Invoke(text);
            isEditing = false;
        }
    }
}
#endif
