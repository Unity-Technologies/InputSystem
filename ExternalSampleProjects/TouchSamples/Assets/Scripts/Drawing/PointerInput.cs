using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InputSamples.Drawing
{
    /// <summary>
    /// Simple object to contain information for drag inputs.
    /// </summary>
    public struct PointerInput
    {
        public bool Contact;

        /// <summary>
        /// ID of input type.
        /// </summary>
        public int InputId;

        /// <summary>
        /// Position of draw input.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Orientation of draw input pen.
        /// </summary>
        public Vector2? Tilt;

        /// <summary>
        /// Pressure of draw input.
        /// </summary>
        public float? Pressure;

        /// <summary>
        /// Radius of draw input.
        /// </summary>
        public Vector2? Radius;

        /// <summary>
        /// Twist of draw input.
        /// </summary>
        public float? Twist;
    }

    // What we do in PointerInputManager is to simply create a separate action for each input we need for PointerInput.
    // This here shows a possible alternative that sources all inputs as a single value using a composite. Has pros
    // and cons. Biggest pro is that all the controls actuate together and deliver one input value.
    //
    // NOTE: In PointerControls, we are binding mouse and pen separately from touch. If we didn't care about multitouch,
    //       we wouldn't have to to that but could rather just bind `<Pointer>/position` etc. However, to source each touch
    //       as its own separate PointerInput source, we need to have multiple PointerInputComposites.
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public class PointerInputComposite : InputBindingComposite<PointerInput>
    {
        [InputControl(layout = "Button")]
        public int contact;

        [InputControl(layout = "Vector2")]
        public int position;

        [InputControl(layout = "Vector2")]
        public int tilt;

        [InputControl(layout = "Vector2")]
        public int radius;

        [InputControl(layout = "Axis")]
        public int pressure;

        [InputControl(layout = "Axis")]
        public int twist;

        [InputControl(layout = "Integer")]
        public int inputId;

        public override PointerInput ReadValue(ref InputBindingCompositeContext context)
        {
            var contact = context.ReadValueAsButton(this.contact);
            var pointerId = context.ReadValue<int>(inputId);
            var pressure = context.ReadValue<float>(this.pressure);
            var radius = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.radius);
            var tilt = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.tilt);
            var position = context.ReadValue<Vector2, Vector2MagnitudeComparer>(this.position);
            var twist = context.ReadValue<float>(this.twist);

            return new PointerInput
            {
                Contact = contact,
                InputId = pointerId,
                Position = position,
                Tilt = tilt != default ? tilt : (Vector2?)null,
                Pressure = pressure > 0 ? pressure : (float?)null,
                Radius = radius.sqrMagnitude > 0 ? radius : (Vector2?)null,
                Twist = twist > 0 ? twist : (float?)null,
            };
        }

        #if UNITY_EDITOR
        static PointerInputComposite()
        {
            Register();
        }

        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<PointerInputComposite>();
        }
    }
}
