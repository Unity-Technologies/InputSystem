using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;

////REVIEW: introduce separate base class for ButtonControl and AxisControl instead of deriving ButtonControl from AxisControl?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// An axis that has a trigger point beyond which it is considered to be pressed.
    /// </summary>
    /// <remarks>
    /// By default stored as a single bit. In that format, buttons will only yield 0
    /// and 1 as values. However, buttons return are <see cref="AxisControl"/>s and
    /// yield full floating-point values and may thus have a range of values. See
    /// <see cref="pressPoint"/> for how button presses on such buttons are handled.
    /// </remarks>
    public class ButtonControl : AxisControl
    {
        ////REVIEW: are per-control press points really necessary? can we just drop them?
        /// <summary>
        /// The minimum value the button has to reach for it to be considered pressed.
        /// </summary>
        /// <value>Button press threshold.</value>
        /// <remarks>
        /// The button is considered pressed, if it has a value equal to or greater than
        /// this value.
        ///
        /// By default, this property is set to -1. If the value of the property is negative,
        /// <see cref="InputSettings.defaultButtonPressPoint"/> is used.
        ///
        /// The value can be configured as a parameter in a layout.
        ///
        /// <example>
        /// <code>
        /// public class MyDevice : InputDevice
        /// {
        ///     [InputControl(parameters = "pressPoint=0.234")]
        ///     public ButtonControl button { get; private set; }
        ///
        ///     //...
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        /// <seealso cref="pressPointOrDefault"/>
        /// <seealso cref="isPressed"/>
        public float pressPoint = -1;

        /// <summary>
        /// Return <see cref="pressPoint"/> if set, otherwise return <see cref="InputSettings.defaultButtonPressPoint"/>.
        /// </summary>
        /// <value>Effective value to use for press point thresholds.</value>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        public float pressPointOrDefault => pressPoint > 0 ? pressPoint : s_GlobalDefaultButtonPressPoint;

        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        /// <remarks>
        /// The default format for the control is <see cref="InputStateBlock.FormatBit"/>.
        /// The control's minimum value is set to 0 and the maximum value to 1.
        /// </remarks>
        public ButtonControl()
        {
            m_StateBlock.format = InputStateBlock.FormatBit;
            m_MinValue = 0f;
            m_MaxValue = 1f;
        }

        /// <summary>
        /// Whether the given value would be considered pressed for this button.
        /// </summary>
        /// <param name="value">Value for the button.</param>
        /// <returns>True if <paramref name="value"/> crosses the threshold to be considered pressed.</returns>
        /// <seealso cref="pressPoint"/>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool IsValueConsideredPressed(float value)
        {
            return value >= pressPointOrDefault;
        }

        /// <summary>
        /// Whether the button is currently pressed.
        /// </summary>
        /// <value>True if button is currently pressed.</value>
        /// <remarks>
        /// A button is considered pressed if its value is equal to or greater
        /// than its button press threshold (<see cref="pressPointOrDefault"/>).
        /// You can use this to read whether specific keys are currently pressed by using isPressed on keys, as shown in the following examples:
        /// <example>
        /// <code>
        /// // Using KeyControl property directly.
        /// Keyboard.current.spaceKey.isPressed
        /// Keyboard.current.aKey.isPressed // etc.
        ///
        /// // Using Key enum.
        /// Keyboard.current[Key.Space].isPressed
        ///
        /// // Using key name.
        /// (KeyControl)Keyboard.current["space"]).isPressed
        /// </code>
        /// </example>
        /// _Note_: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use <see cref="KeyControl.displayName"/>.
        /// You can also use this to read mouse buttons, as shown in the following examples:
        /// <example>
        /// <code>
        /// Mouse.current.leftButton.isPressed
        /// Mouse.current.rightButton.isPressed
        /// Mouse.current.middleButton.isPressed
        ///
        /// // You can also go through all buttons on the mouse (does not allocate).
        /// var controls = Mouse.current.allControls;
        /// for (var i = 0; i &lt; controls.Count; ++i)
        /// {
        ///     var button = controls[i] as ButtonControl;
        ///     if (button != null && button.isPressed)
        ///     {
        ///         // respond to mouse button press here...
        ///     }
        /// }
        ///
        /// // Or look up controls by name.
        /// ((ButtonControl)Mouse.current["leftButton"]).isPressed
        ///
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        /// <seealso cref="pressPoint"/>
        /// <seealso cref="InputSystem.onAnyButtonPress"/>
        public bool isPressed => IsValueConsideredPressed(value);

        /// <summary>
        /// Whether the press started this frame.
        /// </summary>
        /// <value>True if the current press of the button started this frame.</value>
        /// <remarks>
        /// <example>
        /// <code>
        /// // An example showing the use of this property on a gamepad button and a keyboard key.
        ///
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class ExampleScript : MonoBehaviour
        /// {
        ///     void Update()
        ///     {
        ///         bool buttonPressed = Gamepad.current.aButton.wasPressedThisFrame;
        ///         bool spaceKeyPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// _Note_: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use <see cref="KeyControl.displayName"/>.
        ///
        /// You can also use this property to read mouse buttons. For example:
        ///
        /// <example>
        /// <code>
        /// Mouse.current.leftButton.wasPressedThisFrame
        /// Mouse.current.rightButton.wasPressedThisFrame
        /// Mouse.current.middleButton.wasPressedThisFrame
        /// </code>
        /// </example>
        ///
        ///
        /// </remarks>
        public bool wasPressedThisFrame => device.wasUpdatedThisFrame && IsValueConsideredPressed(value) && !IsValueConsideredPressed(ReadValueFromPreviousFrame());

        /// <summary>
        /// Whether the press ended this frame.
        /// </summary>
        /// <value>True if the current press of the button ended this frame.</value>
        /// <remarks>
        /// <example>
        /// <code>
        /// // An example showing the use of this property on a gamepad button and a keyboard key.
        ///
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class ExampleScript : MonoBehaviour
        /// {
        ///     void Update()
        ///     {
        ///         bool buttonPressed = Gamepad.current.aButton.wasReleasedThisFrame;
        ///         bool spaceKeyPressed = Keyboard.current.spaceKey.wasReleasedThisFrame;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// _Note_: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use <see cref="KeyControl.displayName"/>.
        /// </remarks>
        public bool wasReleasedThisFrame => device.wasUpdatedThisFrame && !IsValueConsideredPressed(value) && IsValueConsideredPressed(ReadValueFromPreviousFrame());

        // We make the current global default button press point available as a static so that we don't have to
        // constantly make the hop from InputSystem.settings -> InputManager.m_Settings -> defaultButtonPressPoint.
        internal static float s_GlobalDefaultButtonPressPoint;
        internal static float s_GlobalDefaultButtonReleaseThreshold;

        // We clamp button press points to this value as allowing 0 as the press point causes all buttons
        // to implicitly be pressed all the time. Not useful.
        internal const float kMinButtonPressPoint = 0.0001f;
    }
}
