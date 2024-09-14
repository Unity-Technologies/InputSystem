using UnityEditor;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A class providing cross-platform first-person-shooter (FPS) input binding presets inspired by popular games in
    /// the genre.
    /// </summary>
    public static class FirstPersonShooter
    {
        /// <summary>
        /// Expresses intent to move.
        /// </summary>
        /// <returns>Binding producing a normalized unit vector in 2D space.</returns>
        public static BindableInput<Vector2> Move()
        {
            var binding = new BindableInput<Vector2>();
            binding.AddBinding(Combine.Composite(
                negativeX: Devices.Keyboard.A, 
                positiveX: Devices.Keyboard.D, 
                negativeY: Devices.Keyboard.S, 
                positiveY: Devices.Keyboard.W));
            binding.AddBinding(Devices.Gamepad.leftStick.Deadzone());
            // TODO Conditional left hand-side relative touch drag if no gamepad is present
            return binding;
        }

        /// <summary>
        /// Expresses intent to make a relative change to look orientation.
        /// </summary>
        /// <returns>Binding producing a normalized relative vector in 2D space.</returns>
        public static BindableInput<Vector2> Look()
        {
            var binding = new BindableInput<Vector2>();
            // TODO binding.AddBinding(Devices.Mouse.Delta.ScaleWith(MouseSettings.sensitivitySetting));
            // TODO binding.AddBinding(Devices.Gamepad.RightStick.Deadzone().ScaleWith(GamepadSettings.rightStickSensitivitySetting).ScaleWithDeltaTime());
            binding.AddBinding(Devices.Gamepad.RightStick.Deadzone().ScaleWithDeltaTime());
            // TODO Conditional right hand-side relative touch drag if no gamepad is present
            return binding;
        }

        /// <summary>
        /// Expresses whether the user is intending to fire the weapon (weapon trigger held).
        /// </summary>
        /// <returns>Observable boolean condition.</returns>
        public static BindableInput<bool> FireWeapon()
        {
            var binding = new BindableInput<bool>();
            // TODO binding.AddBinding(Devices.Gamepad.RightTrigger.Actuated());
            // TODO binding.AddBinding(Devices.Mouse.PrimaryButton);
            return binding;
        }

        /// <summary>
        /// Expresses whether the user intend to switch weapon.
        /// </summary>
        /// <returns>Observable event.</returns>
        public static BindableInput<InputEvent> SwitchWeapon()
        {
            var binding = new BindableInput<InputEvent>();
            // TODO binding.AddBinding(Devices.Gamepad.RightShoulder.Tap());
            return binding;
        }

        /// <summary>
        /// Expresses an intent to show the weapon wheel for quick weapon selection.
        /// </summary>
        /// <returns>Observable event.</returns>
        public static BindableInput<InputEvent> WeaponWheel()
        {
            var binding = new BindableInput<InputEvent>();
            binding.AddBinding(Devices.Keyboard.Q.Held()); // TODO This would be for a show action, its likely that we want a boolean condition here thats true once held for X seconds. Then one could use Press/Release on that.
            return binding;
        }

        /// <summary>
        /// Expresses an intent to jump.
        /// </summary>
        /// <returns>Observable event.</returns>
        public static BindableInput<InputEvent> Jump()
        {
            var binding = new BindableInput<InputEvent>();
            binding.AddBinding(Devices.Keyboard.Space.Pressed());
            binding.AddBinding(Devices.Gamepad.ButtonSouth.Pressed());
            return binding;
        }

        /// <summary>
        /// Expresses the intent to perform a melee attack.
        /// </summary>
        /// <returns>Observable event.</returns>
        public static BindableInput<InputEvent> Melee()
        {
            var binding = new BindableInput<InputEvent>();
            binding.AddBinding(Devices.Keyboard.E.Pressed());
            binding.AddBinding(Devices.Gamepad.RightStickHat.Pressed());
            return binding;
        }
    }
}
