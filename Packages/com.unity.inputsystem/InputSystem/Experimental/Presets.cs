// TODO Generate code and documentation from preset assets. 
// TODO Generate exact binding descriptions from asset configuration.
// TODO Consider if more useful to have delegates than interfaces for this purpose to allow inlining and inline caching.

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a source of input bindings.
    /// </summary>
    /// <typeparam name="T">The associated binding value type.</typeparam>
    public interface IInputBindingSource<T> where T : struct
    {
        /// <summary>
        /// Applies associated bindings to the given target.
        /// </summary>
        /// <param name="target">The target for which to apply bindings.</param>
        public void ApplyTo(BindableInput<T> target);
    }

    /// <summary>
    /// Provides input binding presents.
    /// </summary>
    public static class Presets
    {

    }
}
