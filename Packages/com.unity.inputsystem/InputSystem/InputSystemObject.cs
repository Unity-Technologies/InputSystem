#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;

namespace UnityEngine.Experimental.Input
{
    // A hidden object we put in the editor to bundle input system state
    // and help us survive domain relods.
    // Player doesn't need this stuff because there's no domain reloads to
    // survive.
    internal class InputSystemObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] public InputManager manager;
        [NonSerialized] public InputRemoting remote;
        [SerializeField] public RemoteInputPlayerConnection playerConnection;
        [SerializeField] private bool m_OldInputSystemWarningTriggered;

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

        private SerializedObject GetSerializedPlayerSettings()
        {
            return new SerializedObject(Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault());
        }

        public bool IsNewInputSystemActiveInPlayerSettings()
        {
            var serializedPlayerSettings = GetSerializedPlayerSettings();

            if (serializedPlayerSettings != null)
                return serializedPlayerSettings.FindProperty("enableNativePlatformBackendsForNewInputSystem").boolValue;

            return true;
        }

        public void DisplayNativeBackendsDisabledWarningDialog()
        {
            const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings." +
                "This means that no input from native devices will come through." +
                "\n\nDo you want to enable the backends. Doing so requires a restart of the editor.";

            // Only display this dialog once to the user per editor session.
            if (!m_OldInputSystemWarningTriggered)
            {
                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                {
                    var serializedPlayerSettings = GetSerializedPlayerSettings();
                    if (serializedPlayerSettings != null)
                    {
                        serializedPlayerSettings.FindProperty("enableNativePlatformBackendsForNewInputSystem").boolValue = true;
                        serializedPlayerSettings.ApplyModifiedProperties();
                    }
                }

                m_OldInputSystemWarningTriggered = true;
            }
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
