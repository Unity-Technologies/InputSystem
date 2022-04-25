#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

public class InputActionsTreeViewItem : VisualElement
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

		RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
		RegisterCallback<KeyDownEvent>(OnKeyDownEventForRename);
	}

	public Label label => this.Q<Label>();

	private void OnKeyDownEventForRename(KeyDownEvent e)
	{
		if (e.keyCode != KeyCode.F2)
			return;

		FocusOnRenameTextField();
		e.PreventDefault();
	}

	private void OnMouseDownEventForRename(MouseDownEvent e)
	{
		if (e.clickCount != 2 || e.button != (int)MouseButton.LeftMouse || e.target == null)
			return;

		FocusOnRenameTextField();

		e.PreventDefault();
	}
	
	private void FocusOnRenameTextField()
	{
		var renameTextfield = this.Q<TextField>(kRenameTextField);
		var nameLabel = this.Q<Label>();

		renameTextfield.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);

		nameLabel?.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);

		renameTextfield.SelectAll();
	}

	public TextField CreateRenamingTextField(VisualElement documentElement, Label nameLabel)
	{
		var renameTextfield = new TextField()
		{
			name = kRenameTextField,
			isDelayed = true
		};
		
		renameTextfield.RegisterCallback<KeyUpEvent>(e =>
		{
			if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
			{
				(e.currentTarget as VisualElement)?.Blur();
				return;
			}

			e.StopImmediatePropagation();
		});

		renameTextfield.RegisterCallback<FocusOutEvent>(e =>
		{
			OnEditTextFinished(renameTextfield);
		});

		return renameTextfield;
	}

	public void OnEditTextFinished(TextField renameTextField)
	{
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

		EditTextFinished?.Invoke(text);
	}
}
#endif