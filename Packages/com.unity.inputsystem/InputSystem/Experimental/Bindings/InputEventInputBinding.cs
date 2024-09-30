// This is an auto-generated source file. Any manual edits will be lost.
namespace UnityEngine.InputSystem.Experimental
{
    public class InputEventInputBinding : WrappedScriptableInputBinding<InputEvent> { }

    struct BootstrapInputEventInputBinding
    {
        [UnityEditor.InitializeOnLoadMethod]
        public static void Install()
        {
            ScriptableInputBinding.RegisterInputBindingType(typeof(InputEvent), typeof(InputEventInputBinding));
        }
    }
}

