// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
            // RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            // RegisterCallback<KeyDownEvent>(OnKeyDownEventForRename);
            //
            // renameTextfield.RegisterCallback<KeyUpEvent>(e =>
            // {
            //  if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
            //  {
            //   (e.currentTarget as VisualElement)?.Blur();
            //   return;
            //  }
            //
            //  e.StopImmediatePropagation();
            // });
            //
            // renameTextfield.RegisterCallback<FocusOutEvent>(e =>
            // {
            //  OnEditTextFinished(renameTextfield);
            // });
        }

        public Label label => this.Q<Label>();
        public TextField renameTextfield => this.Q<TextField>(kRenameTextField);

        private void OnKeyDownEventForRename(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.F2)
                return;

            FocusOnRenameTextField();
            e.StopPropagation();
        }

        private void OnMouseDownEventForRename(MouseDownEvent e)
        {
            if (e.clickCount != 2 || e.button != (int)MouseButton.LeftMouse || e.target == null)
                return;

            FocusOnRenameTextField();
            e.StopPropagation();
        }

        private void FocusOnRenameTextField()
        {
            delegatesFocus = true;

            renameTextfield.SetValueWithoutNotify(label.text);
            renameTextfield.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label?.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);

            renameTextfield.Q<TextElement>().Focus();
            renameTextfield.SelectAll();
        }

        public void OnEditTextFinished(TextField renameTextField)
        {
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
        }
    }
}
#endif
