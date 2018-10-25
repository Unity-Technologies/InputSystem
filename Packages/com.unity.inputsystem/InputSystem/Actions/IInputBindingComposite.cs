using System;
using UnityEngine.Experimental.Input.Utilities;

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
    }
}
