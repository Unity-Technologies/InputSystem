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
    public static partial class Presets
    {
        private struct MoveBindings : IInputBindingSource<Vector2>
        {
            public void ApplyTo(BindableInput<Vector2> target)
            {
                target.Bind(Devices.Gamepad.LeftStick); // Allows explicit type
                // TODO Keyboard composite
            }
        }
        
        private struct JumpBindings : IInputBindingSource<InputEvent>
        {
            public void ApplyTo(BindableInput<InputEvent> target)
            {
                target.Bind(Devices.Gamepad.ButtonEast.Pressed());
                target.Bind(Devices.Keyboard.Space.Pressed());
            }
        }

        /// <summary>
        /// General purpose cross-platform input binding preset for most common interaction to perform a Jump in games.
        /// </summary>
        /// <remarks>
        /// Binds the following controls:
        /// - Press (X) to jump on DualShock or DualSense controller.
        /// - Press (A) to jump on XBox controller.
        /// </remarks>
        /// <returns>Binding preset.</returns>
        public static IInputBindingSource<InputEvent> Jump()
        {
            return new JumpBindings();
        }
    }
}
