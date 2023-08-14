#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorView : ViewBase<InputActionsEditorView.ViewState>
    {
        private const string saveButtonId = "save-asset-toolbar-button";
        private const string autoSaveToggleId = "auto-save-toolbar-toggle";

        public InputActionsEditorView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
            BuildUI();
        }

        public void BuildUI()
        {
            var mainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.MainEditorViewNameUxml);

            mainEditorAsset.CloneTree(m_Root);
            var actionsTreeView = new ActionsTreeView(m_Root, stateContainer);
            CreateChildView(new ActionMapsView(m_Root, stateContainer));
            CreateChildView(actionsTreeView);
            CreateChildView(new PropertiesView(m_Root, stateContainer));
            InputActionViewsControlsHolder.Initialize(m_Root, actionsTreeView);

            var menuButton = m_Root.Q<ToolbarMenu>("control-schemes-toolbar-menu");
            menuButton.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(m_Root));
            menuButton.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(m_Root, true), DropdownMenuAction.Status.Disabled);
            menuButton.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(m_Root), DropdownMenuAction.Status.Disabled);
            menuButton.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme, DropdownMenuAction.Status.Disabled);

            var saveButton = m_Root.Q<ToolbarButton>(name: saveButtonId);
            saveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
            saveButton.clicked += OnSaveButton;

            var autoSaveToggle = m_Root.Q<ToolbarToggle>(name: autoSaveToggleId);
            autoSaveToggle.value = InputEditorUserSettings.autoSaveInputActionAssets;
            autoSaveToggle.RegisterValueChangedCallback(OnAutoSaveToggle);

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

        private void OnSaveButton()
        {
            Dispatch(Commands.SaveAsset());
        }

        private void OnAutoSaveToggle(ChangeEvent<bool> evt)
        {
            Dispatch(Commands.ToggleAutoSave(evt.newValue));
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

            var saveButton = m_Root.Q<ToolbarButton>(name: saveButtonId);
            saveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
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

            controlSchemesView.OnClosing += _ => DestroyChildView(controlSchemesView);
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

#endif
