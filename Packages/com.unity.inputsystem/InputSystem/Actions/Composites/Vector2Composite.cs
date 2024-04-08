using System;
using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

////TODO: add support for ramp up/down

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A 2D planar motion vector computed from an up+down button pair and a left+right
    /// button pair.
    /// </summary>
    /// <remarks>
    /// This composite allows to grab arbitrary buttons from a device and arrange them in
    /// a D-Pad like configuration. Based on button presses, the composite will return a
    /// normalized direction vector (normalization can be turned off via <see cref="mode"/>).
    ///
    /// Opposing motions cancel each other out. This means that if, for example, both the left
    /// and right horizontal button are pressed, the resulting horizontal movement value will
    /// be zero.
    ///
    /// <example>
    /// <code>
    /// // Set up WASD style keyboard controls.
    /// action.AddCompositeBinding("2DVector")
    ///     .With("Up", "&lt;Keyboard&gt;/w")
    ///     .With("Left", "&lt;Keyboard&gt;/a")
    ///     .With("Down", "&lt;Keyboard&gt;/s")
    ///     .With("Right", "&lt;Keyboard&gt;/d");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Vector3Composite"/>
    [DisplayStringFormat("{up}/{left}/{down}/{right}")] // This results in WASD.
    [DisplayName("Up/Down/Left/Right Composite")]
    public class Vector2Composite : InputBindingComposite<Vector2>
    {
        /// <summary>
        /// Binding for the button that represents the up (that is, <c>(0,1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int up;

        /// <summary>
        /// Binding for the button represents the down (that is, <c>(0,-1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int down;

        /// <summary>
        /// Binding for the button represents the left (that is, <c>(-1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int left;

        /// <summary>
        /// Binding for the button that represents the right (that is, <c>(1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        [InputControl(layout = "Axis")] public int right;

        [Obsolete("Use Mode.DigitalNormalized with 'mode' instead")]
        public bool normalize = true;

        /// <summary>
        /// How to synthesize a <c>Vector2</c> from the values read from <see cref="up"/>, <see cref="down"/>,
        /// <see cref="left"/>, and <see cref="right"/>.
        /// </summary>
        /// <value>Determines how X and Y of the resulting <c>Vector2</c> are formed from input values.</value>
        /// <remarks>
        /// <example>
        /// <code>
        /// var action = new InputAction();
        ///
        /// // DigitalNormalized composite (the default). Turns gamepad left stick into
        /// // control equivalent to the D-Pad.
        /// action.AddCompositeBinding("2DVector(mode=0)")
        ///     .With("up", "&lt;Gamepad&gt;/leftStick/up")
        ///     .With("down", "&lt;Gamepad&gt;/leftStick/down")
        ///     .With("left", "&lt;Gamepad&gt;/leftStick/left")
        ///     .With("right", "&lt;Gamepad&gt;/leftStick/right");
        ///
        /// // Digital composite. Turns gamepad left stick into control equivalent
        /// // to the D-Pad except that diagonals will not be normalized.
        /// action.AddCompositeBinding("2DVector(mode=1)")
        ///     .With("up", "&lt;Gamepad&gt;/leftStick/up")
        ///     .With("down", "&lt;Gamepad&gt;/leftStick/down")
        ///     .With("left", "&lt;Gamepad&gt;/leftStick/left")
        ///     .With("right", "&lt;Gamepad&gt;/leftStick/right");
        ///
        /// // Analog composite. In this case results in setup that behaves exactly
        /// // the same as leftStick already does. But you could use it, for example,
        /// // to swap directions by binding "up" to leftStick/down and "down" to
        /// // leftStick/up.
        /// action.AddCompositeBinding("2DVector(mode=2)")
        ///     .With("up", "&lt;Gamepad&gt;/leftStick/up")
        ///     .With("down", "&lt;Gamepad&gt;/leftStick/down")
        ///     .With("left", "&lt;Gamepad&gt;/leftStick/left")
        ///     .With("right", "&lt;Gamepad&gt;/leftStick/right");
        /// </code>
        /// </example>
        /// </remarks>
        public Mode mode;

        /// <inheritdoc />
        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            var mode = this.mode;

            if (mode == Mode.Analog)
            {
                var upValue = context.ReadValue<float>(up);
                var downValue = context.ReadValue<float>(down);
                var leftValue = context.ReadValue<float>(left);
                var rightValue = context.ReadValue<float>(right);

                return DpadControl.MakeDpadVector(upValue, downValue, leftValue, rightValue);
            }

            var upIsPressed = context.ReadValueAsButton(up);
            var downIsPressed = context.ReadValueAsButton(down);
            var leftIsPressed = context.ReadValueAsButton(left);
            var rightIsPressed = context.ReadValueAsButton(right);

            // Legacy. We need to reference the obsolete member here so temporarily
            // turn of the warning.
            #pragma warning disable CS0618
            if (!normalize) // Was on by default.
                mode = Mode.Digital;
            #pragma warning restore CS0618

            return DpadControl.MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed, mode == Mode.DigitalNormalized);
        }

        /// <inheritdoc />
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            return value.magnitude;
        }

        /// <summary>
        /// Determines how a <c>Vector2</c> is synthesized from part controls.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Part controls are treated as analog meaning that the floating-point values read from controls
            /// will come through as is (minus the fact that the down and left direction values are negated).
            /// </summary>
            Analog = 2,

            /// <summary>
            /// Part controls are treated as buttons (on/off) and the resulting vector is normalized. This means
            /// that if, for example, both left and up are pressed, instead of returning a vector (-1,1), a vector
            /// of roughly (-0.7,0.7) (that is, corresponding to <c>new Vector2(-1,1).normalized</c>) is returned instead.
            /// The resulting 2D area is diamond-shaped.
            /// </summary>
            DigitalNormalized = 0,

            /// <summary>
            /// Part controls are treated as buttons (on/off) and the resulting vector is not normalized. This means
            /// that if, for example, both left and up are pressed, the resulting vector is (-1,1) and has a length
            /// greater than 1. The resulting 2D area is box-shaped.
            /// </summary>
            Digital = 1
        }
    }

    #if UNITY_EDITOR
    internal class Vector2CompositeEditor : InputParameterEditor<Vector2Composite>
    {
        private GUIContent m_ModeLabel = new GUIContent("Mode",
            "How to synthesize a Vector2 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.");

        public override void OnGUI()
        {
            target.mode = (Vector2Composite.Mode)EditorGUILayout.EnumPopup(m_ModeLabel, target.mode);
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(m_ModeLabel.text, target.mode)
            {
                tooltip = m_ModeLabel.tooltip
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.mode = (Vector2Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }

#endif
    }
    #endif
}
