#if UNITY_EDITOR
using System;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A hidden object we put in the editor to bundle input system state
    /// and help us survive domain relods.
    /// </summary>
    /// <remarks>
    /// Player doesn't need this stuff because there's no domain reloads to survive.
    /// </remarks>
    internal class InputSystemObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] public InputManager manager;
        [NonSerialized] public InputRemoting remote;
        [SerializeField] public RemoteInputPlayerConnection playerConnection;
        [SerializeField] public bool newInputBackendsCheckedAsEnabled;

        [SerializeField] private InputRemoting.SerializedState m_RemotingState;

        public void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;

            manager = new InputManager();
            manager.Initialize(NativeInputRuntime.instance);

            // In the editor, we always set up for remoting.
            // NOTE: We use delayCall as our initial startup will run in editor initialization before
            //       PlayerConnection is itself ready. If we call SetupRemote() directly here, we won't
            //       see any errors but the callbacks we register for will not trigger.
            EditorApplication.delayCall += SetUpRemoting;
        }

        public void ReviveAfterDomainReload()
        {
            manager.InstallRuntime(NativeInputRuntime.instance);
            manager.InstallGlobals();
            SetUpRemoting();
        }

        private void SetUpRemoting()
        {
            remote = new InputRemoting(manager);
            remote.RestoreState(m_RemotingState, manager);

            if (playerConnection != null)
                DestroyImmediate(playerConnection);

            playerConnection = CreateInstance<RemoteInputPlayerConnection>();

            remote.Subscribe(playerConnection); // Feed messages from players into editor.
            playerConnection.Subscribe(remote); // Feed messages from editor into players.

            playerConnection.Bind(EditorConnection.instance, false);

            // We don't enable sending on the editor's remote by default.
            // By default, the editor acts as a receiver only.

            m_RemotingState = new InputRemoting.SerializedState();
        }

        public void OnDestroy()
        {
            InputActionMapState.ResetGlobals();
            manager.Destroy();
            EditorInputControlLayoutCache.Clear();
            DestroyImmediate(playerConnection);
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
        }

        public void OnBeforeSerialize()
        {
            if (remote != null)
                m_RemotingState = remote.SaveState();
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
#endif // UNITY_EDITOR
