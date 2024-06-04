/*namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a source of bindings.
    /// </summary>
    /// <typeparam name="T">The associated type.</typeparam>
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
        private struct JumpSource : IInputBindingSource<InputEvent>
        {
            public void ApplyTo(BindableInput<InputEvent> target)
            {
                target.Bind(Device.Gamepad.buttonSouth.Pressed());
                target.Bind(Device.Keyboard.space.Pressed());
            }
        }

        /// <summary>
        /// General purpose cross-platform input binding preset. 
        /// </summary>
        /// <returns>Binding preset.</returns>
        public static IInputBindingSource<InputEvent> Jump()
        {
            return new JumpSource();
        }
    }
}*/