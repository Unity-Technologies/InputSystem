using System;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionMapsView : UIToolkitView
    {
        private readonly VisualElement m_Root;

        public ActionMapsView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
        }

        private DropdownField actionMapsDropdown => m_Root?.Q<DropdownField>("selected-action-map-dropdown");
        private Button addActionMapButton => m_Root?.Q<Button>("add-new-action-map-button");

        public override void CreateUI(GlobalInputActionsEditorState state)
        {
            if (actionMapsDropdown == null)
                throw new ArgumentNullException("Expected the root visual element to contain an element called " +
                    "'selected-action-map-dropdown'.");

            if (addActionMapButton == null)
                throw new ArgumentNullException("Expected the root visual element to contain an element called " +
                    "'add-new-action-map-button'.");

            var actionMap = Selectors.GetSelectedActionMap(state);
            actionMapsDropdown.choices.Clear();
            actionMapsDropdown.choices.AddRange(Selectors.GetActionMapNames(state));
            actionMapsDropdown.index = actionMapsDropdown.choices.FindIndex(x => x == actionMap.name);

            actionMapsDropdown.RegisterCallback<ChangeEvent<string>>(SelectActionMap);

            addActionMapButton.clicked += ShowAddActionMapWindow;
        }

        public override void ClearUI()
        {
            actionMapsDropdown.UnregisterValueChangedCallback(SelectActionMap);
        }

        private void SelectActionMap(ChangeEvent<string> evt)
        {
            Dispatch(Commands.SelectActionMap(evt.newValue));
        }

        private void ShowAddActionMapWindow()
        {
        }
    }
}
