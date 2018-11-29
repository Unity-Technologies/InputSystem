#if UNITY_EDITOR || UNITY_WSA
using System.Linq;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;


namespace UnityEngine.Experimental.Input.Plugins.WSA
{
    public static class WSASupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<WSAScreenKeyboard>();
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
