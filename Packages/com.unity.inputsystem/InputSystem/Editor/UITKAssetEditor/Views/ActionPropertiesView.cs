#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class ActionPropertiesView : ViewBase<(SerializedInputAction?, List<string>)>
    {
        private readonly VisualElement m_Root;

        public ActionPropertiesView(VisualElement root, StateContainer stateContainer)
            : base(stateContainer)
        {
            m_Root = root;

            // TODO: Consider IEquatable<T> and how to compare selector data
            CreateSelector(Selectors.GetSelectedAction,
                (inputAction, _) =>
                {
                    if (!inputAction.HasValue)
                        return (null, new List<string>());
                    return (inputAction.Value, Selectors.BuildSortedControlList(inputAction.Value.type).ToList());
                });
        }

        public override void RedrawUI((SerializedInputAction ? , List<string>) viewState)
        {
            var inputAction = viewState.Item1;
            if (!inputAction.HasValue)
                return;

            m_Root.Clear();

            var actionType = new EnumField("Action Type", inputAction.Value.type)
            {
                tooltip = inputAction.Value.actionTypeTooltip
            };
            actionType.RegisterValueChangedCallback(evt =>
            {
                Dispatch(Commands.ChangeActionType(inputAction.Value, (InputActionType)evt.newValue));
            });
            m_Root.Add(actionType);

            if (inputAction.Value.type != InputActionType.Button)
            {
                var controlTypes = viewState.Item2;
                var controlType = new DropdownField("Control Type");
                controlType.choices.Clear();
                controlType.choices.AddRange(controlTypes.Select(ObjectNames.NicifyVariableName).ToList());
                var controlTypeIndex = controlTypes.FindIndex(s => s == inputAction.Value.expectedControlType);
                controlType.SetValueWithoutNotify(controlType.choices[controlTypeIndex]);
                controlType.tooltip = inputAction.Value.expectedControlTypeTooltip;

                controlType.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeActionControlType(inputAction.Value, controlType.index));
                });
                m_Root.Add(controlType);
            }

            if (inputAction.Value.type != InputActionType.Value)
            {
                var initialStateCheck = new Toggle("Initial State Check")
                {
                    tooltip = InputActionsEditorConstants.InitialStateCheckTooltip
                };
                initialStateCheck.RegisterValueChangedCallback(evt =>
                {
                    Dispatch(Commands.ChangeInitialStateCheck(inputAction.Value, evt.newValue));
                });
                m_Root.Add(initialStateCheck);
            }
        }
    }
}

#endif
