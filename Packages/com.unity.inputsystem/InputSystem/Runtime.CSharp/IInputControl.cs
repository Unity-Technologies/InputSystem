using Unity.InputSystem.Runtime;

namespace Unity.InputSystem
{
    public interface IInputControl
    {
        InputControlTypeRef controlTypeRef { get; }

        InputControlRef controlRef { get; }
    }
}