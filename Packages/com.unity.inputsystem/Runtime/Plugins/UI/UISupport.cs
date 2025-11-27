using UnityEngine.InputSystem;

////FIXME: This should be UnityEngine.InputSystem.UI

#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
public
#else
internal
#endif
static class UISupport
{
    public static void Initialize()
    {
        InputSystem.RegisterLayout(@"
            {
                ""name"" : ""VirtualMouse"",
                ""extend"" : ""Mouse""
            }
        ");
    }
}
