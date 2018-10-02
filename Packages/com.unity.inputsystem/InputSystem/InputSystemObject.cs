#if UNITY_EDITOR
namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A hidden, internal object we put in the editor to bundle input system state
    /// and help us survive domain reloads.
    /// </summary>
    /// <remarks>
    /// Player doesn't need this stuff because there's no domain reloads to survive.
    /// </remarks>
    internal class InputSystemObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] public InputSystem.State systemState;
        [SerializeField] public bool newInputBackendsCheckedAsEnabled;

        public void OnBeforeSerialize()
        {
            // Save current system state.
            systemState.manager = InputSystem.s_Manager;
            systemState.remote = InputSystem.s_Remote;
            systemState.remoteConnection = InputSystem.s_RemoteConnection;
            systemState.managerState = InputSystem.s_Manager.SaveState();
            systemState.remotingState = InputSystem.s_Remote.SaveState();
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
#endif // UNITY_EDITOR
