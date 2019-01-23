using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

////TODO: add support for ramp up/down

namespace UnityEngine.Experimental.Input.Composites
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
            var upValue = context.ReadValue<float>(up);
            var downValue = context.ReadValue<float>(down);
            var leftValue = context.ReadValue<float>(left);
            var rightValue = context.ReadValue<float>(right);

            ////REVIEW: should this respect press points?

            var upIsPressed = upValue > 0;
            var downIsPressed = downValue > 0;
            var leftIsPressed = leftValue > 0;
            var rightIsPressed = rightValue > 0;

            return DpadControl.MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed, normalize);
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            return value.magnitude;
        }
    }
}
