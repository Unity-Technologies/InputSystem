using System.Runtime.InteropServices;

namespace ISX
{
    // Combine a single pointer with buttons.
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseState
    {
        public static FourCC kFormat => new FourCC('M', 'O', 'U', 'S');

        public PointerState pointer;

        [InputControl(name = "Left", template = "Button", bit = (int)Button.Left, usages = new string[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "Right", template = "Button", bit = (int)Button.Right, usages = new string[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "Middle", template = "Button", bit = (int)Button.Middle)]
        [InputControl(name = "Forward", template = "Button", bit = (int)Button.Forward, usage = "Forward")]
        [InputControl(name = "Backward", template = "Button", bit = (int)Button.Backward, usage = "Back")]
        public int buttons;

        public enum Button
        {
            Left,
            Right,
            Middle,
            Forward,
            Backward
        }
    }

    [InputState(typeof(MouseState))]
    public class Mouse : Pointer
    {
        public new static Mouse current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }
}
