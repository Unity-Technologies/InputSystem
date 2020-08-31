using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.iOS;

namespace UnityEngine.InputLegacy
{
    internal class ApiShimDataProvider : Input.DataProvider
    {
        public static void OnTextChange(char x)
        {
            if (s_InputStringStep != InputUpdate.s_UpdateStepCount)
            {
                s_InputStringData = x.ToString();
                s_InputStringStep = InputUpdate.s_UpdateStepCount;
            }
            else
                s_InputStringData += x.ToString();
        }

        public static void OnDeviceChange()
        {
            s_KeyboardMapping = null;
        }

        // Maps Keycode+Shift+Numlock to array of button controls.
        // Mapping is layout sensitive, so should be reset every time keyboard layout changes.
        private static IDictionary<(KeyCode keyCode, bool shiftStatus, bool numlockStatus), ButtonControl[]>
            s_KeyboardMapping;

        private static string s_InputStringData = "";
        private static uint s_InputStringStep = 0;

        private static bool ResolveState(InputSystem.Controls.ButtonControl control, Request request)
        {
            if (control == null)
                return false;

            switch (request)
            {
                case Request.Pressed:
                    return control.isPressed;
                case Request.PressedThisFrame:
                    return control.wasPressedThisFrame;
                case Request.ReleasedThisFrame:
                    return control.wasReleasedThisFrame;
                default:
                    return false;
            }
        }

        // private static bool ResolveState(ActionStateListener stateListener, StateRequest request)
        // {
        //     if (stateListener == null)
        //         return false;
        //
        //     switch (request)
        //     {
        //         case StateRequest.Pressed:
        //             return stateListener.isPressed;
        //         case StateRequest.PressedThisFrame:
        //             return stateListener.action.triggered;
        //         case StateRequest.ReleasedThisFrame:
        //             return stateListener.cancelled;
        //         default:
        //             return false;
        //     }
        // }

        public override float GetAxis(string axisName)
        {
            //var actionName = ActionNameMapper.GetAxisActionNameFromAxisName(axisName);
            //return stateListeners.TryGetValue(actionName, out var listener) ? listener.action.ReadValue<float>() : 0.0f;

            // TODO
            return 0.0f;
        }

        public override bool GetButton(string axisName, Request request)
        {
            //var actionName = ActionNameMapper.GetAxisActionNameFromAxisName(axisName);
            //return stateListeners.TryGetValue(actionName, out var listener) && ResolveState(listener, stateRequest);

            // TODO
            return false;
        }

        private static ButtonControl GetMouseButtonControlForMouseButton(Mouse mouse, MouseButton mouseButton)
        {
            if (mouse == null)
                return null;
            switch (mouseButton)
            {
                case MouseButton.Left: return mouse.leftButton;
                case MouseButton.Right: return mouse.rightButton;
                case MouseButton.Middle: return mouse.middleButton;
                case MouseButton.Forward: return mouse.forwardButton;
                case MouseButton.Back: return mouse.backButton;
                default: return null;
            }
        }

        public override bool GetKey(KeyCode keyCode, Request request)
        {
            switch (keyCode)
            {
                case var keyboardKeyCode when (keyCode >= KeyCode.None && keyCode <= KeyCode.Menu):
                {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
                    if (s_KeyboardMapping == null)
                        s_KeyboardMapping = WindowsKeyboardMapping.GetMappingForCurrentLayout();

                    var shiftStatus = Keyboard.current.shiftKey.isPressed;
                    var numlockStatus = WindowsKeyboardMapping.GetNumlockState();
#endif

                    if (!s_KeyboardMapping.TryGetValue((keyboardKeyCode, shiftStatus, numlockStatus),
                        out var buttonControls))
                        return false;

                    foreach (var buttonControl in buttonControls)
                        if (ResolveState(buttonControl, request))
                            return true;

                    return false;
                }

                case var mouseKeyCode when (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6):
                {
                    var mouseButton = KeyCodeMapping.KeyCodeToMouseButton(mouseKeyCode);
                    return mouseButton.HasValue &&
                           ResolveState(GetMouseButtonControlForMouseButton(Mouse.current, mouseButton.Value), request);
                }

                /*
                case var joystickKeyCode when (keyCode >= KeyCode.JoystickButton0 && keyCode <= KeyCode.Joystick8Button19):
                {
                    var (joyNum, joystick0KeyCode) = KeyCodeMapping.KeyCodeToJoystickNumberAndJoystick0KeyCode(joystickKeyCode);
                    var gamepadButton = KeyCodeMapping.Joystick0KeyCodeToGamepadButton(joystick0KeyCode);

                    if (joyNum >= 0 && joyNum < DeviceMonitor.Joysticks.Length && gamepadButton.HasValue)
                    {
                        var gamepad = (Gamepad) DeviceMonitor.Joysticks[joyNum];
                        return gamepad != null && ResolveState(gamepad[gamepadButton.Value], request);
                    }

                    // TODO
                    return false;
                }
                */

                default:
                    return false;
            }
        }

        public override bool GetKey(string keyCodeName, Request request)
        {
            return GetKey(KeyCodeMapping.KeyNameToKeyCode(keyCodeName), request);
        }

        public override bool IsAnyKey(Request request)
        {
            return ResolveState(Keyboard.current?.anyKey, request);
        }

        public override string GetInputString()
        {
            if (s_InputStringStep == InputUpdate.s_UpdateStepCount)
                return s_InputStringData;
            return "";
        }

        public override bool IsMousePresent()
        {
            return Mouse.current != null;
        }

        public override Vector3 GetMousePosition()
        {
            // seems like Z is always 0.0f
            return Mouse.current != null
                ? new Vector3(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0.0f)
                : Vector3.zero;
        }

        public override Vector2 GetMouseScrollDelta()
        {
            return Mouse.current != null
                ? new Vector2(Mouse.current.scroll.x.ReadValue(), Mouse.current.scroll.y.ReadValue())
                : Vector2.zero;
        }

        public override Touch GetTouch(int index)
        {
            if (index >= InputSystem.EnhancedTouch.Touch.activeTouches.Count)
                return new Touch();

            var t = new Touch();
            var f = InputSystem.EnhancedTouch.Touch.activeTouches[index];
            t.fingerId = f.touchId; // ???
            t.position = f.screenPosition;
            t.rawPosition = f.screenPosition; // ???
            t.deltaPosition = f.delta;
            t.deltaTime = (float) f.time; // ???
            t.tapCount = f.tapCount;
            switch (f.phase)
            {
                case InputSystem.TouchPhase.None:
                case InputSystem.TouchPhase.Began:
                    t.phase = TouchPhase.Began;
                    break;
                case InputSystem.TouchPhase.Moved:
                    t.phase = TouchPhase.Moved;
                    break;
                case InputSystem.TouchPhase.Ended:
                    t.phase = TouchPhase.Ended;
                    break;
                case InputSystem.TouchPhase.Canceled:
                    t.phase = TouchPhase.Canceled;
                    break;
                case InputSystem.TouchPhase.Stationary:
                    t.phase = TouchPhase.Stationary;
                    break;
            }

            t.type = TouchType.Direct; // ???
            t.pressure = f.pressure;
            t.maximumPossiblePressure = 1.0f; // seems to be normalized?
            t.radius = f.radius.magnitude; // ???
            t.radiusVariance = 0.0f; // ???
            t.altitudeAngle = 0.0f; // ???
            t.azimuthAngle = 0.0f; // ???
            return t;
        }

        public override int GetTouchCount()
        {
            return InputSystem.EnhancedTouch.Touch.activeTouches.Count;
        }

        public override bool GetTouchPressureSupported()
        {
            return true; // ???
        }

        public override bool GetStylusTouchSupported()
        {
            return false;
        }

        public override bool GetTouchSupported()
        {
            return true; // ???
        }

        public override void SetMultiTouchEnabled(bool enable)
        {
        }

        public override bool GetMultiTouchEnabled()
        {
            return true; // ??
        }
    };
}