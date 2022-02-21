using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class BindingsPanelView : UIToolkitView
    {
	    private const float TreeItemIndentPerLevel = 15;

	    private readonly VisualElement m_Root;

	    public BindingsPanelView(VisualElement root, StateContainer stateContainer)
	        : base(stateContainer)
        {
	        m_Root = root;
	        var bindingItemRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                GlobalInputActionsConstants.PackagePath +
                GlobalInputActionsConstants.ResourcesPath +
                GlobalInputActionsConstants.BindingsPanelRowTemplate);

            var bindingsListView = root.Q<ListView>("bindings-list-view");
            bindingsListView.selectionType = UIElements.SelectionType.Single;
            bindingsListView.bindItem = (e, i) =>
            {
                var bindingState = (Selectors.BindingViewState)bindingsListView.itemsSource[i];
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
	            var viewState = (Selectors.BindingViewState)bindingsListView.selectedItem;
	            stateContainer.Dispatch(Commands.SelectBinding(viewState.binding));
            };
        }

        public override void CreateUI(GlobalInputActionsEditorState state)
        {
	        var bindingsListView = m_Root.Q<ListView>("bindings-list-view");
	        bindingsListView.itemsSource?.Clear();
	        bindingsListView.itemsSource = Selectors.GetVisibleBindingsForSelectedAction(state).ToList();
	        bindingsListView.Rebuild();
        }
    }
}