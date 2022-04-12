using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorView : ViewBase<InputActionsEditorView.ViewState>
    {
        public InputActionsEditorView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
            var mainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.MainEditorViewNameUxml);

            mainEditorAsset.CloneTree(root);
            CreateChildView(new ActionMapsView(root, stateContainer));
            CreateChildView(new ActionsListView(root, stateContainer));
            CreateChildView(new BindingsListView(root, stateContainer));
            CreateChildView(new PropertiesView(root, stateContainer));

            var menuButton = root.Q<ToolbarMenu>("control-schemes-toolbar-menu");
            menuButton.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(root));
            menuButton.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(root, true), DropdownMenuAction.Status.Disabled);
            menuButton.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(root), DropdownMenuAction.Status.Disabled);
            menuButton.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme, DropdownMenuAction.Status.Disabled);

            // only register the state changed event here in the parent. Changes will be cascaded
            // into child views.
            stateContainer.StateChanged += OnStateChanged;

            CreateSelector(
                s => s.selectedControlSchemeIndex,
                s => new ViewStateCollection<InputControlScheme>(Selectors.GetControlSchemes(s)),
                (_, controlSchemes, state) => new ViewState
                {
                    controlSchemes = controlSchemes,
                    selectedControlSchemeIndex = state.selectedControlSchemeIndex
                });
        }

        public override void RedrawUI(ViewState viewState)
        {
            var toolbarMenu = m_Root.Q<ToolbarMenu>("control-schemes-toolbar-menu");
            toolbarMenu.menu.MenuItems().Clear();


            if (viewState.controlSchemes.Any())
            {
                toolbarMenu.text = viewState.selectedControlSchemeIndex == -1
                    ? "All Control Schemes"
                    : viewState.controlSchemes.ElementAt(viewState.selectedControlSchemeIndex).name;

                toolbarMenu.menu.AppendAction("All Control Schemes", _ => SelectControlScheme(-1),
                    viewState.selectedControlSchemeIndex == -1 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                viewState.controlSchemes.ForEach((scheme, i) =>
                    toolbarMenu.menu.AppendAction(scheme.name, _ => SelectControlScheme(i),
                        viewState.selectedControlSchemeIndex == i ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal));
                toolbarMenu.menu.AppendSeparator();
            }


            toolbarMenu.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(m_Root));
            toolbarMenu.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(m_Root, true),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbarMenu.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(m_Root),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbarMenu.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme,
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void AddOrUpdateControlScheme(VisualElement parent, bool updateExisting = false)
        {
            if (!updateExisting)
                Dispatch(ControlSchemeCommands.AddNewControlScheme());

            ShowControlSchemeEditor(parent, updateExisting);
        }

        private void DuplicateControlScheme(VisualElement parent)
        {
            Dispatch(ControlSchemeCommands.DuplicateSelectedControlScheme());
            ShowControlSchemeEditor(parent);
        }

        private void DeleteControlScheme(DropdownMenuAction obj)
        {
            Dispatch(ControlSchemeCommands.DeleteSelectedControlScheme());
        }

        private void ShowControlSchemeEditor(VisualElement parent, bool updateExisting = false)
        {
            var controlSchemesView = CreateChildView(new ControlSchemesView(parent, stateContainer, updateExisting));
            controlSchemesView.UpdateView(stateContainer.GetState());

            controlSchemesView.OnClosing += _ => DestroyView(controlSchemesView);
        }

        private void SelectControlScheme(int controlSchemeIndex)
        {
            Dispatch(ControlSchemeCommands.SelectControlScheme(controlSchemeIndex));
        }

        private readonly VisualElement m_Root;

        public class ViewState
        {
            public IEnumerable<InputControlScheme> controlSchemes;
            public int selectedControlSchemeIndex;
        }
    }

    internal static partial class Selectors
    {
        public static IEnumerable<InputControlScheme> GetControlSchemes(InputActionsEditorState state)
        {
            var controlSchemesArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
            foreach (SerializedProperty controlScheme in controlSchemesArray)
            {
                yield return new InputControlScheme(controlScheme);
            }
        }
    }
}
