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
                    negativeX: Keyboard.A,
                    positiveX: Keyboard.D,
                    negativeY: Keyboard.S,
                    positiveY: Keyboard.W),
                Gamepad.leftStick.Deadzone());
        }

        /// <summary>
        /// Expresses intent to make a relative change to look orientation.
        /// </summary>
        /// <returns>Binding producing a normalized relative vector in 2D space.</returns>
        [InputPreset(category: Category)]
        public static Merge<Vector2> Look()
        {
            return Combine.Merge(Mouse.any.delta,       // TODO Ideally this should be compilation error, should have Sum()
                Gamepad.RightStick.Deadzone());
        }

        /// <summary>
        /// Expresses whether the user is intending to fire the weapon (weapon trigger held).
        /// </summary>
        /// <returns>Observable boolean condition.</returns>
        [InputPreset(category: Category, displayName: "Fire Weapon")]
        public static /*Merge<bool>*/ IObservableInput<bool> FireWeapon()
        {
            return Keyboard.LeftCtrl;
            /*return Combine.Merge<bool>(Mouse.any.buttons[0],
                Gamepad.RightTrigger.GreaterThan(0.5f));*/
        }

        /// <summary>
        /// Expresses whether the user intend to switch weapon.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category, displayName: "Switch Weapon")]
        public static Merge<InputEvent> SwitchWeapon()
        {
            return Combine.Merge(Keyboard.Q.Pressed(), 
                Gamepad.RightShoulder.Pressed());
        }

        [InputPreset(category: Category, displayName: "Weapon Mod")]
        public static Merge<InputEvent> WeaponMod()
        {
            return Combine.Merge(Mouse.any.buttons[0].Pressed(), 
                Gamepad.LeftTrigger.AsButton().Pressed());
        }
        
        [InputPreset(category: Category, displayName: "Switch Weapon Mod")]
        public static Merge<InputEvent> SwitchWeaponMod()
        {
            return Combine.Merge(Keyboard.F.Pressed(), Gamepad.Up.Pressed());
        }

        /// <summary>
        /// Expresses an intent to show the weapon wheel for quick weapon selection.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category, displayName: "Weapon Wheel")]
        public static Merge<InputEvent> WeaponWheel()
        {
            return Combine.Merge(Keyboard.Q.Held(), Gamepad.RightShoulder.Held());
        }

        /// <summary>
        /// Expresses an intent to jump.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Jump()
        {
            return Combine.Merge(Keyboard.Space.Pressed(), Gamepad.ButtonSouth.Pressed());
        }

        /// <summary>
        /// Expresses an intent to dash.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Dash()
        {
            return Combine.Merge(Keyboard.LeftShift.Pressed(), Gamepad.ButtonEast.Pressed());
        }

        /// <summary>
        /// Expresses the intent to perform a melee attack.
        /// </summary>
        /// <returns>Observable event.</returns>
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Melee()
        {
            return Combine.Merge(Keyboard.E.Pressed(), Gamepad.RightStickHat.Pressed());
        }
        
        [InputPreset(category: Category)]
        public static Merge<InputEvent> Equipment()
        {
            return Combine.Merge(Keyboard.LeftCtrl.Pressed(), Gamepad.LeftShoulder.Pressed());
        }
        
        [InputPreset(category: Category, displayName: "Switch Equipment")]
        public static Merge<InputEvent> SwitchEquipment()
        {
            return Combine.Merge(Keyboard.G.Pressed(), Gamepad.Left.Pressed());
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
            return Keyboard.Digit1.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon2()
        {
            return Keyboard.Digit2.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon3()
        {
            return Keyboard.Digit3.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon4()
        {
            return Keyboard.Digit4.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon5()
        {
            return Keyboard.Digit5.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon6()
        {
            return Keyboard.Digit6.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon7()
        {
            return Keyboard.Digit7.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Weapon8()
        {
            return Keyboard.Digit8.Pressed();
        }
        
        [InputPreset(category: Category)]
        public static IObservableInput<InputEvent> Inventory()
        {
            return Keyboard.Tab.Pressed();
        }
        
        [InputPreset(category: Category, displayName: "Voice Chat")]
        public static IObservableInput<InputEvent> VoiceChat()
        {
            return Keyboard.B.Pressed();
        }
        
        [InputPreset(category: Category, displayName: "Mission Information")]
        public static IObservableInput<InputEvent> MissionInformation()
        {
            return Keyboard.LeftAlt.Pressed();
        }
    }
}
