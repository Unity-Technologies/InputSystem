using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: expose whether pen actually has eraser and which barrel buttons it has

////TODO: hook up pointerId in backend to allow identifying different pens

////REVIEW: have surface distance property to detect how far pen is when hovering?

////REVIEW: does it make sense to have orientation support for pen, too?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state layout for pen devices.
    /// </summary>
    // IMPORTANT: Must match with PenInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct PenState : IInputStateTypeInfo
    {
        /// <summary>
        /// Format code for PenState.
        /// </summary>
        /// <value>Returns "PEN ".</value>
        /// <seealso cref="InputStateBlock.format"/>
        public static FourCC Format => new FourCC('P', 'E', 'N');

        /// <summary>
        /// Current screen-space position of the pen.
        /// </summary>
        /// <value>Screen-space position.</value>
        /// <seealso cref="Pointer.position"/>
        [InputControl(usage = "Point", dontReset = true)]
        [FieldOffset(0)]
        public Vector2 position;

        /// <summary>
        /// Screen-space motion delta.
        /// </summary>
        /// <value>Screen-space motion delta.</value>
        /// <seealso cref="Pointer.delta"/>
        [InputControl(usage = "Secondary2DMotion", layout = "Delta")]
        [FieldOffset(8)]
        public Vector2 delta;

        /// <summary>
        /// The way the pen is leaned over perpendicular to the tablet surface. X goes [-1..1] left to right
        /// (with -1 and 1 being completely flush to the surface) and Y goes [-1..1] bottom to top.
        /// </summary>
        /// <value>Amount pen is leaning over.</value>
        /// <seealso cref="Pen.tilt"/>
        [InputControl(layout = "Vector2", displayName = "Tilt", usage = "Tilt")]
        [FieldOffset(16)]
        public Vector2 tilt;

        /// <summary>
        /// Pressure with which the pen is pressed against the surface. 0 is none, 1 is full pressure.
        /// </summary>
        /// <value>Pressure with which the pen is pressed.</value>
        /// <remarks>
        /// May go beyond 1 depending on pressure calibration on the system. The maximum pressure point
        /// may be set to less than the physical maximum pressure point determined by the hardware.
        /// </remarks>
        /// <seealso cref="Pointer.pressure"/>
        [InputControl(layout = "Analog", usage = "Pressure", defaultState = 0.0f)]
        [FieldOffset(24)]
        public float pressure;

        /// <summary>
        /// Amount by which the pen is rotated around itself.
        /// </summary>
        /// <value>Rotation of the pen around itself.</value>
        /// <seealso cref="Pen.twist"/>
        [InputControl(layout = "Axis", displayName = "Twist", usage = "Twist")]
        [FieldOffset(28)]
        public float twist;

        /// <summary>
        /// Button mask for which buttons on the pen are active.
        /// </summary>
        /// <value>Bitmask for buttons on the pen.</value>
        [InputControl(name = "tip", displayName = "Tip", layout = "Button", bit = (int)PenButton.Tip, usage = "PrimaryAction")]
        [InputControl(name = "press", useStateFrom = "tip", synthetic = true, usages = new string[0])]
        [InputControl(name = "eraser", displayName = "Eraser", layout = "Button", bit = (int)PenButton.Eraser)]
        [InputControl(name = "inRange", displayName = "In Range?", layout = "Button", bit = (int)PenButton.InRange, synthetic = true)]
        [InputControl(name = "barrel1", displayName = "Barrel Button #1", layout = "Button", bit = (int)PenButton.BarrelFirst, alias = "barrelFirst", usage = "SecondaryAction")]
        [InputControl(name = "barrel2", displayName = "Barrel Button #2", layout = "Button", bit = (int)PenButton.BarrelSecond, alias = "barrelSecond")]
        [InputControl(name = "barrel3", displayName = "Barrel Button #3", layout = "Button", bit = (int)PenButton.BarrelThird, alias = "barrelThird")]
        [InputControl(name = "barrel4", displayName = "Barrel Button #4", layout = "Button", bit = (int)PenButton.BarrelFourth, alias = "barrelFourth")]
        // "Park" unused controls.
        [InputControl(name = "radius", layout = "Vector2", format = "VEC2", sizeInBits = 64, usage = "Radius", offset = InputStateBlock.AutomaticOffset)]
        [InputControl(name = "pointerId", layout = "Digital", format = "UINT", sizeInBits = 32, offset = InputStateBlock.AutomaticOffset)] ////TODO: this should be used
        [FieldOffset(32)]
        public ushort buttons;

        /// <summary>
        /// The index of the display that was touched.
        /// </summary>
        [InputControl(name = "displayIndex", displayName = "Display Index", layout = "Integer")]
        [FieldOffset(34)]
        ushort displayIndex;

        /// <summary>
        /// Set or unset the bit in <see cref="buttons"/> for the given <paramref name="button"/>.
        /// </summary>
        /// <param name="button">Button whose state to set.</param>
        /// <param name="state">Whether the button is on or off.</param>
        /// <returns>Same PenState with an updated <see cref="buttons"/> mask.</returns>
        public PenState WithButton(PenButton button, bool state = true)
        {
            Debug.Assert((int)button < 16, $"Expected button < 16, so we fit into the 16 bit wide bitmask");
            var bit = 1U << (int)button;
            if (state)
                buttons |= (ushort)bit;
            else
                buttons &= (ushort)~bit;
            return this;
        }

        /// <inheritdoc />
        public FourCC format => Format;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Enumeration of buttons on a <see cref="Pen"/>.
    /// </summary>
    public enum PenButton
    {
        /// <summary>
        /// Button at the tip of a pen.
        /// </summary>
        /// <seealso cref="Pen.tip"/>
        Tip,

        /// <summary>
        /// Button located end of pen opposite to <see cref="Tip"/>.
        /// </summary>
        /// <remarks>
        /// Pens do not necessarily have an eraser. If a pen doesn't, the respective button
        /// does nothing and will always be unpressed.
        /// </remarks>
        /// <seealso cref="Pen.eraser"/>
        Eraser,

        /// <summary>
        /// First button on the side of the pen.
        /// </summary>
        /// <see cref="Pen.firstBarrelButton"/>
        BarrelFirst,

        /// <summary>
        /// Second button on the side of the pen.
        /// </summary>
        /// <seealso cref="Pen.secondBarrelButton"/>
        BarrelSecond,

        /// <summary>
        /// Artificial button that indicates whether the pen is in detection range or not.
        /// </summary>
        /// <remarks>
        /// Range detection may not be supported by a pen/tablet.
        /// </remarks>
        /// <seealso cref="Pen.inRange"/>
        InRange,

        /// <summary>
        /// Third button on the side of the pen.
        /// </summary>
        /// <seealso cref="Pen.thirdBarrelButton"/>
        BarrelThird,

        /// <summary>
        /// Fourth button on the side of the pen.
        /// </summary>
        /// <see cref="Pen.fourthBarrelButton"/>
        BarrelFourth,

        /// <summary>
        /// Synonym for <see cref="BarrelFirst"/>.
        /// </summary>
        Barrel1 = BarrelFirst,

        /// <summary>
        /// Synonym for <see cref="BarrelSecond"/>.
        /// </summary>
        Barrel2 = BarrelSecond,

        /// <summary>
        /// Synonym for <see cref="BarrelThird"/>.
        /// </summary>
        Barrel3 = BarrelThird,

        /// <summary>
        /// Synonym for <see cref="BarrelFourth"/>.
        /// </summary>
        Barrel4 = BarrelFourth,
    }

    /// <summary>
    /// Represents a pen/stylus input device.
    /// </summary>
    /// <remarks>
    /// Unlike mice but like touch, pens are absolute pointing devices moving across a fixed
    /// surface area.
    ///
    /// The <see cref="tip"/> acts as a button that is considered pressed as long as the pen is in contact with the
    /// tablet surface.
    /// </remarks>
    [InputControlLayout(stateType = typeof(PenState), isGenericTypeOfDevice = true)]
    public class Pen : Pointer
    {
        ////TODO: give the tip and eraser a very low press point
        /// <summary>
        /// The tip button of the pen.
        /// </summary>
        /// <value>Control representing the tip button.</value>
        /// <seealso cref="PenButton.Tip"/>
        public ButtonControl tip { get; protected set; }

        /// <summary>
        /// The eraser button of the pen, i.e. the button on the end opposite to the tip.
        /// </summary>
        /// <value>Control representing the eraser button.</value>
        /// <remarks>
        /// If the pen does not have an eraser button, this control will still be present
        /// but will not trigger.
        /// </remarks>
        /// <seealso cref="PenButton.Eraser"/>
        public ButtonControl eraser { get; protected set; }

        /// <summary>
        /// The button on the side of the pen barrel and located closer to the tip of the pen.
        /// </summary>
        /// <value>Control representing the first side button.</value>
        /// <remarks>
        /// If the pen does not have barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        /// <seealso cref="PenButton.BarrelFirst"/>
        public ButtonControl firstBarrelButton { get; protected set; }

        /// <summary>
        /// The button on the side of the pen barrel and located closer to the eraser end of the pen.
        /// </summary>
        /// <value>Control representing the second side button.</value>
        /// <remarks>
        /// If the pen does not have barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        /// <seealso cref="PenButton.BarrelSecond"/>
        public ButtonControl secondBarrelButton { get; protected set; }

        /// <summary>
        /// Third button the side of the pen barrel.
        /// </summary>
        /// <value>Control representing the third side button.</value>
        /// <remarks>
        /// If the pen does not have a third barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        /// <seealso cref="PenButton.BarrelThird"/>
        public ButtonControl thirdBarrelButton { get; protected set; }

        /// <summary>
        /// Fourth button the side of the pen barrel.
        /// </summary>
        /// <value>Control representing the fourth side button.</value>
        /// <remarks>
        /// If the pen does not have a fourth barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        /// <seealso cref="PenButton.BarrelFourth"/>
        public ButtonControl fourthBarrelButton { get; protected set; }

        /// <summary>
        /// Button control that indicates whether the pen is in range of the tablet surface or not.
        /// </summary>
        /// <remarks>
        /// This is a synthetic control (<see cref="InputControl.synthetic"/>).
        ///
        /// If range detection is not supported by the pen, this button will always be "pressed".
        /// </remarks>
        /// <seealso cref="PenButton.InRange"/>
        public ButtonControl inRange { get; protected set; }

        /// <summary>
        /// Orientation of the pen relative to the tablet surface, i.e. the amount by which it is leaning
        /// over along the X and Y axis.
        /// </summary>
        /// <value>Control presenting the amount the pen is leaning over.</value>
        /// <remarks>
        /// X axis goes from [-1..1] left to right with -1 and 1 meaning the pen is flush with the tablet surface. Y axis
        /// goes from [-1..1] bottom to top.
        /// </remarks>
        public Vector2Control tilt { get; protected set; }

        /// <summary>
        /// Rotation of the pointer around its own axis. 0 means the pointer is facing away from the user (12 'o clock position)
        /// and ~1 means the pointer has been rotated clockwise almost one full rotation.
        /// </summary>
        /// <value>Control representing the twist of the pen around itself.</value>
        /// <remarks>
        /// Twist is generally only supported by pens and even among pens, twist support is rare. An example product that
        /// supports twist is the Wacom Art Pen.
        ///
        /// The axis of rotation is the vector facing away from the pointer surface when the pointer is facing straight up
        /// (i.e. the surface normal of the pointer surface). When the pointer is tilted, the rotation axis is tilted along
        /// with it.
        /// </remarks>
        public AxisControl twist { get; protected set; }

        /// <summary>
        /// The pen that was active or connected last or <c>null</c> if there is no pen.
        /// </summary>
        public new static Pen current { get; internal set; }

        /// <summary>
        /// Return the given pen button.
        /// </summary>
        /// <param name="button">Pen button to return.</param>
        /// <exception cref="ArgumentException"><paramref name="button"/> is not a valid pen button.</exception>
        public ButtonControl this[PenButton button]
        {
            get
            {
                switch (button)
                {
                    case PenButton.Tip: return tip;
                    case PenButton.Eraser: return eraser;
                    case PenButton.BarrelFirst: return firstBarrelButton;
                    case PenButton.BarrelSecond: return secondBarrelButton;
                    case PenButton.BarrelThird: return thirdBarrelButton;
                    case PenButton.BarrelFourth: return fourthBarrelButton;
                    case PenButton.InRange: return inRange;
                    default:
                        throw new InvalidEnumArgumentException(nameof(button), (int)button, typeof(PenButton));
                }
            }
        }

        /// <summary>
        /// Make this the last used pen, i.e. <see cref="current"/>.
        /// </summary>
        /// <remarks>
        /// This is called automatically by the system when a pen is added or receives
        /// input.
        /// </remarks>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <summary>
        /// Called when the pen is removed from the system.
        /// </summary>
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            tip = GetChildControl<ButtonControl>("tip");
            eraser = GetChildControl<ButtonControl>("eraser");
            firstBarrelButton = GetChildControl<ButtonControl>("barrel1");
            secondBarrelButton = GetChildControl<ButtonControl>("barrel2");
            thirdBarrelButton = GetChildControl<ButtonControl>("barrel3");
            fourthBarrelButton = GetChildControl<ButtonControl>("barrel4");
            inRange = GetChildControl<ButtonControl>("inRange");
            tilt = GetChildControl<Vector2Control>("tilt");
            twist = GetChildControl<AxisControl>("twist");
            displayIndex = GetChildControl<IntegerControl>("displayIndex");
            base.FinishSetup();
        }
    }
}
