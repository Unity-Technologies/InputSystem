using System;
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
    public class DpadComposite : IInputBindingComposite<Vector2>
    {
        [InputControl(layout = "Button")] public int up;
        [InputControl(layout = "Button")] public int down;
        [InputControl(layout = "Button")] public int left;
        [InputControl(layout = "Button")] public int right;

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

            var upValue = context.ReadValue<float>(up);
            var downValue = context.ReadValue<float>(down);
            var leftValue = context.ReadValue<float>(left);
            var rightValue = context.ReadValue<float>(right);

            var upIsPressed = upValue > 0;
            var downIsPressed = downValue > 0;
            var leftIsPressed = leftValue > 0;
            var rightIsPressed = rightValue > 0;

            var result =
                DpadControl.MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed, normalize);

            *(Vector2*)buffer = result;
        }
    }
}
