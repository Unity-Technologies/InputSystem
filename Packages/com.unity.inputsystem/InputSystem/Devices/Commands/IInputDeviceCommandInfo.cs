using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface implemented by all input device command structs which reports the data format identifier of the command.
    /// </summary>
    public interface IInputDeviceCommandInfo
    {
        FourCC typeStatic { get; }
    }
}
