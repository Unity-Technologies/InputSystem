using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: cache processors globally; there's no need to instantiate the same processor with the same parameters multiple times

////TODO: make processor effects visible on children (e.g. leftStick/x should reflect deadzoning of leftStick)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A processor that conditions/transforms input values.
    /// </summary>
    public abstract class InputProcessor
    {
        /// <summary>
        /// Process an input value, given as an object, and return the processed value as an object.
        /// </summary>
        /// <param name="value">A value of type <see cref="valueType"/>.</param>
        /// <param name="control">Optional control that the value originated from. Must have the same value type
        /// that the processor has (<see cref="valueType"/>).</param>
        /// <returns>A processed value based on <paramref name="value"/>.</returns>
        /// <remarks>
        /// This method allocates GC memory. To process values without allocating GC memory, it is necessary to know
        /// the value type of a processor at compile time and call <see cref="InputProcessor{TValue}.Process"/> directly.
        /// </remarks>
        public abstract object ProcessAsObject(object value, InputControl control);

        public abstract unsafe void Process(void* buffer, int bufferSize, InputControl control);

        /// <summary>
        /// Value type expected by the processor.
        /// </summary>
        public abstract Type valueType { get; }

        internal static TypeTable s_Processors;

        /// <summary>
        /// Get the value type of a processor without having to instantiate it and use <see cref="valueType"/>.
        /// </summary>
        /// <param name="processorType"></param>
        /// <returns>Value type of the given processor or null if it could not be determined statically.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="processorType"/> is null.</exception>
        /// <remarks>
        /// This method is reliant on the processor being based on <see cref="InputProcessor{TValue}"/>. It will return
        /// the <c>TValue</c> argument used with the class. If the processor is not based on <see cref="InputProcessor{TValue}"/>,
        /// this method returns null.
        /// </remarks>
        internal static Type GetValueTypeFromType(Type processorType)
        {
            if (processorType == null)
                throw new ArgumentNullException(nameof(processorType));

            return TypeHelpers.GetGenericTypeArgumentFromHierarchy(processorType, typeof(InputProcessor<>), 0);
        }
    }

    /// <summary>
    /// A processor that conditions/transforms input values.
    /// </summary>
    /// <remarks>
    /// Each <see cref="InputControl"/> can have a stack of processors assigned to it.
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
    /// //    InputSystem.RegisterControlProcessor&lt;ScalingProcessor&gt;("scale");
    /// //
    /// public class ScalingProcessor : InputProcessor&lt;float&gt;
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
    public abstract class InputProcessor<TValue> : InputProcessor
        where TValue : struct
    {
        /// <summary>
        /// Process the given value and return the result.
        /// </summary>
        /// <remarks>
        /// The implementation of this method must not be stateful.
        /// </remarks>
        /// <param name="value">Input value to process.</param>
        /// <param name="control">Control that the value originally came from. This can be null if the value did
        /// not originate from a control. This can be the case, for example, if the processor sits on a composite
        /// binding (<see cref="InputBindingComposite"/>) as composites are not directly associated with controls
        /// but rather source their values through their child bindings.</param>
        /// <returns>Processed input value.</returns>
        public abstract TValue Process(TValue value, InputControl<TValue> control);

        public override Type valueType => typeof(TValue);

        public override object ProcessAsObject(object value, InputControl control)
        {
            if (!(value is TValue))
                throw new ArgumentException(
                    $"Expecting value of type '{typeof(TValue).Name}' but got value '{value}' of type '{value.GetType().Name}'",
                    nameof(value));

            var valueOfType = (TValue)value;

            var controlOfType = control as InputControl<TValue>;
            if (controlOfType == null && control != null)
                throw new ArgumentException(
                    $"Expecting control of type 'InputControl<{typeof(TValue).Name}>' but got control '{control}' of type '{control.GetType().Name}' instead");

            return Process(valueOfType, controlOfType);
        }

        public override unsafe void Process(void* buffer, int bufferSize, InputControl control)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var valueSize = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < valueSize)
                throw new ArgumentException(
                    $"Expected buffer of at least {valueSize} bytes but got buffer with just {bufferSize} bytes",
                    nameof(bufferSize));

            var controlOfType = control as InputControl<TValue>;
            if (controlOfType == null && control != null)
                throw new ArgumentException(
                    $"Expecting control of type 'InputControl<{typeof(TValue).Name}>' but got control '{control}' of type '{control.GetType().Name}' instead");

            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, buffer, valueSize);

            value = Process(value, controlOfType);

            valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(buffer, valuePtr, valueSize);
        }
    }
}
