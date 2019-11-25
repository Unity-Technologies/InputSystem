using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Contextual data made available when processing values of composite bindings.
    /// </summary>
    /// <remarks>
    /// An instance of this struct is passed to <see
    /// cref="InputBindingComposite{TValue}.ReadValue(InputBindingComposite)"/>.
    /// Use it to access contextual data such as the value for individual part bindings.
    ///
    /// Note that an instance of this struct should never be held on to past the duration
    /// of the call to <c>ReadValue</c>. The data it retrieves is only valid during
    /// the callback.
    /// </remarks>
    /// <seealso cref="InputBindingComposite"/>
    /// <seealso cref="InputBindingComposite{TValue}"/>
    /// <seealso cref="InputBindingComposite{TValue}.ReadValue(ref InputBindingCompositeContext)"/>
    public struct InputBindingCompositeContext
    {
        /// <summary>
        /// Read the value of the giving part binding.
        /// </summary>
        /// <param name="partNumber">Number of the part to read. This is assigned
        /// automatically by the input system and should be treated as an opaque
        /// identifier. See the example below.</param>
        /// <typeparam name="TValue">Type of value to read. This must match the
        /// value type expected from controls bound to the part.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        /// <exception cref="InvalidOperationException">The given <typeparamref name="TValue"/>
        /// value type does not match the actual value type of the control(s) bound
        /// to the part.</exception>
        /// <remarks>
        /// If no control is bound to the given part, the return value will always
        /// be <c>default(TValue)</c>. If a single control is bound to the part, the
        /// value will be that of the control. If multiple controls are bound to a
        /// part, the return value will be that greatest one according to <c>IComparable</c>
        /// implemented by <typeparamref name="TValue"/>.
        ///
        /// Note that this method only works with values that are <c>IComparable</c>.
        /// To read a value type that is not <c>IComparable</c> or to supply a custom
        /// comparer, use <see cref="ReadValue{TValue,TComparer}(int,TComparer)"/>.
        ///
        /// If an invalid <paramref name="partNumber"/> is supplied, the return value
        /// will simply be <c>default(TValue)</c>. No exception is thrown.
        ///
        /// <example>
        /// <code>
        /// public class MyComposite : InputBindingComposite&lt;float&gt;
        /// {
        ///     // Defines a "part" binding for the composite. Each part can be
        ///     // bound to arbitrary many times (including not at all). The "layout"
        ///     // property of the attribute we supply determines what kind of
        ///     // control is expected to be bound to the part.
        ///     //
        ///     // When initializing a composite instance, the input system will
        ///     // automatically assign part numbers and store them in the fields
        ///     // we define here.
        ///     [InputControl(layout = "Button")]
        ///     public int firstPart;
        ///
        ///     // Defines a second part.
        ///     [InputControl(layout = "Vector2")]
        ///     public int secondPart;
        ///
        ///     public override float ReadValue(ref InputBindingCompositeContext context)
        ///     {
        ///         // Read the button.
        ///         var firstValue = context.ReadValue&lt;float&gt;();
        ///
        ///         // Read the vector.
        ///         var secondValue = context.ReadValue&lt;Vector2&gt;();
        ///
        ///         // Perform some computation based on the inputs. Here, we just
        ///         // scale the vector by the value we got from the button.
        ///         return secondValue * firstValue;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ReadValue{TValue,TComparer}(int,TComparer)"/>
        /// <seealso cref="InputControl{TValue}.ReadValue"/>
        public unsafe TValue ReadValue<TValue>(int partNumber)
            where TValue : struct, IComparable<TValue>
        {
            if (m_State == null)
                return default;

            return m_State.ReadCompositePartValue<TValue, DefaultComparer<TValue>>
                    (m_BindingIndex, partNumber, null, out _);
        }

        /// <summary>
        /// Same as <see cref="ReadValue{TValue}(int)"/> but also return the control
        /// from which the value was read.
        /// </summary>
        /// <param name="partNumber">Number of the part to read. This is assigned
        /// automatically by the input system and should be treated as an opaque
        /// identifier.</param>
        /// <param name="sourceControl">Receives the <see cref="InputControl"/> from
        /// which the value was read. If multiple controls are bound to the given part,
        /// this is the control whose value was ultimately selected. Will be set to
        /// <c>null</c> if <paramref name="partNumber"/> is not a valid part or if no
        /// controls are bound to the part.</param>
        /// <typeparam name="TValue">Type of value to read. This must match the
        /// value type expected from controls bound to the part.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        /// <remarks>
        /// Like <see cref="ReadValue{TValue}(int)"/>, this method relies on using <c>IComparable</c>
        /// implemented by <typeparamref name="TValue"/> to determine the greatest value
        /// if multiple controls are bound to the specified part.
        /// </remarks>
        /// <seealso cref="ReadValue{TValue}(int)"/>
        public unsafe TValue ReadValue<TValue>(int partNumber, out InputControl sourceControl)
            where TValue : struct, IComparable<TValue>
        {
            if (m_State == null)
            {
                sourceControl = null;
                return default;
            }

            var value = m_State.ReadCompositePartValue<TValue, DefaultComparer<TValue>>(m_BindingIndex, partNumber,
                null, out var controlIndex);
            if (controlIndex != InputActionState.kInvalidIndex)
                sourceControl = m_State.controls[controlIndex];
            else
                sourceControl = null;

            return value;
        }

        /// <summary>
        /// Read the value of the given part bindings and use the given <paramref name="comparer"/>
        /// to determine which value to return if multiple controls are bound to the part.
        /// </summary>
        /// <param name="partNumber">Number of the part to read. This is assigned
        /// automatically by the input system and should be treated as an opaque
        /// identifier.</param>
        /// <param name="comparer">Instance of <typeparamref name="TComparer"/> for comparing
        /// multiple values.</param>
        /// <typeparam name="TValue">Type of value to read. This must match the
        /// value type expected from controls bound to the part.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        /// <typeparam name="TComparer">Comparer to use if multiple controls are bound to
        /// the given part. All values will be compared using <c>TComparer.Compare</c> and
        /// the greatest value will be returned.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        /// <remarks>
        /// This method is a useful alternative to <see cref="ReadValue{TValue}(int)"/> for
        /// value types that do not implement <c>IComparable</c> or when the default comparison
        /// behavior is undesirable.
        ///
        /// <example>
        /// <code>
        /// public class CompositeWithVector2Part : InputBindingComposite&lt;Vector2&gt;
        /// {
        ///     [InputControl(layout = "Vector2")]
        ///     public int part;
        ///
        ///     public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        ///     {
        ///         // Return the Vector3 with the greatest magnitude.
        ///         return context.ReadValue&lt;Vector2, Vector2MagnitudeComparer&gt;(part);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Utilities.Vector2MagnitudeComparer"/>
        /// <seealso cref="Utilities.Vector3MagnitudeComparer"/>
        public unsafe TValue ReadValue<TValue, TComparer>(int partNumber, TComparer comparer = default)
            where TValue : struct
            where TComparer : IComparer<TValue>
        {
            if (m_State == null)
                return default;

            return m_State.ReadCompositePartValue<TValue, TComparer>(
                m_BindingIndex, partNumber, null, out _, comparer);
        }

        /// <summary>
        /// Like <see cref="ReadValue{TValue,TComparer}(int,TComparer)"/> but also return
        /// the control from which the value has ultimately been read.
        /// </summary>
        /// <param name="partNumber">Number of the part to read. This is assigned
        /// automatically by the input system and should be treated as an opaque
        /// identifier.</param>
        /// <param name="sourceControl">Receives the <see cref="InputControl"/> from
        /// which the value was read. If multiple controls are bound to the given part,
        /// this is the control whose value was ultimately selected. Will be set to
        /// <c>null</c> if <paramref name="partNumber"/> is not a valid part or if no
        /// controls are bound to the part.</param>
        /// <param name="comparer">Instance of <typeparamref name="TComparer"/> for comparing
        /// multiple values.</param>
        /// <typeparam name="TValue">Type of value to read. This must match the
        /// value type expected from controls bound to the part.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        /// <typeparam name="TComparer">Comparer to use if multiple controls are bound to
        /// the given part. All values will be compared using <c>TComparer.Compare</c> and
        /// the greatest value will be returned.</typeparam>
        /// <returns>The value read from the part bindings.</returns>
        public unsafe TValue ReadValue<TValue, TComparer>(int partNumber, out InputControl sourceControl, TComparer comparer = default)
            where TValue : struct
            where TComparer : IComparer<TValue>
        {
            if (m_State == null)
            {
                sourceControl = null;
                return default;
            }

            var value = m_State.ReadCompositePartValue<TValue, TComparer>(m_BindingIndex, partNumber, null,
                out var controlIndex, comparer);

            if (controlIndex != InputActionState.kInvalidIndex)
                sourceControl = m_State.controls[controlIndex];
            else
                sourceControl = null;

            return value;
        }

        /// <summary>
        /// Like <see cref="ReadValue{TValue}(int)"/> but treat bound controls as buttons. This means
        /// that custom <see cref="Controls.ButtonControl.pressPoint"/> are respected and that floating-point
        /// values from non-ButtonControls will be compared to <see cref="InputSettings.defaultButtonPressPoint"/>.
        /// </summary>
        /// <param name="partNumber">Number of the part to read. This is assigned
        /// automatically by the input system and should be treated as an opaque
        /// identifier.</param>
        /// <returns>True if any button bound to the part is pressed.</returns>
        /// <remarks>
        /// This method expects all controls bound to the part to be of type <c>InputControl&lt;float&gt;</c>.
        ///
        /// This method is different from just calling <see cref="ReadValue{TValue}(int)"/> with a <c>float</c>
        /// parameter and comparing the result to <see cref="InputSettings.defaultButtonPressPoint"/> in that
        /// custom press points set on individual ButtonControls will be respected.
        /// </remarks>
        /// <seealso cref="Controls.ButtonControl"/>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        public unsafe bool ReadValueAsButton(int partNumber)
        {
            if (m_State == null)
                return default;

            var buttonValue = false;
            m_State.ReadCompositePartValue<float, DefaultComparer<float>>(m_BindingIndex, partNumber, &buttonValue,
                out _);
            return buttonValue;
        }

        internal InputActionState m_State;
        internal int m_BindingIndex;

        private struct DefaultComparer<TValue> : IComparer<TValue>
            where TValue : IComparable<TValue>
        {
            public int Compare(TValue x, TValue y)
            {
                return x.CompareTo(y);
            }
        }
    }
}
