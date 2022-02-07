namespace UnityEngine.InputSystem.ActionsV2
{
	public struct AxisComposite : IBinding<float, float>
	{
		public event ControlActuatedDelegate<float, float> ControlActuated;

		private readonly Binding<float> m_Negative;
		private readonly Binding<float> m_Positive;

		private float m_Value;

		[Tooltip("Value to return when the negative side is fully actuated.")]
		public float minValue;

		[Tooltip("Value to return when the positive side is fully actuated.")]
		public float maxValue;

		public AxisComposite(Binding<float> negative, Binding<float> positive)
		{
			m_Negative = negative;
			m_Positive = positive;
			m_Value = 0;
			minValue = -1;
			maxValue = 1;
			activeControl = null;
			ControlActuated = null;

			m_Negative.ControlActuated += OnNegativeActuated;
			m_Positive.ControlActuated += OnPositiveActuated;
		}

		private void OnPositiveActuated(ref IBinding<float, float> binding, float newvalue, InputControl<float> inputcontrol, double time)
		{
			
		}

		private void OnNegativeActuated(ref IBinding<float, float> binding, float newvalue, InputControl<float> inputcontrol, double time)
		{
			
		}

		public float ReadValue()
		{
			return 0;
		}

		public void Enable()
		{
			throw new System.NotImplementedException();
		}

		public void Disable()
		{
			throw new System.NotImplementedException();
		}

		public float EvaluateMagnitude()
		{
			throw new System.NotImplementedException();
		}

		public bool IsActuated(float magnitude)
		{
			throw new System.NotImplementedException();
		}

		public InputControl<float> activeControl { get; }
	}
}