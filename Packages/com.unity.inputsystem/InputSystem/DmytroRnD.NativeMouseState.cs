using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.DmytroRnD
{
    [StructLayout(LayoutKind.Explicit, Size = 30)]
    internal struct NativeMouseState
    {
        //[InputControl(usage = "Point")]
        [FieldOffset(0)]
        public Vector2 Position;

        //[InputControl(usage = "Secondary2DMotion")]
        [FieldOffset(8)]
        public Vector2 Delta;

        //[InputControl(displayName = "Scroll")]
        //[InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal", displayName = "Left/Right")]
        //[InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical", displayName = "Up/Down", shortDisplayName = "Wheel")]
        [FieldOffset(16)]
        public Vector2 Scroll;

        //[InputControl(name = "press", useStateFrom = "leftButton", synthetic = true, usages = new string[0])]
        //[InputControl(name = "leftButton", layout = "Button", bit = (int)MouseButton.Left, usage = "PrimaryAction", displayName = "Left Button", shortDisplayName = "LMB")]
        //[InputControl(name = "rightButton", layout = "Button", bit = (int)MouseButton.Right, usage = "SecondaryAction", displayName = "Right Button", shortDisplayName = "RMB")]
        //[InputControl(name = "middleButton", layout = "Button", bit = (int)MouseButton.Middle, displayName = "Middle Button", shortDisplayName = "MMB")]
        //[InputControl(name = "forwardButton", layout = "Button", bit = (int)MouseButton.Forward, usage = "Forward", displayName = "Forward")]
        //[InputControl(name = "backButton", layout = "Button", bit = (int)MouseButton.Back, usage = "Back", displayName = "Back")]
        [FieldOffset(24)]
        //[InputControl(name = "pressure", layout = "Axis", usage = "Pressure", offset = InputStateBlock.AutomaticOffset, format = "FLT", sizeInBits = 32)]
        //[InputControl(name = "radius", layout = "Vector2", usage = "Radius", offset = InputStateBlock.AutomaticOffset, format = "VEC2", sizeInBits = 64)]
        //[InputControl(name = "pointerId", layout = "Digital", format = "BIT", sizeInBits = 1, offset = InputStateBlock.AutomaticOffset)] // Will stay at 0.
        public ushort Buttons;

        [FieldOffset(26)] private ushort _displayIndex; // unused

        //[InputControl(layout = "Integer", displayName = "Click Count", synthetic = true)]
        [FieldOffset(28)] public ushort ClickCount;
    }
}