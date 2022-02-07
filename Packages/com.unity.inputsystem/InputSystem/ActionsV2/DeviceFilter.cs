namespace UnityEngine.InputSystem.ActionsV2
{
	public struct DeviceFilter<TValue, TControl> where TValue:struct where TControl:struct
	{
		public event ControlActuatedDelegate<TValue, TControl> ControlActuated;
		
		private InputDevice[] m_Devices;

		public DeviceFilter(InputDevice[] devices)
		{
			m_Devices = devices;
			ControlActuated = null;
		}

		public void OnControlActuated(ref IBinding<TValue, TControl> binding, TValue newValue, InputControl<TControl> control, double time)
		{
			for (var i = 0; i < m_Devices.Length; i++)
			{
				if (m_Devices[i]?.deviceId != control.device.deviceId) continue;

				ControlActuated?.Invoke(ref binding, newValue, control, time);
				return;
			}
		}
	}
}