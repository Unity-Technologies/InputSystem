using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: remove control argument and rename interface to `IInputProcessor`

////REVIEW: should control processors on parent controls somehow affect their children automatically as well?
////        (ATM it can be confusing that leftStick is deadzoned but leftStick/x and leftStick/y are not)

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A processor that conditions input values.
    /// </summary>
    /// <remarks>
    /// <see cref="InputControl">InputControls</see> can have stacks of processors assigned to them.
    ///
    /// Note that processors CANNOT be stateful. If you need processing that requires keeping
    /// mutating state over time, use InputActions. All mutable state needs to be
    /// kept in the central state buffers.
    ///
    /// However, processors can have configurable parameters. Every public field on a processor
    /// object can be set using "parameters" in JSON or by supplying parameters through the
    /// <see cref="InputControlAttribute.processors"/> field.
    /// </remarks>
    /// <typeparam name="TValue">Type of value to be processed. Only InputControls that use the
    /// same value type will be compatible with the processor.</typeparam>
    /// <example>
    /// <code>
    /// // To register the processor, call
    /// //
    /// //    InputSystem.RegisterControlProcessor<ScalingProcessor>("scale");
    /// //
    /// public class ScalingProcessor : IInputControlProcessor<float>
    /// {
    ///     // This field can be set as a parameter. See examples below.
    ///     // If not explicitly configured, will have its default value.
    ///     public float factor = 2.0f;
    ///
    ///     public float Process(float value, InputControl control)
    ///     {
    ///         return value * factor;
    ///     }
    /// }
    ///
    /// // Use processor in JSON:
    /// const string json = @"
    ///     {
    ///         ""name"" : ""MyDevice"",
    ///         ""controls"" : [
    ///             { ""name"" : ""axis"", ""layout"" : ""Axis"", ""processors"" : ""scale(factor=4)"" }
    ///         ]
    ///     }
    /// ";
    ///
    /// // Use processor on C# state struct:
    /// public struct MyDeviceState : IInputStateTypeInfo
    /// {
    ///     [InputControl(layout = "Axis", processors = "scale(factor=4)"]
    ///     public float axis;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="InputSystem.RegisterControlProcessor"/>
    public interface IInputControlProcessor<TValue>
        where TValue : struct
    {
        /// <summary>
        /// Process the given value and return the result.
        /// </summary>
        /// <remarks>
        /// The implementation of this method must not be stateful.
        /// </remarks>
        /// <param name="value">Input value to process.</param>
        /// <param name="control">The control the value is processed for. Note that <paramref name="value"/> is
        /// not necessarily equal to <see cref="InputControl{TValue}.value"/> as other processors in the stack
        /// may have already altered the value.</param>
        /// <returns>Processed input value.</returns>
        TValue Process(TValue value, InputControl control);
    }

    internal static class InputControlProcessor
    {
        public static TypeTable s_Processors;
    }
}
