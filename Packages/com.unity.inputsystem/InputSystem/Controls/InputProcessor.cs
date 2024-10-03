using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: come up with a mechanism to allow (certain) processors to be stateful

////TODO: cache processors globally; there's no need to instantiate the same processor with the same parameters multiple times
////      (except if they do end up being stateful)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A processor that conditions/transforms input values.
    /// </summary>
    /// <remarks>
    /// To define a custom processor, it is usable best to derive from <see cref="InputProcessor{TValue}"/>
    /// instead of from this class. Doing so will avoid having to deal with things such as the raw memory
    /// buffers of <see cref="Process"/>.
    ///
    /// Note, however, that if you do want to define a processor that can process more than one type of
    /// value, you can derive directly from this class.
    /// </remarks>
    /// <seealso cref="InputBinding.processors"/>
    /// <seealso cref="InputControlLayout.ControlItem.processors"/>
    /// <seealso cref="InputSystem.RegisterProcessor{T}"/>
    /// <seealso cref="InputActionRebindingExtensions.GetParameterValue(InputAction,string,InputBinding)"/>
    /// <seealso cref="InputActionRebindingExtensions.ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
    public abstract class InputProcessor
    {
        /// <summary>
        /// Process an input value, given as an object, and return the processed value as an object.
        /// </summary>
        /// <param name="value">A value matching the processor's value type.</param>
        /// <param name="control">Optional control that the value originated from. Must have the same value type
        /// that the processor has.</param>
        /// <returns>A processed value based on <paramref name="value"/>.</returns>
        /// <remarks>
        /// This method allocates GC heap memory. To process values without allocating GC memory, it is necessary to either know
        /// the value type of a processor at compile time and call <see cref="InputProcessor{TValue}.Process(TValue,UnityEngine.InputSystem.InputControl)"/>
        /// directly or to use <see cref="Process"/> instead and process values in raw memory buffers.
        /// </remarks>
        public abstract object ProcessAsObject(object value, InputControl control);

        /// <summary>
        /// Process an input value stored in the given memory buffer.
        /// </summary>
        /// <param name="buffer">Memory buffer containing the input value. Must be at least large enough
        /// to hold one full value as indicated by <paramref name="bufferSize"/>.</param>
        /// <param name="bufferSize">Size (in bytes) of the value inside <paramref name="buffer"/>.</param>
        /// <param name="control">Optional control that the value originated from. Must have the same value type
        /// that the processor has.</param>
        /// <remarks>
        /// This method allows processing values of arbitrary size without allocating memory on the GC heap.
        /// </remarks>
        public abstract unsafe void Process(void* buffer, int bufferSize, InputControl control);

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

        /// <summary>
        /// Caching policy regarding usage of return value from processors.
        /// </summary>
        public enum CachingPolicy
        {
            /// <summary>
            /// Cache result value if unprocessed value has not been changed.
            /// </summary>
            CacheResult = 0,

            /// <summary>
            /// Process value every call to <see cref="InputControl{TValue}.ReadValue()"/> even if unprocessed value has not been changed.
            /// </summary>
            EvaluateOnEveryRead = 1
        }

        /// <summary>
        /// Caching policy of the processor. Override this property to provide a different value.
        /// </summary>
        public virtual CachingPolicy cachingPolicy => CachingPolicy.CacheResult;
    }

    /// <summary>
    /// A processor that conditions/transforms input values.
    /// </summary>
    /// <typeparam name="TValue">Type of value to be processed. Only InputControls that use the
    /// same value type will be compatible with the processor.</typeparam>
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
    ///
    /// <example>
    /// <code>
    /// // To register the processor, call
    /// //
    /// //    InputSystem.RegisterProcessor&lt;ScalingProcessor&gt;("scale");
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
    ///
    /// See <see cref="Editor.InputParameterEditor{T}"/> for how to define custom parameter
    /// editing UIs for processors.
    /// </remarks>
    /// <seealso cref="InputSystem.RegisterProcessor"/>
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
        ///     not originate from a control. This can be the case, for example, if the processor sits on a composite
        ///     binding (<see cref="InputBindingComposite"/>) as composites are not directly associated with controls
        ///     but rather source their values through their child bindings.</param>
        /// <returns>Processed input value.</returns>
        public abstract TValue Process(TValue value, InputControl control);

        public override object ProcessAsObject(object value, InputControl control)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!(value is TValue))
                throw new ArgumentException(
                    $"Expecting value of type '{typeof(TValue).Name}' but got value '{value}' of type '{value.GetType().Name}'",
                    nameof(value));

            var valueOfType = (TValue)value;

            return Process(valueOfType, control);
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

            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, buffer, valueSize);

            value = Process(value, control);

            valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(buffer, valuePtr, valueSize);
        }
    }
}
