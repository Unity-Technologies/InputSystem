using UnityEngine.InputSystem.Experimental.Devices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A class providing cross-platform first-person-shooter (FPS) input binding presets inspired by popular games in
    /// the genre.
    /// </summary>
    public static class FirstPersonShooter
    {
        private const string Category = "First-Person Shooter (FPS)";

        /// <summary>
        /// Expresses intent to move.
        /// </summary>
        /// <returns>Binding producing a normalized unit vector in 2D space.</returns>
        [InputPreset(category: Category)]
        public static Merge<Vector2> Move()
        {
            return Combine.Merge(Combine.Composite(
                    negativeX: Devices.Keyboard.A,
                    positiveX: Devices.Keyboard.D,
                    negativeY: Devices.Keyboard.S,
                    positiveY: Devices.Keyboard.W),
                Devices.Gamepad.leftStick.Deadzone());
        }

        /// <summary>
        /// Expresses intent to make a relative change to look orientation.
        /// </summary>
        /// <returns>Binding producing a normalized relative vector in 2D space.</returns>
        [InputPreset(category: Category)]
        public static Merge<Vector2> Look()
        {
            return Combine.Merge(Devices.Mouse.any.delta,       // TODO Ideally this should be compilation error, should have Sum()
                Devices.Gamepad.RightStick.Deadzone());
        }

        /// <summary>
        /// Expresses whether the user is intending to fire the weapon (weapon trigger held).
        /// </summary>
        /// <returns>Observable boolean condition.</returns>
        [InputPreset(category: Category, displayName: "Fire Weapon")]
        public static Merge<bool> FireWeapon()
        {
            return Combine.Merge<bool>(Devices.Mouse.any.buttons[0], 
                Devices.Gamepad.RightTrigger.GreaterThan(0.5f));
        }

        /// <summary>
        /// Expresses whether the user intend to switch weapon.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category, displayName: "Switch Weapon")]
        public static Merge<InputEvent> SwitchWeapon()
        {
            return Combine.Merge(Devices.Keyboard.Q.Pressed(), 
                Devices.Gamepad.RightShoulder.Pressed());
        }

        [InputPreset(category: Category, displayName: "Weapon Mod")]
        public static Merge<InputEvent> WeaponMod()
        {
            return Combine.Merge(Devices.Mouse.any.buttons[0].Pressed(), 
                Devices.Gamepad.LeftTrigger.AsButton().Pressed());
        }
        
        [InputPreset(category: Category, displayName: "Switch Weapon Mod")]
        public static Merge<InputEvent> SwitchWeaponMod()
        {
            return Combine.Merge(Devices.Keyboard.F.Pressed(), Devices.Gamepad.Up.Pressed());
        }

        /// <summary>
        /// Expresses an intent to show the weapon wheel for quick weapon selection.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category, displayName: "Weapon Wheel")]
        public static Merge<InputEvent> WeaponWheel()
        {
            return Combine.Merge(Devices.Keyboard.Q.Held(), Devices.Gamepad.RightShoulder.Held());
        }

        /// <summary>
        /// Expresses an intent to jump.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Jump()
        {
            return Combine.Merge(Devices.Keyboard.Space.Pressed(), Devices.Gamepad.ButtonSouth.Pressed());
        }

        /// <summary>
        /// Expresses an intent to dash.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Dash()
        {
            return Combine.Merge(Devices.Keyboard.LeftShift.Pressed(), Devices.Gamepad.ButtonEast.Pressed());
        }

        /// <summary>
        /// Expresses the intent to perform a melee attack.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Melee()
        {
            return Combine.Merge(Devices.Keyboard.E.Pressed(), Devices.Gamepad.RightStickHat.Pressed());
        }
        
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Equipment()
        {
            return Combine.Merge(Devices.Keyboard.LeftCtrl.Pressed(), Devices.Gamepad.LeftShoulder.Pressed());
        }
        
        [InputPreset(category: Category, displayName: "Switch Equipment")]
        public static Merge<InputEvent> SwitchEquipment()
        {
            return Combine.Merge(Devices.Keyboard.G.Pressed(), Devices.Gamepad.Left.Pressed());
        }
        
        [InputPreset(category: Category, displayName: "Next Weapon")]
        public static IObservableInput<InputEvent> NextWeapon()
        {
            return null; // TODO Scroll wheel up
        }
        
        [InputPreset(category: Category, displayName: "Previous Weapon")]
        public static IObservableInput<InputEvent> PreviousWeapon()
        {
            return null; // TODO Scroll wheel up
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon1()
        {
            return Devices.Keyboard.Digit1.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon2()
        {
            return Devices.Keyboard.Digit2.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon3()
        {
            return Devices.Keyboard.Digit3.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon4()
        {
            return Devices.Keyboard.Digit4.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon5()
        {
            return Devices.Keyboard.Digit5.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon6()
        {
            return Devices.Keyboard.Digit6.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon7()
        {
            return Devices.Keyboard.Digit7.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon8()
        {
            return Devices.Keyboard.Digit8.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Inventory()
        {
            return Devices.Keyboard.Tab.Pressed();
        }
        
        [InputPreset(category: Category, displayName: "Voice Chat")]
        public static IObservableInput<InputEvent> VoiceChat()
        {
            return Devices.Keyboard.B.Pressed();
        }
        
        [InputPreset(category: Category, displayName: "Mission Information")]
        public static IObservableInput<InputEvent> MissionInformation()
        {
            return Devices.Keyboard.LeftAlt.Pressed();
        }
    }
}
