using System.ComponentModel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A single axis value computed from one axis that pulls in the <see cref="negative"/> direction (<see cref="minValue"/>) and one
    /// axis that pulls in the <see cref="positive"/> direction (<see cref="maxValue"/>).
    /// </summary>
    /// <remarks>
    /// The limits of the axis are determined by <see cref="minValue"/> and <see cref="maxValue"/>.
    /// By default, they are set to <c>[-1..1]</c>. The values can be set as parameters.
    ///
    /// <example>
    /// <code>
    /// var action = new InputAction();
    /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2)")
    ///     .With("Negative", "&lt;Keyboard&gt;/a")
    ///     .With("Positive", "&lt;Keyboard&gt;/d");
    /// </code>
    /// </example>
    ///
    /// If both axes are actuated at the same time, the behavior depends on <see cref="whichSideWins"/>.
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
    /// The actual <em>absolute</em> values of <see cref="negative"/> and <see cref="positive"/> are used
    /// to scale <see cref="minValue"/> and <see cref="maxValue"/> respectively. So if, for example, <see cref="positive"/>
    /// is bound to <see cref="Gamepad.rightTrigger"/> and the trigger is at a value of 0.5, then the resulting
    /// value is <c>maxValue * 0.5</c> (the actual formula is <c>midPoint + (maxValue - midPoint) * positive</c>).
    /// </remarks>
    [DisplayStringFormat("{negative}/{positive}")]
    [DisplayName("Positive/Negative Binding")]
    public class AxisComposite : InputBindingComposite<float>
    {
        /// <summary>
        /// Binding for the axis input that controls the negative [<see cref="minValue"/>..0] direction of the
        /// combined axis.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int negative = 0;

        /// <summary>
        /// Binding for the axis input that controls the positive [0..<see cref="maxValue"/>] direction of the
        /// combined axis.
        /// </summary>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        [InputControl(layout = "Axis")] public int positive = 0;

        /// <summary>
        /// The lower bound that the axis is limited to. -1 by default.
        /// </summary>
        /// <remarks>
        /// This value corresponds to the full actuation of the control(s) bound to <see cref="negative"/>.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2)")
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
        /// action.AddCompositeBinding("Axis(minValue=0,maxValue=2)")
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
            var negativeValue = Mathf.Abs(context.ReadValue<float>(negative));
            var positiveValue = Mathf.Abs(context.ReadValue<float>(positive));

            var negativeIsActuated = negativeValue > Mathf.Epsilon;
            var positiveIsActuated = positiveValue > Mathf.Epsilon;

            if (negativeIsActuated == positiveIsActuated)
            {
                switch (whichSideWins)
                {
                    case WhichSideWins.Negative:
                        positiveIsActuated = false;
                        break;

                    case WhichSideWins.Positive:
                        negativeIsActuated = false;
                        break;

                    case WhichSideWins.Neither:
                        return midPoint;
                }
            }

            var mid = midPoint;

            if (negativeIsActuated)
                return mid - (mid - minValue) * negativeValue;

            return mid + (maxValue - mid) * positiveValue;
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

    #if UNITY_EDITOR
    internal class AxisCompositeEditor : InputParameterEditor<AxisComposite>
    {
        private GUIContent m_WhichAxisWinsLabel = new GUIContent("Which Side Wins",
            "Determine which axis 'wins' if both are actuated at the same time. "
            + "If 'Neither' is selected, the result is 0 (or, more precisely, "
            + "the midpoint between minValue and maxValue).");

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets)) return;
#endif
            target.whichSideWins = (AxisComposite.WhichSideWins)EditorGUILayout.EnumPopup(m_WhichAxisWinsLabel, target.whichSideWins);
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(m_WhichAxisWinsLabel.text, target.whichSideWins)
            {
                tooltip = m_WhichAxisWinsLabel.tooltip
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.whichSideWins = (AxisComposite.WhichSideWins)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }

#endif
    }
    #endif
}
