using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Composites
{
    /// <summary>
    /// A single [-1..1] axis value computed from a "negative" and a "positive" button.
    /// </summary>
    /// <remarks>
    /// This composite allows to arrange any arbitrary two buttons from a device in an
    /// axis configuration such that one button pushes in one direction and the other
    /// pushes in the opposite direction.
    ///
    /// If both buttons are pressed at the same time, the behavior depends on <see cref="whichSideWins"/>.
    /// By default, neither side will win (<see cref="WhichSideWins.Neither"/>) and the result
    /// will be 0. This can be customized to make the positive side win (<see cref="WhichSideWins.Positive"/>)
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
    public class AxisComposite : InputBindingComposite<float>
    {
        /// <summary>
        /// Binding for the button that controls the positive direction of the axis.
        /// </summary>
        [InputControl(layout = "Button")] public int negative;

        /// <summary>
        /// Binding for the button that controls the negative direction of the axis.
        /// </summary>
        [InputControl(layout = "Button")] public int positive;

        /// <summary>
        /// If both the <see cref="positive"/> and <see cref="negative"/> button are actuated, this
        /// determines which value is returned from the composite.
        /// </summary>
        public WhichSideWins whichSideWins;

        ////TODO: add parameters to control ramp up&down

        /// <inheritdoc />
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            var negativeValue = context.ReadValue<float>(negative);
            var positiveValue = context.ReadValue<float>(positive);

            var negativeIsPressed = negativeValue > 0;
            var positiveIsPressed = positiveValue > 0;

            if (negativeIsPressed == positiveIsPressed)
            {
                switch (whichSideWins)
                {
                    case WhichSideWins.Negative:
                        return -negativeValue;

                    case WhichSideWins.Positive:
                        return positiveValue;

                    case WhichSideWins.Neither:
                        return 0;
                }
            }

            if (negativeIsPressed)
                return -negativeValue;

            return positiveValue;
        }

        /// <summary>
        /// What happens to the value of an <see cref="AxisComposite"/> if both <see cref="positive"/>
        /// and <see cref="negative"/> are actuated at the same time.
        /// </summary>
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
