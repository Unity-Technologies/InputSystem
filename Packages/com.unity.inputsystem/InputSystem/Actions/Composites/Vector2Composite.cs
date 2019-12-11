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
    /// normalized direction vector (normalization can be turned off via <see cref="normalize"/>).
    ///
    /// Opposing motions cancel each other out. Meaning that if, for example, both the left
    /// and right horizontal button are pressed, the resulting horizontal movement value will
    /// be zero.
    /// </remarks>
    [Scripting.Preserve]
    public class Vector2Composite : InputBindingComposite<Vector2>
    {
        /// <summary>
        /// Binding for the button that up (i.e. <c>(0,1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Button")] public int up = 0;

        /// <summary>
        /// Binding for the button that down (i.e. <c>(0,-1)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Button")] public int down = 0;

        /// <summary>
        /// Binding for the button that left (i.e. <c>(-1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Button")] public int left = 0;

        /// <summary>
        /// Binding for the button that right (i.e. <c>(1,0)</c>) direction of the vector.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        [InputControl(layout = "Button")] public int right = 0;

        /// <summary>
        /// If true (default), then the resulting vector will be normalized. Otherwise, diagonal
        /// vectors will have a magnitude > 1 (i.e. will be <c>new Vector2(1,1)</c>, for example,
        /// instead of <c>new Vector2(1,1).normalized</c>).
        /// </summary>
        /// <value>Whether the normalize the resulting vector.</value>
        public bool normalize = true;

        /// <summary>
        /// If true (default), the up/down/left/right inputs will be treated as analog meaning that
        /// their actual values will be used. If this is false, the values will be read as buttons
        /// meaning that instead if an input value is below the button press threshold, the value
        /// that is used is 0 and otherwise the value that is used is 1.
        /// </summary>
        /// <value>Whether to treat part bindings as analog controls.</value>
        public bool analog = true;

        /// <inheritdoc />
        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            if (!analog)
            {
                var upIsPressed = context.ReadValueAsButton(up);
                var downIsPressed = context.ReadValueAsButton(down);
                var leftIsPressed = context.ReadValueAsButton(left);
                var rightIsPressed = context.ReadValueAsButton(right);

                return DpadControl.MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed, normalize);
            }

            var upValue = context.ReadValue<float>(up);
            var downValue = context.ReadValue<float>(down);
            var leftValue = context.ReadValue<float>(left);
            var rightValue = context.ReadValue<float>(right);

            return DpadControl.MakeDpadVector(upValue, downValue, leftValue, rightValue, normalize);
        }

        /// <inheritdoc />
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            return value.magnitude;
        }
    }
}
