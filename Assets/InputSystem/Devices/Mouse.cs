using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // Combine a single pointer with buttons.
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('M', 'O', 'U', 'S');

        public PointerState pointer;

        [InputControl(template = "Vector2")]
        [InputControl(name = "scroll/x", aliases = new[] { "horizontal" }, usage = "ScrollHorizontal")]
        [InputControl(name = "scroll/y", aliases = new[] { "vertical" }, usage = "ScrollVertical")]
        public Vector2 scroll;

        [InputControl(name = "leftButton", template = "Button", bit = (int)Button.Left, usages = new string[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "rightButton", template = "Button", bit = (int)Button.Right, usages = new string[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "middleButton", template = "Button", bit = (int)Button.Middle)]
        [InputControl(name = "forwardButton", template = "Button", bit = (int)Button.Forward, usage = "Forward")]
        [InputControl(name = "backButton", template = "Button", bit = (int)Button.Backward, usage = "Back")]
        public int buttons;

        public enum Button
        {
            Left,
            Right,
            Middle,
            Forward,
            Backward
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(MouseState))]
    public class Mouse : Pointer
    {
        public new static Mouse current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
