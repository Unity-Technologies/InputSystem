using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine
{
    public static partial class Input
    {
        public static string[] GetJoystickNames()
        {
            var result = new string[s_JoystickCount];
            for (var i = 0; i < s_JoystickCount; ++i)
                result[i] = s_Joysticks[i].name;
            return result;
        }

        /*
        public static int GetJoystickCount()
        {
            return m_JoystickNameCount;
        }

        public static string GetJoystickName(int index)
        {
            //...
        }

        public static InputDevice GetJoystick(int index)
        {
            //...
        }
        */

        public static bool IsJoystickPreconfigured(string joystickName)
        {
            return false;
        }

        private static int s_JoystickCount;
        private static JoystickInfo[] s_Joysticks;

        private struct JoystickInfo
        {
            public int index;
            public string name;
            public InputDevice device;
            public InputActionMap map;

            public bool connected => name != string.Empty;

            public void Connect(int index, InputDevice device)
            {
                name = device.displayName;
                this.device = device;
                this.index = index;

                // Tag the device such that bindings can bind to it by index.
                // NOTE: Plus 1 here because joystick #0 is not an actual device but an aggregated
                //       view of all joysticks.
                InputSystem.InputSystem.AddDeviceUsage(device, "Joystick" + (index + 1));

                if (map == null)
                    map = CreateJoystickButtonMap();

                map.devices = new[] { device };
                map.Enable();
            }

            public void Disconnect()
            {
                name = string.Empty;
                map.Disable();

                // Remove tag.
                InputSystem.InputSystem.RemoveDeviceUsage(device, "Joystick" + (index + 1));

                // We keep the device around in case we reconnect.
            }

            public InputAction Button(int index)
            {
                return map.actions[index];
            }

            private static InputActionMap CreateJoystickButtonMap()
            {
                var map = new InputActionMap();
                for (var i = 0; i < kMaxButtonsPerJoystickAsPerKeyCodeEnum; ++i)
                {
                    var code = KeyCode.JoystickButton0 + i;
                    var action = map.AddAction("JoystickButton" + i, type: InputActionType.Button);
                    foreach (var path in code.JoystickButtonToBindingPath())
                        action.AddBinding(path);
                }
                return map;
            }
        }

        private static int GetJoystickIndex(InputDevice device)
        {
            for (var i = 0; i < s_JoystickCount; ++i)
                if (s_Joysticks[i].device == device)
                    return i;
            return -1;
        }

        private static void AddJoystick(InputDevice device)
        {
            // Try to find the device in our s_Joysticks list.
            var index = GetJoystickIndex(device);
            if (index == -1)
            {
                // Not found. See if there is an empty slot in the joystick list.
                for (index = 0; index < s_JoystickCount; ++index)
                {
                    if (!s_Joysticks[index].connected)
                        break;
                }
            }

            if (index < 0 || index >= s_JoystickCount)
            {
                // No available entry. Append new joystick to end of array.
                index = ArrayHelpers.AppendWithCapacity(ref s_Joysticks, ref s_JoystickCount, default);
            }

            // Switch slot to joystick. Maybe a new connect, a reconnect, or a switch
            // to a different joystick/gamepad.
            s_Joysticks[index].Connect(index, device);
        }

        private static void RemoveJoystick(InputDevice device)
        {
            var index = GetJoystickIndex(device);
            if (index != -1)
            {
                // Mark joystick as disconnected.
                s_Joysticks[index].Disconnect();
            }
        }
    }
}
