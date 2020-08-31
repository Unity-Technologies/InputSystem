using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputLegacy
{
    public static class OldInputCompatibilityManager
    {
        /*
        // TODO remove
        private static IDictionary<string, string> remapDict = new Dictionary<string, string>
        {
            {"up", "<Keyboard>/upArrow"},
            {"down", "<Keyboard>/downArrow"},
            {"left", "<Keyboard>/leftArrow"},
            {"right", "<Keyboard>/rightArrow"}
        };

        private static IDictionary<string, ctionWrapper> axes = new Dictionary<string, ActionWrapper>();

        private static string RemapButtons(string name)
        {
            if (name.Length == 0)
                return null;

            if (name.StartsWith("joystick"))
            {
                // "joystick 1 button 0" format

                var parts = name.Split(' ');
                if (parts.Length < 3 || parts[0] != "joystick" || parts[2] != "button")
                    return null;

                var joyNum = Int32.Parse(parts[1]);
                var button = Int32.Parse(parts[3]);

                // a very rough mapping based on http://wiki.unity3d.com/index.php?title=Xbox360Controller
                // TODO where joyNum goes?
                switch (button)
                {
                    case 0: return $"<Gamepad>/buttonSouth";
                    case 1: return $"<Gamepad>/buttonEast";
                    case 2: return $"<Gamepad>/buttonWest";
                    case 3: return $"<Gamepad>/buttonNorth";
                }

                throw new NotImplementedException($"not supported joystick '{name}'");
            }
            else if (remapDict.TryGetValue(name, out string remap))
                return remap;
            else
                return $"<Keyboard>/{name}";
        }

        private static void ConsumeInputManagerAxisSettings(SerializedProperty p)
        {
            if (p == null)
                return;

            // foreach (SerializedProperty b in p)
            //     Debug.Log($"type={b.propertyType}, name={b.name}");
            // Debug.Log("----");
            // return;

            var name = p.FindPropertyRelative("m_Name").stringValue;

            var mappedButtons = new List<(string axisDirection, string propertyName)>
                {
                    ("Positive", "positiveButton"),
                    ("Negative", "negativeButton"),
                    ("Positive", "altPositiveButton"),
                    ("Negative", "altNegativeButton")
                }
                .Select(t => (t.axisDirection,
                    buttonBinding: RemapButtons(p.FindPropertyRelative(t.propertyName).stringValue)))
                .ToArray();

            var axisType = p.FindPropertyRelative("type").enumValueIndex;
            var axisValue = p.FindPropertyRelative("axis").enumValueIndex;
            var joyNum = p.FindPropertyRelative("joyNum").enumValueIndex;

            ActionWrapper wrap = null;
            if (!axes.TryGetValue(name, out wrap))
            {
                wrap = new ActionWrapper(name);
                axes[name] = wrap;
                Debug.Log($"add action {name}");
            }

            switch (axisType)
            {
                case 0: // button
                    if (mappedButtons.Any())
                    {
                        var binding = wrap.action.AddCompositeBinding("Axis");
                        foreach (var mappedButton in mappedButtons)
                            binding = binding.With(mappedButton.axisDirection, mappedButton.buttonBinding);
                    }

                    break;
                case 1: // mouse
                    //throw new NotImplementedException("Mouse axes are not supported");
                    break;
                case 2: // joystick
                    Debug.Log($"joystick {joyNum} axis {axisValue}");
                    // TODO completely not clear how to combine/split two axes with a 2d controller?

                    switch (axisValue)
                    {
                        case 0:
                            wrap.action.AddBinding("<Gamepad>/leftStick/x");
                            break;
                        case 1:
                            wrap.action.AddBinding("<Gamepad>/leftStick/y");
                            break;
                        case 2:
                            wrap.action.AddBinding("<Gamepad>/rightStick/x");
                            break;
                        case 3:
                            wrap.action.AddBinding("<Gamepad>/rightStick/y");
                            break;
                    }

                    break;
            }

            // * m_Name
            // * negativeButton
            // * positiveButton
            // * altNegativeButton
            // * altPositiveButton
            // * gravity
            // * dead
            // * sensitivity
            // * snap
            // * invert
            // * type
            // * axis
            // * joyNum

            // enum AxisType
            // {
            //     kAxisButton,
            //     kAxisMouse,
            //     kAxisJoystick,
            // };
        }
        */

        /*
        private static InputActionMap s_Actions;
        private static IDictionary<string, ActionStateListener> s_ActionStateListeners; // TODO remove me
        private static ActionStateListener[] s_KeyActions;
        */

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunInitializeInPlayer()
        {
            BootstrapInputConfiguration();
        }

        public static void BootstrapInputConfiguration()
        {
            /*
            s_Actions = new InputActionMap("InputManagerLegacy");
            s_ActionStateListeners = new Dictionary<string, ActionStateListener>();
            s_KeyActions = new ActionStateListener[(int) KeyCode.Joystick8Button19 + 1];
            */

            // // emulate any keyboard key
            // var anyKeyAction =
            //     s_Actions.AddAction(ActionNameMapper.GetKeyboardAnyKeyActionName(), InputActionType.Button);
            // anyKeyAction.AddBinding("<Keyboard>/anyKey");
            // var anyKeyActionListener = new ActionStateListener(anyKeyAction);
            //
            // // emulate all keys
            // foreach (var keyCode in (KeyCode[]) Enum.GetValues(typeof(KeyCode)))
            // {
            //     var actionName = ActionNameMapper.GetKeyboardActionNameForKeyCode(keyCode);
            //     var action = s_Actions.FindAction(actionName);
            //     var controlPaths = ControlPathMapper.GetAllControlPathsForKeyCode(keyCode, null);
            //
            //     if (controlPaths.Length == 0)
            //         continue;
            //
            //     if (action == null)
            //     {
            //         action = s_Actions.AddAction(actionName, InputActionType.Button);
            //         s_KeyActions[(int) keyCode] =
            //             s_ActionStateListeners[actionName] = new ActionStateListener(action);
            //     }
            //
            //     foreach (var controlPath in controlPaths)
            //         action.AddBinding(controlPath);
            // }

            // mouse position is emulated via accessing Mouse.current directly
            // TODO reevaluate if mouse position should also use actions

            // foreach (var axis in InputManagerConfiguration.GetCurrent())
            // {
            //     var actionName = ActionNameMapper.GetAxisActionNameFromAxisName(axis.name);
            //
            //     // Add action, if we haven't already.
            //     ////REVIEW: Unlike the old input manager, FindAction is case-insensitive. Might be undesirable here.
            //     var action = s_Actions.FindAction(actionName);
            //     if (action == null)
            //     {
            //         // All InputManager axis are float values. We don't really know from the configuration
            //         // what's considered a button and was is considered just an axis.
            //         action = s_Actions.AddAction(actionName, InputActionType.Value);
            //         action.expectedControlType = "Axis";
            //
            //         s_ActionStateListeners[actionName] = new ActionStateListener(action);
            //     }
            //
            //     switch (axis.type)
            //     {
            //         case InputManagerConfiguration.AxisType.Button:
            //             AddButtonBindings(action, axis);
            //             break;
            //
            //         case InputManagerConfiguration.AxisType.Mouse:
            //             ////TODO
            //             break;
            //
            //         case InputManagerConfiguration.AxisType.Joystick:
            //             ////TODO
            //             break;
            //     }
            // }

            DeviceMonitor.Enable(ApiShimDataProvider.OnTextChange, ApiShimDataProvider.OnDeviceChange);
            InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            Input.provider = new ApiShimDataProvider(
/*
                s_Actions,
                s_ActionStateListeners,
                s_KeyActions
*/
            );
        }

        /*

        private static void AddButtonBindings(InputAction action, InputManagerConfiguration.Axis axis)
        {
            ////TODO: add support for having only negativeButton/altNegativeButton (i.e. [-1..0])

            ////REVIEW: These probably don't apply to buttons?
            var processors = StringHelpers.Join(new[]
            {
                axis.invert ? "invert" : null,
                !Mathf.Approximately(axis.sensitivity, 0) ? $"scale(factor={axis.sensitivity})" : null,
                ////TODO: snap
                ////TODO: gravity
            }, ",");

            var positiveControlPaths =
                ControlPathMapper.GetAllControlPathsForKeyName(axis.positiveButton, null);
            var altPositiveControlPaths =
                ControlPathMapper.GetAllControlPathsForKeyName(axis.altPositiveButton, null);
            var negativeControlPaths =
                ControlPathMapper.GetAllControlPathsForKeyName(axis.negativeButton, null);
            var altNegativeControlPaths =
                ControlPathMapper.GetAllControlPathsForKeyName(axis.altNegativeButton, null);

            AddButtonBindingsWithDirection(action, "Positive",
                positiveControlPaths.Concat(altPositiveControlPaths).ToArray(), processors);
            AddButtonBindingsWithDirection(action, "Negative",
                negativeControlPaths.Concat(altNegativeControlPaths).ToArray(), processors);
        }

        private static void AddButtonBindingsWithDirection(InputAction action, string direction, string[] controlPaths,
            string processors)
        {
            if (controlPaths.Length == 0)
                return;

            var binding = action.AddCompositeBinding("Axis");
            foreach (var controlPath in controlPaths)
            {
                Debug.Log($"{action.name} -> {controlPath}");
                binding = binding.With(direction, controlPath, processors: processors);
            }
        }

        public static void Enable()
        {
            s_Actions.Enable();
        }

        public static void Disable()
        {
            s_Actions.Disable();
        }

        */
    }
}