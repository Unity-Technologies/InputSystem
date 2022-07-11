using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.Pool;

namespace UnityEngine.InputSystem.HighLevelAPI
{
    public enum Inputs
    {
        Key_Space,
        Key_Enter,
        Key_Tab,
        Key_Backquote,
        Key_Quote,
        Key_Semicolon,
        Key_Comma,
        Key_Period,
        Key_Slash,
        Key_Backslash,
        Key_LeftBracket,
        Key_RightBracket,
        Key_Minus,
        Key_Equals,
        Key_A,
        Key_B,
        Key_C,
        Key_D,
        Key_E,
        Key_F,
        Key_G,
        Key_H,
        Key_I,
        Key_J,
        Key_K,
        Key_L,
        Key_M,
        Key_N,
        Key_O,
        Key_P,
        Key_Q,
        Key_R,
        Key_S,
        Key_T,
        Key_U,
        Key_V,
        Key_W,
        Key_X,
        Key_Y,
        Key_Z,
        Key_Digit1,
        Key_Digit2,
        Key_Digit3,
        Key_Digit4,
        Key_Digit5,
        Key_Digit6,
        Key_Digit7,
        Key_Digit8,
        Key_Digit9,
        Key_Digit0,
        Key_LeftShift,
        Key_RightShift,
        Key_LeftAlt,
        Key_RightAlt,
        Key_AltGr = Key_RightAlt,
        Key_LeftCtrl,
        Key_RightCtrl,
        Key_LeftMeta,
        Key_RightMeta,
        Key_LeftWindows = Key_LeftMeta,
        Key_RightWindows = Key_RightMeta,
        Key_LeftApple = Key_LeftMeta,
        Key_RightApple = Key_RightMeta,
        Key_LeftCommand = Key_LeftMeta,
        Key_RightCommand = Key_RightMeta,
        Key_ContextMenu,
        Key_Escape,
        Key_LeftArrow,
        Key_RightArrow,
        Key_UpArrow,
        Key_DownArrow,
        Key_Backspace,
        Key_PageDown,
        Key_PageUp,
        Key_Home,
        Key_End,
        Key_Insert,
        Key_Delete,
        Key_CapsLock,
        Key_NumLock,
        Key_PrintScreen,
        Key_ScrollLock,
        Key_Pause,
        Key_NumpadEnter,
        Key_NumpadDivide,
        Key_NumpadMultiply,
        Key_NumpadPlus,
        Key_NumpadMinus,
        Key_NumpadPeriod,
        Key_NumpadEquals,
        Key_Numpad0,
        Key_Numpad1,
        Key_Numpad2,
        Key_Numpad3,
        Key_Numpad4,
        Key_Numpad5,
        Key_Numpad6,
        Key_Numpad7,
        Key_Numpad8,
        Key_Numpad9,
        Key_F1,
        Key_F2,
        Key_F3,
        Key_F4,
        Key_F5,
        Key_F6,
        Key_F7,
        Key_F8,
        Key_F9,
        Key_F10,
        Key_F11,
        Key_F12,
        Key_OEM1,
        Key_OEM2,
        Key_OEM3,
        Key_OEM4,
        Key_OEM5,


        Mouse_Left,
        Mouse_Right,
        Mouse_Middle,
        Mouse_Forward,
        Mouse_Back,


        Gamepad_DpadUp,
        Gamepad_DpadDown,
        Gamepad_DpadLeft,
        Gamepad_DpadRight,
        Gamepad_North,
        Gamepad_East,
        Gamepad_South,
        Gamepad_West,
        Gamepad_LeftStick,  // left stick pressed
        Gamepad_RightStick, // right stick pressed
        Gamepad_LeftStickX,
        Gamepad_LeftStickY,
        Gamepad_RightStickX,
        Gamepad_RightStickY,
        Gamepad_LeftShoulder,
        Gamepad_RightShoulder,
        Gamepad_LeftTrigger,
        Gamepad_RightTrigger,
        Gamepad_Start,
        Gamepad_Select,
        Gamepad_X = Gamepad_West,
        Gamepad_Y = Gamepad_North,
        Gamepad_A = Gamepad_South,
        Gamepad_B = Gamepad_East,
        Gamepad_Cross = Gamepad_South,
        Gamepad_Square = Gamepad_West,
        Gamepad_Triangle = Gamepad_North,
        Gamepad_Circle = Gamepad_East,

        Joystick_Trigger
    }

    public enum GamepadAxis
    {
        LeftStick,
        RightStick
    }

    // These also exists in UnityEngine.InputSystem.LowLevel namespace but we shouldn't require the user to 
    // pull in another namespace just to get basic gamepad input.
    public enum GamepadButton
    {
	    DpadUp,
	    DpadDown,
	    DpadLeft,
	    DpadRight,
	    North,
	    East,
	    South,
	    West,
	    LeftStick,  // left stick pressed
	    RightStick, // right stick pressed
	    LeftShoulder,
	    RightShoulder,
        LeftTrigger,
        RightTrigger,
	    Start,
	    Select,
	    X = West,
	    Y = North,
	    A = South,
	    B = East,
	    Cross = South,
	    Square = West,
	    Triangle = North,
	    Circle = East
    }

    public enum GamepadSlot
    {
        Slot1 = 0,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot10,
        Slot11,
        Slot12,
        All = Int32.MaxValue,
        Any = Int32.MaxValue
    }

    public static partial class Input
    {
	    public static InputActionAsset globalActions { get; private set; }

	    private static readonly IList<Keyboard> s_keyboards;
	    private static readonly InputEvent s_LastInput;

	    static Input()
	    {
		    s_keyboards = InputSystem.devices.Where(d => d is Keyboard).Cast<Keyboard>().ToList();

		    InputSystem.onDeviceChange += OnDeviceChanged;
	    }

	    private static void OnDeviceChanged(InputDevice device, InputDeviceChange change)
	    {
		    if (change == InputDeviceChange.Added)
		    {
                if(device is Keyboard keyboard)
	                s_keyboards.Add(keyboard);
		    }
            else if (change == InputDeviceChange.Removed)
		    {
			    if (device is Keyboard keyboard && keyboards.Contains(keyboard))
				    s_keyboards.Remove(keyboard);
		    }
	    }
        
        // REVIEW: If the gamepads collection is slot indexed, shouldn't all device collections be? You would
        // be able to hold on to the gamepads collection but not the others because devices move around in those
        // arrays.

        public static IReadOnlyList<Keyboard> keyboards { get; }
        public static IReadOnlyList<Mouse> mice { get; }
        public static IReadOnlyList<Joystick> joysticks { get; }

        /// <summary>
        /// A collection of all gamepads connected to the system. This should be "slot" indexed. Unlike
        /// Gamepad.all, where if a device from the middle of the collection disconnects, all other devices
        /// slide down, this collection is stable in the sense that if a device from the middle disconnects,
        /// the indices of other devices stay as they were, and when another device connects, it takes
        /// the first free slot.
        /// </summary>
	    public static IReadOnlyList<Gamepad> gamepads { get; }


        // -------------------------------------
        // Basic input
        // -------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Can be used for gamepad input but only ever looks at the first device.
        /// </remarks>
        /// <param name="input"></param>
        /// <returns></returns>
	    public static bool IsControlPressed(Inputs input)
        {
	        switch (input)
	        {
                case Inputs.Key_Space:
	                return Keyboard.current.spaceKey.isPressed;

                case Inputs.Gamepad_A:
	                return Gamepad.current.aButton.isPressed;

                case Inputs.Mouse_Left:
                    return Mouse.current.leftButton.isPressed;
	        }
            return true;
        }

        /// <summary>
        /// True in the frame that the input was pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool IsControlDown(Inputs input)
        {
	        throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the input was released.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool IsControlUp(Inputs input)
        {
	        throw new NotImplementedException();
        }

        /// <summary>
        /// Turns any two inputs into an axis value between -1 and 1.
        /// </summary>
        /// <param name="minAxis"></param>
        /// <param name="maxAxis"></param>
        /// <returns></returns>
        public static float GetAxis(Inputs minAxis, Inputs maxAxis)
	    {
            // find the action in the list of temporary actions or add it if it doesn't exist
		    var action = new InputAction();

		    return action.ReadValue<float>();
	    }

        /// <summary>
        /// Turns any four inputs into a non-normalized vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Vector2 GetAxis(Inputs left, Inputs right, Inputs up, Inputs down)
	    {
		    return Vector2.zero;
	    }

        /// <summary>
        /// Turns any four inputs into a normalized vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Vector2 GetAxisNormalized(Inputs left, Inputs right, Inputs up, Inputs down)
        {
	        return Vector2.zero;
        }

        /// <summary>
        /// Get the value of either stick on a specific gamepad, or any gamepad if gamepadSlot is All.
        /// </summary>
        /// <param name="stick"></param>
        /// <param name="gamepadSlotIndex">If -1, uses the current gamepad, otherwise the gamepad at this index.</param>
        /// <returns></returns>
        public static Vector2 GetAxis(GamepadAxis stick, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
	        return Vector2.zero;
        }

        /// <summary>
        /// Get the value of the main axis on the joystick at index joystickIndex.
        /// </summary>
        /// <param name="joystickIndex"></param>
        /// <returns></returns>
        public static Vector2 GetJoystickAxis(int joystickIndex)
        {
	        return Vector2.zero;
        }

        /// <summary>
        /// True when the button is held.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public static bool IsGamepadButtonPressed(GamepadButton button, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
	        switch (button)
	        {
		        default:
			        return false;
	        }
        }

        /// <summary>
        /// True in the frame the button was pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gamepadSlotIndex">If -1, uses the current gamepad, otherwise the gamepad at this index.</param>
        /// <returns></returns>
        public static bool IsGamepadButtonDown(GamepadButton button, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
	        switch (button)
	        {
                default:
	                return false;
	        }
        }

        /// <summary>
        /// True in the frame the button was released.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gamepadSlotIndex">If -1, uses the current gamepad, otherwise the gamepad at this index.</param>
        /// <returns></returns>
        public static bool IsGamepadButtonUp(GamepadButton input, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
	        switch (input)
	        {
		        default:
			        return false;
	        }
        }

        /// <summary>
        /// If slot index is -1 here, set the trigger press points manually of both triggers on all connected
        /// gamepads. Save the values so that when new gamepads are connected, their press points can also be
        /// set.
        /// </summary>
        /// <param name="pressPoint"></param>
        /// <param name="gamepadSlotIndex"></param>
        public static void SetGamepadTriggerPressPoint(float pressPoint, GamepadSlot gamepadSlot = GamepadSlot.All)
        {
            
        }

        /// <summary>
        /// Same as SetGamepadTriggerPressPoint for slot index of -1.
        /// </summary>
        /// <param name="deadzone"></param>
        /// <param name="gamepadSlot"></param>
        public static void SetGamepadStickDeadzone(float deadzone, GamepadSlot gamepadSlot = GamepadSlot.All)
        {

        }

        public static bool IsGamepadConnected(GamepadSlot slot)
        {
	        return false;
        }

        public static Vector2 mousePosition { get; }
        public static Vector2 mousePresent { get; }
        public static Vector2 mouseScrollDelta { get; }
        
        /// <summary>
        /// All text input from the current frame. Do uGUI and UIToolkit handle those these days?
        /// </summary>
        public static string lastString { get; set; }


        // -------------------------------------------
        // Input action related APIs
        // -------------------------------------------
        public static bool IsActionPerforming<TValue>(Input<TValue> input) where TValue : struct
        {
	        return false;
        }

        public static bool HasActionStarted<TValue>(Input<TValue> input) where TValue : struct
        {
	        return false;
        }

        public static bool HasActionEnded<TValue>(Input<TValue> input) where TValue : struct
        {
	        return false;
        }

        /// <summary>
        /// True if the specified action is currently triggered. 
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Useful for single player games where there is no need to create a player instance. The
        /// action can be fired from any device without any need for device assignment or control
        /// scheme schenanigans.
        /// </remarks>
        public static bool IsActionPerforming(string actionName)
        {
	        return false;
        }

        /// <summary>
        /// True in the frame that an action started.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static bool HasActionStarted(string actionName)
        {
	        return false;
        }
        
        /// <summary>
        /// True in the frame that an action ended.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static bool HasActionEnded(string actionName)
        {
	        return false;
        }


        // -------------------------------------------
        // Player related APIs
        // -------------------------------------------
        // These APIs don't allow for assigning a player to multiple devices simultaneously because the intention
        // is that they are only used in two scenarios:
        //   - local multi-player games
        //   - console games where TRCs require players to be assigned to gamepads
        // The escape hatch here for more control is that the player exposes the underlying InputUser, which can
        // be used to assign as many devices as you want.
        // Question: Should we deprecate PlayerInput and PlayerInputManager now? For PlayerInputManager, we should
        // offer an alternative for easily dealing with the split screen functionality, but that functionality
        // always felt like it was in the wrong place anyway.

        /// <summary>
        /// Start listening for input from all devices and assign the given player to the next unassigned device
        /// that has input. 
        /// </summary>
        /// <remarks>
        /// On player assignment, creates a clone of the global actions and uses the functionality
        /// in InputUser to limit those actions to the assigned device.
        /// 
        /// What should happen when multiple calls to this are made sequentially? Do we queue player assignments
        /// internally? Or does each subsequent call overwrite the previous one, so only one assignment can be
        /// made at a time? Queue sounds better.
        /// </remarks>
        /// <param name="playerDeviceAssignment"></param>
        public static InputPlayer AssignNewPlayerToNextDevice(DeviceType deviceTypesFilter = DeviceType.Any)
        {
	        return new InputPlayer();
        }

        /// <summary>
        /// This overload will assign the player to the next device that triggers the specified action. The
        /// intention is to use this like a "join" action.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static InputPlayer AssignNewPlayerToNextDevice(InputAction action)
        {
	        return new InputPlayer();
        }

        public static InputPlayer AssignNewPlayerToDevice(InputDevice device)
        {
	        return new InputPlayer();
        }

        
        // --------------------------------------------
        // Event APIs
        // --------------------------------------------
        public static ref readonly InputEvent lastInput => ref s_LastInput;


        // --------------------------------------------
        // Haptics
        // --------------------------------------------
        /// <summary>
        /// Shake it!
        /// </summary>
        /// <remarks>
        /// The device represented by deviceId must implement IDualMotorRumble or IQuadMotorRumble if
        /// all four values are specified.
        /// </remarks>
        /// <param name="deviceId"></param>
        /// <param name="lowFrequencyMotor"></param>
        /// <param name="highFrequencyMotor"></param>
        /// <param name="leftTrigger"></param>
        /// <param name="rightTrigger"></param>
        public static void SetVibration(InputDevice inputDevice, float lowFrequencyMotor, float highFrequencyMotor,
	        float leftTrigger = 0, float rightTrigger = 0)
        {
        }
    }

    public struct Input<TValue> where TValue : struct
    {
	    public InputAction action => m_Action;
	    public bool isPerforming => m_Action.IsPressed();
	    public bool hasStarted => m_Action.WasPressedThisFrame();
	    public bool hasEnded => m_Action.WasReleasedThisFrame();

	    public Input(InputAction action)
	    {
		    m_Action = action;
	    }

        /// <summary>
        /// Find all interactions of type TInteraction and sets the specified parameter on them.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="expr"></param>
        /// <param name="value"></param>
        /// <remarks>
        /// This treats all bindings on the action as if they all have the interaction.
        /// </remarks>
        public void SetInteractionParameter<TInteraction, TParameter>(Expression<Func<TInteraction, TParameter>> expr, TParameter value)
        {

        }

        /// <summary>
        /// Returns the value of the first interaction of type TInteraction that exists on the bindings of this action.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public TParameter GetInteractionParameter<TInteraction, TParameter>(Expression<Func<TInteraction, TParameter>> expr)
        {
	        return default;
        }

        /// <summary>
        /// Add an interaction to all bindings on this action. For more control over what bindings the interaction gets
        /// added to, drop down to using the ApplyBindingOverride method directly.
        /// </summary>
        /// <param name="interaction"></param>
        public void AddInteraction(IInputInteraction interaction)
        {

        }

        /// <summary>
        /// Remove all interactions of type TInteraction from all bindings attached to this action.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        public void RemoveInteraction<TInteraction>() where TInteraction : IInputInteraction
        {

        }

        /// <summary>
        /// Find the binding that has the specific interaction instance and remove it.
        /// </summary>
        /// <param name="interaction"></param>
        public void RemoveInteraction(IInputInteraction interaction)
        {

        }

        public static implicit operator TValue(Input<TValue> input)
	    {
		    return input.m_Action.ReadValue<TValue>();
	    }

        public static implicit operator InputAction(Input<TValue> input)
	    {
		    return input.action;
	    }

	    private InputAction m_Action;
    }

    [Flags]
    public enum DeviceType
    {
        None = 0,
        Keyboard = 1 << 1,
        Mouse = 1 << 2,
        Gamepad = 1 << 3,
        Touch = 1 << 4,
        XR = 1 << 5,
        Joystick = 1 << 6,
        RacingWheel = 1 << 7,
        FlightStick = 1 << 8,
        ArcadeStick = 1 << 9,
        Motion = 1 << 10,
        Any = ~None
    }
    
    public class InputPlayer
    {
	    public InputPlayer()
	    {
	    }

        public int playerIndex { get; private set; }
        public bool isAssigned { get; private set; }

        // TODO: Should this have access to the cloned actions?

        /// <summary>
        /// Is the control pressed on any of the devices assigned to this player.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool IsControlPressed(Inputs input)
        {
            return true;
        }

        /// <summary>
        /// True in the frame that the input was pressed, but only on devices assigned to this player.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// </remarks>
        public bool IsControlDown(Inputs input)
        {
	        throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the input was released, but only on devices assigned to this player.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool IsControlUp(Inputs input)
        {
	        throw new NotImplementedException();
        }

        public static float GetAxis(Inputs minAxis, Inputs maxAxis)
	    {
            // find the action in the list of temporary actions or add it if it doesn't exist
		    var action = new InputAction();

		    return action.ReadValue<float>();
	    }

        public static Vector2 GetAxis(Inputs left, Inputs right, Inputs up, Inputs down)
	    {
		    return Vector2.zero;
	    }

        public static Vector2 GetAxisNormalized(Inputs left, Inputs right, Inputs up, Inputs down)
        {
	        return Vector2.zero;
        }

        /// <summary>
        /// Get the value of the specified stick on any of the gamepads attached to this player.
        /// </summary>
        /// <param name="stick"></param>
        /// <returns></returns>
        public static Vector2 GetAxis(GamepadAxis stick)
        {
	        return Vector2.zero;
        }

        // NOTE: InputPlayer doesn't have versions of IsGamepadButton... because those only exist on the
        // static Input class to allow getting gamepad input from a specific gamepad, but on the player,
        // IsControl... methods already query only the controls attached to the player, so just passing
        // a gamepad specific Inputs enum value does the same thing.
        

	    public bool IsActionPerforming<TValue>(Input<TValue> input) where TValue : struct
		{
		    return false;
	    }

	    public bool HasActionStarted<TValue>(Input<TValue> input) where TValue : struct
	    {
		    return false;
	    }

	    public bool HasActionEnded<TValue>(Input<TValue> input) where TValue : struct
	    {
		    return false;
	    }
        
	    public bool IsActionPerforming(string actionName)
	    {
		    return false;
	    }

	    public bool HasActionStarted(string actionName)
	    {
		    return false;
	    }

	    public bool HasActionEnded(string actionName)
	    {
		    return false;
	    }

        /// <summary>
        /// Unassign the player from the devices they are assigned to but leave the player instance intact.
        /// </summary>
	    public void ReleaseDevices()
	    {
		    
	    }

        /// <summary>
        /// Adds the next unassigned device to trigger the specified action to the devices assigned to the player.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="action"></param>
        /// <remarks>
        /// To know when this has completed, keep a reference to the AsyncDeviceAssignment instance and query the
        /// 'isComplete' property.
        /// Since there could be multiple requests made simultaneously for different players, these should be
        /// serviced in the order they come in.
        /// </remarks>
        public AsyncDeviceAssignmentRequest AssignToNextDevice<TValue>(Input<TValue> action) where TValue : struct
        {
	        return new AsyncDeviceAssignmentRequest();
        }

        /// <summary>
        /// Assign the player to a specific device. This is additive on top of any devices already assigned.
        /// </summary>
        /// <param name="inputDevice"></param>
        /// <remarks>
        /// What happens when the device is already assigned to someone else or doesn't exist?
        /// </remarks>
        public void AssignToDevice(InputDevice device)
        {
	        // TODO: Consider failure conditions here.
        }

        /// <summary>
        /// Limit the controls that can trigger this players' actions to those that are in the specified
        /// group.
        /// </summary>
        /// <param name="controlGroupName"></param>
        /// <remarks>
        /// Groups are just control schemes, so these still need to be created in the editor.
        /// </remarks>
        public void UseControlGroup(string controlGroupName)
        {
	        m_InputUser.ActivateControlScheme(controlGroupName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputModule"></param>
        public void AssignInputModule(InputSystemUIInputModule inputModule)
        {
	        inputModule.actionsAsset = m_Actions;
        }

        /// <summary>
        /// Vibrate all devices assigned to this player.
        /// </summary>
        /// <param name="lowFrequencyMotor"></param>
        /// <param name="highFrequencyMotor"></param>
        /// <param name="leftTrigger"></param>
        /// <param name="rightTrigger"></param>
        /// <exception cref="NotImplementedException"></exception>
	    public void SetVibration(float lowFrequencyMotor, float highFrequencyMotor,
		    float leftTrigger = 0, float rightTrigger = 0)
	    {
		    throw new NotImplementedException();
	    }

        private InputUser m_InputUser;
        private InputActionAsset m_Actions;
    }

    public struct AsyncDeviceAssignmentRequest
    {
        /// <summary>
        /// Set to true when the device is assigned.
        /// </summary>
        public bool isComplete { get; private set; }
        public PlayerInput player { get; private set; }
        public InputDevice device { get; private set; }

        public void Cancel()
        {
            // stop listening for input from unassigned devices
        }
    }
}