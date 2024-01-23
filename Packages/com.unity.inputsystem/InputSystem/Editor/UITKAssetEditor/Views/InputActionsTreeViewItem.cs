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
    internal class InputActionsTreeViewItem : VisualElement
    {
        public EventCallback<string> EditTextFinishedCallback;
        public EventCallback<int> DeleteCallback;
        public EventCallback<int> DuplicateCallback;

        private const string kRenameTextField = "rename-text-field";
        public event EventCallback<string> EditTextFinished;
        public event EventCallback<int> OnDeleteItem;
        public event EventCallback<int> OnDuplicateItem;

        private bool m_IsEditing;

        DropManipulator m_Manipulator;

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


            // RegisterCallback<MouseDownEvent>(OnMouseDownEventForRename);
            // RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            // RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            // RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            // RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            // RegisterCallback<DragExitedEvent>(OnDragExitedEvent);

            renameTextfield.RegisterCallback<FocusOutEvent>(e => OnEditTextFinished());
        }

        void OnDragExitedEvent(DragExitedEvent evt)
        {
            Debug.Log("Drag exited");
            // ((Label)evt.target).style.backgroundColor = Color.clear;
            object draggedLabel = DragAndDrop.GetGenericData("string");
            if (DragManipulator.dragging)
            {
                DragManipulator.dragging = false;
            }
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            object data = DragAndDrop.GetGenericData("string");
            Debug.Log("Drag performed with data: " + data + "to target: " + evt.currentTarget);
            DragManipulator.dragging = false;
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            Debug.Log("Drag UPDATE event " + (evt.target as VisualElement));
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            // Unclear why we need this, but saw it in another example.
            // evt.StopPropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            Debug.Log("Drag leave");
            // ((Label)evt.target).style.backgroundColor = Color.clear;
        }

        void OnDragEnterEvent(DragEnterEvent evt)
        {
            Debug.Log("Drag enter event " + (evt.target as VisualElement));

            // This makes sure that drop is only allowed on the correct type of element that started the drag.
            // The drag element will set a string in the DragAndDrop generic data, which we can use to check if the
            // drop is allowed.
            if (DragAndDrop.GetGenericData("string") != null)
            {
                // ((Label)evt.target).style.backgroundColor = Color.gray;
            }
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

            m_IsEditing = true;
        }

        async void DelayCall()
        {
            await Task.Delay(120);
            renameTextfield.Q<TextField>().Focus();
        }

        public void DeleteItem()
        {
            OnDeleteItem?.Invoke(0);
        }

        public void DuplicateItem()
        {
            OnDuplicateItem?.Invoke(0);
        }

        private void OnEditTextFinished()
        {
            if (!m_IsEditing)
                return;
            lastSingleClick = 0;
            delegatesFocus = false;

            renameTextfield.AddToClassList(InputActionsEditorConstants.HiddenStyleClassName);
            label.RemoveFromClassList(InputActionsEditorConstants.HiddenStyleClassName);
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
