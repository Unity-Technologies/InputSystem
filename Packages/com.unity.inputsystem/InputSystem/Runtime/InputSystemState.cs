using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
#if DEBUG
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
#endif
}
