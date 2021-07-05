using System.ComponentModel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A single axis value computed from a "negative" and a "positive" button.
    /// </summary>
    /// <remarks>
    /// This composite allows to arrange any arbitrary two buttons from a device in an
    /// axis configuration such that one button pushes in one direction and the other
    /// pushes in the opposite direction.
    ///
    /// The limits of the axis are determined by <see cref="minValue"/> and <see cref="maxValue"/>.
    /// By default, they are set to <c>[-1..1]</c>. The values can be set as parameters.
    ///
    /// <example>
    /// <code>
    /// var action = new InputAction();
    /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2")
    ///     .With("Negative", "&lt;Keyboard&gt;/a")
    ///     .With("Positive", "&lt;Keyboard&gt;/d");
    /// </code>
    /// </example>
    ///
    /// If both buttons are pressed at the same time, the behavior depends on <see cref="whichSideWins"/>.
    /// By default, neither side will win (<see cref="WhichSideWins.Neither"/>) and the result
    /// will be 0 (or, more precisely, the midpoint between <see cref="minValue"/> and <see cref="maxValue"/>).
    /// This can be customized to make the positive side win (<see cref="WhichSideWins.Positive"/>)
    /// or the negative one (<see cref="WhichSideWins.Negative"/>).
    ///
    /// This is useful, for example, in a driving game where break should cancel out accelerate.
    /// By binding <see cref="negative"/> to the break control(s) and <see cref="positive"/> to the
    /// acceleration control(s), and setting <see cref="whichSideWins"/> to <see cref="WhichSideWins.Negative"/>,
    /// if the break button is pressed, it will always cause the acceleration button to be ignored.
    ///
    /// The values returned are the actual actuation values of the buttons, unaltered for <see cref="positive"/>
    /// and inverted for <see cref="negative"/>. This means that if the buttons are actual axes (e.g.
    /// the triggers on gamepads), then the values correspond to how much the axis is actuated.
    /// </remarks>
    [Preserve]
    [DisplayStringFormat("{negative}/{positive}")]
    [DisplayName("Positive/Negative Binding")]
    public class AxisComposite : InputBindingComposite<float>
    {
        /// <summary>
        /// Binding for the button that controls the positive direction of the axis.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Button")] public int negative = 0;

        /// <summary>
        /// Binding for the button that controls the negative direction of the axis.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Button")] public int positive = 0;

        /// <summary>
        /// The lower bound that the axis is limited to. -1 by default.
        /// </summary>
        /// <remarks>
        /// This value corresponds to the full actuation of the control(s) bound to <see cref="negative"/>.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2")
        ///     .With("Negative", "&lt;Keyboard&gt;/a")
        ///     .With("Positive", "&lt;Keyboard&gt;/d");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="maxValue"/>
        /// <seealso cref="negative"/>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [Tooltip("Value to return when the negative side is fully actuated.")]
        public float minValue = -1;

        /// <summary>
        /// The upper bound that the axis is limited to. 1 by default.
        /// </summary>
        /// <remarks>
        /// This value corresponds to the full actuation of the control(s) bound to <see cref="positive"/>.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2")
        ///     .With("Negative", "&lt;Keyboard&gt;/a")
        ///     .With("Positive", "&lt;Keyboard&gt;/d");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="minValue"/>
        /// <seealso cref="positive"/>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [Tooltip("Value to return when the positive side is fully actuated.")]
        public float maxValue = 1;

        /// <summary>
        /// If both the <see cref="positive"/> and <see cref="negative"/> button are actuated, this
        /// determines which value is returned from the composite.
        /// </summary>
        [Tooltip("If both the positive and negative side are actuated, decides what value to return. 'Neither' (default) means that " +
            "the resulting value is the midpoint between min and max. 'Positive' means that max will be returned. 'Negative' means that " +
            "min will be returned.")]
        public WhichSideWins whichSideWins = WhichSideWins.Neither;

        /// <summary>
        /// The value that is returned if the composite is in a neutral position, that is, if
        /// neither <see cref="positive"/> nor <see cref="negative"/> are actuated or if
        /// <see cref="whichSideWins"/> is set to <see cref="WhichSideWins.Neither"/> and
        /// both <see cref="positive"/> and <see cref="negative"/> are actuated.
        /// </summary>
        public float midPoint => (maxValue + minValue) / 2;

        ////TODO: add parameters to control ramp up&down

        /// <inheritdoc />
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            var negativeMagnitude = context.EvaluateMagnitude(negative);
            var positiveMagnitude = context.EvaluateMagnitude(positive);

            var negativeIsPressed = negativeMagnitude > 0;
            var positiveIsPressed = positiveMagnitude > 0;

            if (negativeIsPressed == positiveIsPressed)
            {
                switch (whichSideWins)
                {
                    case WhichSideWins.Negative:
                        positiveIsPressed = false;
                        break;

                    case WhichSideWins.Positive:
                        negativeIsPressed = false;
                        break;

                    case WhichSideWins.Neither:
                        return midPoint;
                }
            }

            var mid = midPoint;

            if (negativeIsPressed)
                return mid - (mid - minValue) * negativeMagnitude;

            return mid + (maxValue - mid) * positiveMagnitude;
        }

        /// <inheritdoc />
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var value = ReadValue(ref context);
            if (value < midPoint)
            {
                value = Mathf.Abs(value - midPoint);
                return NormalizeProcessor.Normalize(value, 0, Mathf.Abs(minValue), 0);
            }

            value = Mathf.Abs(value - midPoint);
            return NormalizeProcessor.Normalize(value, 0, Mathf.Abs(maxValue), 0);
        }

        /// <summary>
        /// What happens to the value of an <see cref="AxisComposite"/> if both <see cref="positive"/>
        /// and <see cref="negative"/> are actuated at the same time.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1717:OnlyFlagsEnumsShouldHavePluralNames", Justification = "False positive: `Wins` is not a plural form.")]
        public enum WhichSideWins
        {
            /// <summary>
            /// If both <see cref="positive"/> and <see cref="negative"/> are actuated, the sides cancel
            /// each other out and the result is 0.
            /// </summary>
            Neither = 0,

            /// <summary>
            /// If both <see cref="positive"/> and <see cref="negative"/> are actuated, the value of
            /// <see cref="positive"/> wins and <see cref="negative"/> is ignored.
            /// </summary>
            Positive = 1,

            /// <summary>
            /// If both <see cref="positive"/> and <see cref="negative"/> are actuated, the value of
            /// <see cref="negative"/> wins and <see cref="positive"/> is ignored.
            /// </summary>
            Negative = 2,
        }
    }
}
