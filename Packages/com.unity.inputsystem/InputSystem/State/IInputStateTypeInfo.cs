using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    // Allows to generically query information from a state struct.
    public interface IInputStateTypeInfo
    {
        FourCC GetFormat();
    }
}
