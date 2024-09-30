namespace UnityEngine.InputSystem.Experimental
{
    public static class Platformer2D
    {
        private const string Category = "2D Platformer";
        
        [InputPreset(category: Category)]
        public static Merge<Vector2> Move()
        {
            return Combine.Merge(Combine.Composite(
                    negativeX: Keyboard.LeftArrow,
                    positiveX: Keyboard.RightArrow,
                    negativeY: Keyboard.DownArrow,
                    positiveY: Keyboard.UpArrow),
                Gamepad.leftStick.Deadzone()); // TODO Add Gamepad DPad composite
        }
        
        // TODO Run: Right trigger, left shift
        // TODO Jump: Button south, space
        // TODO Hit: Button west, S
        // TODO Back: Button east, backspace
        // TODO Pause: Start, ESC
    }
}