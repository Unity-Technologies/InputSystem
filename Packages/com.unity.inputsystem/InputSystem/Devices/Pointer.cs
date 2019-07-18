using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: add capabilities indicating whether pressure and tilt is supported

////REVIEW: is there an opportunity to collapse "press" and "pressure" into one? after all, if there's any pressure, isn't the pointer pressed?

////REVIEW: should "displayIndex" be called "windowIndex"? or be part of a better thought-out multi-display API altogether?

////REVIEW: add click and clickCount controls directly to Pointer?
////        (I gave this a look but in my initial try, found it somewhat difficult to add click detection at the Pointer level due
////        to the extra state it involves)

////REVIEW: should we put lock state directly on Pointer?

////REVIEW: should pointer IDs be required to be globally unique across pointing devices?
////REVIEW: should we create new devices instead of using pointer IDs?

////FIXME: pointer deltas in EditorWindows need to be Y *down*

////REVIEW: kill EditorWindowSpace processor and add GetPositionInEditorWindowSpace() and GetDeltaInEditorWindowSpace()?
////        (if we do this, every touch control has to get this, too)

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state structure for pointer devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PointerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('P', 'T', 'R');

        [InputControl(layout = "Digital")]
        public uint pointerId;

        /// <summary>
        /// Position of the pointer in screen space.
        /// </summary>
#if UNITY_EDITOR
        [InputControl(layout = "Vector2", usage = "Point", processors = "AutoWindowSpace", displayName = "Position")]
#else
        [InputControl(layout = "Vector2", usage = "Point", displayName = "Position")]
#endif
        public Vector2 position;

        ////REVIEW: if we have Secondary2DMotion on this, seems like this should be normalized
        [InputControl(layout = "Vector2", usage = "Secondary2DMotion")]
        public Vector2 delta;

        [InputControl(layout = "Analog", usage = "Pressure")]
        public float pressure;

        [InputControl(layout = "Axis", usage = "Twist")]
        public float twist;

        [InputControl(layout = "Vector2", usage = "Tilt")]
        public Vector2 tilt;

        [InputControl(layout = "Vector2", usage = "Radius")]
        public Vector2 radius;

        [InputControl(name = "press", layout = "Button", format = "BIT", bit = 0)]
        public ushort buttons;

        [InputControl(layout = "Digital")]
        public ushort displayIndex;

        public FourCC format
        {
            get { return kFormat; }
        }
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Base class for pointer-style devices moving on a 2D screen.
    /// </summary>
    /// <remarks>
    /// Note that a pointer may have "multi-point" ability as is the case with multi-touch where
    /// multiple touches represent multiple concurrent "pointers". However, for any pointer device
    /// with multiple pointers, only one pointer is considered "primary" and drives the pointer
    /// controls present on the base class.
    /// </remarks>
    /// <seealso cref="Mouse"/>
    /// <seealso cref="Pen"/>
    /// <seealso cref="Touchscreen"/>
    [InputControlLayout(stateType = typeof(PointerState), isGenericTypeOfDevice = true)]
    public class Pointer : InputDevice, IInputStateCallbackReceiver
    {
        ////REVIEW: shouldn't this be done for every touch position, too?
        /// <summary>
        /// The current pointer coordinates in window space.
        /// </summary>
        /// <remarks>
        /// Within player code, the coordinates are in the coordinate space of the <see cref="UnityEngine.Display">
        /// Display</see> space that is current according to <see cref="displayIndex"/>. When running with a
        /// single display, that means the coordinates will always be in window space of the first display.
        ///
        /// Within editor code, the coordinates are in the coordinate space of the current <see cref="UnityEditor.EditorWindow"/>.
        /// This means that if you query <see cref="Mouse.position"/> in <see cref="UnityEditor.EditorWindow.OnGUI"/>, for example,
        /// the returned 2D vector will be in the coordinate space of your local GUI (same as
        /// <see cref="UnityEngine.Event.mousePosition"/>).
        /// </remarks>
        public Vector2Control position { get; private set; }

        public Vector2Control delta { get; private set; }

        public Vector2Control tilt { get; private set; }
        public Vector2Control radius { get; private set; }

        /// <summary>
        /// Normalized pressure with which the pointer is currently pressed while in contact with the pointer surface.
        /// </summary>
        /// <remarks>
        /// This is only meaningful for pointing devices that support pressure. Mice do not, pens usually do, and touch
        /// usually does on mobile platforms.
        ///
        /// Note that it is possible for the value to go above 1 even though it is considered normalized. The reason is
        /// that calibration on the system can put the maximum pressure point below the physically supported maximum value.
        /// </remarks>
        public AxisControl pressure { get; private set; }

        /// <summary>
        /// Rotation of the pointer around its own axis. 0 means the pointer is facing away from the user (12 'o clock position)
        /// and ~1 means the pointer has been rotated clockwise almost one full rotation.
        /// </summary>
        /// <remarks>
        /// Twist is generally only supported by pens and even among pens, twist support is rare. An example product that
        /// supports twist is the Wacom Art Pen.
        ///
        /// The axis of rotation is the vector facing away from the pointer surface when the pointer is facing straight up
        /// (i.e. the surface normal of the pointer surface). When the pointer is tilted, the rotation axis is tilted along
        /// with it.
        /// </remarks>
        public AxisControl twist { get; private set; }

        public IntegerControl pointerId { get; private set; }
        public IntegerControl displayIndex { get; private set; }

        /// <summary>
        /// Whether the pointer is pressed down.
        /// </summary>
        /// <remarks>
        /// What this means exactly depends on the nature of the pointer. For mice (<see cref="Mouse"/>), it means
        /// that the left button is pressed. For pens (<see cref="Pen"/>), it means that the pen tip is touching
        /// the screen/tablet surface. For touchscreens (<see cref="Touchscreen"/>), it means that there is at least
        /// one finger touching the screen.
        /// </remarks>
        public ButtonControl press { get; private set; }

        /// <summary>
        /// The pointer that was added or used last by the user or <c>null</c> if there is no pointer
        /// device connected to the system.
        /// </summary>
        public static Pointer current { get; internal set; }

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
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            position = builder.GetControl<Vector2Control>(this, "position");
            delta = builder.GetControl<Vector2Control>(this, "delta");
            tilt = builder.GetControl<Vector2Control>(this, "tilt");
            radius = builder.GetControl<Vector2Control>(this, "radius");
            pressure = builder.GetControl<AxisControl>(this, "pressure");
            twist = builder.GetControl<AxisControl>(this, "twist");
            pointerId = builder.GetControl<IntegerControl>(this, "pointerId");
            displayIndex = builder.GetControl<IntegerControl>(this, "displayIndex");
            press = builder.GetControl<ButtonControl>(this, "press");

            base.FinishSetup(builder);
        }

        protected static unsafe void Accumulate(InputControl<float> control, void* oldStatePtr, InputEventPtr newState)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!control.ReadUnprocessedValueFromEvent(newState, out var newDelta))
                return; // Value for the control not contained in the given event.

            var oldDelta = control.ReadUnprocessedValueFromState(oldStatePtr);
            control.WriteValueIntoEvent(oldDelta + newDelta, newState);
        }

        protected void OnNextUpdate()
        {
            InputState.Change(delta, Vector2.zero);
        }

        protected unsafe void OnEvent(InputEventPtr eventPtr)
        {
            var statePtr = currentStatePtr;

            Accumulate(delta.x, statePtr, eventPtr);
            Accumulate(delta.y, statePtr, eventPtr);

            InputState.Change(this, eventPtr);
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnEvent(eventPtr);
        }
    }
}
