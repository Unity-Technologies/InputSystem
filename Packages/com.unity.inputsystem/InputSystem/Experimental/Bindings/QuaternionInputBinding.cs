// This is an auto-generated source file. Any manual edits will be lost.
namespace UnityEngine.InputSystem.Experimental
{
    public class QuaternionInputBinding : WrappedScriptableInputBinding<Quaternion> { }

    struct BootstrapQuaternionInputBinding
    {
        [UnityEditor.InitializeOnLoadMethod]
        public static void Install()
        {
            ScriptableInputBinding.RegisterInputBindingType(typeof(Quaternion), typeof(QuaternionInputBinding));
        }
    }
}

