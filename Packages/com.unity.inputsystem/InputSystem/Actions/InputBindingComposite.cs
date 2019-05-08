using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: support nested composites

////REVIEW: composites probably need a reset method, too (like interactions), so that they can be stateful

////REVIEW: isn't this about arbitrary value processing? can we open this up more and make it
////        not just be about composing multiple bindings?

////REVIEW: when we get blittable type constraints, we can probably do away with the pointer-based ReadValue version

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A binding that synthesizes a value from from several component bindings.
    /// </summary>
    public abstract class InputBindingComposite
    {
        public abstract Type valueType { get; }
        public abstract int valueSizeInBytes { get; }
        public abstract unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize);
        public abstract object ReadValueAsObject(ref InputBindingCompositeContext context);
        public abstract float EvaluateMagnitude(ref InputBindingCompositeContext context);

        internal static TypeTable s_Composites;

        /// <summary>
        /// Return the name of the control layout that is expected for the given part (e.g. "Up") on the given
        /// composite (e.g. "Dpad").
        /// </summary>
        /// <param name="composite"></param>
        /// <param name="part"></param>
        /// <returns>The layout name (such as "Button") expected for the given part on the composite or null if
        /// there is no composite with the given name or no part on the composite with the given name.</returns>
        /// <remarks>
        /// Expected control layouts can be set on composite parts by setting the <see cref="InputControlAttribute.layout"/>
        /// property on them.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputBindingComposite.GetExpectedControlLayoutName("Dpad", "Up") // Returns "Button"
        ///
        /// // This is how Dpad communicates that:
        /// [InputControl(layout = "Button")] public int up;
        /// </code>
        /// </example>
        internal static string GetExpectedControlLayoutName(string composite, string part)
        {
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentNullException(nameof(composite));
            if (string.IsNullOrEmpty(part))
                throw new ArgumentNullException(nameof(part));

            var compositeType = s_Composites.LookupTypeRegistration(composite);
            if (compositeType == null)
                return null;

            ////TODO: allow it being properties instead of just fields
            var field = compositeType.GetField(part,
                BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
            if (field == null)
                return null;

            var attribute = field.GetCustomAttribute<InputControlAttribute>(false);
            return attribute?.layout;
        }

        internal static IEnumerable<string> GetPartNames(string composite)
        {
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentNullException(nameof(composite));

            var compositeType = s_Composites.LookupTypeRegistration(composite);
            if (compositeType == null)
                yield break;

            foreach (var field in compositeType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var controlAttribute = field.GetCustomAttribute<InputControlAttribute>();
                if (controlAttribute != null)
                    yield return field.Name;
            }
        }
    }

    /// <summary>
    /// A binding composite arranges several bindings such that they form a "virtual control".
    /// </summary>
    /// <remarks>
    /// Composites are useful for arranging controls on a given device in a way
    /// that is not present on the device itself. A keyboard, for example, has no
    /// inherent way of controlling a 2D planar motion vector, for example. However,
    /// a WASD-style key arrangement is commonly used to achieve just that kind of
    /// control.
    ///
    /// Composites join several controls together such that they ultimately deliver
    /// a single value.
    /// </remarks>
    /// <typeparam name="TValue">Type of value computed by the composite.</typeparam>
    /// <example>
    /// <code>
    /// // A composite that uses two buttons to emulate a radial dial control.
    /// // Yields values in degrees.
    /// class ButtonDialComposite : InputBindingComposite&lt;float&gt;
    /// {
    ///     ////TODO
    /// }
    /// </code>
    /// </example>
    public abstract class InputBindingComposite<TValue> : InputBindingComposite
        where TValue : struct
    {
        public override Type valueType => typeof(TValue);

        public override int valueSizeInBytes => UnsafeUtility.SizeOf<TValue>();

        public abstract TValue ReadValue(ref InputBindingCompositeContext context);

        public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var valueSize = valueSizeInBytes;
            if (bufferSize < valueSize)
                throw new ArgumentException(
                    $"Expected buffer of at least {valueSizeInBytes} bytes but got buffer of only {bufferSize} bytes instead",
                    nameof(bufferSize));

            var value = ReadValue(ref context);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            UnsafeUtility.MemCpy(buffer, valuePtr, valueSize);
        }

        public override unsafe object ReadValueAsObject(ref InputBindingCompositeContext context)
        {
            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            ReadValue(ref context, valuePtr, UnsafeUtility.SizeOf<TValue>());

            return value;
        }
    }
}
