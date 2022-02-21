using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface implemented by all input device state structs which reports the data format identifier of the state.
    /// </summary>
    public interface IInputStateTypeInfo
    {
        FourCC format { get; }
    }
}
