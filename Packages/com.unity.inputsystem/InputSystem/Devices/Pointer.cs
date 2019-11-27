using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: add capabilities indicating whether pressure is supported

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

        uint pointerId;

        /// <summary>
        /// Position of the pointer in screen space.
        /// </summary>
#if UNITY_EDITOR
        [InputControl(layout = "Vector2", displayName = "Position", usage = "Point", processors = "AutoWindowSpace")]
#else
        [InputControl(layout = "Vector2", displayName = "Position", usage = "Point")]
#endif
        public Vector2 position;

        ////REVIEW: if we have Secondary2DMotion on this, seems like this should be normalized
        [InputControl(layout = "Vector2", displayName = "Delta", usage = "Secondary2DMotion")]
        public Vector2 delta;

        [InputControl(layout = "Analog", displayName = "Pressure", usage = "Pressure")]
        public float pressure;

        [InputControl(layout = "Vector2", displayName = "Radius", usage = "Radius")]
        public Vector2 radius;

        [InputControl(name = "press", displayName = "Press", layout = "Button", format = "BIT", bit = 0)]
        public ushort buttons;

        public FourCC format => kFormat;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Base class for pointer-style devices moving on a 2D screen.
    /// </summary>
    /// <remarks>
    /// This class abstracts over general "pointing" behavior where a pointer is moved across a 2D
    /// surface. Operating at the <c>Pointer</c> level allows treating <see>Mouse</see>, <see>Pen</see>,
    /// and <see>Touchscreen</see> all as pointers with a set of shared behaviors.
    ///
    /// Note that a pointer may have "multi-point" ability as is the case with multi-touch where
    /// multiple touches represent multiple concurrent "pointers". However, for any pointer device
    /// with multiple pointers, only one pointer is considered "primary" and drives the pointer
    /// controls present on the base class.
    /// </remarks>
    /// <seealso cref="Mouse"/>
    /// <seealso cref="Pen"/>
    /// <seealso cref="Touchscreen"/>
    [InputControlLayout(stateType = typeof(PointerState), isGenericTypeOfDevice = true)]
    [Preserve]
    public class Pointer : InputDevice, IInputStateCallbackReceiver
    {
        ////REVIEW: shouldn't this be done for every touch position, too?
        /// <summary>
        /// The current pointer coordinates in window space.
        /// </summary>
        /// <value>Control representing the current position of the pointer on screen.</value>
        /// <remarks>
        /// Within player code, the coordinates are in the coordinate space of Unity's <c>Display</c>.
        ///
        /// Within editor code, the coordinates are in the coordinate space of the current <c>EditorWindow</c>
        /// This means that if you query <see cref="Mouse.position"/> in <c>EditorWindow.OnGUI</c>, for example,
        /// the returned 2D vector will be in the coordinate space of your local GUI (same as
        /// <c>Event.mousePosition</c>).
        /// </remarks>
        public Vector2Control position { get; private set; }

        /// <summary>
        /// The current window-space motion delta of the pointer.
        /// </summary>
        /// <value>Control representing the motion delta of the pointer.</value>
        /// <remarks>
        /// Every time a pointer is moved, it generates a motion delta. This control represents
        /// this motion.
        ///
        /// Note that some pointers have the ability to generate motion deltas <em>without</em>
        /// actually changing the position of the pointer. This is the case for <see cref="Mouse"/>
        /// which even when, for example, bumping up against the edges of the screen or when being
        /// locked in place, can generate motion. This means that activity on <c>delta</c> is not
        /// necessarily correlated with activity on <see cref="position"/>.
        ///
        /// Deltas have two special behaviors attached to them that makes them quite unique
        /// among input controls.
        ///
        /// For one, deltas will automatically reset to <c>(0,0)</c> between frames. If, for example,
        /// the current delta value is <c>(12,8)</c>, then after the next <see cref="InputSystem.Update"/>,
        /// the delta is automatically set to <c>(0,0)</c>. More precisely, deltas will reset as part
        /// of <see cref="InputSystem.onBeforeUpdate"/>. This happens every time regardless of whether
        /// there are pending motion events for the pointer or not. But because it happens in
        /// <see cref="InputSystem.onBeforeUpdate"/> (i.e. <em>before</em> events are processed),
        /// subsequent motion deltas are incorporated normally.
        ///
        /// Note that the resetting is visible to <see cref="InputAction"/>s. This means that when
        /// binding to a delta control from an action that is not using <see cref="InputActionType.PassThrough"/>,
        /// you will see the action getting cancelled at the start of every frame. With a <c>PassThrough</c>
        /// actions, you will instead see it perform one extra time with a zero value.
        ///
        /// The other special behavior of deltas is accumulation. When receiving more than one
        /// motion update in a frame, deltas will not simply switch from one value to the other
        /// but instead accumulate them. For example, if two events are received for a pointer
        /// in a frame and one has a motion delta of <c>(1,1)</c> and the other has a motion delta
        /// of <c>(2,2)</c>, then once <see cref="InputSystem.Update"/> has finished processing
        /// events, the value of the delta control will be <c>(3,3)</c> and not <c>(2,2)</c>.
        ///
        /// Note that just like resetting, accumulation is also visible to <see cref="InputAction"/>s.
        /// This means that because the delta control changes value twice, the action will trigger
        /// twice but the value when it is triggered the second time will be <c>(3,3)</c> and
        /// not <c>(2,2)</c> even though that's the value received from the event.
        /// </remarks>
        /// <seealso cref="InputControlExtensions.AccumulateValueInEvent"/>
        public Vector2Control delta { get; private set; }

        ////REVIEW: move this down to only TouchScreen?
        /// <summary>
        /// Window-space radius of the pointer contact with the surface.
        /// </summary>
        /// <value>Control representing the horizontal and vertical extents of the pointer contact.</value>
        /// <remarks>
        /// Usually, only touch input has radius detection.
        /// </remarks>
        /// <seealso cref="TouchControl.radius"/>
        public Vector2Control radius { get; private set; }

        /// <summary>
        /// Normalized pressure with which the pointer is currently pressed while in contact with the pointer surface.
        /// </summary>
        /// <value>Control representing the pressure with which the pointer is pressed down.</value>
        /// <remarks>
        /// This is only meaningful for pointing devices that support pressure. Mice do not, pens usually do, and touch
        /// usually does on mobile platforms.
        ///
        /// Note that it is possible for the value to go above 1 even though it is considered normalized. The reason is
        /// that calibration on the system can put the maximum pressure point below the physically supported maximum value.
        /// </remarks>
        public AxisControl pressure { get; private set; }

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
        /// <value>Currently active <c>Pointer</c> or <c>null</c>.</value>
        public static Pointer current { get; internal set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            position = GetChildControl<Vector2Control>("position");
            delta = GetChildControl<Vector2Control>("delta");
            radius = GetChildControl<Vector2Control>("radius");
            pressure = GetChildControl<AxisControl>("pressure");
            press = GetChildControl<ButtonControl>("press");

            base.FinishSetup();
        }

        /// <summary>
        /// Called whenever the input system advances by one frame.
        /// </summary>
        /// <seealso cref="InputSystem.Update"/>
        protected void OnNextUpdate()
        {
            InputState.Change(delta, Vector2.zero);
        }

        /// <summary>
        /// Called when the pointer receives a state event.
        /// </summary>
        /// <param name="eventPtr">The input event.</param>
        protected unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            var statePtr = currentStatePtr;

            delta.x.AccumulateValueInEvent(statePtr, eventPtr);
            delta.y.AccumulateValueInEvent(statePtr, eventPtr);

            InputState.Change(this, eventPtr);
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
            OnNextUpdate();
        }

        void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            OnStateEvent(eventPtr);
        }

        bool IInputStateCallbackReceiver.GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }
    }
}
