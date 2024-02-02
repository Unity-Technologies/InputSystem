#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
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
        private const string menuButtonId = "asset-menu";

        private readonly ToolbarMenu m_MenuButtonToolbar;
        private readonly ToolbarButton m_SaveButton;

        internal Action postSaveAction;
        internal Action<InputActionAsset> postResetAction;

        public InputActionsEditorView(VisualElement root, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            var mainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.MainEditorViewNameUxml);

            mainEditorAsset.CloneTree(root);
            var actionsTreeView = new ActionsTreeView(root, stateContainer);
            CreateChildView(new ActionMapsView(root, stateContainer));
            CreateChildView(actionsTreeView);
            CreateChildView(new PropertiesView(root, stateContainer));
            InputActionViewsControlsHolder.Initialize(root, actionsTreeView);

            m_MenuButtonToolbar = root.Q<ToolbarMenu>("control-schemes-toolbar-menu");
            m_MenuButtonToolbar.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(root));
            m_MenuButtonToolbar.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(root, true), DropdownMenuAction.Status.Disabled);
            m_MenuButtonToolbar.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(root), DropdownMenuAction.Status.Disabled);
            m_MenuButtonToolbar.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme, DropdownMenuAction.Status.Disabled);

            m_SaveButton = root.Q<ToolbarButton>(name: saveButtonId);
            m_SaveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
            m_SaveButton.clicked += OnSaveButton;

            var autoSaveToggle = root.Q<ToolbarToggle>(name: autoSaveToggleId);
            autoSaveToggle.value = InputEditorUserSettings.autoSaveInputActionAssets;
            autoSaveToggle.RegisterValueChangedCallback(OnAutoSaveToggle);


            var assetMenuButton = root.Q<VisualElement>(name: menuButtonId);
            var isGlobalAsset = stateContainer.GetState().serializedObject.targetObject.name == "ProjectWideInputActions";
            assetMenuButton.visible = isGlobalAsset;
            assetMenuButton.AddToClassList(EditorGUIUtility.isProSkin ? "asset-menu-button-dark-theme" : "asset-menu-button");
            var _ = new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction("Reset", _ => OnReset());
            }) { target = assetMenuButton, activators = { new ManipulatorActivationFilter() {button = MouseButton.LeftMouse} }};

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

        private void OnReset()
        {
            Dispatch(Commands.ResetGlobalInputAsset(postResetAction));
        }

        private void OnSaveButton()
        {
            Dispatch(Commands.SaveAsset(postSaveAction));

            // Don't let focus linger after clicking (ISX-1482). Ideally this would be only applied on mouse click,
            // rather than if the user is using tab to navigate UI, but there doesn't seem to be a way to differentiate
            // between those interactions at the moment.
            m_SaveButton.Blur();
        }

        private void OnAutoSaveToggle(ChangeEvent<bool> evt)
        {
            Dispatch(Commands.ToggleAutoSave(evt.newValue, postSaveAction));
        }

        public override void RedrawUI(ViewState viewState)
        {
            m_MenuButtonToolbar.menu.MenuItems().Clear();

            if (viewState.controlSchemes.Any())
            {
                m_MenuButtonToolbar.text = viewState.selectedControlSchemeIndex == -1
                    ? "All Control Schemes"
                    : viewState.controlSchemes.ElementAt(viewState.selectedControlSchemeIndex).name;

                m_MenuButtonToolbar.menu.AppendAction("All Control Schemes", _ => SelectControlScheme(-1),
                    viewState.selectedControlSchemeIndex == -1 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                viewState.controlSchemes.ForEach((scheme, i) =>
                    m_MenuButtonToolbar.menu.AppendAction(scheme.name, _ => SelectControlScheme(i),
                        viewState.selectedControlSchemeIndex == i ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal));
                m_MenuButtonToolbar.menu.AppendSeparator();
            }

            m_MenuButtonToolbar.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(rootElement));
            m_MenuButtonToolbar.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(rootElement, true),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            m_MenuButtonToolbar.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(rootElement),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            m_MenuButtonToolbar.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme,
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            m_SaveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
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
