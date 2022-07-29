using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace UnityEngine.InputSystem.HighLevel
{
	public class InputEventSystem
	{
		internal void Initialize()
		{
			// hook up event handlers to global asset action maps and InputSystem.onEvent (or equivalent. Remember that
			// events arriving through InputSystem.onEvent haven't had control processors applied)
			throw new NotImplementedException();
		}

		internal void Shutdown()
		{
			// detach all events
			throw new NotImplementedException();
		}

		public void RegisterInputEventBuilder(IInputEventBuilder eventBuilder)
		{
			throw new NotImplementedException();
		}

		private void OnEvent(InputEventPtr inputEventPtr, InputDevice device)
		{
			// loop through each changed control in the event pointer and for each one, loop through all input
			// event builders and give each a chance to create event data from it. Each changed control should
			// be surfaced as one InputEvent instance. InputEvent's should all get a unique Id that can be used
			// to look up event data from native arrays where they are stored. 

			throw new NotImplementedException();
		}

		internal bool TryGetEventData<TInputEventData>(int eventId, int typeIndex, out TInputEventData inputEventData)
			where TInputEventData : struct, IInputEventData
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called from input event builders to add a piece of event data to an input event.
		/// </summary>
		/// <typeparam name="TData"></typeparam>
		/// <param name="inputEvent"></param>
		/// <param name="data"></param>
		public void AddEventData<TData>(in InputEvent inputEvent, TData data) where TData : struct, IInputEventData
		{
			// this needs to insert the event data into unmanaged memory in a way that can be retrieved using the
			// inputEvent's eventId. We also need a way to persistently and rapidly index (persistent within a play session) into
			// an array containing event data by event type. This can be achieved using the TypeIndex<T> class in this branch
			// as an example, which creates monotonically increasing indexes for each T that is used, but other ways are possible.
			throw new NotImplementedException();
		}
	}

	internal static class TypeIndex<T> where T : struct
	{
		static TypeIndex()
		{
			index = TypeIndexGenerator.nextIndex;
		}

		public static int index { get; }
	}

	internal static class TypeIndexGenerator
	{
		private static int s_NextIndex;

		public static int nextIndex => s_NextIndex++;
	}

	public interface IInputEventBuilder
	{
		DeviceType processesDevicesOfType { get; }

		void AddEventData(InputDevice device, InputControl control, InputEventSystem eventSystem, in InputEvent inputEvent);
	}

	internal class MouseInputEventBuilder : IInputEventBuilder
	{
		public DeviceType processesDevicesOfType => DeviceType.Mouse;

		public void AddEventData(InputDevice device, InputControl control, InputEventSystem eventSystem,
			in InputEvent inputEvent)
		{
			if (device is Mouse == false)
				return;

			if (control == ((Mouse)device).leftButton)
				eventSystem.AddEventData(in inputEvent, new ButtonEventData<MouseButton>(MouseButton.Left, control.IsPressed()));
		}
	}

	public readonly struct InputEvent
	{
		public DeviceType deviceType => device.deviceType;

		public InputDevice device { get; }

		/// <summary>
		/// Is this event a result of a control being held.
		/// </summary>
		/// <remarks>
		///	Sends one event per frame while a control is held.
		/// </remarks>
		public bool isRepeat { get; }

		/// <summary>
		/// The frame this input occurred in, so it's possible to check if there was any input this frame.
		/// </summary>
		public int frame { get; }

		/// <summary>
		/// Quick access to modifier keys.
		/// </summary>
		public ModifiersEventData modifiers
		{
			get
			{
				TryGetEventData<ModifiersEventData>(out var localModifiers);
				return localModifiers;
			}
		}

		internal InputEvent(int eventId, InputEventSystem eventSystem, InputDevice device, int frame, bool isRepeat)
		{
			m_EventId = eventId;
			m_EventSystem = eventSystem;
			this.device = device;
			this.frame = frame;
			this.isRepeat = isRepeat;
		}
		
		public bool TryGetEventData<TInputEventData>(out TInputEventData component) where TInputEventData : struct, IInputEventData
		{
			// One possibility for how this method can be implemented
			// 
			// component = default;
			// var typeIndex = TypeIndex<TInputEventData>.index;
			// if (m_EventSystem.TryGetEventData(m_EventId, typeIndex, out TInputEventData eventData))
			// {
			// 	component = eventData;
			// 	return true;
			// }
			//
			// return false;
			throw new NotImplementedException();
		}

		public bool IsAction<TValueType>(Input<TValueType> input) where TValueType : struct
		{
			return TryGetEventData(out InputActionEventData actionEventData) && 
			       actionEventData.IsAction(input);
		}

		public bool IsDeviceTypeAnyOf(DeviceType deviceTypes)
		{
			return (deviceType | deviceTypes) != 0;
		}

		private readonly int m_EventId;
		private readonly InputEventSystem m_EventSystem;
	}

	public interface IInputEventData
	{
	}
    
	public readonly struct PointerEventData : IInputEventData
	{
		public PointerEventData(Vector2 position, Vector2 delta)
		{
			this.position = position;
			this.delta = delta;
		}

		public Vector2 position { get; }
		public Vector2 delta { get; }
	}

	public readonly struct PenEventData : IInputEventData
	{
		public PenEventData(float pressure, float tilt, float twist)
		{
			this.pressure = pressure;
			this.tilt = tilt;
			this.twist = twist;
		}

		public float pressure { get; }
		public float tilt { get; }
		public float twist { get; }
	}
	
	public readonly struct MouseScrollEventData : IInputEventData
	{
		public MouseScrollEventData(Vector2 scrollDelta)
		{
			this.scrollDelta = scrollDelta;
		}

		public Vector2 scrollDelta { get; }
	}

	public readonly struct ButtonEventData<TButtonType> : IInputEventData 
		where TButtonType : struct
	{
		public ButtonEventData(TButtonType button, bool press)
		{
			this.button = button;
			this.press = press;
		}

		public TButtonType button { get; }
		public bool press { get; }

		public bool IsButtonPress(TButtonType button)
		{
			return EqualityComparer<TButtonType>.Default.Equals(this.button, button) && press;
		}

		public bool IsButtonRelease(TButtonType button)
		{
			return EqualityComparer<TButtonType>.Default.Equals(this.button, button) && !press;
		}
	}

	public readonly struct KeyEventData : IInputEventData
	{
		public KeyEventData(bool press, Key physicalKey, string keyValue)
		{
			this.press = press;
			this.physicalKey = physicalKey;
			this.keyValue = keyValue;
		}

		/// <summary>
		/// True for the event where the key was pressed.
		/// </summary>
		/// <remarks>
		///	False in repeat events.
		/// </remarks>
		public bool press { get; }
		public Key physicalKey { get; }
		public string keyValue { get; }
	}

	public readonly struct ImeTextEventData : IInputEventData
	{
		public ImeTextEventData(IMECompositionString text)
		{
			this.text = text;
		}

		public IMECompositionString text { get; }
	}

	public readonly struct ModifiersEventData : IInputEventData
	{
		public ModifiersEventData(bool ctrl, bool shift, bool alt, bool command, bool meta)
		{
			this.ctrl = ctrl;
			this.shift = shift;
			this.alt = alt;
			this.command = command;
			this.meta = meta;
		}

		public bool ctrl { get; }
		public bool shift { get; }
		public bool alt { get; }
		public bool command { get; }
		public bool meta { get; }
	}

	public readonly struct GamepadStickEventData : IInputEventData
	{
		public GamepadStickEventData(Vector2 value, GamepadAxis stick)
		{
			this.value = value;
			this.stick = stick;
		}

		public Vector2 value { get; }
		public GamepadAxis stick { get; }
	}

	public readonly struct JoystickEventData : IInputEventData
	{
		public JoystickEventData(Vector2 value)
		{
			this.value = value;
		}

		public Vector2 value { get; }
	}

	public readonly struct TouchEventData : IInputEventData
	{
		public TouchEventData(int finger, bool press, Vector2 position)
		{
			this.finger = finger;
			this.press = press;
			this.position = position;
		}

		public int finger { get; }
		public bool press { get; }
		public Vector2 position { get; }
	}

	public readonly struct InputActionEventData : IInputEventData
	{
		public InputActionEventData(InputAction action, bool started, bool performed, bool canceled, InputControl control, 
			IInputInteraction interaction, double time, double startTime, double duration, InputUser player)
		{
			this.action = action;
			this.started = started;
			this.performed = performed;
			this.canceled = canceled;
			this.control = control;
			this.interaction = interaction;
			this.time = time;
			this.startTime = startTime;
			this.duration = duration;
			this.player = player;
		}

		public InputAction action { get; }
		public bool started { get; }
		public bool performed { get; }
		public bool canceled { get; }
		public InputControl control { get; }
		public IInputInteraction interaction { get; }
		public double time { get; }
		public double startTime { get; }
		public double duration { get; }

		// Should this be the player or the player index? What if the player changes between this event data being created and
		// the code checking it? InputUser.valid exists but developers would need to know to check it. Think about this some more.
		// NOTE: This will only make sense when the new player API is done because we need a centralised way to create a clone of
		// global actions for a new user and have the events hooked up to the InputEventSystem.
		public InputUser player { get; }
		
		public bool IsAction(InputAction inputAction)
		{
			return action == inputAction;
		}

		public bool IsInteraction<TInteraction, TAction>(InputInteraction<TInteraction, TAction> interaction) 
			where TInteraction : IInputInteraction 
			where TAction : struct
		{
			return this.interaction is TInteraction;
		}

		// This need to return the same value for the lifetime of the event data (which is one frame, unless it's cloned).
		// This implies some store of untyped data and using UnsafeUtility.AddressOf()
		public TValue ReadValue<TValue>() where TValue : struct
		{
			return action.ReadValue<TValue>();
		}
	}

	public static class InputEventExtensions
	{
		public static bool TryGetMouseScrollEventData(this InputEvent inputEvent, out MouseScrollEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetPointerPositionEventData(this InputEvent inputEvent, out PointerEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetMouseButtonEventData(this InputEvent inputEvent, out ButtonEventData<MouseButton> data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetKeyEventData(this InputEvent inputEvent, out KeyEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetGamepadButtonEventData(this InputEvent inputEvent, out ButtonEventData<GamepadButton> data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetGamepadStickEventData(this InputEvent inputEvent, out GamepadStickEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetJoystickEventData(this InputEvent inputEvent, out JoystickEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetTouchEventData(this InputEvent inputEvent, out TouchEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetPenEventData(this InputEvent inputEvent, out PenEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetImeTextEventData(this InputEvent inputEvent, out ImeTextEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}

		public static bool TryGetInputActionEventData(this InputEvent inputEvent, out InputActionEventData data)
		{
			return inputEvent.TryGetEventData(out data);
		}
	}
}
