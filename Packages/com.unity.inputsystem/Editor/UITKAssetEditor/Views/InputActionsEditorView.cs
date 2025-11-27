#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    interface IPasteListener
    {
        void OnPaste(InputActionsEditorState state);
    }
    internal class InputActionsEditorView : ViewBase<InputActionsEditorView.ViewState>, IPasteListener
    {
        private const string saveButtonId = "save-asset-toolbar-button";
        private const string autoSaveToggleId = "auto-save-toolbar-toggle";
        private const string menuButtonId = "asset-menu";

        private readonly ToolbarMenu m_ControlSchemesToolbar;
        private readonly ToolbarMenu m_DevicesToolbar;
        private readonly ToolbarButton m_SaveButton;

        private readonly Action m_SaveAction;

        public InputActionsEditorView(VisualElement root, StateContainer stateContainer, bool isProjectSettings,
                                      Action saveAction)
            : base(root, stateContainer)
        {
            m_SaveAction = saveAction;

            var mainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.MainEditorViewNameUxml);

            mainEditorAsset.CloneTree(root);
            var actionsTreeView = new ActionsTreeView(root, stateContainer);
            CreateChildView(new ActionMapsView(root, stateContainer));
            CreateChildView(actionsTreeView);
            CreateChildView(new PropertiesView(root, stateContainer));

            m_ControlSchemesToolbar = root.Q<ToolbarMenu>("control-schemes-toolbar-menu");
            m_ControlSchemesToolbar.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(root));
            m_ControlSchemesToolbar.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(root, true), DropdownMenuAction.Status.Disabled);
            m_ControlSchemesToolbar.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(root), DropdownMenuAction.Status.Disabled);
            m_ControlSchemesToolbar.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme, DropdownMenuAction.Status.Disabled);

            m_DevicesToolbar = root.Q<ToolbarMenu>("control-schemes-filter-toolbar-menu");
            m_DevicesToolbar.SetEnabled(false);

            m_SaveButton = root.Q<ToolbarButton>(name: saveButtonId);
            m_SaveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
            m_SaveButton.clicked += OnSaveButton;

            var autoSaveToggle = root.Q<ToolbarToggle>(name: autoSaveToggleId);
            autoSaveToggle.value = InputEditorUserSettings.autoSaveInputActionAssets;
            autoSaveToggle.RegisterValueChangedCallback(OnAutoSaveToggle);

            // Hide save toolbar if there is no save action provided since we cannot support it
            if (saveAction == null)
            {
                var element = root.Q("save-asset-toolbar-container");
                if (element != null)
                {
                    element.style.visibility = Visibility.Hidden;
                    element.style.display = DisplayStyle.None;
                }
            }

            VisualElement assetMenuButton = null;
            try
            {
                // This only exists in the project settings version
                assetMenuButton = root.Q<VisualElement>(name: menuButtonId);
            }
            catch {}

            if (assetMenuButton != null)
            {
                assetMenuButton.visible = isProjectSettings;
                assetMenuButton.AddToClassList(EditorGUIUtility.isProSkin ? "asset-menu-button-dark-theme" : "asset-menu-button");
                var _ = new ContextualMenuManipulator(menuEvent =>
                {
                    menuEvent.menu.AppendAction("Reset to Defaults", _ => OnReset());
                    menuEvent.menu.AppendAction("Remove All Action Maps", _ => OnClearActionMaps());
                })
                { target = assetMenuButton, activators = { new ManipulatorActivationFilter() { button = MouseButton.LeftMouse } } };
            }

            // only register the state changed event here in the parent. Changes will be cascaded
            // into child views.
            stateContainer.StateChanged += OnStateChanged;

            CreateSelector(
                s => s.selectedControlSchemeIndex,
                s => new ViewStateCollection<InputControlScheme>(Selectors.GetControlSchemes(s)),
                (_, controlSchemes, state) => new ViewState
                {
                    controlSchemes = controlSchemes,
                    selectedControlSchemeIndex = state.selectedControlSchemeIndex,
                    selectedDeviceIndex = state.selectedDeviceRequirementIndex
                });

            s_OnPasteCutElements.Add(this);
        }

        private void OnReset()
        {
            Dispatch(Commands.ReplaceActionMaps(ProjectWideActionsAsset.GetDefaultAssetJson()));
        }

        private void OnClearActionMaps()
        {
            Dispatch(Commands.ClearActionMaps());
        }

        private void OnSaveButton()
        {
            Dispatch(Commands.SaveAsset(m_SaveAction));

            // Don't let focus linger after clicking (ISX-1482). Ideally this would be only applied on mouse click,
            // rather than if the user is using tab to navigate UI, but there doesn't seem to be a way to differentiate
            // between those interactions at the moment.
            m_SaveButton.Blur();
        }

        private void OnAutoSaveToggle(ChangeEvent<bool> evt)
        {
            Dispatch(Commands.ToggleAutoSave(evt.newValue, m_SaveAction));
        }

        public override void RedrawUI(ViewState viewState)
        {
            SetUpControlSchemesMenu(viewState);
            SetUpDevicesMenu(viewState);
            m_SaveButton.SetEnabled(InputEditorUserSettings.autoSaveInputActionAssets == false);
        }

        private string SetupControlSchemeName(string name)
        {
            //On Windows the '&' is considered an accelerator character and will always be stripped.
            //Since the ControlScheme menu isn't creating hotkeys, it can be safely assumed that they are meant to be text
            //so we want to escape the character for MenuItem
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                name = name.Replace("&", "&&");
            }
            return name;
        }

        private void SetUpControlSchemesMenu(ViewState viewState)
        {
            m_ControlSchemesToolbar.menu.MenuItems().Clear();

            if (viewState.controlSchemes.Any())
            {
                m_ControlSchemesToolbar.text = viewState.selectedControlSchemeIndex == -1
                    ? "All Control Schemes"
                    : viewState.controlSchemes.ElementAt(viewState.selectedControlSchemeIndex).name;

                m_ControlSchemesToolbar.menu.AppendAction("All Control Schemes", _ => SelectControlScheme(-1),
                    viewState.selectedControlSchemeIndex == -1 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                viewState.controlSchemes.ForEach((scheme, i) =>
                    m_ControlSchemesToolbar.menu.AppendAction(SetupControlSchemeName(scheme.name), _ => SelectControlScheme(i),
                        viewState.selectedControlSchemeIndex == i ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal));
                m_ControlSchemesToolbar.menu.AppendSeparator();
            }

            m_ControlSchemesToolbar.menu.AppendAction("Add Control Scheme...", _ => AddOrUpdateControlScheme(rootElement));
            m_ControlSchemesToolbar.menu.AppendAction("Edit Control Scheme...", _ => AddOrUpdateControlScheme(rootElement, true),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            m_ControlSchemesToolbar.menu.AppendAction("Duplicate Control Scheme...", _ => DuplicateControlScheme(rootElement),
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            m_ControlSchemesToolbar.menu.AppendAction("Delete Control Scheme...", DeleteControlScheme,
                viewState.selectedControlSchemeIndex != -1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void SetUpDevicesMenu(ViewState viewState)
        {
            if (!viewState.controlSchemes.Any() || viewState.selectedControlSchemeIndex == -1)
            {
                m_DevicesToolbar.text = "All Devices";
                m_DevicesToolbar.SetEnabled(false);
                return;
            }
            m_DevicesToolbar.SetEnabled(true);
            var currentControlScheme = viewState.controlSchemes.ElementAt(viewState.selectedControlSchemeIndex);
            if (viewState.selectedDeviceIndex == -1)
                m_DevicesToolbar.text = "All Devices";

            m_DevicesToolbar.menu.MenuItems().Clear();
            m_DevicesToolbar.menu.AppendAction("All Devices", _ => SelectDevice(-1), viewState.selectedDeviceIndex == -1
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);
            currentControlScheme.deviceRequirements.ForEach(
                (device, i) =>
                {
                    InputControlPath.ToHumanReadableString(device.controlPath, out var name, out _);
                    m_DevicesToolbar.menu.AppendAction(name, _ => SelectDevice(i),
                        viewState.selectedDeviceIndex == i
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
                    if (viewState.selectedDeviceIndex == i)
                        m_DevicesToolbar.text = name;
                });
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
            SelectDevice(-1);
        }

        private void SelectDevice(int deviceIndex)
        {
            Dispatch(ControlSchemeCommands.SelectDeviceRequirement(deviceIndex));
        }

        public class ViewState
        {
            public IEnumerable<InputControlScheme> controlSchemes;
            public int selectedControlSchemeIndex;
            public int selectedDeviceIndex;
        }

        internal static List<IPasteListener> s_OnPasteCutElements = new();

        public override void DestroyView()
        {
            base.DestroyView();
            s_OnPasteCutElements.Remove(this);
        }

        public void OnPaste(InputActionsEditorState state)
        {
            if (state.Equals(stateContainer.GetState()))
                return;
            Dispatch(Commands.DeleteCutElements());
        }
    }

    internal static partial class Selectors
    {
        public static IEnumerable<InputControlScheme> GetControlSchemes(InputActionsEditorState state)
        {
            var controlSchemesArray = state.serializedObject.FindProperty(nameof(InputActionAsset.m_ControlSchemes));
            if (controlSchemesArray == null)
                yield break;

            foreach (SerializedProperty controlScheme in controlSchemesArray)
            {
                yield return new InputControlScheme(controlScheme);
            }
        }
    }
}

#endif
