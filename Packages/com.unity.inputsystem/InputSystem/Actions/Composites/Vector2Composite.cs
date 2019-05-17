using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

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
    /// normalized direction vector.
    ///
    /// Opposing motions cancel each other out. Meaning that if, for example, both the left
    /// and right horizontal button are pressed, the resulting horizontal movement value will
    /// be zero.
    /// </remarks>
    public class Vector2Composite : InputBindingComposite<Vector2>
    {
        [InputControl(layout = "Button")] public int up = 0;
        [InputControl(layout = "Button")] public int down = 0;
        [InputControl(layout = "Button")] public int left = 0;
        [InputControl(layout = "Button")] public int right = 0;

        /// <summary>
        /// If true (default), then the resulting vector will be normalized. Otherwise, diagonal
        /// vectors will have a magnitude > 1.
        /// </summary>
        public bool normalize = true;

        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            var upIsPressed = context.ReadValueAsButton(up);
            var downIsPressed = context.ReadValueAsButton(down);
            var leftIsPressed = context.ReadValueAsButton(left);
            var rightIsPressed = context.ReadValueAsButton(right);

            return DpadControl.MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed, normalize);
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            return value.magnitude;
        }
    }
}
