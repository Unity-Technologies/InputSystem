namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface to allow custom input devices to receive callbacks when the input system is updated.
    /// </summary>
    /// <remarks>
    /// If an <see cref="InputDevice"/> class implements the IInputUpdateCallbackReceiver interface, any instance of the
    /// InputDevice will have it's <see cref="OnUpdate"/> method called whenever the input system updates. This can be used
    /// to implement custom state update logic for virtual input devices which track some state in the project.
    /// </remarks>
    public interface IInputUpdateCallbackReceiver
    {
        void OnUpdate();
    }
}
