using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Plugins.UI;
using UnityEngine.InputSystem.Plugins.Users;
using UnityEngine.InputSystem.Utilities;

////REVIEW: having everything coupled to component enable/disable is quite restrictive; can we allow PlayerInputs
////        to be disabled without them leaving the game? would help when wanting to keep players around in the background
////        and only temporarily disable them

////TODO: refresh caches when asset is modified at runtime

////TODO: handle required actions ahead of time so that we catch it if a device matches by type but doesn't otherwise

////TODO: handle case of control scheme not having any devices in its requirements

////FIXME: why can't I join with a mouse left click?

//pairing with specific devices requires PlayerInputManager

//what happens if multiple local players are supposed to be able to operate the same UIs?

//how does the player get his devices?

//case 1: only one player in the game; can use whatever devices supported by the actions
//        optional: can have default control scheme
//        optional: can let users switch control schemes freely
//case 2:

//case1: start with default control scheme; optionally: let player switch automatically
//case2:

//in single player, will check last used device and base default control scheme on that

//how do we distinguish between an action that needs a continuous state and one that is one-off?
// for messages: send xxxStarted/xxxStopped variants
// for events: user has to use context to determine what happened


// if it's coming from a press interaction, send OnXXXDown and OnXXXUp?

namespace UnityEngine.InputSystem.Plugins.PlayerInput
{
    /// <summary>
    /// A wrapper around the input system that takes care of managing input actions
    /// and players.
    /// </summary>
    /// <remarks>
    /// This is a helper to get set up with the new input system quickly. It takes
    /// care of <see cref="InputAction"/> bookkeeping and has a custom UI to help
    /// setting up input.
    ///
    /// The component supports local multiplayer implicitly. Each PlayerInput instance
    /// represents a distinct user with its own set of devices and actions. To orchestrate
    /// player management and facilitate mechanics such as joining by device activity, use
    /// <see cref="PlayerInputManager"/>.
    ///
    /// The way PlayerInput notifies script code of events is determined by <see cref="notificationBehavior"/>.
    /// By default, this is set to <see cref="PlayerNotifications.SendMessages"/> which will use <see
    /// cref="GameObject.SendMessage(string,object)"/> to send messages to the <see cref="GameObject"/>
    /// that PlayerInput sits on.
    ///
    /// Notifications are sent
    ///
    /// 1) when actions are triggered,
    /// 2) when a player loses or regains a device,
    /// 3) when the player switches to a different control scheme,
    /// 4) when the player changes bindings,
    ///
    /// If <see cref="notificationBehavior"/> is set to <see cref="PlayerNotifications.SendMessages"/> or
    /// <see cref="PlayerNotifications.BroadcastMessages"/>, then an ... OnFireStarted
    ///
    /// <example>
    /// <code>
    /// // Component to sit next to PlayerInput.
    /// public class MyPlayerLogic : MonoBehaviour
    /// {
    ///     public GameObject projectilePrefab;
    ///
    ///     private Vector2 m_Look;
    ///     private Vector2 m_Move;
    ///     private bool m_Fire;
    ///
    ///     // Player has joined the game.
    ///     public void OnPlayerJoined()
    ///     {
    ///         // Put player at spawn point.
    ///         transform.position = PickSpawnPoint().transform.position;
    ///     }
    ///
    ///     // 'Fire' input action has been triggered. For 'Fire' we want continuous
    ///     // action (i.e. firing) while the fire buttion is held such that the action
    ///     // gets triggered repeatedly while the button is down. We can easily set this
    ///     // up by having a "Press" interaction on the button and setting it to repeat
    ///     // at fixed intervals.
    ///     public void OnFire()
    ///     {
    ///         Instantiate(projectilePrefab);
    ///     }
    ///
    ///     // 'Move' input action has been triggered.
    ///     public void OnMove(InputValue value)
    ///     {
    ///         TODO
    ///     }
    ///
    ///     public void OnFixedUpdate()
    ///     {
    ///         TODO
    ///     }
    ///
    ///     private GameObject PickSpawnPoint()
    ///     {
    ///         // Pick a spawn point for the player...
    ///     }
    /// }
    /// </code>
    /// </example>
    ///
    /// A player can be uniquely associated with a
    ///
    ///
    /// When a game is started, a PlayerInput component will pick from the locally available
    /// devices to decide which to use with the component's input actions (<see cref="InputActionAsset.devices"/>).
    /// Once picked, the devices will be for the exclusive use by the PlayerInput component.
    ///
    /// The picking process can be guided in one of several ways:
    ///
    /// 1) By control scheme. XXX
    ///
    ///
    /// Note that this component prioritizes usability over performance and involves a certain
    /// overhead at runtime.
    ///
    /// The implementation is based on the functionality made available by <see cref="InputUser"/>.
    /// If the component does not fit the specific requirements of an application, its functionality
    /// can be reimplemented on top of the same API.
    /// </remarks>
    /// <seealso cref="PlayerInputManager"/>
    [AddComponentMenu("Input/Player Input")]
    [DisallowMultipleComponent]
    public class PlayerInput : MonoBehaviour
    {
        public const string DeviceLostMessage = "OnDeviceLost";
        public const string DeviceRegainedMessage = "OnDeviceRegained";
        public const string ControlSchemeChangedMessage = "OnControlSchemeChanged";
        public const string BindingsChangedMessage = "OnBindingsChanged";

        public bool active => m_InputActive;

        /// <summary>
        /// Unique index of the player.
        /// </summary>
        /// <remarks>
        /// Once assigned, a player index will not change.
        ///
        /// Note that the player index does not necessarily correspond to the player's index in <see cref="all"/>.
        /// The array will always contain all currently enabled players so when a player is disabled or destroyed,
        /// it will be removed from the array. However, the player index of the remaining players will not change.
        /// </remarks>
        public int playerIndex => m_PlayerIndex;

        /// <summary>
        /// If split-screen is enabled (<see cref="PlayerInputManager.splitScreen"/>), this is the index of the
        /// screen area used by the player.
        /// </summary>
        /// <seealso cref="camera"/>
        public int splitScreenIndex => m_SplitScreenIndex;

        /// <summary>
        /// Input actions associated with the player.
        /// </summary>
        /// <remarks>
        /// Note that every player will maintain a unique copy of the given actions such that
        /// each player receives an identical copy. When assigning the same actions to multiple players,
        /// the first player will use the given actions as is but any subsequent player will make a copy
        /// of the actions using <see cref="Object.Instantiate(Object)"/>.
        ///
        /// The asset may contain an arbitrary number of action maps. However, the first action map in the
        /// asset is treated special ...
        ///
        /// Notifications will be send for all actions in the asset, not just for those in the first action
        /// map. This means that if additional maps are manually enabled and disabled,
        ///
        /// There is one exception to this, however. For any action from the asset that is also referenced
        /// by an <see cref="InputSystemUIInputModule"/> sitting on the <see cref="GameObject"/> of
        /// <see cref="uiEventSystem"/>, no notification will be triggered when the action is fired.
        /// </remarks>
        /// <seealso cref="InputUser.actions"/>
        public InputActionAsset actions
        {
            get => m_Actions;
            set
            {
                if (m_Actions == value)
                    return;

                // Make sure that if we already have actions, they get disabled.
                if (m_Actions != null)
                {
                    m_Actions.Disable();
                    if (m_Enabled)
                        UninitializeActions();
                }

                m_Actions = value;

                if (m_Enabled)
                {
                    ClearCaches();
                    AssignUserAndDevices();
                    InitializeActions();
                    if (m_InputActive)
                        ActivateInput();
                }
            }
        }

        public string controlScheme
        {
            get
            {
                if (!m_InputUser.valid)
                    return null;

                var scheme = m_InputUser.controlScheme;
                return scheme?.name;
            }
        }

        public string defaultControlScheme
        {
            get => m_DefaultControlScheme;
            set => m_DefaultControlScheme = value;
        }

        /// <summary>
        /// Name or ID (in the form of "{...}") of the action map to enable by default.
        /// </summary>
        /// <remarks>
        /// TODO
        /// </remarks>
        public string defaultActionMap
        {
            get => m_DefaultActionMap;
            set => m_DefaultActionMap = value;
        }

        /// <summary>
        /// Determines how the component notifies listeners about input actions and other input-related
        /// events pertaining to the player.
        /// </summary>
        /// <remarks>
        /// By default, the component will use <see cref="GameObject.SendMessage(string,object)"/> to send messages
        /// to the <see cref="GameObject"/>. This can be changed by selecting a different <see cref="PlayerNotifications"/>
        /// behavior.
        /// </remarks>
        /// <seealso cref="actionEvents"/>
        /// <seealso cref="deviceLostEvent"/>
        /// <seealso cref="deviceRegainedEvent"/>
        public PlayerNotifications notificationBehavior
        {
            get => m_NotificationBehavior;
            set
            {
                if (m_NotificationBehavior == value)
                    return;

                if (m_Enabled)
                    UninitializeActions();

                m_NotificationBehavior = value;

                if (m_Enabled)
                    InitializeActions();
            }
        }

        /// <summary>
        /// List of events invoked in response to actions being triggered.
        /// </summary>
        /// <remarks>
        /// This array is only used if <see cref="notificationBehavior"/> is set to
        /// <see cref="PlayerNotifications.InvokeUnityEvents"/>.
        /// </remarks>
        public ReadOnlyArray<ActionEvent> actionEvents
        {
            get => m_ActionEvents;
            set
            {
                if (m_Enabled)
                    UninitializeActions();

                m_ActionEvents = value.ToArray();

                if (m_Enabled)
                    InitializeActions();
            }
        }

        /// <summary>
        /// Event that is triggered when the player loses a device (e.g. the batteries run out).
        /// </summary>
        /// <remarks>
        /// This event is only used if <see cref="notificationBehavior"/> is set to
        /// <see cref="PlayerNotifications.InvokeUnityEvents"/>.
        /// </remarks>
        public DeviceLostEvent deviceLostEvent
        {
            get
            {
                if (m_DeviceLostEvent == null)
                    m_DeviceLostEvent = new DeviceLostEvent();
                return m_DeviceLostEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when the player recovers from device loss and is good to go again.
        /// </summary>
        /// <remarks>
        /// This event is only used if <see cref="notificationBehavior"/> is set to
        /// <see cref="PlayerNotifications.InvokeUnityEvents"/>.
        /// </remarks>
        public DeviceRegainedEvent deviceRegainedEvent
        {
            get
            {
                if (m_DeviceRegainedEvent == null)
                    m_DeviceRegainedEvent = new DeviceRegainedEvent();
                return m_DeviceRegainedEvent;
            }
        }

        public event Action<InputAction.CallbackContext> onActionTriggered
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_ActionTriggeredCallbacks.AppendWithCapacity(value, 5);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = m_ActionTriggeredCallbacks.IndexOf(value);
                if (index != -1)
                    m_ActionTriggeredCallbacks.RemoveAtWithCapacity(index);
            }
        }

        public event Action<PlayerInput> onDeviceLost
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_DeviceLostCallbacks.AppendWithCapacity(value, 5);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = m_DeviceLostCallbacks.IndexOf(value);
                if (index != -1)
                    m_DeviceLostCallbacks.RemoveAtWithCapacity(index);
            }
        }

        public event Action<PlayerInput> onDeviceRegained
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_DeviceRegainedCallbacks.AppendWithCapacity(value, 5);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = m_DeviceRegainedCallbacks.IndexOf(value);
                if (index != -1)
                    m_DeviceRegainedCallbacks.RemoveAtWithCapacity(index);
            }
        }

        /// <summary>
        /// The camera associated with the player.
        /// </summary>
        /// <remarks>
        /// This is null by default.
        ///
        /// Associating a camera with a player is necessary when using split-screen.
        /// </remarks>
        public new Camera camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        /// <summary>
        /// UI InputModule that should have it's input actions synchronized to this PlayerInput's actions.
        /// </summary>
        public InputSystemUIInputModule uiInputModule
        {
            get { return m_UIInputModule; }
            set { m_UIInputModule = value; }
        }

        /// <summary>
        /// The internal user tied to the player.
        /// </summary>
        public InputUser user => m_InputUser;

        /// <summary>
        /// The devices used by the player.
        /// </summary>
        /// <seealso cref="InputUser.pairedDevices"/>
        public ReadOnlyArray<InputDevice> devices
        {
            get
            {
                if (!m_InputUser.valid)
                    return new ReadOnlyArray<InputDevice>();

                return m_InputUser.pairedDevices;
            }
        }

        public bool hasMissingRequiredDevices => user.hasMissingRequiredDevices;

        public static event Action<PlayerInput> onAdded
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public static event Action<PlayerInput> onRemoved
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public static ReadOnlyArray<PlayerInput> all => new ReadOnlyArray<PlayerInput>(s_AllActivePlayers, 0, s_AllActivePlayersCount);

        public static bool isSinglePlayer =>
            s_AllActivePlayersCount <= 1 &&
            (PlayerInputManager.instance == null || !PlayerInputManager.instance.joiningEnabled);

        public void ActivateInput()
        {
            // Enable default action map, if set.
            if (m_Actions != null && !string.IsNullOrEmpty(m_DefaultActionMap))
            {
                var actionMap = m_Actions.TryGetActionMap(m_DefaultActionMap);
                if (actionMap != null)
                {
                    actionMap.Enable();
                    m_EnabledActionMap = actionMap;
                }
                else
                    Debug.LogError($"Cannot find action map '{m_DefaultActionMap}' in '{m_Actions}'", this);
            }
            m_InputActive = true;
        }

        public void PassivateInput()
        {
            m_EnabledActionMap?.Disable();

            m_InputActive = false;
        }

        public void SwitchCurrentActionMap(string mapNameOrId)
        {
            // Must be enabled.
            if (!m_Enabled)
            {
                Debug.LogError($"Cannot switch to actions '{mapNameOrId}'; input is not enabled", this);
                return;
            }

            // Must have actions.
            if (m_Actions == null)
            {
                Debug.LogError($"Cannot switch to actions '{mapNameOrId}'; no actions set on PlayerInput", this);
                return;
            }

            // Must have map.
            var actionMap = m_Actions.TryGetActionMap(mapNameOrId);
            if (actionMap == null)
            {
                Debug.LogError($"Cannot find action map '{mapNameOrId}' in actions '{m_Actions}'", this);
                return;
            }

            currentActionMap = actionMap;
        }

        public static PlayerInput GetPlayerByIndex(int playerIndex)
        {
            for (var i = 0; i < s_AllActivePlayersCount; ++i)
                if (s_AllActivePlayers[i].playerIndex == playerIndex)
                    return s_AllActivePlayers[i];
            return null;
        }

        public static PlayerInput FindFirstPairedToDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            for (var i = 0; i < s_AllActivePlayersCount; ++i)
            {
                if (s_AllActivePlayers[i].devices.ContainsReference(device))
                    return s_AllActivePlayers[i];
            }

            return null;
        }

        public static PlayerInput Instantiate(GameObject prefab, int playerIndex = -1, string controlScheme = null,
            int splitScreenIndex = -1, InputDevice pairWithDevice = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            // Set initialization data.
            s_InitPlayerIndex = playerIndex;
            s_InitSplitScreenIndex = splitScreenIndex;
            s_InitControlScheme = controlScheme;
            if (pairWithDevice != null)
                ArrayHelpers.AppendWithCapacity(ref s_InitPairWithDevices, ref s_InitPairWithDevicesCount, pairWithDevice);

            return DoInstantiate(prefab);
        }

        ////TODO: allow instantiating with an existing InputUser

        /// <summary>
        /// A wrapper around <see cref="Object.Instantiate(Object)"/> that allows instantiating a player prefab and
        /// automatically pair one or more specific devices to the newly created player.
        /// </summary>
        /// <param name="prefab">A player prefab containing a <see cref="PlayerInput"/> component in its hierarchy.</param>
        /// <param name="playerIndex"></param>
        /// <param name="controlScheme"></param>
        /// <param name="splitScreenIndex"></param>
        /// <param name="pairWithDevices"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note that unlike <see cref="Object.Instantiate(Object)"/>, this method will always activate the resulting
        /// <see cref="GameObject"/> and its components.
        /// </remarks>
        public static PlayerInput Instantiate(GameObject prefab, int playerIndex = -1, string controlScheme = null,
            int splitScreenIndex = -1, params InputDevice[] pairWithDevices)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            // Set initialization data.
            s_InitPlayerIndex = playerIndex;
            s_InitSplitScreenIndex = splitScreenIndex;
            s_InitControlScheme = controlScheme;
            if (pairWithDevices != null)
            {
                for (var i = 0; i < pairWithDevices.Length; ++i)
                    ArrayHelpers.AppendWithCapacity(ref s_InitPairWithDevices, ref s_InitPairWithDevicesCount, pairWithDevices[i]);
            }

            return DoInstantiate(prefab);
        }

        private static PlayerInput DoInstantiate(GameObject prefab)
        {
            GameObject instance;
            try
            {
                instance = Object.Instantiate(prefab);
                instance.SetActive(true);
            }
            finally
            {
                // Reset init data.
                s_InitPairWithDevicesCount = 0;
                if (s_InitPairWithDevices != null)
                    Array.Clear(s_InitPairWithDevices, 0, s_InitPairWithDevicesCount);
                s_InitControlScheme = null;
                s_InitPlayerIndex = -1;
                s_InitSplitScreenIndex = -1;
            }

            var playerInput = instance.GetComponentInChildren<PlayerInput>();
            if (playerInput == null)
            {
                DestroyImmediate(instance);
                Debug.LogError("The GameObject does not have a PlayerInput component", prefab);
                return null;
            }

            return playerInput;
        }

        [Tooltip("Input actions associated with the player.")]
        [SerializeField] internal InputActionAsset m_Actions;
        [Tooltip("Determine how notifications should be sent when an input-related event associated with the player happens.")]
        [SerializeField] internal PlayerNotifications m_NotificationBehavior;
        [Tooltip("UI InputModule that should have it's input actions synchronized to this PlayerInput's actions.")]
        [SerializeField] internal InputSystemUIInputModule m_UIInputModule;
        [Tooltip("Event that is triggered when the PlayerInput loses a paired device (e.g. its battery runs out).")]
        [SerializeField] internal DeviceLostEvent m_DeviceLostEvent;
        [SerializeField] internal DeviceRegainedEvent m_DeviceRegainedEvent;
        [SerializeField] internal ActionEvent[] m_ActionEvents;
        [SerializeField] internal bool m_AutoSwitchControlScheme;
        [SerializeField] internal string m_DefaultControlScheme;////REVIEW: should we have IDs for these so we can rename safely?
        [SerializeField] internal string m_DefaultActionMap;
        [SerializeField] internal int m_SplitScreenIndex = -1;
        [Tooltip("Reference to the player's view camera. Note that this is only required when using split-screen and/or "
            + "per-player UIs. Otherwise it is safe to leave this property uninitialized.")]
        [SerializeField] internal Camera m_Camera;

        // Value object we use when sending messages via SendMessage() or BroadcastMessage(). Can be ignored
        // by the receiver. We reuse the same object over and over to avoid allocating garbage.
        [NonSerialized] private InputValue m_InputValueObject;

        [NonSerialized] internal InputActionMap m_EnabledActionMap;

        public InputActionMap currentActionMap
        {
            get => m_EnabledActionMap;
            set
            {
                m_EnabledActionMap?.Disable();
                m_EnabledActionMap = value;
                m_EnabledActionMap?.Enable();
            }
        }

        [NonSerialized] private int m_PlayerIndex = -1;
        [NonSerialized] private bool m_InputActive;
        [NonSerialized] private bool m_Enabled;
        [NonSerialized] private Dictionary<string, string> m_ActionMessageNames;
        [NonSerialized] private InputUser m_InputUser;
        [NonSerialized] private Action<InputAction.CallbackContext> m_ActionTriggeredDelegate;
        [NonSerialized] private InlinedArray<Action<PlayerInput>> m_DeviceLostCallbacks;
        [NonSerialized] private InlinedArray<Action<PlayerInput>> m_DeviceRegainedCallbacks;
        [NonSerialized] private InlinedArray<Action<InputAction.CallbackContext>> m_ActionTriggeredCallbacks;

        internal static int s_AllActivePlayersCount;
        internal static PlayerInput[] s_AllActivePlayers;
        internal static Action<InputUser, InputUserChange, InputDevice> s_UserChangeDelegate;
        internal static Action<InputControl> s_UnpairedDeviceUsedDelegate;
        internal static bool s_OnUnpairedDeviceHooked;

        // The following information is used when the next PlayerInput component is enabled.

        private static int s_InitPairWithDevicesCount;
        private static InputDevice[] s_InitPairWithDevices;
        private static int s_InitPlayerIndex = -1;
        private static int s_InitSplitScreenIndex = -1;
        private static string s_InitControlScheme;

        private void InitializeActions()
        {
            if (m_Actions == null)
                return;

            ////REVIEW: should we *always* Instantiate()?
            // Check if we need to duplicate our actions by looking at all other players. If any
            // has the same actions, duplicate.
            for (var i = 0; i < s_AllActivePlayersCount; ++i)
                if (s_AllActivePlayers[i].m_Actions == m_Actions && s_AllActivePlayers[i] != this)
                {
                    m_Actions = Instantiate(m_Actions);
                    break;
                }

            if (uiInputModule != null)
                uiInputModule.actionsAsset = m_Actions;

            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                case PlayerNotifications.BroadcastMessages:
                    if (m_ActionTriggeredDelegate == null)
                        m_ActionTriggeredDelegate = OnActionTriggered;
                    foreach (var actionMap in m_Actions.actionMaps)
                        actionMap.actionTriggered += m_ActionTriggeredDelegate;
                    if (m_ActionMessageNames == null)
                        CacheMessageNames();
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                {
                    // Hook up all action events.
                    if (m_ActionEvents != null)
                    {
                        foreach (var actionEvent in m_ActionEvents)
                        {
                            var id = actionEvent.actionId;
                            if (string.IsNullOrEmpty(id))
                                continue;

                            // Find action for event.
                            var action = m_Actions.FindAction(id);
                            if (action != null)
                            {
                                ////REVIEW: really wish we had a single callback
                                action.performed += actionEvent.Invoke;
                                action.canceled += actionEvent.Invoke;
                                action.started += actionEvent.Invoke;
                            }
                            else
                            {
                                // Cannot find action. Log error.
                                if (!string.IsNullOrEmpty(actionEvent.actionName))
                                {
                                    // We have an action name. Show in message.
                                    Debug.LogError(
                                        $"Cannot find action '{actionEvent.actionName}' with ID '{actionEvent.actionId}' in '{actions}",
                                        this);
                                }
                                else
                                {
                                    // We have no action name. Best we have is ID.
                                    Debug.LogError(
                                        $"Cannot find action with ID '{actionEvent.actionId}' in '{actions}",
                                        this);
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UninitializeActions()
        {
            if (m_Actions == null)
                return;

            if (m_ActionTriggeredDelegate != null)
                foreach (var actionMap in m_Actions.actionMaps)
                    actionMap.actionTriggered -= m_ActionTriggeredDelegate;

            if (m_NotificationBehavior == PlayerNotifications.InvokeUnityEvents && m_ActionEvents != null)
            {
                foreach (var actionEvent in m_ActionEvents)
                {
                    var id = actionEvent.actionId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    // Find action for event.
                    var action = m_Actions.FindAction(id);
                    if (action != null)
                    {
                        ////REVIEW: really wish we had a single callback
                        action.performed -= actionEvent.Invoke;
                        action.canceled -= actionEvent.Invoke;
                        action.started -= actionEvent.Invoke;
                    }
                }
            }
        }

        ////REVIEW: should this take the action *type* into account? e.g. have different behavior when the type is "Button"?
        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!m_InputActive)
                return;

            // We shouldn't go through this method when using UnityEvents. With events,
            // the callbacks should be wired up directly rather than going all to this method.
            Debug.Assert(m_NotificationBehavior != PlayerNotifications.InvokeUnityEvents);
            if (m_NotificationBehavior == PlayerNotifications.InvokeUnityEvents)
                return;

            // ATM we only care about `performed` and, in the case of continuous actions, `canceled`.
            var action = context.action;
            if (!(context.performed || (context.canceled && action.continuous)))
                return;

            // Find message name for action.
            if (m_ActionMessageNames == null)
                CacheMessageNames();
            var messageName = m_ActionMessageNames[action.m_Id];

            // Cache value.
            if (m_InputValueObject == null)
                m_InputValueObject = new InputValue();
            m_InputValueObject.m_Context = context;

            // Send message.
            if (m_NotificationBehavior == PlayerNotifications.BroadcastMessages)
                BroadcastMessage(messageName, m_InputValueObject, SendMessageOptions.DontRequireReceiver);
            else
                SendMessage(messageName, m_InputValueObject, SendMessageOptions.DontRequireReceiver);

            // Reset context so calling Get() will result in an exception.
            m_InputValueObject.m_Context = null;
        }

        private void CacheMessageNames()
        {
            if (m_Actions == null)
                return;

            if (m_ActionMessageNames != null)
                m_ActionMessageNames.Clear();
            else
                m_ActionMessageNames = new Dictionary<string, string>();

            foreach (var action in m_Actions)
            {
                action.MakeSureIdIsInPlace();

                var name = action.name;
                if (char.IsLower(name[0]))
                    name = char.ToUpper(name[0]) + name.Substring(1);
                m_ActionMessageNames[action.m_Id] = "On" + name;
            }
        }

        private void ClearCaches()
        {
        }

        /// <summary>
        /// Initialize <see cref="user"/> and <see cref="devices"/>.
        /// </summary>
        private void AssignUserAndDevices()
        {
            // If we already have a user at this point, clear out all its paired devices
            // to start the pairing process from scratch.
            if (m_InputUser.valid)
                m_InputUser.UnpairDevices();

            // All our input goes through actions so there's no point setting
            // anything up if we have none.
            if (m_Actions == null)
            {
                // If we have devices we are meant to pair with, do so.  Otherwise, don't
                // do anything as we don't know what kind of input to look for.
                if (s_InitPairWithDevicesCount > 0)
                {
                    for (var i = 0; i < s_InitPairWithDevicesCount; ++i)
                        m_InputUser = InputUser.PerformPairingWithDevice(s_InitPairWithDevices[i], m_InputUser);
                }
                else
                {
                    // Make sure user is invalid.
                    m_InputUser = new InputUser();
                }

                return;
            }

            // If we have control schemes, try to find the one we should use.
            if (m_Actions.controlSchemes.Count > 0)
            {
                if (!string.IsNullOrEmpty(s_InitControlScheme))
                {
                    // We've been given a control scheme to initialize this. Try that one and
                    // that one only. Might mean we end up with missing devices.

                    var controlScheme = m_Actions.TryGetControlScheme(s_InitControlScheme);
                    if (controlScheme == null)
                    {
                        Debug.LogError($"No control scheme '{s_InitControlScheme}' in '{m_Actions}'", this);
                    }
                    else
                    {
                        TryToActivateControlScheme(controlScheme.Value);
                    }
                }
                else if (!string.IsNullOrEmpty(m_DefaultControlScheme))
                {
                    // There's a control scheme we should try by default.

                    var controlScheme = m_Actions.TryGetControlScheme(m_DefaultControlScheme);
                    if (controlScheme == null)
                    {
                        Debug.LogError($"Cannot find default control scheme '{m_DefaultControlScheme}' in '{m_Actions}'", this);
                    }
                    else
                    {
                        TryToActivateControlScheme(controlScheme.Value);
                    }
                }

                // If we did not end up with a usable scheme by now but we've been given devices to pair with,
                // search for a control scheme matching the given devices.
                if (s_InitPairWithDevicesCount > 0 && (!m_InputUser.valid || m_InputUser.controlScheme == null))
                {
                    foreach (var controlScheme in m_Actions.controlSchemes)
                    {
                        for (var i = 0; i < s_InitPairWithDevicesCount; ++i)
                        {
                            var device = s_InitPairWithDevices[i];
                            if (controlScheme.SupportsDevice(device) && TryToActivateControlScheme(controlScheme))
                                break;
                        }
                    }
                }
                // If we don't have a working control scheme by now and we haven't been instructed to use
                // one specific control scheme, try each one in the asset one after the other until we
                // either find one we can use or run out of options.
                else if ((!m_InputUser.valid || m_InputUser.controlScheme == null) && string.IsNullOrEmpty(s_InitControlScheme))
                {
                    foreach (var controlScheme in m_Actions.controlSchemes)
                    {
                        if (TryToActivateControlScheme(controlScheme))
                            break;
                    }
                }
            }
            else
            {
                // There's no control schemes in the asset. If we've been given a set of devices,
                // we run with those (regardless of whether there's bindings for them in the actions or not).
                // If we haven't been given any devices, we go through all bindings in the asset and whatever
                // device is present that matches the binding and that isn't used by any other player, we'll
                // pair to the player.

                if (s_InitPairWithDevicesCount > 0)
                {
                    for (var i = 0; i < s_InitPairWithDevicesCount; ++i)
                        m_InputUser = InputUser.PerformPairingWithDevice(s_InitPairWithDevices[i], m_InputUser);
                }
                else
                {
                    using (var availableDevices = InputUser.GetUnpairedInputDevices())
                    {
                        foreach (var actionMap in m_Actions.actionMaps)
                        {
                            foreach (var binding in actionMap.bindings)
                            {
                                // See if the binding matches anything available.
                                InputDevice matchesDevice = null;
                                foreach (var device in availableDevices)
                                {
                                    if (InputControlPath.TryFindControl(device, binding.effectivePath) != null)
                                    {
                                        matchesDevice = device;
                                        break;
                                    }
                                }

                                if (matchesDevice == null)
                                    continue;

                                if (m_InputUser.valid && m_InputUser.pairedDevices.ContainsReference(matchesDevice))
                                {
                                    // Already paired to this device.
                                    continue;
                                }

                                m_InputUser = InputUser.PerformPairingWithDevice(matchesDevice, m_InputUser);
                            }
                        }
                    }
                }
            }

            // If we don't have a valid user at this point, we don't have any paired devices.
            if (m_InputUser.valid)
                m_InputUser.AssociateActionsWithUser(m_Actions);
        }

        private void UnassignUserAndDevices()
        {
            if (m_InputUser.valid)
                m_InputUser.UnpairDevicesAndRemoveUser();
            if (m_Actions != null)
                m_Actions.devices = null;
        }

        private bool TryToActivateControlScheme(InputControlScheme controlScheme)
        {
            ////FIXME: this will fall apart if account management is involved and a user needs to log in on device first

            // Pair any devices we may have been given.
            if (s_InitPairWithDevicesCount > 0)
            {
                ////REVIEW: should AndPairRemainingDevices() require that there is at least one existing
                ////        device paired to the user that is usable with the given control scheme?

                // First make sure that all of the devices actually work with the given control scheme.
                // We're fine having to pair additional devices but we don't want the situation where
                // we have the player grab all the devices in s_InitPairWithDevices along with a control
                // scheme that fits none of them and then AndPairRemainingDevices() supplying the devices
                // actually needed by the control scheme.
                for (var i = 0; i < s_InitPairWithDevicesCount; ++i)
                {
                    var device = s_InitPairWithDevices[i];
                    if (!controlScheme.SupportsDevice(device))
                        return false;
                }

                // We're good. Give the devices to the user.
                for (var i = 0; i < s_InitPairWithDevicesCount; ++i)
                {
                    var device = s_InitPairWithDevices[i];
                    m_InputUser = InputUser.PerformPairingWithDevice(device, m_InputUser);
                }
            }

            if (!m_InputUser.valid)
                m_InputUser = InputUser.CreateUserWithoutPairedDevices();

            m_InputUser.ActivateControlScheme(controlScheme).AndPairRemainingDevices();
            if (user.hasMissingRequiredDevices)
            {
                m_InputUser.ActivateControlScheme(null);
                m_InputUser.UnpairDevices();
                return false;
            }

            return true;
        }

        private void AssignPlayerIndex()
        {
            if (s_InitPlayerIndex != -1)
                m_PlayerIndex = s_InitPlayerIndex;
            else
            {
                var minPlayerIndex = int.MaxValue;
                var maxPlayerIndex = int.MinValue;

                for (var i = 0; i < s_AllActivePlayersCount; ++i)
                {
                    var playerIndex = s_AllActivePlayers[i].playerIndex;
                    minPlayerIndex = Math.Min(minPlayerIndex, playerIndex);
                    maxPlayerIndex = Math.Max(maxPlayerIndex, playerIndex);
                }

                if (minPlayerIndex != int.MaxValue && minPlayerIndex > 0)
                {
                    // There's an index between 0 and the current minimum available.
                    m_PlayerIndex = minPlayerIndex - 1;
                }
                else if (maxPlayerIndex != int.MinValue)
                {
                    // There may be an index between the minimum and maximum available.
                    // Search the range. If there's nothing, create a new maximum.
                    for (var i = minPlayerIndex; i < maxPlayerIndex; ++i)
                    {
                        if (GetPlayerByIndex(i) == null)
                        {
                            m_PlayerIndex = i;
                            return;
                        }
                    }

                    m_PlayerIndex = maxPlayerIndex + 1;
                }
                else
                    m_PlayerIndex = 0;
            }
        }

        private void OnEnable()
        {
            m_Enabled = true;

            AssignPlayerIndex();
            InitializeActions();
            AssignUserAndDevices();
            ActivateInput();

            // Split-screen index defaults to player index.
            if (s_InitSplitScreenIndex >= 0)
                m_SplitScreenIndex = splitScreenIndex;
            else
                m_SplitScreenIndex = playerIndex;

            // Add to global list and sort it by player index.
            ArrayHelpers.AppendWithCapacity(ref s_AllActivePlayers, ref s_AllActivePlayersCount, this);
            for (var i = 1; i < s_AllActivePlayersCount; ++i)
                for (var j = i; j > 0 && s_AllActivePlayers[j - 1].playerIndex > s_AllActivePlayers[j].playerIndex; --j)
                    s_AllActivePlayers.SwapElements(j, j - 1);

            // If it's the first player, hook into user change notifications.
            if (s_AllActivePlayersCount == 1)
            {
                if (s_UserChangeDelegate == null)
                    s_UserChangeDelegate = OnUserChange;
                InputUser.onChange += s_UserChangeDelegate;
            }

            // In single player, set up for automatic control scheme switching.
            // Otherwise make sure it's disabled.
            if (isSinglePlayer && !s_OnUnpairedDeviceHooked)
            {
                if (s_UnpairedDeviceUsedDelegate == null)
                    s_UnpairedDeviceUsedDelegate = OnUnpairedDeviceUsed;
                InputUser.onUnpairedDeviceUsed += s_UnpairedDeviceUsedDelegate;
                ++InputUser.listenForUnpairedDeviceActivity;
                s_OnUnpairedDeviceHooked = true;
            }
            else if (s_OnUnpairedDeviceHooked)
            {
                InputUser.onUnpairedDeviceUsed -= s_UnpairedDeviceUsedDelegate;
                --InputUser.listenForUnpairedDeviceActivity;
                s_OnUnpairedDeviceHooked = false;
            }

            // Trigger join event.
            PlayerInputManager.instance?.NotifyPlayerJoined(this);
        }

        private void OnDisable()
        {
            m_Enabled = false;

            // Remove from global list.
            var index = ArrayHelpers.IndexOfReference(s_AllActivePlayers, this, s_AllActivePlayersCount);
            if (index != -1)
                ArrayHelpers.EraseAtWithCapacity(s_AllActivePlayers, ref s_AllActivePlayersCount, index);

            // Unhook from change notifications if we're the last player.
            if (s_AllActivePlayersCount == 0 && s_UserChangeDelegate != null)
                InputUser.onChange -= s_UserChangeDelegate;
            if (s_AllActivePlayersCount == 0 && s_OnUnpairedDeviceHooked)
            {
                InputUser.onUnpairedDeviceUsed -= s_UnpairedDeviceUsedDelegate;
                --InputUser.listenForUnpairedDeviceActivity;
                s_OnUnpairedDeviceHooked = false;
            }

            // Trigger leave event.
            PlayerInputManager.instance?.NotifyPlayerLeft(this);

            PassivateInput();
            UnassignUserAndDevices();
            UninitializeActions();

            m_PlayerIndex = -1;
        }

        /// <summary>
        /// Debug helper method that can be hooked up to actions when using <see cref="PlayerNotifications.InvokeUnityEvents"/>.
        /// </summary>
        public void DebugLogAction(InputAction.CallbackContext context)
        {
            Debug.Log(context.ToString());
        }

        private void HandleDeviceLost()
        {
            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(DeviceLostMessage, this);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(DeviceLostMessage, this);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_DeviceLostEvent?.Invoke(this);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceLostCallbacks, this, "onDeviceLost");
                    break;
            }
        }

        private void HandleDeviceRegained()
        {
            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(DeviceRegainedMessage, this);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(DeviceRegainedMessage, this);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_DeviceRegainedEvent?.Invoke(this);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_DeviceRegainedCallbacks, this, "onDeviceRegained");
                    break;
            }
        }

        private static void OnUserChange(InputUser user, InputUserChange change, InputDevice device)
        {
            switch (change)
            {
                case InputUserChange.DeviceLost:
                case InputUserChange.DeviceRegained:
                    for (var i = 0; i < s_AllActivePlayersCount; ++i)
                    {
                        var player = s_AllActivePlayers[i];
                        if (player.m_InputUser == user)
                        {
                            if (change == InputUserChange.DeviceLost)
                                player.HandleDeviceLost();
                            else if (change == InputUserChange.DeviceRegained)
                                player.HandleDeviceRegained();
                        }
                    }
                    break;
            }
        }

        private static void OnUnpairedDeviceUsed(InputControl control)
        {
            // We only support automatic control scheme switching in single player mode.
            // OnEnable() should automatically unhook us.
            if (!isSinglePlayer)
                return;

            var player = all[0];
            if (player.m_Actions == null)
                return;

            // Go through all control schemes and see if there is one usable with the device.
            // If so, switch to it.
            var controlScheme = InputControlScheme.FindControlSchemeForDevice(control.device, player.m_Actions.controlSchemes);
            if (controlScheme != null)
            {
                // First remove the currently paired devices, then pair the device that was used,
                // and finally switch to the new control scheme and grab whatever other devices we're missing.
                player.user.UnpairDevices();
                InputUser.PerformPairingWithDevice(control.device, user: player.user);
                player.user.ActivateControlScheme(controlScheme.Value).AndPairRemainingDevices();
            }
        }

        [Serializable]
        public class ActionEvent : UnityEvent<InputAction.CallbackContext>
        {
            public string actionId => m_ActionId;
            public string actionName => m_ActionName;

            [SerializeField] private string m_ActionId;
            [SerializeField] private string m_ActionName;

            public ActionEvent()
            {
            }

            public ActionEvent(InputAction action)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                if (action.isSingletonAction)
                    throw new ArgumentException($"Action must be part of an asset (given action '{action}' is a singleton)");
                if (action.actionMap.asset == null)
                    throw new ArgumentException($"Action must be part of an asset (given action '{action}' is not)");

                m_ActionId = action.id.ToString();
                m_ActionName = $"{action.actionMap.name}/{action.name}";
            }

            public ActionEvent(Guid actionGUID, string name = null)
            {
                m_ActionId = actionGUID.ToString();
                m_ActionName = name;
            }
        }

        [Serializable]
        public class DeviceLostEvent : UnityEvent<PlayerInput>
        {
        }

        [Serializable]
        public class DeviceRegainedEvent : UnityEvent<PlayerInput>
        {
        }

        [Serializable]
        public class ControlSchemeChangeEvent : UnityEvent<PlayerInput>
        {
        }
    }
}
