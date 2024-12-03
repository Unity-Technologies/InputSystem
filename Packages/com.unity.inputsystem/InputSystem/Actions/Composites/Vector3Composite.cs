using System;
using System.ComponentModel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A 3D vector formed from six floating-point inputs.
    /// </summary>
    /// <remarks>
    /// Depending on the setting of <see cref="mode"/>, the vector is either in  the [-1..1]
    /// range on each axis (normalized or not depending on <see cref="mode"/>) or is in the
    /// full value range of the input controls.
    ///
    /// <example>
    /// <code>
    /// action.AddCompositeBinding("3DVector")
    ///     .With("Forward", "&lt;Keyboard&gt;/w")
    ///     .With("Backward", "&lt;Keyboard&gt;/s")
    ///     .With("Left", "&lt;Keyboard&gt;/a")
    ///     .With("Right", "&lt;Keyboard&gt;/d")
    ///     .With("Up", "&lt;Keyboard&gt;/q")
    ///     .With("Down", "&lt;Keyboard&gt;/e");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Vector2Composite"/>
    [DisplayStringFormat("{up}+{down}/{left}+{right}/{forward}+{backward}")]
    [DisplayName("Up/Down/Left/Right/Forward/Backward Composite")]
    public class Vector3Composite : InputBindingComposite<Vector3>
    {
        /// <summary>
        /// Binding for the button that represents the up (that is, <c>(0,1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int up;

        /// <summary>
        /// Binding for the button that represents the down (that is, <c>(0,-1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int down;

        /// <summary>
        /// Binding for the button that represents the left (that is, <c>(-1,0,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int left;

        /// <summary>
        /// Binding for the button that represents the right (that is, <c>(1,0,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int right;

        /// <summary>
        /// Binding for the button that represents the right (that is, <c>(0,0,1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int forward;

        /// <summary>
        /// Binding for the button that represents the right (that is, <c>(0,0,-1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int backward;

        /// <summary>
        /// How to synthesize a <c>Vector3</c> from the values read from <see cref="up"/>, <see cref="down"/>,
        /// <see cref="left"/>, <see cref="right"/>, <see cref="forward"/>, and <see cref="backward"/>.
        /// </summary>
        /// <value>Determines how X, Y, and Z of the resulting <c>Vector3</c> are formed from input values.</value>
        public Mode mode = Mode.Analog;

        /// <inheritdoc/>
        public override Vector3 ReadValue(ref InputBindingCompositeContext context)
        {
            if (mode == Mode.Analog)
            {
                var upValue = context.ReadValue<float>(up);
                var downValue = context.ReadValue<float>(down);
                var leftValue = context.ReadValue<float>(left);
                var rightValue = context.ReadValue<float>(right);
                var forwardValue = context.ReadValue<float>(forward);
                var backwardValue = context.ReadValue<float>(backward);

                return new Vector3(rightValue - leftValue, upValue - downValue, forwardValue - backwardValue);
            }
            else
            {
                var upValue = context.ReadValueAsButton(up) ? 1f : 0f;
                var downValue = context.ReadValueAsButton(down) ? -1f : 0f;
                var leftValue = context.ReadValueAsButton(left) ? -1f : 0f;
                var rightValue = context.ReadValueAsButton(right) ? 1f : 0f;
                var forwardValue = context.ReadValueAsButton(forward) ? 1f : 0f;
                var backwardValue = context.ReadValueAsButton(backward) ? -1f : 0f;

                var vector = new Vector3(leftValue + rightValue, upValue + downValue, forwardValue + backwardValue);

                if (mode == Mode.DigitalNormalized)
                    vector = vector.normalized;

                return vector;
            }
        }

        /// <inheritdoc />
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            return value.magnitude;
        }

        /// <summary>
        /// Determines how a <c>Vector3</c> is synthesized from part controls.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Part controls are treated as analog meaning that the floating-point values read from controls
            /// will come through as is (minus the fact that the down and left direction values are negated).
            /// </summary>
            Analog,

            /// <summary>
            /// Part controls are treated as buttons (on/off) and the resulting vector is normalized. This means
            /// that if, for example, both left and up are pressed, instead of returning a vector (-1,1,0), a vector
            /// of roughly (-0.7,0.7,0) (that is, corresponding to <c>new Vector3(-1,1,0).normalized</c>) is returned instead.
            /// </summary>
            DigitalNormalized,

            /// <summary>
            /// Part controls are treated as buttons (on/off) and the resulting vector is not normalized. This means
            /// that if both left and up are pressed, for example, the resulting vector is (-1,1,0) and has a length
            /// greater than 1.
            /// </summary>
            Digital,
        }
    }

    #if UNITY_EDITOR
    internal class Vector3CompositeEditor : InputParameterEditor<Vector3Composite>
    {
        private GUIContent m_ModeLabel = new GUIContent("Mode",
            "How to synthesize a Vector3 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.");

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            target.mode = (Vector3Composite.Mode)EditorGUILayout.EnumPopup(m_ModeLabel, target.mode);
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
                target.mode = (Vector3Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }

#endif
    }
    #endif
}
