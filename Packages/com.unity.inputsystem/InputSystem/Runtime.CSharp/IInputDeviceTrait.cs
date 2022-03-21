using Unity.InputSystem.Runtime;

namespace Unity.InputSystem
{

    public interface IInputDeviceTrait
    {
        InputDeviceTraitRef traitRef { get; }
        InputDeviceRef deviceRef { get; }
    }
}