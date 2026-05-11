namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control that can supply a custom actuation press threshold (and a resolved default) for
    /// press-style checks such as <see cref="UnityEngine.InputSystem.InputAction.IsPressed"/>.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="ButtonControl"/> and <see cref="Vector2Control"/> (including
    /// <see cref="StickControl"/>). A negative or zero <see cref="pressPoint"/> means "use default
    /// resolution" via <see cref="pressPointOrDefault"/>; see each type for details.
    /// </remarks>
    /// <seealso cref="ButtonControl"/>
    /// <seealso cref="Vector2Control"/>
    public interface IActuationPressPoint
    {
        /// <summary>
        /// Layout-configured press threshold, or a value less than or equal to zero when unset.
        /// </summary>
        float pressPoint { get; }

        /// <summary>
        /// Effective press threshold: <see cref="pressPoint"/> when set, otherwise the global default.
        /// </summary>
        float pressPointOrDefault { get; }
    }
}
