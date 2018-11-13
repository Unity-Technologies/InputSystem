using System;
using System.Reflection;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////REVIEW: isn't this about arbitrary value processing? can we open this up more and make it
////        not just be about composing multiple bindings?

////REVIEW: why not just name this IInputBinding and have AxisBinding, DpadBinding, etc?

////REVIEW: should composites be able to nest?

////REVIEW: when we get blittable type constraints, we can probably do away with the pointer-based ReadValue version

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A binding composite arranges several bindings such that they form
    /// a "virtual control".
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
    /// class ButtonDialComposite : IInputBindingComposite<float>
    /// {
    ///     ////TODO
    /// }
    /// </code>
    /// </example>
    public interface IInputBindingComposite<TValue> : IInputBindingComposite
        where TValue : struct
    {
        TValue ReadValue(ref InputBindingCompositeContext context);
    }

    public interface IInputBindingComposite
    {
        Type valueType { get; }
        int valueSizeInBytes { get; }
        unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize);
    }

    internal static class InputBindingComposite
    {
        public static TypeTable s_Composites;

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
        public static string GetExpectedControlLayoutName(string composite, string part)
        {
            if (string.IsNullOrEmpty(composite))
                throw new ArgumentNullException("composite");
            if (string.IsNullOrEmpty(part))
                throw new ArgumentNullException("part");

            var compositeType = s_Composites.LookupTypeRegistration(composite);
            if (compositeType == null)
                return null;

            ////TODO: allow it being properties instead of just fields
            var field = compositeType.GetField(part,
                BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
            if (field == null)
                return null;

            var attribute = field.GetCustomAttribute<InputControlAttribute>(false);
            if (attribute == null)
                return null;

            return attribute.layout;
        }
    }
}
