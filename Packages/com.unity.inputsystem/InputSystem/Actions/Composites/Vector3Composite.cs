using System.ComponentModel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Composites
{
    [Preserve]
    [DisplayStringFormat("{up}+{down}/{left}+{right}/{forward}+{backward}")]
    [DisplayName("Up/Down/Left/Right/Forward/Backward Composite")]
    public class Vector3Composite : InputBindingComposite<Vector3>
    {
        [InputControl(layout = "Button")]
        public int up;

        [InputControl(layout = "Button")]
        public int down;

        [InputControl(layout = "Button")]
        public int left;

        [InputControl(layout = "Button")]
        public int right;

        [InputControl(layout = "Button")]
        public int forward;

        [InputControl(layout = "Button")]
        public int backward;

        public Mode mode = Mode.Analog;

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
            /// that if, for example, both left and up are pressed, instead of returning a vector (-1,1), a vector
            /// of roughly (-0.7,0.7) (i.e. corresponding to <c>new Vector2(-1,1).normalized</c>) is returned instead.
            /// The resulting 2D area is diamond-shaped.
            /// </summary>
            DigitalNormalized,

            /// <summary>
            /// Part controls are treated as buttons (on/off) and the resulting vector is not normalized. This means
            /// that if, for example, both left and up are pressed, the resulting vector is (-1,1) and has a length
            /// greater than 1. The resulting 2D area is box-shaped.
            /// </summary>
            Digital,
        }
    }

    #if UNITY_EDITOR
    internal class Vector3CompositeEditor : InputParameterEditor<Vector2Composite>
    {
        private GUIContent m_ModeLabel = new GUIContent("Mode",
            "How to create synthesize a Vector3 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.");

        public override void OnGUI()
        {
            target.mode = (Vector2Composite.Mode)EditorGUILayout.EnumPopup(m_ModeLabel, target.mode);
        }
    }
    #endif
}
