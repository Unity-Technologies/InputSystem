using System;
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

        public Type valueType
        {
            get { return typeof(float); }
        }

        public int valueSizeInBytes
        {
            get { return sizeof(float); }
        }

        public unsafe float ReadValue(ref InputBindingCompositeContext context)
        {
            float result;
            ReadValue(ref context, &result, sizeof(float));
            return result;
        }

        public unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (bufferSize < sizeof(float))
                throw new ArgumentException("bufferSize < sizeof(float)", "bufferSize");

            var negativeIsPressed = negative.isPressed;
            var positiveIsPressed = positive.isPressed;

            float result;
            if (negativeIsPressed == positiveIsPressed)
                result = 0f;
            else if (negativeIsPressed)
                result = -1f;
            else
                result = 1f;

            *((float*)buffer) = result;
        }
    }
}
