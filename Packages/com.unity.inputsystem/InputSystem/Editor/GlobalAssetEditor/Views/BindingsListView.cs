using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Lists all of the bindings for the selected action.
    /// </summary>
    internal class BindingsListView : UIToolkitView<List<BindingsListView.ViewState>>
    {
	    private const string RowTemplateUxml = GlobalInputActionsConstants.PackagePath +
	                                           GlobalInputActionsConstants.ResourcesPath +
	                                           GlobalInputActionsConstants.BindingsPanelRowTemplateUxml;

	    private const float TreeItemIndentPerLevel = 15;

	    private readonly VisualElement m_Root;

	    public BindingsListView(VisualElement root, StateContainer stateContainer)
	        : base(stateContainer)
        {
	        m_Root = root;
	        var bindingItemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                RowTemplateUxml);

            var bindingsListView = root.Q<ListView>("bindings-list-view");
            bindingsListView.selectionType = UIElements.SelectionType.Single;
            bindingsListView.bindItem = (e, i) =>
            {
                var bindingState = (ViewState)bindingsListView.itemsSource[i];
                var binding = bindingState.binding;

                // TODO: Too much logic in the view here. Pull this out into selectors.
                var name = string.Empty;
                if (binding.isPartOfComposite)
                    name = $"{ObjectNames.NicifyVariableName(binding.name)}: " +
                           $"{InputControlPath.ToHumanReadableString(binding.path)}";
                else if (binding.isComposite)
	                name = binding.name;
                else
                    name = InputControlPath.ToHumanReadableString(binding.path);

                e.style.paddingLeft = binding.isPartOfComposite ? TreeItemIndentPerLevel : 0;
                e.Q<Label>("name").text = name;
                e.Q<VisualElement>("icon").style.backgroundImage =
	                new StyleBackground(
		                EditorInputControlLayoutCache.GetIconForLayout(bindingState.controlType));

                // only show the foldout icon if a binding has children
                if (!binding.isComposite) return;

                var toggle = e.Q<Toggle>("expando");
                toggle.style.visibility = Visibility.Visible;
                toggle.SetValueWithoutNotify(bindingState.isExpanded);

                // attach an event handler to the foldout button
                var callback = new EventCallback<ChangeEvent<bool>>(evt =>
                {
                    stateContainer.Dispatch(evt.newValue
                        ? Commands.ExpandCompositeBinding(binding)
                        : Commands.CollapseCompositeBinding(binding));

                    evt.StopPropagation();
                });
                toggle.userData = callback;
                toggle.RegisterValueChangedCallback(callback);
            };
            bindingsListView.makeItem = () => bindingItemRowTemplate.CloneTree();
            bindingsListView.unbindItem += (e, i) =>
            {
                var toggle = e.Q<Toggle>("expando");
                toggle.style.visibility = Visibility.Hidden;
                toggle.UnregisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)toggle.userData);
            };
            bindingsListView.onSelectionChange += _ =>
            {
	            var viewState = (ViewState)bindingsListView.selectedItem;
	            Dispatch(Commands.SelectBinding(viewState.binding));
            };

            CreateSelector(
                state => state.selectedActionIndex,
	            state => new ViewStateCollection<ViewState>(
		            Selectors.GetVisibleBindingsForSelectedAction(state), ViewState.comparer),
	            (_, bindings, _) => bindings.ToList());
        }

        public override void RedrawUI(List<ViewState> bindings)
        {
	        var bindingsListView = m_Root.Q<ListView>("bindings-list-view");
	        bindingsListView.itemsSource?.Clear();
	        bindingsListView.itemsSource = bindings;
	        bindingsListView.Rebuild();
        }

        public struct ViewState
        {
	        private sealed class BindingViewStateEqualityComparer : IEqualityComparer<ViewState>
	        {
		        public bool Equals(ViewState x, ViewState y)
		        {
			        return x.binding.Equals(y.binding) && x.isExpanded == y.isExpanded;
		        }

		        public int GetHashCode(ViewState obj)
		        {
			        return HashCode.Combine(obj.binding, obj.isExpanded);
		        }
	        }

	        public static IEqualityComparer<ViewState> comparer { get; } = new BindingViewStateEqualityComparer();

	        public SerializedInputBinding binding { get; }
	        public bool isExpanded { get; }
	        public string controlType { get; }

	        public ViewState(SerializedInputBinding binding, bool isExpanded, string controlType)
	        {
		        this.binding = binding;
		        this.isExpanded = isExpanded;
		        this.controlType = string.IsNullOrEmpty(controlType) ? "InputControl" : controlType;
	        }
        }
    }

    internal static partial class Selectors
    {
	    /// <summary>
	    /// Return a collection of the bindings that should be rendered in the view based on the selected action map, selected action,
	    /// and expanded state.
	    /// </summary>
	    /// <param name="state"></param>
	    /// <returns></returns>
	    public static IEnumerable<BindingsListView.ViewState> GetVisibleBindingsForSelectedAction(GlobalInputActionsEditorState state)
	    {
		    var actionMap = state.serializedObject
			    .FindProperty(nameof(InputActionAsset.m_ActionMaps))
			    .GetArrayElementAtIndex(state.selectedActionMapIndex);
		    var selectedAction = new SerializedInputAction(
			    actionMap.FindPropertyRelative(nameof(InputActionMap.m_Actions))
				    .GetArrayElementAtIndex(state.selectedActionIndex));
            
		    var bindings = actionMap
			    .FindPropertyRelative(nameof(InputActionMap.m_Bindings))
			    .Select(sp => new SerializedInputBinding(sp))
			    .Where(sp => sp.action == selectedAction.name)
			    .ToList();

		    var expandedStates = state.GetOrCreateExpandedState();
		    var indexOfPreviousComposite = -1;
		    foreach (var binding in bindings)
		    {
			    if (binding.isComposite)
			    {
				    indexOfPreviousComposite = binding.indexOfBinding;
				    yield return new BindingsListView.ViewState(binding, expandedStates.Contains(indexOfPreviousComposite),
					    selectedAction.expectedControlType);
			    }
			    else
			    {
				    string controlLayout = string.Empty;
				    try
				    {
					    controlLayout = InputControlPath.TryGetControlLayout(binding.path);
				    }
				    catch (Exception )
				    {
				    }

				    if (binding.isPartOfComposite)
				    {
					    if (expandedStates.Contains(indexOfPreviousComposite) == false)
						    continue;

					    yield return new BindingsListView.ViewState(binding, false, controlLayout);
				    }
				    else
				    {
					    yield return new BindingsListView.ViewState(binding, false, controlLayout);
				    }
			    }
		    }
	    }
    }
}