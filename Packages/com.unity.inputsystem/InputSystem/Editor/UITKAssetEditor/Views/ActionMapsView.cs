using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionMapsView : ViewBase<(SerializedInputActionMap, IEnumerable<string>)>
    {
        private readonly VisualElement m_Root;

        public ActionMapsView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            CreateSelector(Selectors.GetActionMapNames,
                (actionMapNames, state) => (Selectors.GetSelectedActionMap(state), actionMapNames));
        }

        private DropdownField actionMapsDropdown => m_Root?.Q<DropdownField>("selected-action-map-dropdown");
        private Button addActionMapButton => m_Root?.Q<Button>("add-new-action-map-button");

        public override void RedrawUI((SerializedInputActionMap, IEnumerable<string>) viewState)
        {
            var actionMap = viewState.Item1;
            actionMapsDropdown.choices.Clear();
            actionMapsDropdown.choices.AddRange(viewState.Item2);
            actionMapsDropdown.SetValueWithoutNotify(actionMap.name);

            actionMapsDropdown.RegisterCallback<ChangeEvent<string>>(SelectActionMap);

            addActionMapButton.clicked += ShowAddActionMapWindow;
        }

        public override void DestroyView()
        {
            actionMapsDropdown.UnregisterCallback<ChangeEvent<string>>(SelectActionMap);
            addActionMapButton.clicked -= ShowAddActionMapWindow;
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
