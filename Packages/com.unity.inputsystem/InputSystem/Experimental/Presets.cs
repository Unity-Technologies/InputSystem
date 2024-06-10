// TODO Generate code and documentation from preset assets. 
// TODO Generate exact binding descriptions from asset configuration.
// TODO Consider if more useful to have delegates than interfaces for this purpose to allow inlining and inline caching.

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a set of configured bindings.
    /// </summary>
    /// <typeparam name="T">The associated binding type.</typeparam>
    public interface IInputBindingConfiguration<T> where T : struct
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
        private struct MoveBindings : IInputBindingConfiguration<Vector2>
        {
            public void ApplyTo(BindableInput<Vector2> target)
            {
                target.Bind(Devices.Gamepad.leftStick);
                // TODO Keyboard composite
            }
        }
        
        private struct JumpBindings : IInputBindingConfiguration<InputEvent>
        {
            public void ApplyTo(BindableInput<InputEvent> target)
            {
                target.Bind(Devices.Gamepad.buttonEast.Pressed());
                target.Bind(Devices.Keyboard.space.Pressed());
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
        public static IInputBindingConfiguration<InputEvent> Jump()
        {
            return new JumpBindings();
        }
    }
}
