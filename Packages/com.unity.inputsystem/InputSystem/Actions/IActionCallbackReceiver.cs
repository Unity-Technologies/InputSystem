namespace UnityEngine.Experimental.Input
{
    public interface IInputActionCallbackReceiver
    {
        void OnActionTriggered(ref InputAction.CallbackContext context);
    }
}
