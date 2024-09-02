// IMPORTANT: Auto-generated via code generator based off native headers and would sit in module since paired
//            with definitions from native code.
//
// NOTE:      Currently this has been hand-crafted for the sake of proof-of-concept purposes.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class GamepadUsages
        {
            public static readonly Usage LeftStick = new(436321321);
            public static readonly Usage RightStick = new(3213574);
            
            public static readonly Usage LeftTrigger = new(699321321);
            public static readonly Usage RightTrigger = new(921359974);
            
            public static readonly Usage ButtonEast = new(64155486);
            public static readonly Usage ButtonSouth = new(2313185468);
            public static readonly Usage ButtonWest = new(12312312);
            public static readonly Usage ButtonNorth = new(2123123468);
            public static readonly Usage Select = new(852255456);
            public static readonly Usage Start = new(678345453);
            public static readonly Usage Up = new(674442313);
            public static readonly Usage Down = new(91911556);
            public static readonly Usage Left = new(8217333);
            public static readonly Usage Right = new(9511593);
            public static readonly Usage LeftShoulder = new(9511593);
            public static readonly Usage RightShoulder = new(9511593);
            public static readonly Usage RumbleHaptic = new(2521315);
        }
    }

    public class ButtonAttribute : Attribute
    {
        // TODO Might need absolute/relative flags?
        // TODO Might need on/off values?

        public Type enumType { get; set; }
    }

    public class AxisAttribute : Attribute
    {
        
    }
    
    public class Axis2Attribute : Attribute
    {
        
    }

    public class NormalizedValueAttribute : Attribute
    {
        
    }
    
    // NOTE: Auto-generated from C struct definition. Aliased controls are basically C# counterpart of union.
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack=1)]
    public struct GamepadState
    {
        public enum GamepadButtonBitShift : int
        {
            South = 0,
            West = 1,
            East = 2,
            North = 3,
            LeftPaddle = 4,
            RightPaddle = 5,
            LeftStickHat = 6,
            RightStickHat = 7,
            LeftShoulder = 8,
            RightShoulder = 9,
            Start = 10,
            Select = 11
        }
        
        // NOTE: Auto-generated from C enum with attribute type constraint since exported type depends on it.
        [Flags]
        [Serializable]
        public enum GamepadButton : uint
        {
            None = 0,                                                  // 0
            South = 1 << GamepadButtonBitShift.South,                  // 1
            West = 1 << GamepadButtonBitShift.West,                    // 2
            East = 1 << GamepadButtonBitShift.East,                    // 4
            North = 1 << GamepadButtonBitShift.North,                  // 8
            LeftPaddle = 1 << GamepadButtonBitShift.LeftPaddle,        // 16
            RightPaddle = 1 << GamepadButtonBitShift.RightPaddle,      // 32
            LeftStickHat = 1 << GamepadButtonBitShift.LeftStickHat,    // 64
            RightStickHat = 1 << GamepadButtonBitShift.RightStickHat,  // 128
            LeftShoulder = 1 << GamepadButtonBitShift.LeftShoulder,    // 256
            RightShoulder = 1 << GamepadButtonBitShift.RightShoulder,  // 512
            Start = 1 << GamepadButtonBitShift.Start,                  // 1024
            Select = 1 << GamepadButtonBitShift.Select,                // 2048
        }
        
        [Button] [FieldOffset(0)] public GamepadButton buttons;        // Byte 0-3
        [Axis] [FieldOffset(4)] public float leftStickX;               // Byte 4-5
        [Axis] [FieldOffset(6)] public float leftStickY;               // Byte 6-7
        [Axis2] [FieldOffset(4)] public Vector2 leftStick;             // Byte 4-7 (Aliased)
        [Axis] [FieldOffset(8)] public float rightStickX;              // Byte 8-9
        [Axis] [FieldOffset(10)] public float rightStickY;             // Byte 10-11
        [Axis2] [FieldOffset(8)] public Vector2 rightStick;            // Byte 8-11 (Aliased)
        [NormalizedValue] [FieldOffset(12)] public float leftTrigger;  // Byte 12-13
        [NormalizedValue] [FieldOffset(14)] public float rightTrigger; // Byte 14-15

        // Convenience accessors for individual buttons for this device model generated based on enum being
        // flagged for bit-flag access.
        public bool buttonSouth
        {
            get => 0 != (buttons & GamepadButton.South);
            set
            {
                if (value)
                    buttons |= GamepadButton.South;
                else
                    buttons &= ~GamepadButton.South;
            } 
        }

        public bool buttonWest
        {
            get => 0 != (buttons & GamepadButton.West);
            set
            {
                if (value)
                    buttons |= GamepadButton.West;
                else
                    buttons &= ~GamepadButton.West;
            }
        }
        
        public bool buttonEast 
        {
            get => 0 != (buttons & GamepadButton.East);
            set
            {
                if (value)
                    buttons |= GamepadButton.East;
                else
                    buttons &= ~GamepadButton.East;
            }
        }

        public bool buttonNorth 
        {
            get => 0 != (buttons & GamepadButton.North);
            set
            {
                if (value)
                    buttons |= GamepadButton.North;
                else
                    buttons &= ~GamepadButton.North;
            }
        }
        
        public bool leftPaddle 
        {
            get => 0 != (buttons & GamepadButton.North);
            set
            {
                if (value)
                    buttons |= GamepadButton.North;
                else
                    buttons &= ~GamepadButton.North;
            }
        }
        
        public bool rightPaddle 
        {
            get => 0 != (buttons & GamepadButton.RightPaddle);
            set
            {
                if (value)
                    buttons |= GamepadButton.RightPaddle;
                else
                    buttons &= ~GamepadButton.RightPaddle;
            }
        }
        
        public bool leftStickHat 
        {
            get => 0 != (buttons & GamepadButton.LeftStickHat);
            set
            {
                if (value)
                    buttons |= GamepadButton.LeftStickHat;
                else
                    buttons &= ~GamepadButton.LeftStickHat;
            }
        }
        
        public bool rightStickHat 
        {
            get => 0 != (buttons & GamepadButton.RightStickHat);
            set
            {
                if (value)
                    buttons |= GamepadButton.RightStickHat;
                else
                    buttons &= ~GamepadButton.RightStickHat;
            }
        }
        
        // Convenience accessors for value type fields
    }
    
    // TODO Should we skip doing this and let Roslyn generate it for us?

    /*struct IndexedDevice<T>
    {
        private readonly ushort m_DeviceID;
        
        public T this[int instance]
        {
            get
            {
                
            }
        }
    }*/

    // TODO We should generate this from the state definition
    /// <summary>
    /// Represents the binding surface of a standard-model Gamepad.
    /// </summary>
    /// <remarks>
    /// Auto-generated from native code standard model usage definitions. This replaced current binding syntax, e.g.
    /// "Gamepad/leftStick".
    /// </remarks>
    [InputSource]
    public readonly struct Gamepad // TODO A gamepad instance itself should also be an observable input node
    {
        // Gamepad.LeftStick.Subscribe(...)
        
        public static Gamepad any => new Gamepad(); // TODO Should this be an observable current gamepad device?

        /// <summary>
        /// Returns all currently connected <c>Gamepad</c> devices on the system.
        /// </summary>
        /// <example>
        /// <code>
        /// Gamepad.devices[0].buttons.south;
        /// </code>
        /// </example>
        public static ReadOnlySpan<Gamepad> devices => GetDevices(Context.instance);
        
        /// <summary>
        /// Returns all currently connected <c>Gamepad</c> devices on the system for the given context.
        /// </summary>
        /// <param name="context">The context for which to retrieve devices.</param>
        /// <returns>ReadOnlySpan&lt;Gamepad&gt; containing all available devices.</returns>
        public static ReadOnlySpan<Gamepad> GetDevices(Context context) => context.GetDevices<Gamepad>(); // TODO Consider a DeviceCollection<Gamepad> capable of also indexing on ID etc.
        
        //private Stream<GamepadState> m_Stream;
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations
        
        // TODO Consider getting rid of displayName
        
        //public readonly ObservableInput<Vector2> LeftStick = new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");
        
        private static readonly ObservableInputNode<bool>[] Buttons = 
        {
            new (Usages.GamepadUsages.ButtonSouth, "Gamepad.ButtonSouth"), 
            new(Usages.GamepadUsages.ButtonEast, "Gamepad.ButtonEast"),
            new(Usages.GamepadUsages.ButtonWest, "Gamepad.ButtonWest"),
            new(Usages.GamepadUsages.ButtonNorth, "Gamepad.ButtonNorth"),
            new(Usages.GamepadUsages.LeftShoulder, "Gamepad.LeftShoulder"),
            new(Usages.GamepadUsages.RightShoulder, "Gamepad.RightShoulder"),
            new(Usages.GamepadUsages.Select, "Gamepad.Select"),
            new(Usages.GamepadUsages.Start, "Gamepad.Start"),
            new(Usages.GamepadUsages.Up, "Gamepad.Up"),
            new(Usages.GamepadUsages.Left, "Gamepad.Left"),
            new(Usages.GamepadUsages.Right, "Gamepad.Right"),
            new(Usages.GamepadUsages.Down, "Gamepad.Down")
        };
        private static readonly ObservableInputNode<Vector2>[] Sticks = 
        {
            new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick"),
            new(Usages.GamepadUsages.RightStick, "Gamepad.RightStick")
        };

        public static ReadOnlySpan<ObservableInputNode<bool>> buttons => // TODO Return type would actually be a custom collection type
            new ReadOnlySpan<ObservableInputNode<bool>>(Buttons);
        public static ReadOnlySpan<ObservableInputNode<Vector2>> sticks =>
            new ReadOnlySpan<ObservableInputNode<Vector2>>(Sticks);
        
        public static ObservableInputNode<Vector2> leftStick => Sticks[0];
        public static readonly ObservableInputNode<Vector2> RightStick = Sticks[1];
        
        public static readonly ObservableInputNode<float> LeftTrigger = new(Usages.GamepadUsages.LeftTrigger, "Gamepad.LeftTrigger");
        public static readonly ObservableInputNode<float> RightTrigger = new(Usages.GamepadUsages.RightTrigger, "Gamepad.RightTrigger");
        
        public static readonly ObservableInputNode<bool> ButtonSouth = Buttons[0];
        public static readonly ObservableInputNode<bool> ButtonEast = Buttons[1];
        public static readonly ObservableInputNode<bool> ButtonNorth = Buttons[2];
        public static readonly ObservableInputNode<bool> ButtonWest = Buttons[3];
        public static readonly ObservableInputNode<bool> LeftShoulder = Buttons[4];
        public static readonly ObservableInputNode<bool> RightShoulder = Buttons[5];
        public static readonly ObservableInputNode<bool> Select = Buttons[6];
        public static readonly ObservableInputNode<bool> Start = Buttons[7];
        public static readonly ObservableInputNode<bool> Up = Buttons[8];
        public static readonly ObservableInputNode<bool> Left = Buttons[9];
        public static readonly ObservableInputNode<bool> Right = Buttons[10];
        public static readonly ObservableInputNode<bool> Down = Buttons[11];
        
        public static OutputBindingTarget<float> RumbleHaptic = new(Usages.GamepadUsages.RumbleHaptic); // TODO Move to HapticDevice
    }
}
