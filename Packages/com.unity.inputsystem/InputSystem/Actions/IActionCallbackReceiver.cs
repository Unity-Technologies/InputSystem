////TODO: nuke this and come up with a better mechanism to centrally handle callbacks

namespace UnityEngine.Experimental.Input
{
    public interface IInputActionCallbackReceiver
    {
        void OnActionTriggered(ref InputAction.CallbackContext context);
    }
}
