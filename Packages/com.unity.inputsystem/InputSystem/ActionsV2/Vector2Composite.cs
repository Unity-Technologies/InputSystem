using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputSystem.ActionsV2
{
	public struct Vector2Composite : IBinding<Vector2, float>
	{
		public event ControlActuatedDelegate<Vector2, float> ControlActuated;
			
		private readonly Binding<float> m_Up;
		private readonly Binding<float> m_Down;
		private readonly Binding<float> m_Left;
		private readonly Binding<float> m_Right;

		public Vector2 value { get; set; }

		public Vector2Composite(Binding<float> up, Binding<float> down, Binding<float> left, Binding<float> right)
		{
			m_Up = up;
			m_Down = down;
			m_Left = left;
			m_Right = right;

			value = Vector2.zero;
			ControlActuated = null;
			activeControl = null;

			m_Up.ControlActuated += OnUpActuated;
			m_Down.ControlActuated += OnDownActuated;
			m_Left.ControlActuated += OnLeftActuated;
			m_Right.ControlActuated += OnRightActuated;
		}

		// TODO: Is it necessary to call ReadValue on each of the bindings? If the binding was hooked up while
		// the controls were actuated, the value might be off until all controls were actuated. Maybe on enabled,
		// this should be doing an initial ReadValue? This would break if we kept the polling vs evented switch
		// and the child bindings were set to polling only.
		public Vector2 ReadValue()
		{
			var upValue = m_Up.ReadValue();
			var downValue = m_Down.ReadValue();
			var leftValue = m_Left.ReadValue();
			var rightValue = m_Right.ReadValue();

			return DpadControl.MakeDpadVector(upValue, downValue, leftValue, rightValue);
		}

		public void Enable()
		{
			m_Up.Enable();
			m_Down.Enable();
			m_Left.Enable();
			m_Right.Enable();
		}

		public void Disable()
		{
			m_Up.Disable();
			m_Down.Disable();
			m_Left.Disable();
			m_Right.Disable();
		}

		public float EvaluateMagnitude()
		{
			return value.magnitude;
		}

		public bool IsActuated(float magnitude)
		{
			return magnitude > float.MinValue;
		}

		public InputControl<Vector2> activeControl { get; }

		private void OnUpActuated(ref IBinding<float, float> binding, float controlValue, InputControl<float> control, double time)
		{
			value = new Vector2(value.x, controlValue);
			var temp = (IBinding<Vector2, float>)this;
			ControlActuated?.Invoke(ref temp, value, control, time);
		}

		private void OnDownActuated(ref IBinding<float, float> binding, float controlValue, InputControl<float> control, double time)
		{
			value = new Vector2(value.x, -controlValue);
			var temp = (IBinding<Vector2, float>)this;
			ControlActuated?.Invoke(ref temp, value, control, time);
		}

		private void OnLeftActuated(ref IBinding<float, float> binding, float controlValue, InputControl<float> control, double time)
		{
			value = new Vector2(-controlValue, value.y);
			var temp = (IBinding<Vector2, float>)this;
			ControlActuated?.Invoke(ref temp, value, control, time);
		}

		private void OnRightActuated(ref IBinding<float, float> binding, float controlValue, InputControl<float> control, double time)
		{
			value = new Vector2(controlValue, value.y);
			var temp = (IBinding<Vector2, float>)this;
			ControlActuated?.Invoke(ref temp, value, control, time);
		}
	}
}