using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.ActionsV2
{
	public class InputAction<TValue> where TValue : struct
	{
		public event Action<CallbackContext<TValue>> started;
		public event Action<CallbackContext<TValue>> performed;
		public event Action<CallbackContext<TValue>> cancelled;

		private Binding<TValue> m_ActiveBinding;

		public List<Binding<TValue>> bindings { get; set; }
		public InputActionPhase phase { get; private set; }

		public Binding<TValue> activeBinding => m_ActiveBinding;

		public InputActionType inputActionType { get; set; }
		public bool isPassThrough => inputActionType == InputActionType.PassThrough;
		public bool isButton => inputActionType == InputActionType.Button;

		public void Enable()
		{
			foreach (var binding in bindings)
			{
				binding.Enable();
			}
		}

		public void Disable()
		{
			foreach (var binding in bindings)
			{
				binding.Disable();
			}
		}

		public void ControlValueChanged(Binding<TValue> binding, TValue newValue)
		{
			m_ActiveBinding = binding;
		}

		public TValue ReadValue()
		{
			var binding = m_ActiveBinding ?? bindings[0];
			return binding.ReadValue();
		}

		public void ChangePhaseOfAction(InputActionPhase newPhase, InputActionPhase phaseAfterPerformedOrCanceled = InputActionPhase.Waiting)
		{
			if (newPhase == InputActionPhase.Performed && phase == InputActionPhase.Waiting)
			{
				ChangePhaseOfActionInternal(InputActionPhase.Started);
				ChangePhaseOfActionInternal(newPhase);
				if(phaseAfterPerformedOrCanceled == InputActionPhase.Waiting)
					ChangePhaseOfActionInternal(InputActionPhase.Canceled);

				phase = phaseAfterPerformedOrCanceled;
			}
			else if (phase != newPhase || newPhase == InputActionPhase.Performed)
			{
				ChangePhaseOfActionInternal(newPhase);

				if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Canceled)
					phase = phaseAfterPerformedOrCanceled;
			}
		}

		private void ChangePhaseOfActionInternal(InputActionPhase newPhase)
        {
            // We need to make sure here that any HaveMagnitude flag we may be carrying over from actionState
            // is handled correctly (case 1239551).
            newState.flags = actionState->flags; // Preserve flags.
            if (newPhase != InputActionPhase.Canceled)
                newState.magnitude = trigger.haveMagnitude
                    ? trigger.magnitude
                    : ComputeMagnitude(trigger.bindingIndex, trigger.controlIndex);
            else
                newState.magnitude = 0;

            newState.phase = newPhase;
            if (newPhase == InputActionPhase.Performed)
            {
                newState.lastPerformedInUpdate = InputUpdate.s_UpdateStepCount;
                newState.lastCanceledInUpdate = actionState->lastCanceledInUpdate;
            }
            else if (newPhase == InputActionPhase.Canceled)
            {
                newState.lastCanceledInUpdate = InputUpdate.s_UpdateStepCount;
                newState.lastPerformedInUpdate = actionState->lastPerformedInUpdate;
            }
            else
            {
                newState.lastPerformedInUpdate = actionState->lastPerformedInUpdate;
                newState.lastCanceledInUpdate = actionState->lastCanceledInUpdate;
            }
            newState.pressedInUpdate = actionState->pressedInUpdate;
            newState.releasedInUpdate = actionState->releasedInUpdate;
            if (newPhase == InputActionPhase.Started)
                newState.startTime = newState.time;
            *actionState = newState;

            // Let listeners know.
            trigger.phase = newPhase;
            switch (newPhase)
            {
                case InputActionPhase.Started:
                {
                    CallActionListeners(newPhase, started, "started");
                    break;
                }

                case InputActionPhase.Performed:
                {
                    CallActionListeners(newPhase, performed, "performed");
                    break;
                }

                case InputActionPhase.Canceled:
                {
                    CallActionListeners(newPhase, cancelled, "canceled");
                    break;
                }
            }
        }

		private void CallActionListeners(InputActionPhase newPhase, Action<CallbackContext<TValue>> callback, string actionName)
		{

		}
	}

	public struct CallbackContext<TValue> where TValue : struct
	{
		public TValue value { get; }
		public InputActionPhase phase { get; }

		public bool started => phase == InputActionPhase.Started;

		public bool performed => phase == InputActionPhase.Performed;

		public bool canceled => phase == InputActionPhase.Canceled;

		public InputAction<TValue> action { get; }

		public IInputInteraction<TValue> interaction => action?.activeBinding?.activeInteraction;
	}
}