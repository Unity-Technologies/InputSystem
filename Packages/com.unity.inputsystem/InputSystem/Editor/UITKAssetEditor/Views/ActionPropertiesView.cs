#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionPropertiesView : ViewBase<(SerializedInputAction?, List<string>)>
    {
        private readonly Foldout m_ParentFoldout;
        private readonly int m_DropdownLabelWidth = 90;

        public ActionPropertiesView(VisualElement root, Foldout foldout, StateContainer stateContainer)
            : base(root, stateContainer)
        {
            m_ParentFoldout = foldout;

            // TODO: Consider IEquatable<T> and how to compare selector data
            CreateSelector(Selectors.GetSelectedAction,
                (inputAction, _) =>
                {
                    if (!inputAction.HasValue)
                        return (null, new List<string>());
                    return (inputAction.Value, Selectors.BuildControlTypeList(inputAction.Value.type).ToList());
                });
        }

        public override void RedrawUI((SerializedInputAction ? , List<string>) viewState)
        {
            if (!viewState.Item1.HasValue)
                return;

            m_ParentFoldout.text = "Action";
            var inputAction = viewState.Item1.Value;

            rootElement.Clear();

            var actionType = new EnumField("Action Type", inputAction.type)
            {
                tooltip = inputAction.actionTypeTooltip
            };

            // Tighten up the gap between the label and dropdown so the latter is more readable when the parent pane is at min width.
            var actionLabel = actionType.Q<Label>();
            actionLabel.style.minWidth = m_DropdownLabelWidth;
            actionLabel.style.width = m_DropdownLabelWidth;

            actionType.RegisterValueChangedCallback(evt =>
            {
                Dispatch(Commands.ChangeActionType(inputAction, (InputActionType)evt.newValue));
            });
            rootElement.Add(actionType);

            if (inputAction.type != InputActionType.Button)
            {
                var controlTypes = viewState.Item2;
                var controlType = new DropdownField("Control Type");

                // Tighten up the gap between the label and dropdown so the latter is more readable when the parent pane is at min width.
                var controlLabel = controlType.Q<Label>();
                controlLabel.style.minWidth = m_DropdownLabelWidth;
                controlLabel.style.width = m_DropdownLabelWidth;

                controlType.choices.Clear();
                controlType.choices.AddRange(controlTypes.Select(ObjectNames.NicifyVariableName).ToList());
                var controlTypeIndex = controlTypes.FindIndex(s => s == inputAction.expectedControlType);
                //if type changed and index is -1 clamp to 0, prevent overflowing indices
                controlTypeIndex = Math.Clamp(controlTypeIndex, 0, controlTypes.Count - 1);
                controlType.SetValueWithoutNotify(controlType.choices[controlTypeIndex]);
                controlType.tooltip = inputAction.expectedControlTypeTooltip;

                controlType.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeActionControlType(inputAction, controlType.index));
                });

                // ISX-1916 - When changing ActionType to a non-Button type, we must also update the ControlType
                // to the currently selected value; the ValueChangedCallback is not fired in this scenario.
                Dispatch(Commands.ChangeActionControlType(inputAction, controlType.index));

                rootElement.Add(controlType);
            }
            else
            {
                // ISX-1916 - When changing ActionType to a Button, we must also reset the ControlType
                Dispatch(Commands.ChangeActionControlType(inputAction, 0));
            }

            if (inputAction.type != InputActionType.Value)
            {
                var initialStateCheck = new Toggle("Initial State Check")
                {
                    tooltip = InputActionsEditorConstants.InitialStateCheckTooltip
                };
                initialStateCheck.SetValueWithoutNotify(inputAction.initialStateCheck);
                initialStateCheck.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeInitialStateCheck(inputAction, evt.newValue));
                });
                rootElement.Add(initialStateCheck);
            }
        }
    }
}

#endif
