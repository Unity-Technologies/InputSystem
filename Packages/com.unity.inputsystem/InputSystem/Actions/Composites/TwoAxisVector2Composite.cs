#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;


#endif

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A Vector2 composite consisting of two Axis values.
    /// </summary>
    /// <remarks>
    /// This composite allows for two Axis inputs to be combined into a Vector2 representation.
    /// 
    /// This is particularly useful for legacy Joysticks without Gamepad-promised Vector2 representations.
    /// </remarks>
    [DisplayStringFormat("{xAxis}+{yAxis}")]
    public class TwoAxisVector2Composite : InputBindingComposite<Vector2>
    {
        [InputControl(layout = "Axis")]
        public int xAxis;

        [InputControl(layout = "Axis")]
        public int yAxis;

        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            var firstPartValue = context.ReadValue<float>(xAxis);
            var secondPartValue = context.ReadValue<float>(yAxis);

            return new(firstPartValue, secondPartValue);
        }

        static TwoAxisVector2Composite()
        {
            InputSystem.RegisterBindingComposite<TwoAxisVector2Composite>();
        }

        [RuntimeInitializeOnLoadMethod]
        static void Init() { } // Trigger static constructor.
    }
}