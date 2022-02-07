using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.ActionsV2
{
	public delegate void ActionExecuted<TValue, TControl>(ref CallbackContext<TValue, TControl> callbackContext) 
		where TValue:struct
		where TControl:struct;
	
	public class InputAction<TValue, TControl> where TValue : struct where TControl:struct
	{
		public event ActionExecuted<TValue, TControl> Executed;

		private IBinding<TValue, TControl> m_LastTriggeredBinding;
		private IList<IInputInteraction<TValue, TControl>> m_Interactions;
		private List<IBinding<TValue, TControl>> m_PreviouslyActuatedBindings;
		private bool m_IsPressed;
		private uint m_PressedInUpdate;
		private uint m_ReleasedInUpdate;
		private IBinding<TValue, TControl>[] m_Bindings;

		public InputAction(ref IBinding<TValue, TControl> binding)
		{
			m_PreviouslyActuatedBindings = new List<IBinding<TValue, TControl>>();
			m_Interactions = new List<IInputInteraction<TValue, TControl>>();
			m_Bindings = new[] { binding };
		}

		public ReadOnlyArray<IBinding<TValue, TControl>> bindings => new ReadOnlyArray<IBinding<TValue, TControl>>(m_Bindings);

		public InputActionPhase phase { get; private set; }

		public InputActionType inputActionType { get; set; }
		public bool isPassThrough => inputActionType == InputActionType.PassThrough;
		public bool isButton => inputActionType == InputActionType.Button;

		public bool isPressed => m_IsPressed;

		public void Enable()
		{
			foreach (ref var binding in m_Bindings.AsSpan())
			{
				binding.EnableDirect();
				binding.ControlActuated += ControlValueChanged;
			}
		}

		public void Disable()
		{
			foreach (ref var binding in m_Bindings.AsSpan())
			{
				binding.Disable();
				binding.ControlActuated -= ControlValueChanged;
			}
		}

		public void FilterByDevices(DeviceFilter<TValue, TControl> filter)
		{
			foreach (ref var binding in m_Bindings.AsSpan())
			{
				binding.ControlActuated -= ControlValueChanged;
				binding.ControlActuated += filter.OnControlActuated;
				filter.ControlActuated += ControlValueChanged;
			}
		}

		public void ControlValueChanged(ref IBinding<TValue, TControl> binding, TValue newValue, InputControl<TControl> inputControl, double time)
		{
			// there could be multiple bindings sending values. We could record all active bindings but then how do we know 
			// when a binding is not active anymore, since for some controls we don't get a finished-type event. Buttons yes,
			// continuous controls no. We could ask each binding that was recorded as having input on it for its magnitude
			// whenever any binding sends control values changed, and for bindings that are now not actuated, remove them
			// from consideration. Unless there's any input, bindings stay in the active bindings list

			// if the magnitude of the active control on the binding that just sent an input value is higher than the active
			// binding, we need to switch to this new control

			if (!isPassThrough && GetMostActuatedBinding(binding) != binding) return;

			if(!m_PreviouslyActuatedBindings.Contains(binding))
				m_PreviouslyActuatedBindings.Add(binding);

			UpdatePressedState(binding);
			m_LastTriggeredBinding = binding;
			NotifyListeners(newValue, inputControl, time);
		}

		private IBinding<TValue, TControl> GetMostActuatedBinding(IBinding<TValue, TControl> binding)
		{
			var mostActuatedBinding = binding;
			if (m_PreviouslyActuatedBindings.Count <= 0) return mostActuatedBinding;

			if (m_PreviouslyActuatedBindings.Count == 1 && m_PreviouslyActuatedBindings[0] == binding)
				return mostActuatedBinding;

			var currentBindingMagnitude = binding.EvaluateMagnitude();
			for (var i = m_PreviouslyActuatedBindings.Count - 1; i >= 0; i--)
			{
				if (binding == m_PreviouslyActuatedBindings[i])
					continue;

				var bindingMagnitude = m_PreviouslyActuatedBindings[i].EvaluateMagnitude();

				// if the binding is no longer actuated, remove it from the list
				if (m_PreviouslyActuatedBindings[i].IsActuated(bindingMagnitude) == false)
				{
					m_PreviouslyActuatedBindings.RemoveAt(i);
					continue;
				}

				// set the active binding to the one with the highest magnitude
				if (currentBindingMagnitude > bindingMagnitude)
					mostActuatedBinding = m_PreviouslyActuatedBindings[i];
			}

			return mostActuatedBinding;
		}

		private void UpdatePressedState(IBinding<TValue, TControl> binding)
		{
			var actuation = binding.EvaluateMagnitude();
			var pressPoint = binding.activeControl is ButtonControl button
				? button.pressPointOrDefault
				: ButtonControl.s_GlobalDefaultButtonPressPoint;
			if (!m_IsPressed && actuation >= pressPoint)
			{
				m_PressedInUpdate = InputUpdate.s_UpdateStepCount;
				m_IsPressed = true;
			}
			else if (m_IsPressed)
			{
				var releasePoint = pressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;
				if (actuation <= releasePoint)
				{
					m_ReleasedInUpdate = InputUpdate.s_UpdateStepCount;
					m_IsPressed = false;
				}
			}
		}

		public TInteraction GetInteraction<TInteraction>() where TInteraction:class, IInputInteraction<TValue, TControl>
		{
			return m_Interactions.FirstOrDefault(i => i is TInteraction) as TInteraction;
		}

		public void GetInteractions<TInteraction>(IList<IInputInteraction<TValue, TControl>> interactions) 
			where TInteraction:IInputInteraction<TValue, TControl>
		{
			foreach (var interaction in m_Interactions)
			{
				if(interaction is TInteraction typedInteraction)
					interactions.Add(typedInteraction);
			}
		}

		public TValue ReadValue()
		{
			var binding = m_LastTriggeredBinding ?? bindings[0];
			return binding.ReadValue();
		}

		private void NotifyListeners(TValue newValue, InputControl<TControl> inputControl, double time)
		{
			var ctx = new CallbackContext<TValue, TControl>(inputControl, newValue, time);
			Executed?.Invoke(ref ctx);
			foreach (var interaction in m_Interactions)
			{
				interaction.ProcessInput(ref ctx);
			}
		}

		public void AddInteraction(IInputInteraction<TValue, TControl> interaction)
		{
			m_Interactions.Add(interaction);
		}
	}

	public struct CallbackContext<TValue, TControl> where TValue : struct where TControl:struct
	{
		public CallbackContext(InputControl<TControl> inputControl, TValue newValue, double time)
		{
			this.inputControl = inputControl;
			this.value = newValue;
			this.time = time;
		}

		public TValue value { get; }
		public double time { get; }
		public InputControl<TControl> inputControl { get; }
	}

	public static class BindingExtensions
	{
		public static void EnableDirect<TInterface, TValue, TControl>(this TInterface binding)
			where TInterface : IBinding<TValue, TControl> where TValue : struct where TControl : struct
		{
			binding.Enable();
		}
	}
}