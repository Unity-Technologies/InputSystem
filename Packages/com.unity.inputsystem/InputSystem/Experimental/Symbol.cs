namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a symbol associated with a control usage instance.
    /// </summary>
    public struct Symbol
    {
        public Symbol(uint value)
        {
            this.value = value;
        }

        public readonly uint value;
    }

    public partial class Symbols
    {
        public readonly Symbol GamepadButtonX = new Symbol(0x00000001);
        public readonly Symbol GamepadButtonY = new Symbol(0x00000001);
        public readonly Symbol GamepadButtonA = new Symbol(0x00000001);
        public readonly Symbol GamepadButtonB = new Symbol(0x00000001);
    }
}