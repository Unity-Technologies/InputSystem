using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface implemented by all input device state structs which reports the data format identifier of the state.
    /// </summary>
    public interface IInputStateTypeInfo
    {
        /// <summary>The FourCC format code identifying this state struct type.</summary>
        FourCC format { get; }
    }
}
