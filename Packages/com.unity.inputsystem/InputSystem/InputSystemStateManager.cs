using System;
using UnityEngine.InputSystem;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Snapshot of the state used by the input system.
    /// </summary>
    /// <remarks>
    /// Can be taken across domain reloads.
    /// </remarks>
    [Serializable]
    internal struct InputSystemState
    {
        [NonSerialized] public InputManager manager;
        [NonSerialized] public InputRemoting remote;
        [SerializeField] public RemoteInputPlayerConnection remoteConnection;
        [SerializeField] public InputManager.SerializedState managerState;
        [SerializeField] public InputRemoting.SerializedState remotingState;
#if UNITY_EDITOR
        [SerializeField] public InputEditorUserSettings.SerializedState userSettings;
        [SerializeField] public string systemObject;
#endif
        ////TODO: make these saved states capable of surviving domain reloads
        [NonSerialized] public ISavedState inputActionState;
        [NonSerialized] public ISavedState touchState;
        [NonSerialized] public ISavedState inputUserState;
    }

    // ISX-1860 - #ifdef out Domain Reload specific functionality from CoreCLR
#if UNITY_EDITOR
    /// <summary>
    /// A hidden, internal object we put in the editor to bundle input system state
    /// and help us survive domain reloads.
    /// </summary>
    /// <remarks>
    /// Player doesn't need this stuff because there's no domain reloads to survive, and
    /// also doesn't have domain reloads.
    /// </remarks>
    internal class InputSystemStateManager : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// References the "core" input state that must survive domain reloads.
        /// </summary>
        [SerializeField] public InputSystemState systemState;

        /// <summary>
        /// Triggers Editor restart when enabling NewInput back-ends.
        /// </summary>
        [SerializeField] public bool newInputBackendsCheckedAsEnabled;

        /// <summary>
        /// Saves and restores InputSettings across domain reloads
        /// </summary>
        /// <remarks>
        /// InputSettings are serialized to JSON which this string holds.
        /// </remarks>
        [SerializeField] public string settings;

        /// <summary>
        /// Timestamp retrieved from InputRuntime.currentTime when exiting EditMode.
        /// </summary>
        /// <remarks>
        /// All input events occurring between exiting EditMode and entering PlayMode are discarded.
        /// </remarks>
        [SerializeField] public double exitEditModeTime;

        /// <summary>
        /// Timestamp retrieved from InputRuntime.currentTime when entering PlayMode.
        /// </summary>
        /// <remarks>
        /// All input events occurring between exiting EditMode and entering PlayMode are discarded.
        /// </remarks>
        [SerializeField] public double enterPlayModeTime;

        public void OnBeforeSerialize()
        {
            // Save current system state.
            systemState.manager = InputSystem.manager;
            systemState.remote = InputSystem.remoting;
            systemState.remoteConnection = InputSystem.remoteConnection;
            systemState.managerState = InputSystem.manager.SaveState();
            systemState.remotingState = InputSystem.remoting.SaveState();
            systemState.userSettings = InputEditorUserSettings.s_Settings;
        }

        public void OnAfterDeserialize()
        {
        }
    }
#endif // UNITY_EDITOR
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
}
