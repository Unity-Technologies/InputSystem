using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Input state for a <see cref="RacingWheel"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code>
    /// var wheel = InputSystem.AddDevice&lt;RacingWheel&gt;();
    ///
    /// InputSystem.QueueStateEvent(wheel,
    ///     new RacingWheelState
    ///     {
    ///         gear = 2,
    ///         wheel = 0.5f, // Halfway right.
    ///         throttle = 0.25f, // Quarter pressure on gas pedal.
    ///     });
    /// </code>
    /// </example>
    /// </remarks>
    public struct RacingWheelState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('W', 'E', 'E', 'L');

        [InputControl(name = "dpad", layout = "Dpad", usage = "Hatswitch", displayName = "D-Pad", format = "BIT", sizeInBits = 4, bit = 0)]
        [InputControl(name = "dpad/up", format = "BIT", bit = (uint)RacingWheelButton.DpadUp, sizeInBits = 1)]
        [InputControl(name = "dpad/right", format = "BIT", bit = (uint)RacingWheelButton.DpadRight, sizeInBits = 1)]
        [InputControl(name = "dpad/down", format = "BIT", bit = (uint)RacingWheelButton.DpadDown, sizeInBits = 1)]
        [InputControl(name = "dpad/left", format = "BIT", bit = (uint)RacingWheelButton.DpadLeft, sizeInBits = 1)]
        [InputControl(name = "gearUp", layout = "Button", bit = (uint)RacingWheelButton.GearUp, displayName = "Next Gear")]
        [InputControl(name = "gearDown", layout = "Button", bit = (uint)RacingWheelButton.GearDown, displayName = "Previous Gear")]
        [InputControl(name = "menu", alias = "start", layout = "Button", bit = (uint)RacingWheelButton.Menu, displayName = "Menu")]
        [InputControl(name = "view", alias = "select", layout = "Button", bit = (uint)RacingWheelButton.View, displayName = "View")]
        public uint buttons;

        /// <summary>
        /// Current gear selected in gear shift.
        /// </summary>
        [InputControl(displayName = "Gear", layout = "Integer")]
        public int gear;

        /// <summary>
        /// Position of wheel in [-1,1] range. -1 is all the way left, 1 is all the way right.
        /// </summary>
        [InputControl(displayName = "Wheel", layout = "Axis")]
        public float wheel;

        /// <summary>
        /// Position of throttle/gas pedal in [0,1] range. 0 is fully depressed, 1 is pressed all the way down.
        /// </summary>
        [InputControl(displayName = "Throttle", alias = "gas", layout = "Axis")]
        public float throttle;

        /// <summary>
        /// Position of brake pedal in [0,1] range. 0 is fully depressed, 1 is pressed all the way down.
        /// </summary>
        [InputControl(displayName = "Break", layout = "Axis")]
        public float brake;

        /// <summary>
        /// Position of clutch pedal in [0,1] range. 0 is fully depressed, 1 is pressed all the way down.
        /// </summary>
        [InputControl(displayName = "Clutch", layout = "Axis")]
        public float clutch;

        /// <summary>
        /// Position of handbrake in [0,1] range. 0 is fully released, 1 is engaged at maximum.
        /// </summary>
        [InputControl(displayName = "Handbrake", layout = "Axis")]
        public float handbrake;

        public FourCC format => Format;

        /// <summary>
        /// Set the specific buttons to be pressed or unpressed.
        /// </summary>
        /// <param name="button">A racing wheel button.</param>
        /// <param name="down">Whether to set <paramref name="button"/> to be pressed or not pressed in
        /// <see cref="buttons"/>.</param>
        /// <returns>RacingWheelState with a modified <see cref="buttons"/> mask.</returns>
        public RacingWheelState WithButton(RacingWheelButton button, bool down = true)
        {
            Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
            var bit = 1U << (int)button;
            if (down)
                buttons |= bit;
            else
                buttons &= ~bit;
            return this;
        }
    }

    /// <summary>
    /// Buttons on a <see cref="RacingWheel"/>.
    /// </summary>
    public enum RacingWheelButton
    {
        /// <summary>
        /// The up button on a wheel's dpad.
        /// </summary>
        DpadUp = 0,

        /// <summary>
        /// The down button on a wheel's dpad.
        /// </summary>
        DpadDown = 1,

        /// <summary>
        /// The left button on a wheel's dpad.
        /// </summary>
        DpadLeft = 2,

        /// <summary>
        /// The right button on a wheel's dpad.
        /// </summary>
        DpadRight = 3,

        /// <summary>
        /// Button to shift up.
        /// </summary>
        GearUp = 4,

        /// <summary>
        /// Button to shift down.
        /// </summary>
        GearDown = 5,

        /// <summary>
        /// The "menu" button.
        /// </summary>
        Menu = 6,

        /// <summary>
        /// The "view" button.
        /// </summary>
        View = 7,
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Base class for racing wheels.
    /// </summary>
    [InputControlLayout(stateType = typeof(RacingWheelState), displayName = "Racing Wheel", isGenericTypeOfDevice = true)]
    public class RacingWheel : InputDevice
    {
        /// <summary>
        /// The D-Pad control on the wheel.
        /// </summary>
        public DpadControl dpad { get; protected set; }

        /// <summary>
        /// Button to shift gears up.
        /// </summary>
        public ButtonControl gearUp { get; protected set; }

        /// <summary>
        /// Button to shift gears down.
        /// </summary>
        public ButtonControl gearDown { get; protected set; }

        /// <summary>
        /// The menu/start button on the wheel.
        /// </summary>
        public ButtonControl menu { get; protected set; }

        /// <summary>
        /// The view/select button on the wheel.
        /// </summary>
        public ButtonControl view { get; protected set; }

        /// <summary>
        /// The control that represents the currently selected gear.
        /// </summary>
        public IntegerControl gear { get; protected set; }

        /// <summary>
        /// The position of the wheel in [-1,1] range. -1 is all the way left, 1 is all the way right, 0 is in neutral position.
        /// </summary>
        public AxisControl wheel { get; protected set; }

        /// <summary>
        /// The position of the gas/throttle pedal or control. In [0,1] range. 0 is fully released and 1 is pressed all the way down.
        /// </summary>
        public AxisControl throttle { get; protected set; }

        /// <summary>
        /// The position of the brake pedal or control. In [0,1] range. 0 is fully released and 1 is pressed all the way down.
        /// </summary>
        public AxisControl brake { get; protected set; }

        /// <summary>
        /// The position of the clutch pedal or control. In [0,1] range. 0 is fully released and 1 is pressed all the way down.
        /// </summary>
        public AxisControl clutch { get; protected set; }

        /// <summary>
        /// The position of the handbrake control. In [0,1] range. 0 is fully released and 1 is engaged at maximum.
        /// </summary>
        public AxisControl handbrake { get; protected set; }

        /// <summary>
        /// The currently active racing wheel or <c>null</c> if no racing wheel is present.
        /// </summary>
        public static RacingWheel current { get; private set; }

        /// <summary>
        /// Make this the <see cref="current"/> racing wheel. This happens automatically for a racing wheel
        /// that receives input.
        /// </summary>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            if (this == current)
                current = null;
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            dpad = GetChildControl<DpadControl>("dpad");
            gearUp = GetChildControl<ButtonControl>("gearUp");
            gearDown = GetChildControl<ButtonControl>("gearDown");
            menu = GetChildControl<ButtonControl>("menu");
            view = GetChildControl<ButtonControl>("view");
            gear = GetChildControl<IntegerControl>("gear");
            wheel = GetChildControl<AxisControl>("wheel");
            throttle = GetChildControl<AxisControl>("throttle");
            brake = GetChildControl<AxisControl>("brake");
            clutch = GetChildControl<AxisControl>("clutch");
            handbrake = GetChildControl<AxisControl>("handbrake");
        }
    }
}
