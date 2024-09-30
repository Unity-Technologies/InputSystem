// This is an auto-generated source file. Any manual edits will be lost.
namespace UnityEngine.InputSystem.Experimental
{
    public class BoolInputBinding : WrappedScriptableInputBinding<bool> { }

    struct BootstrapBoolInputBinding
    {
        [UnityEditor.InitializeOnLoadMethod]
        public static void Install()
        {
            ScriptableInputBinding.RegisterInputBindingType(typeof(bool), typeof(BoolInputBinding));
        }
    }
}

