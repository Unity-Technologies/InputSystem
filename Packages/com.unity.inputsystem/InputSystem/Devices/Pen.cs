using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: expose whether pen actually has eraser and which barrel buttons it has

////TODO: hook up pointerId in backend to allow identifying different pens

////REVIEW: have surface distance property to detect how far pen is when hovering?

////REVIEW: does it make sense to have orientation support for pen, too?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state layout for pen devices.
    /// </summary>
    // IMPORTANT: Must match with PenInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 36)]
    public struct PenState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('P', 'E', 'N'); }
        }

        [InputControl(usage = "Point")]
        [FieldOffset(0)]
        public Vector2 position;

        [InputControl(usage = "Secondary2DMotion")]
        [FieldOffset(8)]
        public Vector2 delta;

        [InputControl(layout = "Vector2", usage = "Tilt")]
        [FieldOffset(16)]
        public Vector2 tilt;

        [InputControl(layout = "Analog", usage = "Pressure")]
        [FieldOffset(24)]
        public float pressure;

        [InputControl(layout = "Axis", usage = "Twist")]
        [FieldOffset(28)]
        public float twist;

        [InputControl(name = "tip", layout = "Button", bit = (int)Button.Tip, alias = "button")]
        [InputControl(name = "eraser", layout = "Button", bit = (int)Button.Eraser)]
        [InputControl(name = "barrelFirst", layout = "Button", bit = (int)Button.BarrelFirst, alias = "button", usages = new[] { "PrimaryAction", "PrimaryTrigger" })]
        [InputControl(name = "barrelSecond", layout = "Button", bit = (int)Button.BarrelSecond, usages = new[] { "SecondaryAction", "SecondaryTrigger" })]
        [InputControl(name = "inRange", layout = "Button", bit = (int)Button.InRange)]
        // "Park" unused controls.
        [InputControl(name = "radius", layout = "Vector2", usage = "Radius", offset = InputStateBlock.kInvalidOffset)]
        [InputControl(name = "pointerId", layout = "Digital", offset = InputStateBlock.kInvalidOffset)] // Will stay at 0.
        [InputControl(name = "phase", layout = "PointerPhase", offset = InputStateBlock.kInvalidOffset)] ////TODO: this should be used
        [FieldOffset(32)]
        public ushort buttons;

        [InputControl(layout = "Digital")]
        [FieldOffset(34)]
        public ushort displayIndex;

        public enum Button
        {
            Tip,
            Eraser,
            BarrelFirst,
            BarrelSecond,
            InRange,
        }

        public PenState WithButton(Button button, bool state = true)
        {
            if (state)
                buttons |= (ushort)(1 << (int)button);
            else
                buttons &= (ushort)~(1 << (int)button);
            return this;
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A pen/stylus input device.
    /// </summary>
    /// <remarks>
    /// Unlike mice but like touch, pens are absolute pointing devices moving across a fixed
    /// surface area.
    ///
    /// The <see cref="tip"/> acts as a button that is considered pressed as long as the pen is in contact with the
    /// tablet surface.
    /// </remarks>
    [InputControlLayout(stateType = typeof(PenState))]
    public class Pen : Pointer
    {
        ////TODO: give the tip and eraser a very low press point
        /// <summary>
        /// The tip button of the pen.
        /// </summary>
        public ButtonControl tip { get; private set; }

        /// <summary>
        /// The eraser button of the pen, i.e. the button on the end opposite to the tip.
        /// </summary>
        /// <remarks>
        /// If the pen does not have an eraser button, this control will still be present
        /// but will not trigger.
        /// </remarks>
        public ButtonControl eraser { get; private set; }

        /// <summary>
        /// The button on the side of the pen barrel and located closer to the tip of the pen.
        /// </summary>
        /// <remarks>
        /// If the pen does not have barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        public ButtonControl firstBarrelButton { get; private set; }

        /// <summary>
        /// The button on the side of the pen barrel and located closer to the eraser end of the pen.
        /// </summary>
        /// <remarks>
        /// If the pen does not have barrel buttons, this control will still be present
        /// but will not trigger.
        /// </remarks>
        public ButtonControl secondBarrelButton { get; private set; }

        /// <summary>
        /// Button control that indicates whether the pen is in range of the tablet surface or not.
        /// </summary>
        public ButtonControl inRange { get; private set; }

        public bool isTouching
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The pen that was active or connected last or <c>null</c> if there is no pen.
        /// </summary>
        public new static Pen current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            tip = builder.GetControl<ButtonControl>("tip");
            eraser = builder.GetControl<ButtonControl>("eraser");
            firstBarrelButton = builder.GetControl<ButtonControl>("barrelFirst");
            secondBarrelButton = builder.GetControl<ButtonControl>("barrelSecond");
            inRange = builder.GetControl<ButtonControl>("inRange");
            base.FinishSetup(builder);
        }
    }
}
