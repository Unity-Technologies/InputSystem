using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    // Allows to generically query information from a state struct.
    public interface IInputStateTypeInfo
    {
        FourCC GetFormat();
    }
}
