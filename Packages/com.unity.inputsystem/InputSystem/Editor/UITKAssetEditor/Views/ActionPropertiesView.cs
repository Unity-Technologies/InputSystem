#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionPropertiesView : ViewBase<(SerializedInputAction, List<string>)>
    {
        private readonly VisualElement m_Root;

        public ActionPropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            // TODO: Consider IEquatable<T> and how to compare selector data
            CreateSelector(Selectors.GetSelectedAction,
                (inputAction, _) => (inputAction, Selectors.BuildSortedControlList(inputAction.type).ToList()));
        }

        public override void RedrawUI((SerializedInputAction, List<string>) viewState)
        {
            var inputAction = viewState.Item1;

            m_Root.Clear();

            var actionType = new EnumField("Action Type", inputAction.type)
            {
                tooltip = inputAction.actionTypeTooltip
            };
            actionType.RegisterValueChangedCallback(evt =>
            {
                Dispatch(Commands.ChangeActionType(inputAction, (InputActionType)evt.newValue));
            });
            m_Root.Add(actionType);

            if (inputAction.type != InputActionType.Button)
            {
                var controlTypes = viewState.Item2;
                var controlType = new DropdownField("Control Type");
                controlType.choices.Clear();
                controlType.choices.AddRange(controlTypes.Select(ObjectNames.NicifyVariableName).ToList());
                var controlTypeIndex = controlTypes.FindIndex(s => s == inputAction.expectedControlType);
                controlType.SetValueWithoutNotify(controlType.choices[controlTypeIndex]);
                controlType.tooltip = inputAction.expectedControlTypeTooltip;

                controlType.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeActionControlType(inputAction, controlType.index));
                });
                m_Root.Add(controlType);
            }

            if (inputAction.type != InputActionType.Value)
            {
                var initialStateCheck = new Toggle("Initial State Check")
                {
                    tooltip = InputActionsEditorConstants.InitialStateCheckTooltip
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

#endif
