using System;
using UnityEngine.Experimental.Input.Controls;

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
    public class DpadComposite : IInputBindingComposite<Vector2>
    {
        public ButtonControl up;
        public ButtonControl down;
        public ButtonControl left;
        public ButtonControl right;

        /// <summary>
        /// If true (default), then the resulting vector will be normalized. Otherwise, diagonal
        /// vectors will have a magnitude > 1.
        /// </summary>
        public bool normalize = true;

        public Type valueType
        {
            get { return typeof(Vector2); }
        }

        public unsafe int valueSizeInBytes
        {
            get { return sizeof(Vector2); }
        }
        public unsafe Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            Vector2 result;
            ReadValue(ref context, &result, sizeof(Vector2));
            return result;
        }

        public unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (bufferSize < sizeof(Vector2))
                throw new ArgumentException("bufferSize < sizeof(Vector2)", "bufferSize");

            ////TODO: unify code path with DpadControl.ReadUnprocessedValueFrom()
            var upIsPressed = up.isPressed;
            var downIsPressed = down.isPressed;
            var leftIsPressed = left.isPressed;
            var rightIsPressed = right.isPressed;

            var upValue = upIsPressed ? 1.0f : 0.0f;
            var downValue = downIsPressed ? -1.0f : 0.0f;
            var leftValue = leftIsPressed ? -1.0f : 0.0f;
            var rightValue = rightIsPressed ? 1.0f : 0.0f;

            var result = new Vector2(leftValue + rightValue, upValue + downValue);

            if (normalize)
            {
                // If press is diagonal, adjust coordinates to produce vector of length 1.
                // pow(0.707107) is roughly 0.5 so sqrt(pow(0.707107)+pow(0.707107)) is ~1.
                const float diagonal = 0.707107f;
                if (result.x != 0 && result.y != 0)
                    result = new Vector2(result.x * diagonal, result.y * diagonal);
            }

            *((Vector2*)buffer) = result;
        }
    }
}
