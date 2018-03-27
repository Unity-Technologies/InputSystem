using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    // Allows retrieving information about event types from an instance of the type.
    // As structs can always be default instantiated, this allows us to get data on the struct
    // from an instance of the struct without having to go through vtable dispatches.
    public interface IInputEventTypeInfo
    {
        FourCC GetTypeStatic();
    }
}
