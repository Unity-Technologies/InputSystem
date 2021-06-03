using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: support nested composites

////REVIEW: composites probably need a reset method, too (like interactions), so that they can be stateful

////REVIEW: isn't this about arbitrary value processing? can we open this up more and make it
////        not just be about composing multiple bindings?

////REVIEW: when we get blittable type constraints, we can probably do away with the pointer-based ReadValue version

namespace UnityEngine.InputSystem
{
    ////TODO: clarify whether this can have state or not
    /// <summary>
    /// A binding that synthesizes a value from from several component bindings.
    /// </summary>
    /// <remarks>
    /// This is the base class for composite bindings. See <see cref="InputBindingComposite{TValue}"/>
    /// for more details about composites and for how to define custom composites.
    /// </remarks>
    /// <seealso cref="InputSystem.RegisterBindingComposite{T}"/>
    [Preserve]
    public abstract class InputBindingComposite
    {
        /// <summary>
        /// The type of value returned by the composite.
        /// </summary>
        /// <value>Type of value returned by the composite.</value>
        /// <remarks>
        /// Just like each <see cref="InputControl"/> has a specific type of value it
        /// will return, each composite has a specific type of value it will return.
        /// This is usually implicitly defined by the type parameter of <see
        /// cref="InputBindingComposite{TValue}"/>.
        /// </remarks>
        /// <seealso cref="InputControl.valueType"/>
        /// <seealso cref="InputAction.CallbackContext.valueType"/>
        public abstract Type valueType { get; }

        /// <summary>
        /// Size of a value read by <see cref="ReadValue"/>.
        /// </summary>
        /// <value>Size of values stored in memory buffers by <see cref="ReadValue"/>.</value>
        /// <remarks>
        /// This is usually implicitly defined by the size of values derived
        /// from the type argument to <see cref="InputBindingComposite{TValue}"/>. E.g.
        /// if the type argument is <c>Vector2</c>, this property will be 8.
        /// </remarks>
        /// <seealso cref="InputControl.valueSizeInBytes"/>
        /// <seealso cref="InputAction.CallbackContext.valueSizeInBytes"/>
        public abstract int valueSizeInBytes { get; }

        /// <summary>
        /// Read a value from the composite without having to know the value type (unlike
        /// <see cref="InputBindingComposite{TValue}.ReadValue(ref InputBindingCompositeContext)"/> and
        /// without allocating GC heap memory (unlike <see cref="ReadValueAsObject"/>).
        /// </summary>
        /// <param name="context">Callback context for the binding composite. Use this
        /// to access the values supplied by part bindings.</param>
        /// <param name="buffer">Buffer that receives the value read for the composite.</param>
        /// <param name="bufferSize">Size of the buffer allocated at <paramref name="buffer"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is smaller than
        /// <see cref="valueSizeInBytes"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This API will be used if someone calls <see cref="InputAction.CallbackContext.ReadValue(void*,int)"/>
        /// with the action leading to the composite.
        ///
        /// By deriving from <see cref="InputBindingComposite{TValue}"/>, this will automatically
        /// be implemented for you.
        /// </remarks>
        /// <seealso cref="InputAction.CallbackContext.ReadValue"/>
        public abstract unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize);

        /// <summary>
        /// Read the value of the composite as a boxed object. This allows reading the value
        /// without having to know the value type and without having to deal with raw byte buffers.
        /// </summary>
        /// <param name="context">Callback context for the binding composite. Use this
        /// to access the values supplied by part bindings.</param>
        /// <returns>The current value of the composite according to the state passed in through
        /// <paramref name="context"/>.</returns>
        /// <remarks>
        /// This API will be used if someone calls <see cref="InputAction.CallbackContext.ReadValueAsObject"/>
        /// with the action leading to the composite.
        ///
        /// By deriving from <see cref="InputBindingComposite{TValue}"/>, this will automatically
        /// be implemented for you.
        /// </remarks>
        public abstract object ReadValueAsObject(ref InputBindingCompositeContext context);

        /// <summary>
        /// Determine the current level of actuation of the composite.
        /// </summary>
        /// <param name="context">Callback context for the binding composite. Use this
        /// to access the values supplied by part bindings.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method by default returns -1, meaning that the composite does not support
        /// magnitudes. You can override the method to add support for magnitudes.
        ///
        /// See <see cref="InputControl.EvaluateMagnitude()"/> for details of how magnitudes
        /// work.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public virtual float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            return -1;
        }

        /// <summary>
        /// Called after binding resolution for an <see cref="InputActionMap"/> is complete.
        /// </summary>
        /// <remarks>
        /// Some composites do not have predetermine value types. Two examples of this are
        /// <see cref="Composites.OneModifierComposite"/> and <see cref="Composites.TwoModifiersComposite"/>, which
        /// both have a <c>"binding"</c> part that can be bound to arbitrary controls. This means that the
        /// value type of these bindings can only be determined at runtime.
        ///
        /// Overriding this method allows accessing the actual controls bound to each part
        /// at runtime.
        ///
        /// <example>
        /// <code>
        /// [InputControl] public int binding;
        ///
        /// protected override void FinishSetup(ref InputBindingContext context)
        /// {
        ///     // Get all controls bound to the 'binding' part.
        ///     var controls = context.controls
        ///         .Where(x => x.part == binding)
        ///         .Select(x => x.control);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        protected virtual void FinishSetup(ref InputBindingCompositeContext context)
        {
        }

        // Avoid having to expose internal modifier.
        internal void CallFinishSetup(ref InputBindingCompositeContext context)
        {
            FinishSetup(ref context);
        }

        internal static TypeTable s_Composites;

        internal static Type GetValueType(string composite)
        {
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentNullException(nameof(composite));

            var compositeType = s_Composites.LookupTypeRegistration(composite);
            if (compositeType == null)
                return null;

            return TypeHelpers.GetGenericTypeArgumentFromHierarchy(compositeType, typeof(InputBindingComposite<>), 0);
        }

        /// <summary>
        /// Return the name of the control layout that is expected for the given part (e.g. "Up") on the given
        /// composite (e.g. "Dpad").
        /// </summary>
        /// <param name="composite">Registration name of the composite.</param>
        /// <param name="part">Name of the part.</param>
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
        public static string GetExpectedControlLayoutName(string composite, string part)
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

        internal static string GetDisplayFormatString(string composite)
        {
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentNullException(nameof(composite));

            var compositeType = s_Composites.LookupTypeRegistration(composite);
            if (compositeType == null)
                return null;

            var displayFormatAttribute = compositeType.GetCustomAttribute<DisplayStringFormatAttribute>();
            if (displayFormatAttribute == null)
                return null;

            return displayFormatAttribute.formatString;
        }
    }

    /// <summary>
    /// A binding composite arranges several bindings such that they form a "virtual control".
    /// </summary>
    /// <typeparam name="TValue">Type of value returned by the composite. This must be a "blittable"
    /// type, that is, a type whose values can simply be copied around.</typeparam>
    /// <remarks>
    /// Composite bindings are a special type of <see cref="InputBinding"/>. Whereas normally
    /// an input binding simply references a set of controls and returns whatever input values are
    /// generated by those controls, a composite binding sources input from several controls and
    /// derives a new value from that.
    ///
    /// A good example for that is a classic WASD keyboard binding:
    ///
    /// <example>
    /// <code>
    /// var moveAction = new InputAction(name: "move");
    /// moveAction.AddCompositeBinding("Vector2")
    ///     .With("Up", "&lt;Keyboard&gt;/w")
    ///     .With("Down", "&lt;Keyboard&gt;/s")
    ///     .With("Left", "&lt;Keyboard&gt;/a")
    ///     .With("Right", "&lt;Keyboard&gt;/d")
    /// </code>
    /// </example>
    ///
    /// Here, each direction is represented by a separate binding. "Up" is bound to "W", "Down"
    /// is bound to "S", and so on. Each direction individually returns a 0 or 1 depending
    /// on whether it is pressed or not.
    ///
    /// However, as a composite, the binding to the "move" action returns a combined <c>Vector2</c>
    /// that is computed from the state of each of the directional controls. This is what composites
    /// do. They take inputs from their "parts" to derive an input for the binding as a whole.
    ///
    /// Note that the properties and methods defined in <see cref="InputBindingComposite"/> and this
    /// class will generally be called internally by the input system and are not generally meant
    /// to be called directly from user land.
    ///
    /// The set of composites available in the system is extensible. While some composites are
    /// such as <see cref="Composites.Vector2Composite"/> and <see cref="Composites.ButtonWithOneModifier"/>
    /// are available out of the box, new composites can be implemented by anyone and simply be
    /// registered with <see cref="InputSystem.RegisterBindingComposite{T}"/>.
    ///
    /// See the "Custom Composite" sample (can be installed from package manager UI) for a detailed example
    /// of how to create a custom composite.
    /// </remarks>
    /// <seealso cref="InputSystem.RegisterBindingComposite{T}"/>
    [Preserve]
    public abstract class InputBindingComposite<TValue> : InputBindingComposite
        where TValue : struct
    {
        /// <summary>
        /// The type of value returned by the composite, i.e. <c>typeof(TValue)</c>.
        /// </summary>
        /// <value>Returns <c>typeof(TValue)</c>.</value>
        public override Type valueType => typeof(TValue);

        /// <summary>
        /// The size of values returned by the composite, i.e. <c>sizeof(TValue)</c>.
        /// </summary>
        /// <value>Returns <c>sizeof(TValue)</c>.</value>
        public override int valueSizeInBytes => UnsafeUtility.SizeOf<TValue>();

        /// <summary>
        /// Read a value for the composite given the supplied context.
        /// </summary>
        /// <param name="context">Callback context for the binding composite. Use this
        /// to access the values supplied by part bindings.</param>
        /// <returns>The current value of the composite according to the state made
        /// accessible through <paramref name="context"/>.</returns>
        /// <remarks>
        /// This is the main method to implement in custom composites.
        ///
        /// <example>
        /// <code>
        /// public class CustomComposite : InputBindingComposite&lt;float&gt;
        /// {
        ///     [InputControl(layout = "Button")]
        ///     public int button;
        ///
        ///     public float scaleFactor = 1;
        ///
        ///     public override float ReadValue(ref InputBindingComposite context)
        ///     {
        ///         return context.ReadValue&lt;float&gt;(button) * scaleFactor;
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// The other method to consider overriding is <see cref="InputBindingComposite.EvaluateMagnitude"/>.
        /// </remarks>
        /// <seealso cref="InputAction.ReadValue{TValue}"/>
        /// <seealso cref="InputAction.CallbackContext.ReadValue{TValue}"/>
        public abstract TValue ReadValue(ref InputBindingCompositeContext context);

        /// <inheritdoc />
        public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var valueSize = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < valueSize)
                throw new ArgumentException(
                    $"Expected buffer of at least {UnsafeUtility.SizeOf<TValue>()} bytes but got buffer of only {bufferSize} bytes instead",
                    nameof(bufferSize));

            var value = ReadValue(ref context);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            UnsafeUtility.MemCpy(buffer, valuePtr, valueSize);
        }

        /// <inheritdoc />
        public override unsafe object ReadValueAsObject(ref InputBindingCompositeContext context)
        {
            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            ReadValue(ref context, valuePtr, UnsafeUtility.SizeOf<TValue>());

            return value;
        }
    }
}
