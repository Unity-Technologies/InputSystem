using System.Collections.Generic;

namespace UnityEngine.InputSystem.HighLevelAPI
{
	public struct InputEvent
	{
		public DeviceType deviceType { get; }

		/// <summary>
		/// -1 if no player
		/// </summary>
		public int playerIndex { get; }

		/// <summary>
		/// The frame this input occurred in, so it's possible to check if there was any input this frame.
		/// </summary>
		public int frame { get; }

		public InputEventModifiers modifiers { get; private set; }

		public bool hasModifiers => HasEventComponent<InputEventModifiers>();

		public InputEvent(List<IInputEventComponent> components, DeviceType deviceType, int frame, int playerIndex = -1)
		{
			m_Components = components;
			this.deviceType = deviceType;
			this.frame = frame;
			this.playerIndex = playerIndex;
			modifiers = default;
		}

		public InputEvent(List<IInputEventComponent> components, InputEventModifiers modifiers, DeviceType deviceType, int frame, int playerIndex = -1)
		{
			m_Components = components;
			this.deviceType = deviceType;
			this.frame = frame;
			this.playerIndex = playerIndex;
			this.modifiers = modifiers;
		}

		public bool HasEventComponent<TInputEventComponent>() where TInputEventComponent : struct, IInputEventComponent
		{
			for (var i = 0; i < m_Components.Count; i++)
			{
				if (m_Components[i] is TInputEventComponent component)
					return true;
			}

			return false;
		}

		public TInputEventComponent GetEventComponent<TInputEventComponent>() where TInputEventComponent : struct, IInputEventComponent
		{
			for (var i = 0; i < m_Components.Count; i++)
			{
				if (m_Components[i] is TInputEventComponent component)
					return component;
			}

			return default;
		}

		private List<IInputEventComponent> m_Components;
	}

	public interface IInputEventComponent
	{
	}
    
	public struct InputEventAction : IInputEventComponent
	{
		public InputAction action { get; private set; }
	}

	public struct InputEventMouse : IInputEventComponent
	{
		public Vector2 delta { get; set; }
		
		// buttons
	}

	public struct InputEventKey : IInputEventComponent
	{
		public bool pressed { get; private set; }
		public Key physicalKey { get; private set; }
		public string keyValue { get; set; }
	}

	public struct InputEventModifiers : IInputEventComponent
	{
		public bool ctrl { get; private set; }
		public bool shift { get; private set; }
		public bool alt { get; private set; }
		public bool command { get; private set; }
		public bool meta { get; private set; }
	}
}