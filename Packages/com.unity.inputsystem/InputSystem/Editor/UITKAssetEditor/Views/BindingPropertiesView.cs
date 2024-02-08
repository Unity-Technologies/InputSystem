#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
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
                    selectedInputAction = Selectors.GetSelectedAction(s),
                    showPaths = stateContainer.GetState().showMatchingPaths
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

        private void ShowDerivedBindings(ViewState viewState)
        {
        }

        private void DrawMatchingControlPaths(ViewState viewState)
        {
            bool controlPathUsagePresent = false;
            List<MatchingControlPath> matchingControlPaths = CollectMatchingControlPaths(viewState.selectedBindingPath.stringValue, viewState, ref controlPathUsagePresent);

            if (matchingControlPaths == null || matchingControlPaths.Count != 0)
            {
                var checkbox = new Toggle($"Show Derived Bindings")
                {
                    value = viewState.showPaths
                };
                rootElement.Add(checkbox);

                checkbox.RegisterValueChangedCallback(changeEvent =>
                {
                    Dispatch(Commands.ShowMatchingPaths(changeEvent.newValue));

                    rootElement.Q(className: "matching-controls").EnableInClassList("matching-controls-shown", changeEvent.newValue);
                    /*
                    element.visible = changeEvent.newValue;
                    element.style.flexGrow = changeEvent.newValue ? 1 : 0;
                    element.style.maxHeight = viewState.showPaths ? StyleKeyword.None : 0;
                    */
                });
            }

            if (matchingControlPaths == null)
            {
                var messageString = controlPathUsagePresent ? "No registered controls match this current binding. Some controls are only registered at runtime." :
                    "No other registered controls match this current binding. Some controls are only registered at runtime.";

                var helpBox = new HelpBox(messageString, HelpBoxMessageType.Warning);
                helpBox.AddToClassList("matching-controls");
                helpBox.EnableInClassList("matching-controls-shown", viewState.showPaths);
                rootElement.Add(helpBox);
            }
            else if (matchingControlPaths.Count > 0)
            {
                m_MatchingControlPaths = BuildMatchingControlPathsTreeData(matchingControlPaths);

                var treeView = new TreeView();
                rootElement.Add(treeView);
                treeView.AddToClassList("matching-controls");
                treeView.EnableInClassList("matching-controls-shown", viewState.showPaths);
                treeView.fixedItemHeight = 20;
                treeView.SetRootItems(m_MatchingControlPaths);

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
                    label.text = treeView.GetItemDataForIndex<MatchingControlPath>(index).path;
                };

                treeView.ExpandRootItems();
            }
        }

        protected class MatchingControlPath
        {
            public string path
            {
                get;
            }

            public MatchingControlPath(string path)
            {
                this.path = path;
                this.children = new List<MatchingControlPath>();
            }

            public List<MatchingControlPath> children;
        }

        private List<TreeViewItemData<MatchingControlPath>> BuildMatchingControlPathsTreeData(List<MatchingControlPath> matchingControlPaths)
        {
            int id = 0;
            return BuildMatchingControlPathsTreeDataRecursive(ref id, matchingControlPaths);
        }

        private List<TreeViewItemData<MatchingControlPath>> BuildMatchingControlPathsTreeDataRecursive(ref int id, List<MatchingControlPath> matchingControlPaths)
        {
            var treeViewList = new List<TreeViewItemData<MatchingControlPath>>(matchingControlPaths.Count);
            foreach (var matchingControlPath in matchingControlPaths)
            {
                var childTreeViewList = BuildMatchingControlPathsTreeDataRecursive(ref id, matchingControlPath.children);

                var treeViewItem = new TreeViewItemData<MatchingControlPath>(id++, matchingControlPath, childTreeViewList);
                treeViewList.Add(treeViewItem);
            }

            return treeViewList;
        }

        List<TreeViewItemData<MatchingControlPath>> m_MatchingControlPaths = new List<TreeViewItemData<MatchingControlPath>>();

        private List<MatchingControlPath> CollectMatchingControlPaths(string path, ViewState viewState, ref bool controlPathUsagePresent)
        {
            var matchingControlPaths = new List<MatchingControlPath>();

            if (path == string.Empty)
                return matchingControlPaths;

            var deviceLayoutPath = InputControlPath.TryGetDeviceLayout(path);
            var parsedPath = InputControlPath.Parse(path).ToArray();

            // If the provided path is parseable into device and control components, draw UI which shows control layouts that match the path.
            if (parsedPath.Length >= 2 && !string.IsNullOrEmpty(deviceLayoutPath))
            {
                bool matchExists = false;

                var rootDeviceLayout = EditorInputControlLayoutCache.TryGetLayout(deviceLayoutPath);
                bool isValidDeviceLayout = deviceLayoutPath == InputControlPath.Wildcard || (rootDeviceLayout != null && !rootDeviceLayout.isOverride && !rootDeviceLayout.hideInUI);
                // Exit early if a malformed device layout was provided,
                if (!isValidDeviceLayout)
                    return matchingControlPaths;

                controlPathUsagePresent = parsedPath[1].usages.Count() > 0;
                bool hasChildDeviceLayouts = deviceLayoutPath == InputControlPath.Wildcard || EditorInputControlLayoutCache.HasChildLayouts(rootDeviceLayout.name);

                // If the path provided matches exactly one control path (i.e. has no ui-facing child device layouts or uses control usages), then exit early
                if (!controlPathUsagePresent && !hasChildDeviceLayouts)
                    return matchingControlPaths;

                // Otherwise, we will show either all controls that match the current binding (if control usages are used)
                // or all controls in derived device layouts (if a no control usages are used).

                // If our control path contains a usage, make sure we render the binding that belongs to the root device layout first
                if (deviceLayoutPath != InputControlPath.Wildcard && controlPathUsagePresent)
                {
                    matchExists |= CollectMatchingControlPathsForLayout(rootDeviceLayout, in parsedPath, true, matchingControlPaths);
                }
                // Otherwise, just render the bindings that belong to child device layouts. The binding that matches the root layout is
                // already represented by the user generated control path itself.
                else
                {
                    IEnumerable<InputControlLayout> matchedChildLayouts = Enumerable.Empty<InputControlLayout>();
                    if (deviceLayoutPath == InputControlPath.Wildcard)
                    {
                        matchedChildLayouts = EditorInputControlLayoutCache.allLayouts
                            .Where(x => x.isDeviceLayout && !x.hideInUI && !x.isOverride && x.isGenericTypeOfDevice && x.baseLayouts.Count() == 0).OrderBy(x => x.displayName);
                    }
                    else
                    {
                        matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(rootDeviceLayout.name);
                    }

                    foreach (var childLayout in matchedChildLayouts)
                    {
                        matchExists |= CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, matchingControlPaths);
                    }
                }

                // Otherwise, indicate that no layouts match the current path.
                if (!matchExists)
                {
                    return null;
                }
            }

            return matchingControlPaths;
        }

        /// <summary>
        /// Returns true if the deviceLayout or any of its children has controls which match the provided parsed path. exist matching registered control paths.
        /// </summary>
        /// <param name="deviceLayout">The device layout to draw control paths for</param>
        /// <param name="parsedPath">The parsed path containing details of the Input Controls that can be matched</param>
        private bool CollectMatchingControlPathsForLayout(InputControlLayout deviceLayout, in InputControlPath.ParsedPathComponent[] parsedPath, bool isRoot, List<MatchingControlPath> matchingControlPaths)
        {
            string deviceName = deviceLayout.displayName;
            string controlName = string.Empty;
            bool matchExists = false;

            for (int i = 0; i < deviceLayout.m_Controls.Length; i++)
            {
                ref InputControlLayout.ControlItem controlItem = ref deviceLayout.m_Controls[i];
                if (InputControlPath.MatchControlComponent(ref parsedPath[1], ref controlItem, true))
                {
                    // If we've already located a match, append a ", " to the control name
                    // This is to accomodate cases where multiple control items match the same path within a single device layout
                    // Note, some controlItems have names but invalid displayNames (i.e. the Dualsense HID > leftTriggerButton)
                    // There are instance where there are 2 control items with the same name inside a layout definition, however they are not
                    // labeled significantly differently.
                    // The notable example is that the Android Xbox and Android Dualshock layouts have 2 d-pad definitions, one is a "button"
                    // while the other is an axis.
                    controlName += matchExists ? $", {controlItem.name}" : controlItem.name;

                    // if the parsePath has a 3rd component, try to match it with items in the controlItem's layout definition.
                    if (parsedPath.Length == 3)
                    {
                        var controlLayout = EditorInputControlLayoutCache.TryGetLayout(controlItem.layout);
                        if (controlLayout.isControlLayout && !controlLayout.hideInUI)
                        {
                            for (int j = 0; j < controlLayout.m_Controls.Count(); j++)
                            {
                                ref InputControlLayout.ControlItem controlLayoutItem = ref controlLayout.m_Controls[j];
                                if (InputControlPath.MatchControlComponent(ref parsedPath[2], ref controlLayoutItem))
                                {
                                    controlName += $"/{controlLayoutItem.name}";
                                    matchExists = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        matchExists = true;
                    }
                }
            }

            IEnumerable<InputControlLayout> matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(deviceLayout.name);

            // If this layout does not have a match, or is the top level root layout,
            // skip over trying to draw any items for it, and immediately try processing the child layouts
            if (!matchExists)
            {
                foreach (var childLayout in matchedChildLayouts)
                {
                    matchExists |= CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, matchingControlPaths);
                }
            }
            // Otherwise, draw the items for it, and then only process the child layouts if the foldout is expanded.
            else
            {
                var newMatchingControlPath = new MatchingControlPath($"{deviceName} > {controlName}");
                matchingControlPaths.Add(newMatchingControlPath);

                foreach (var childLayout in matchedChildLayouts)
                {
                    CollectMatchingControlPathsForLayout(childLayout, in parsedPath, false, newMatchingControlPath.children);
                }
            }

            return matchExists;
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
            public bool showPaths;
        }
    }
}

#endif
