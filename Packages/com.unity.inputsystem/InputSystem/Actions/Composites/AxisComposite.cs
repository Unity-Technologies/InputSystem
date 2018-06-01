using UnityEngine.Experimental.Input.Controls;

namespace UnityEngine.Experimental.Input.Composites
{
    /// <summary>
    /// A single axis value computed from a "negative" and a "positive" button.
    /// </summary>
    /// <remarks>
    /// This composite allows to arrange any arbitrary two buttons from a device in an
    /// axis configuration such that one button pushes in one direction and the other
    /// pushes in the opposite direction.
    /// </remarks>
    public class AxisComposite : IInputBindingComposite<float>
    {
        // Controls.
        public ButtonControl negative;
        public ButtonControl positive;

        // Parameters.
        ////TODO: add parameters to control ramp up&down

        public float ReadValue(ref InputBindingCompositeContext context)
        {
            var negativeIsPressed = negative.isPressed;
            var positiveIsPressed = positive.isPressed;

            if (negativeIsPressed && positiveIsPressed)
                return 0f;
            if (negativeIsPressed)
                return -1f;
            return 1f;
        }
    }
}
