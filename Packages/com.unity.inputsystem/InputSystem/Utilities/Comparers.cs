using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Compare two <see cref="Vector2"/> by magnitude.
    /// </summary>
    /// <example>
    /// <code>
    /// </code>
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
    /// </example>
    public struct Vector2MagnitudeComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 x, Vector2 y)
        {
            var lenx = x.sqrMagnitude;
            var leny = y.sqrMagnitude;

            if (lenx < leny)
                return -1;
            if (lenx > leny)
                return 1;
            return 0;
        }
    }

    /// <summary>
    /// Compare two <see cref="Vector3"/> by magnitude.
    /// </summary>
    /// <example>
    /// <code>
    /// </code>
    /// public class CompositeWithVector3Part : InputBindingComposite&lt;Vector3&gt;
    /// {
    ///     [InputControl(layout = "Vector3")]
    ///     public int part;
    ///
    ///     public override Vector3 ReadValue(ref InputBindingCompositeContext context)
    ///     {
    ///         // Return the Vector3 with the greatest magnitude.
    ///         return context.ReadValue&lt;Vector3, Vector2MagnitudeComparer&gt;(part);
    ///     }
    /// }
    /// </example>
    public struct Vector3MagnitudeComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 x, Vector3 y)
        {
            var lenx = x.sqrMagnitude;
            var leny = y.sqrMagnitude;

            if (lenx < leny)
                return -1;
            if (lenx > leny)
                return 1;
            return 0;
        }
    }
}
