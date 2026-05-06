using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    // Allows retrieving information about event types from an instance of the type.
    // As structs can always be default instantiated, this allows us to get data on the struct
    // from an instance of the struct without having to go through vtable dispatches.
    /// <summary>
    /// Interface implemented by all input event structs which reports the data format identifier of the command.
    /// </summary>
    public interface IInputEventTypeInfo
    {
        FourCC typeStatic { get; }
    }
}
