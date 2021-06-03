#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem
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
        [SerializeField] public string settings;
        [SerializeField] public double exitEditModeTime;
        [SerializeField] public double enterPlayModeTime;

        public void OnBeforeSerialize()
        {
            // Save current system state.
            systemState.manager = InputSystem.s_Manager;
            systemState.remote = InputSystem.s_Remote;
            systemState.remoteConnection = InputSystem.s_RemoteConnection;
            systemState.managerState = InputSystem.s_Manager.SaveState();
            systemState.remotingState = InputSystem.s_Remote.SaveState();
            systemState.userSettings = InputEditorUserSettings.s_Settings;
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
#endif // UNITY_EDITOR
