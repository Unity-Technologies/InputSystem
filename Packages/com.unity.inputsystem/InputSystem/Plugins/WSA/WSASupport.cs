#if UNITY_EDITOR || UNITY_WSA
using System.Linq;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;


namespace UnityEngine.InputSystem.Plugins.WSA
{
    public static class WSASupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<WSAScreenKeyboard>();
            InputSystem.AddDevice(InputDevice.Build<WSAScreenKeyboard>());
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
