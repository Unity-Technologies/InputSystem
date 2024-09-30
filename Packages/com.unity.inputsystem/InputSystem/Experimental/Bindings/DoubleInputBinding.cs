// This is an auto-generated source file. Any manual edits will be lost.
namespace UnityEngine.InputSystem.Experimental
{
    public class DoubleInputBinding : WrappedScriptableInputBinding<double> { }

    struct BootstrapDoubleInputBinding
    {
        [UnityEditor.InitializeOnLoadMethod]
        public static void Install()
        {
            ScriptableInputBinding.RegisterInputBindingType(typeof(double), typeof(DoubleInputBinding));
        }
    }
}

