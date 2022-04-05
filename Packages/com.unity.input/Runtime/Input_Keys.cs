using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine
{
    public static partial class Input
    {
        public static bool GetKey(string key)
        {
            return GetKey(StringToKeyCode(key));
        }

        public static bool GetKey(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButton(button);
            }

            if (key.IsJoystickButton())
            {
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    for (var i = 0; i < s_JoystickCount; ++i)
                        if (s_Joysticks[i].Button(buttonIndex).IsPressed())
                            return true;
                    return false;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).IsPressed();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_PressedKeys.Contains(keyId.Value);
        }

        public static bool GetKeyDown(string key)
        {
            return GetKeyDown(StringToKeyCode(key));
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButtonDown(button);
            }

            if (key.IsJoystickButton())
            {
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    var anyWasPressed = false;
                    var noneWasAlreadyPressed = true;

                    for (var i = 0; i < s_JoystickCount; ++i)
                    {
                        var button = s_Joysticks[i].Button(buttonIndex);
                        var wasPressedThisFrame = button.WasPressedThisFrame();
                        anyWasPressed |= wasPressedThisFrame;
                        if (!wasPressedThisFrame)
                            noneWasAlreadyPressed &= !button.IsPressed();
                    }

                    return anyWasPressed && noneWasAlreadyPressed;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).WasPressedThisFrame();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_ThisFramePressedKeys.Contains(keyId.Value);
        }

        public static bool GetKeyUp(string key)
        {
            return GetKeyUp(StringToKeyCode(key));
        }

        public static bool GetKeyUp(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButtonUp(button);
            }

            if (key.IsJoystickButton())
            {
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    var anyWasReleased = false;
                    var noneIsPressed = true;

                    for (var i = 0; i < s_JoystickCount; ++i)
                    {
                        var button = s_Joysticks[i].Button(buttonIndex);
                        var wasReleasedThisFrame = button.WasReleasedThisFrame();
                        anyWasReleased |= wasReleasedThisFrame;
                        if (!wasReleasedThisFrame)
                            noneIsPressed &= !button.IsPressed();
                    }

                    return anyWasReleased && noneIsPressed;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).WasReleasedThisFrame();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_ThisFrameReleasedKeys.Contains(keyId.Value);
        }

        private static KeyCode StringToKeyCode(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            var keyCode = key.ToKeyCode();
            if (keyCode == null)
                throw new ArgumentException($"Input Key named: {key} is unknown");
            return keyCode.Value;
        }

        private struct KeySet
        {
            public int Count => m_Count;
            public Key this[int index] => (Key)m_Keys[index];

            private int m_Count;
            private byte[] m_Keys;

            public void Add(Key key)
            {
                Debug.Assert(key != Key.None, "Must not try to enter Key.None into KeySet");
                ArrayHelpers.AppendWithCapacity(ref m_Keys, ref m_Count, (byte)key);
            }

            public void Remove(Key key)
            {
                for (var i = 0; i < m_Count; ++i)
                {
                    if (m_Keys[i] == (byte)key)
                    {
                        ArrayHelpers.EraseAtByMovingTail(m_Keys, ref m_Count, i);
                        return;
                    }
                }
            }

            public bool Contains(Key key)
            {
                for (var i = 0; i < m_Count; ++i)
                    if (m_Keys[i] == (byte)key)
                        return true;
                return false;
            }

            public void Clear()
            {
                m_Count = 0;
            }
        }

        private static unsafe void SyncKeys(Keyboard keyboard)
        {
            var statePtr = (KeyboardState*)keyboard.currentStatePtr;
            MemoryHelpers.Swap(ref s_PressedKeys, ref s_PressedKeysBefore);
            s_PressedKeys.Clear();

            // Scan int by int.
            for (var i = 0; i < KeyboardState.kSizeInInts; ++i)
            {
                var n = ((int*)statePtr->keys)[i];
                if (n == 0)
                    continue;

                // Scan byte by byte.
                for (var k = 0; k < 4; ++k)
                {
                    var b = (byte)(n & 0xff);
                    n >>= 8;

                    if (b == 0)
                        continue;

                    // Scan bit by bit.
                    for (var j = 0; j < 8; ++j)
                    {
                        if ((b & (1 << j)) != 0)
                        {
                            var key = (Key)(i * 32 + k * 8 + j);
                            s_PressedKeys.Add(key);
                            if (!s_ThisFramePressedKeys.Contains(key))
                                s_ThisFramePressedKeys.Add(key);
                        }
                    }
                }
            }

            // Release keys that are no longer pressed.
            for (var i = 0; i < s_PressedKeysBefore.Count; ++i)
            {
                var key = s_PressedKeysBefore[i];
                if (!s_PressedKeys.Contains(key))
                    s_ThisFrameReleasedKeys.Add(key);
            }
        }

        private static void ReleaseKeys()
        {
            for (var i = 0; i < s_PressedKeys.Count; ++i)
            {
                var key = s_PressedKeys[i];
                if (!s_ThisFrameReleasedKeys.Contains(key))
                    s_ThisFrameReleasedKeys.Add(key);
            }

            s_PressedKeys.Clear();
        }
    }
}
