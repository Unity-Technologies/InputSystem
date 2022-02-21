using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionPropertiesView : UIToolkitView
    {
        private readonly VisualElement m_Root;
        private SerializedInputAction m_LastState;

        public ActionPropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;
        }

        public override void CreateUI(GlobalInputActionsEditorState state)
        {
            var inputAction = Selectors.GetSelectedAction(state);

            if (inputAction.Equals(m_LastState))
                return;

            m_LastState = inputAction;

            m_Root.Clear();

            var actionType = new EnumField("Action Type", inputAction.type);
            actionType.tooltip = inputAction.actionTypeTooltip;
            actionType.RegisterValueChangedCallback(evt =>
            {
                Dispatch(Commands.ChangeActionType(inputAction, (InputActionType)evt.newValue));
            });
            m_Root.Add(actionType);

            if (inputAction.type != InputActionType.Button)
            {
                var controlTypes = Selectors.BuildSortedControlList(inputAction.type).ToList();
                var controlType = new DropdownField("Control Type",
                    controlTypes.Select(ObjectNames.NicifyVariableName).ToList(),
                    controlTypes.FindIndex(s => s == inputAction.expectedControlType));
                controlType.tooltip = inputAction.expectedControlTypeTooltip;
                m_Root.Add(controlType);
            }

            if (inputAction.type != InputActionType.Value)
            {
                var initialStateCheck = new Toggle("Initial State Check")
                {
                    tooltip = GlobalInputActionsConstants.InitialStateCheckTooltip
                };
                initialStateCheck.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeInitialStateCheck(inputAction, evt.newValue));
                });
                m_Root.Add(initialStateCheck);
            }
        }
    }
}
