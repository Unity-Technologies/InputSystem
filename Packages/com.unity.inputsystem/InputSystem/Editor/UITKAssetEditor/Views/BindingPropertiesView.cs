#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal class BindingPropertiesView : ViewBase<BindingPropertiesView.ViewState>
    {
        private readonly Foldout m_ParentFoldout;
        private CompositeBindingPropertiesView m_CompositeBindingPropertiesView;
        private CompositePartBindingPropertiesView m_CompositePartBindingPropertiesView;

        public BindingPropertiesView(VisualElement root, Foldout foldout, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            m_ParentFoldout = foldout;

            CreateSelector(state => state.selectedBindingIndex,
                s => new ViewStateCollection<InputControlScheme>(Selectors.GetControlSchemes(s)),
                (_, controlSchemes, s) => new ViewState
                {
                    controlSchemes = controlSchemes,
                    currentControlScheme = s.selectedControlScheme,
                    selectedBinding = Selectors.GetSelectedBinding(s),
                    selectedBindingIndex = s.selectedBindingIndex,
                    selectedBindingPath = Selectors.GetSelectedBindingPath(s),
                    selectedInputAction = Selectors.GetSelectedAction(s)
                });
        }

        public override void RedrawUI(ViewState viewState)
        {
            var selectedBindingIndex = viewState.selectedBindingIndex;
            if (selectedBindingIndex == -1)
                return;

            rootElement.Clear();

            var binding = viewState.selectedBinding;
            if (!binding.HasValue)
                return;

            m_ParentFoldout.text = "Binding";
            if (binding.Value.isComposite)
            {
                m_ParentFoldout.text = "Composite";
                m_CompositeBindingPropertiesView = CreateChildView(new CompositeBindingPropertiesView(rootElement, stateContainer));
            }
            else if (binding.Value.isPartOfComposite)
            {
                m_CompositePartBindingPropertiesView = CreateChildView(new CompositePartBindingPropertiesView(rootElement, stateContainer));
                DrawMatchingControlPaths(viewState);
                DrawControlSchemeToggles(viewState, binding.Value);
            }
            else
            {
                var controlPathEditor = new InputControlPathEditor(viewState.selectedBindingPath, new InputControlPickerState(),
                    () => { Dispatch(Commands.ApplyModifiedProperties()); });
                controlPathEditor.SetControlPathsToMatch(viewState.currentControlScheme.deviceRequirements.Select(x => x.controlPath));

                var inputAction = viewState.selectedInputAction;
                controlPathEditor.SetExpectedControlLayout(inputAction?.expectedControlType ?? "");

                var controlPathContainer = new IMGUIContainer(controlPathEditor.OnGUI);
                rootElement.Add(controlPathContainer);

                DrawMatchingControlPaths(viewState);
                DrawControlSchemeToggles(viewState, binding.Value);
            }
        }

        static bool s_showMatchingLayouts = false;
        internal void DrawMatchingControlPaths(ViewState viewState)
        {
            bool controlPathUsagePresent = false;
            bool showPaths = s_showMatchingLayouts;
            List<MatchingControlPath> matchingControlPaths = MatchingControlPath.CollectMatchingControlPaths(viewState.selectedBindingPath.stringValue, showPaths, ref controlPathUsagePresent);

            var parentElement = rootElement;
            if (matchingControlPaths == null || matchingControlPaths.Count != 0)
            {
                var controllingElement = new Foldout()
                {
                    text = $"Show Derived Bindings",
                    value = showPaths
                };
                rootElement.Add(controllingElement);

                controllingElement.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target == controllingElement)   // only react to foldout and not tree elements
                        s_showMatchingLayouts = changeEvent.newValue;
                });

                parentElement = controllingElement;
            }

            if (matchingControlPaths == null)
            {
                var messageString = controlPathUsagePresent ? "No registered controls match this current binding. Some controls are only registered at runtime." :
                    "No other registered controls match this current binding. Some controls are only registered at runtime.";

                var helpBox = new HelpBox(messageString, HelpBoxMessageType.Warning);
                helpBox.AddToClassList("matching-controls");
                parentElement.Add(helpBox);
            }
            else if (matchingControlPaths.Count > 0)
            {
                List<TreeViewItemData<MatchingControlPath>> treeViewMatchingControlPaths = MatchingControlPath.BuildMatchingControlPathsTreeData(matchingControlPaths);

                var treeView = new TreeView();
                parentElement.Add(treeView);
                treeView.selectionType = UIElements.SelectionType.None;
                treeView.AddToClassList("matching-controls");
                treeView.fixedItemHeight = 20;
                treeView.SetRootItems(treeViewMatchingControlPaths);

                // Set TreeView.makeItem to initialize each node in the tree.
                treeView.makeItem = () =>
                {
                    var label = new Label();
                    label.AddToClassList("matching-controls-labels");
                    return label;
                };

                // Set TreeView.bindItem to bind an initialized node to a data item.
                treeView.bindItem = (VisualElement element, int index) =>
                {
                    var label = (element as Label);
                    var matchingControlPath = treeView.GetItemDataForIndex<MatchingControlPath>(index);
                    label.text = $"{matchingControlPath.deviceName} > {matchingControlPath.controlName}";
                };

                treeView.ExpandRootItems();
            }
        }

        public override void DestroyView()
        {
            m_CompositeBindingPropertiesView?.DestroyView();
            m_CompositePartBindingPropertiesView?.DestroyView();
        }

        private void DrawControlSchemeToggles(ViewState viewState, SerializedInputBinding binding)
        {
            if (!viewState.controlSchemes.Any()) return;

            var useInControlSchemeLabel = new Label("Use in control scheme")
            {
                name = "control-scheme-usage-title"
            };

            rootElement.Add(useInControlSchemeLabel);

            foreach (var controlScheme in viewState.controlSchemes)
            {
                var checkbox = new Toggle(controlScheme.name)
                {
                    value = binding.controlSchemes.Any(scheme => controlScheme.name == scheme)
                };
                rootElement.Add(checkbox);
                checkbox.RegisterValueChangedCallback(changeEvent =>
                {
                    Dispatch(ControlSchemeCommands.ChangeSelectedBindingsControlSchemes(controlScheme.name, changeEvent.newValue));
                });
            }
        }

        internal class ViewState
        {
            public int selectedBindingIndex;
            public SerializedInputBinding? selectedBinding;
            public ViewStateCollection<InputControlScheme> controlSchemes;
            public InputControlScheme currentControlScheme;
            public SerializedProperty selectedBindingPath;
            public SerializedInputAction? selectedInputAction;
        }
    }
}

#endif
