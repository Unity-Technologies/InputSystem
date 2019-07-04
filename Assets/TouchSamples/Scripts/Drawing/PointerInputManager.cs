using System;
using InputSamples.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSamples.Drawing
{
    /// <summary>
    /// Input manager that interprets pen, mouse and touch input for mostly drag related controls.
    /// Passes pressure, tilt, twist and touch radius through to drawing components for processing.
    /// </summary>
    /// <remarks>
    /// Couple notes about the control setup:
    ///
    /// - Touch is split off from mouse and pen instead of just using `&lt;Pointer&gt;/position` etc.
    ///   in order to support multi-touch. If we just bind to <see cref="Touchscreen.position"/> and
    ///   such, we will correctly receive the primary touch but the primary touch only. So we put
    ///   bindings for pen and mouse separate to those from touch.
    /// - Mouse and pen are put into one composite. The expectation here is that they are not used
    ///   independently from another and thus don't need to be represented as separate pointer sources.
    ///   However, we could just as well have one <see cref="PointerInputComposite"/> for mice and
    ///   one for pens.
    /// - <see cref="InputAction.passThrough"/> is enabled on <see cref="PointerControls.PointerActions.point"/>.
    ///   The reason is that we want to source arbitrary many pointer inputs through one single actions.
    ///   Without pass-through, the default conflict resolution on actions would kick in and let only
    ///   one of the composite bindings through at a time.
    /// </remarks>
    public class PointerInputManager : MonoBehaviour
    {
        /// <summary>
        /// Event fired when the user presses on the screen.
        /// </summary>
        public event Action<PointerInput, double> Pressed;

        /// <summary>
        /// Event fired as the user drags along the screen.
        /// </summary>
        public event Action<PointerInput, double> Dragged;

        /// <summary>
        /// Event fired when the user releases a press.
        /// </summary>
        public event Action<PointerInput, double> Released;

        private bool m_Dragging;
        private PointerControls m_Controls;

        // These are useful for debugging, especially when touch simulation is on.
        [SerializeField] private bool m_UseMouse;
        [SerializeField] private bool m_UsePen;
        [SerializeField] private bool m_UseTouch;

        protected virtual void Awake()
        {
            m_Controls = new PointerControls();

            m_Controls.pointer.point.performed += OnAction;
            // The action isn't likely to actually cancel as we've bound it to all kinds of inputs but we still
            // hook this up so in case the entire thing resets, we do get a call.
            m_Controls.pointer.point.canceled += OnAction;

            SyncBindingMask();
        }

        protected virtual void OnEnable()
        {
            m_Controls?.Enable();
        }

        protected virtual void OnDisable()
        {
            m_Controls?.Disable();
        }

        protected void OnAction(InputAction.CallbackContext context)
        {
            var control = context.control;
            var device = control.device;

            var isMouseInput = device is Mouse;
            var isPenInput = !isMouseInput && device is Pen;

            // Read our current pointer values.
            var drag = context.ReadValue<PointerInput>();
            if (isMouseInput)
                drag.InputId = Helpers.LeftMouseInputId;
            else if (isPenInput)
                drag.InputId = Helpers.PenInputId;

            if (drag.Contact && !m_Dragging)
            {
                Pressed?.Invoke(drag, context.time);
                m_Dragging = true;
            }
            else if (drag.Contact && m_Dragging)
            {
                Dragged?.Invoke(drag, context.time);
            }
            else
            {
                Released?.Invoke(drag, context.time);
                m_Dragging = false;
            }
        }

        private void SyncBindingMask()
        {
            if (m_Controls == null)
                return;

            if (m_UseMouse && m_UsePen && m_UseTouch)
            {
                m_Controls.bindingMask = null;
                return;
            }

            m_Controls.bindingMask = InputBinding.MaskByGroups(new[]
            {
                m_UseMouse ? "Mouse" : null,
                m_UsePen ? "Pen" : null,
                m_UseTouch ? "Touch" : null
            });
        }

        private void OnValidate()
        {
            SyncBindingMask();
        }
    }
}
