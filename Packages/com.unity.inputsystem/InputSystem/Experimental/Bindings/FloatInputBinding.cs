// This is an auto-generated source file. Any manual edits will be lost.
namespace UnityEngine.InputSystem.Experimental
{
    public class FloatInputBinding : WrappedScriptableInputBinding<float> { }

    struct BootstrapFloatInputBinding
    {
        [UnityEditor.InitializeOnLoadMethod]
        public static void Install()
        {
            ScriptableInputBinding.RegisterInputBindingType(typeof(float), typeof(FloatInputBinding));
        }
    }
}

