using System;
using UnityEngine.Events;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

////REVIEW: should we automatically pool/retain up to maxPlayerCount player instances?

////REVIEW: the join/leave messages should probably give a *GameObject* rather than the PlayerInput component (which can be gotten to via a simple GetComponent(InChildren) call)

////TODO: add support for reacting to players missing devices

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Manages joining and leaving of players.
    /// </summary>
    /// <remarks>
    /// This is a singleton component. Only one instance is meant to be active in a game
    /// at any one time. To retrieve the current instance, use <see cref="instance"/>.
    ///
    /// Note that a PlayerInputManager is not strictly required to have multiple <see cref="PlayerInput"/> components.
    /// What PlayerInputManager provides is the implementation of specific player join mechanisms
    /// (<see cref="joinBehavior"/>) as well as automatic assignment of split-screen areas (<see cref="splitScreen"/>).
    /// However, you can always implement your own custom logic instead and simply instantiate multiple GameObjects with
    /// <see cref="PlayerInput"/> yourself.
    /// </remarks>
    [AddComponentMenu("Input/Player Input Manager")]
    [HelpURL(InputSystem.kDocUrl + "/manual/PlayerInputManager.html")]
    public class PlayerInputManager : MonoBehaviour
    {
        /// <summary>
        /// Name of the message that is sent when a player joins the game.
        /// </summary>
        public const string PlayerJoinedMessage = "OnPlayerJoined";

        public const string PlayerLeftMessage = "OnPlayerLeft";

        /// <summary>
        /// If enabled, each player will automatically be assigned a portion of the available screen area.
        /// </summary>
        /// <remarks>
        /// For this to work, each <see cref="PlayerInput"/> component must have an associated <see cref="Camera"/>
        /// object through <see cref="PlayerInput.camera"/>.
        ///
        /// Note that as player join, the screen may be increasingly subdivided and players may see their
        /// previous screen area getting resized.
        /// </remarks>
        public bool splitScreen
        {
            get => m_SplitScreen;
            set
            {
                if (m_SplitScreen == value)
                    return;

                m_SplitScreen = value;

                if (!m_SplitScreen)
                {
                    // Reset rects on all player cameras.
                    foreach (var player in PlayerInput.all)
                    {
                        var camera = player.camera;
                        if (camera != null)
                            camera.rect = new Rect(0, 0, 1, 1);
                    }
                }
                else
                {
                    UpdateSplitScreen();
                }
            }
        }

        ////REVIEW: we probably need support for filling unused screen areas automatically
        /// <summary>
        /// If <see cref="splitScreen"/> is enabled, this property determines whether subdividing the screen is allowed to
        /// produce screen areas that have an aspect ratio different from the screen resolution.
        /// </summary>
        /// <remarks>
        /// By default, when <see cref="splitScreen"/> is enabled, the manager will add or remove screen subdivisions in
        /// steps of two. This means that when, for example, the second player is added, the screen will be subdivided into
        /// a left and a right screen area; the left one allocated to the first player and the right one allocated to the
        /// second player.
        ///
        /// This behavior makes optimal use of screen real estate but will result in screen areas that have aspect ratios
        /// different from the screen resolution. If this is not acceptable, this property can be set to true to enforce
        /// split-screen to only create screen areas that have the same aspect ratio of the screen.
        ///
        /// This results in the screen being subdivided more aggressively. When, for example, a second player is added,
        /// the screen will immediately be divided into a four-way split-screen setup with the lower two screen areas
        /// not being used.
        ///
        /// This property is irrelevant if <see cref="fixedNumberOfSplitScreens"/> is used.
        /// </remarks>
        public bool maintainAspectRatioInSplitScreen => m_MaintainAspectRatioInSplitScreen;

        /// <summary>
        /// If <see cref="splitScreen"/> is enabled, this property determines how many screen divisions there will be.
        /// </summary>
        /// <remarks>
        /// This is only used if <see cref="splitScreen"/> is true.
        ///
        /// By default this is set to -1 which means the screen will automatically be divided to best fit the
        /// current number of players i.e. the highest player index in <see cref="PlayerInput"/>
        /// </remarks>
        public int fixedNumberOfSplitScreens => m_FixedNumberOfSplitScreens;

        /// <summary>
        /// The normalized screen rectangle available for allocating player split-screens into.
        /// </summary>
        /// <remarks>
        /// This is only used if <see cref="splitScreen"/> is true.
        ///
        /// By default it is set to <c>(0,0,1,1)</c>, i.e. the entire screen area will be used for player screens.
        /// If, for example, part of the screen should display a UI/information shared by all players, this
        /// property can be used to cut off the area and not have it used by PlayerInputManager.
        /// </remarks>
        public Rect splitScreenArea => m_SplitScreenRect;

        /// <summary>
        /// The current number of active players.
        /// </summary>
        /// <remarks>
        /// This count corresponds to all <see cref="PlayerInput"/> instances that are currently enabled.
        /// </remarks>
        public int playerCount => PlayerInput.s_AllActivePlayersCount;

        ////FIXME: this needs to be settable
        /// <summary>
        /// Maximum number of players allowed concurrently in the game.
        /// </summary>
        /// <remarks>
        /// If this limit is reached, joining is turned off automatically.
        ///
        /// By default this is set to -1. Any negative value deactivates the player limit and allows
        /// arbitrary many players to join.
        /// </remarks>
        public int maxPlayerCount => m_MaxPlayerCount;

        /// <summary>
        /// Whether new players can currently join.
        /// </summary>
        /// <remarks>
        /// While this is true, new players can join via the mechanism determined by <see cref="joinBehavior"/>.
        /// </remarks>
        /// <seealso cref="EnableJoining"/>
        /// <seealso cref="DisableJoining"/>
        public bool joiningEnabled => m_AllowJoining;

        /// <summary>
        /// Determines the mechanism by which players can join when joining is enabled (<see cref="joiningEnabled"/>).
        /// </summary>
        /// <remarks>
        /// </remarks>
        public PlayerJoinBehavior joinBehavior
        {
            get => m_JoinBehavior;
            set
            {
                if (m_JoinBehavior == value)
                    return;

                var joiningEnabled = m_AllowJoining;
                if (joiningEnabled)
                    DisableJoining();
                m_JoinBehavior = value;
                if (joiningEnabled)
                    EnableJoining();
            }
        }

        /// <summary>
        /// The input action that a player must trigger to join the game.
        /// </summary>
        /// <remarks>
        /// If the join action is a reference to an existing input action, it will be cloned when the PlayerInputManager
        /// is enabled. This avoids the situation where the join action can become disabled after the first user joins which
        /// can happen when the join action is the same as a player in-game action. When a player joins, input bindings from
        /// devices other than the device they joined with are disabled. If the join action had a binding for keyboard and one
        /// for gamepad for example, and the first player joined using the keyboard, the expectation is that the next player
        /// could still join by pressing the gamepad join button. Without the cloning behavior, the gamepad input would have
        /// been disabled.
        ///
        /// For more details about joining behavior, see <see cref="PlayerInput"/>.
        /// </remarks>
        public InputActionProperty joinAction
        {
            get => m_JoinAction;
            set
            {
                if (m_JoinAction == value)
                    return;

                ////REVIEW: should we suppress notifications for temporary disables?

                var joinEnabled = m_AllowJoining && m_JoinBehavior == PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
                if (joinEnabled)
                    DisableJoining();

                m_JoinAction = value;

                if (joinEnabled)
                    EnableJoining();
            }
        }

        public PlayerNotifications notificationBehavior
        {
            get => m_NotificationBehavior;
            set => m_NotificationBehavior = value;
        }

        public PlayerJoinedEvent playerJoinedEvent
        {
            get
            {
                if (m_PlayerJoinedEvent == null)
                    m_PlayerJoinedEvent = new PlayerJoinedEvent();
                return m_PlayerJoinedEvent;
            }
        }

        public PlayerLeftEvent playerLeftEvent
        {
            get
            {
                if (m_PlayerLeftEvent == null)
                    m_PlayerLeftEvent = new PlayerLeftEvent();
                return m_PlayerLeftEvent;
            }
        }

        public event Action<PlayerInput> onPlayerJoined
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerJoinedCallbacks.AddCallback(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerJoinedCallbacks.RemoveCallback(value);
            }
        }

        public event Action<PlayerInput> onPlayerLeft
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerLeftCallbacks.AddCallback(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerLeftCallbacks.RemoveCallback(value);
            }
        }

        /// <summary>
        /// Reference to the prefab that the manager will instantiate when players join.
        /// </summary>
        /// <value>Prefab to instantiate for new players.</value>
        public GameObject playerPrefab
        {
            get => m_PlayerPrefab;
            set => m_PlayerPrefab = value;
        }

        /// <summary>
        /// Singleton instance of the manager.
        /// </summary>
        /// <value>Singleton instance or null.</value>
        public static PlayerInputManager instance { get; private set; }

        /// <summary>
        /// Allow players to join the game based on <see cref="joinBehavior"/>.
        /// </summary>
        /// <seealso cref="DisableJoining"/>
        /// <seealso cref="joiningEnabled"/>
        public void EnableJoining()
        {
            switch (m_JoinBehavior)
            {
                case PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed:
                    ValidateInputActionAsset();

                    if (!m_UnpairedDeviceUsedDelegateHooked)
                    {
                        if (m_UnpairedDeviceUsedDelegate == null)
                            m_UnpairedDeviceUsedDelegate = OnUnpairedDeviceUsed;
                        InputUser.onUnpairedDeviceUsed += m_UnpairedDeviceUsedDelegate;
                        m_UnpairedDeviceUsedDelegateHooked = true;
                        ++InputUser.listenForUnpairedDeviceActivity;
                    }
                    break;

                case PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered:
                    // Hook into join action if we have one.
                    if (m_JoinAction.action != null)
                    {
                        if (!m_JoinActionDelegateHooked)
                        {
                            if (m_JoinActionDelegate == null)
                                m_JoinActionDelegate = JoinPlayerFromActionIfNotAlreadyJoined;
                            m_JoinAction.action.performed += m_JoinActionDelegate;
                            m_JoinActionDelegateHooked = true;
                        }
                        m_JoinAction.action.Enable();
                    }
                    else
                    {
                        Debug.LogError(
                            $"No join action configured on PlayerInputManager but join behavior is set to {nameof(PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered)}",
                            this);
                    }
                    break;
            }

            m_AllowJoining = true;
        }

        /// <summary>
        /// Inhibit players from joining the game.
        /// </summary>
        /// <remarks>
        /// Note that this method might disable the action, depending on how the player
        /// joined initially. Specifically, if the initial joining was triggered using
        /// the <see cref="PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered"/> behavior,
        /// this method also disables the join action.
        /// </remarks>
        /// <seealso cref="EnableJoining"/>
        /// <seealso cref="joiningEnabled"/>
        public void DisableJoining()
        {
            switch (m_JoinBehavior)
            {
                case PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed:
                    if (m_UnpairedDeviceUsedDelegateHooked)
                    {
                        InputUser.onUnpairedDeviceUsed -= m_UnpairedDeviceUsedDelegate;
                        m_UnpairedDeviceUsedDelegateHooked = false;
                        --InputUser.listenForUnpairedDeviceActivity;
                    }
                    break;

                case PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered:
                    if (m_JoinActionDelegateHooked)
                    {
                        var joinAction = m_JoinAction.action;
                        if (joinAction != null)
                            m_JoinAction.action.performed -= m_JoinActionDelegate;
                        m_JoinActionDelegateHooked = false;
                    }
                    m_JoinAction.action?.Disable();
                    break;
            }

            m_AllowJoining = false;
        }

        ////TODO
        /// <summary>
        /// Join a new player based on input on a UI element.
        /// </summary>
        /// <remarks>
        /// This should be called directly from a UI callback such as <see cref="Button.onClick"/>. The device
        /// that the player joins with is taken from the device that was used to interact with the UI element.
        /// </remarks>
        internal void JoinPlayerFromUI()
        {
            if (!CheckIfPlayerCanJoin())
                return;

            //find used device; InputSystemUIInputModule should probably make that available

            throw new NotImplementedException();
        }

        /// <summary>
        /// Join a new player based on input received through an <see cref="InputAction"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <remarks>
        /// </remarks>
        public void JoinPlayerFromAction(InputAction.CallbackContext context)
        {
            if (!CheckIfPlayerCanJoin())
                return;

            var device = context.control.device;
            JoinPlayer(pairWithDevice: device);
        }

        public void JoinPlayerFromActionIfNotAlreadyJoined(InputAction.CallbackContext context)
        {
            if (!CheckIfPlayerCanJoin())
                return;

            var device = context.control.device;
            if (PlayerInput.FindFirstPairedToDevice(device) != null)
                return;

            JoinPlayer(pairWithDevice: device);
        }

        /// <summary>
        /// Spawn a new player from <see cref="playerPrefab"/>.
        /// </summary>
        /// <param name="playerIndex">Optional explicit <see cref="PlayerInput.playerIndex"/> to assign to the player. Must be unique within
        /// <see cref="PlayerInput.all"/>. If not supplied, a player index will be assigned automatically (smallest unused index will be used).</param>
        /// <param name="splitScreenIndex">Optional <see cref="PlayerInput.splitScreenIndex"/>. If supplied, this assigns a split-screen area to the player. For example,
        /// a split-screen index of </param>
        /// <param name="controlScheme">Control scheme to activate on the player (optional). If not supplied, a control scheme will
        /// be selected based on <paramref name="pairWithDevice"/>. If no device is given either, the first control scheme that matches
        /// the currently available unpaired devices (see <see cref="InputUser.GetUnpairedInputDevices()"/>) is used.</param>
        /// <param name="pairWithDevice">Device to pair to the player. Also determines which control scheme to use if <paramref name="controlScheme"/>
        /// is not given.</param>
        /// <returns>The newly instantiated player or <c>null</c> if joining failed.</returns>
        /// <remarks>
        /// Joining must be enabled (see <see cref="joiningEnabled"/>) or the method will fail.
        ///
        /// To pair multiple devices, use <see cref="JoinPlayer(int,int,string,InputDevice[])"/>.
        /// </remarks>
        public PlayerInput JoinPlayer(int playerIndex = -1, int splitScreenIndex = -1, string controlScheme = null, InputDevice pairWithDevice = null)
        {
            if (!CheckIfPlayerCanJoin(playerIndex))
                return null;

            PlayerInput.s_DestroyIfDeviceSetupUnsuccessful = true;
            return PlayerInput.Instantiate(m_PlayerPrefab, playerIndex: playerIndex, splitScreenIndex: splitScreenIndex,
                controlScheme: controlScheme, pairWithDevice: pairWithDevice);
        }

        /// <summary>
        /// Spawn a new player from <see cref="playerPrefab"/>.
        /// </summary>
        /// <param name="playerIndex">Optional explicit <see cref="PlayerInput.playerIndex"/> to assign to the player. Must be unique within
        /// <see cref="PlayerInput.all"/>. If not supplied, a player index will be assigned automatically (smallest unused index will be used).</param>
        /// <param name="splitScreenIndex">Optional <see cref="PlayerInput.splitScreenIndex"/>. If supplied, this assigns a split-screen area to the player. For example,
        /// a split-screen index of </param>
        /// <param name="controlScheme">Control scheme to activate on the player (optional). If not supplied, a control scheme will
        /// be selected based on <paramref name="pairWithDevices"/>. If no device is given either, the first control scheme that matches
        /// the currently available unpaired devices (see <see cref="InputUser.GetUnpairedInputDevices()"/>) is used.</param>
        /// <param name="pairWithDevices">Devices to pair to the player. Also determines which control scheme to use if <paramref name="controlScheme"/>
        /// is not given.</param>
        /// <returns>The newly instantiated player or <c>null</c> if joining failed.</returns>
        /// <remarks>
        /// Joining must be enabled (see <see cref="joiningEnabled"/>) or the method will fail.
        /// </remarks>
        public PlayerInput JoinPlayer(int playerIndex = -1, int splitScreenIndex = -1, string controlScheme = null, params InputDevice[] pairWithDevices)
        {
            if (!CheckIfPlayerCanJoin(playerIndex))
                return null;

            PlayerInput.s_DestroyIfDeviceSetupUnsuccessful = true;
            return PlayerInput.Instantiate(m_PlayerPrefab, playerIndex: playerIndex, splitScreenIndex: splitScreenIndex,
                controlScheme: controlScheme, pairWithDevices: pairWithDevices);
        }

        [SerializeField] internal PlayerNotifications m_NotificationBehavior;
        [Tooltip("Set a limit for the maximum number of players who are able to join.")]
        [SerializeField] internal int m_MaxPlayerCount = -1;
        [SerializeField] internal bool m_AllowJoining = true;
        [SerializeField] internal PlayerJoinBehavior m_JoinBehavior;
        [SerializeField] internal PlayerJoinedEvent m_PlayerJoinedEvent;
        [SerializeField] internal PlayerLeftEvent m_PlayerLeftEvent;
        [SerializeField] internal InputActionProperty m_JoinAction;
        [SerializeField] internal GameObject m_PlayerPrefab;
        [SerializeField] internal bool m_SplitScreen;
        [SerializeField] internal bool m_MaintainAspectRatioInSplitScreen;
        [Tooltip("Explicitly set a fixed number of screens or otherwise allow the screen to be divided automatically to best fit the number of players.")]
        [SerializeField] internal int m_FixedNumberOfSplitScreens = -1;
        [SerializeField] internal Rect m_SplitScreenRect = new Rect(0, 0, 1, 1);

        [NonSerialized] private bool m_JoinActionDelegateHooked;
        [NonSerialized] private bool m_UnpairedDeviceUsedDelegateHooked;
        [NonSerialized] private Action<InputAction.CallbackContext> m_JoinActionDelegate;
        [NonSerialized] private Action<InputControl, InputEventPtr> m_UnpairedDeviceUsedDelegate;
        [NonSerialized] private CallbackArray<Action<PlayerInput>> m_PlayerJoinedCallbacks;
        [NonSerialized] private CallbackArray<Action<PlayerInput>> m_PlayerLeftCallbacks;

        internal static string[] messages => new[]
        {
            PlayerJoinedMessage,
            PlayerLeftMessage,
        };

        private bool CheckIfPlayerCanJoin(int playerIndex = -1)
        {
            if (m_PlayerPrefab == null)
            {
                Debug.LogError("playerPrefab must be set in order to be able to join new players", this);
                return false;
            }

            if (m_MaxPlayerCount >= 0 && playerCount >= m_MaxPlayerCount)
            {
                Debug.LogWarning("Maximum number of supported players reached: " + maxPlayerCount, this);
                return false;
            }

            // If we have a player index, make sure it's unique.
            if (playerIndex != -1)
            {
                for (var i = 0; i < PlayerInput.s_AllActivePlayersCount; ++i)
                    if (PlayerInput.s_AllActivePlayers[i].playerIndex == playerIndex)
                    {
                        Debug.LogError(
                            $"Player index #{playerIndex} is already taken by player {PlayerInput.s_AllActivePlayers[i]}",
                            PlayerInput.s_AllActivePlayers[i]);
                        return false;
                    }
            }

            return true;
        }

        private void OnUnpairedDeviceUsed(InputControl control, InputEventPtr eventPtr)
        {
            if (!m_AllowJoining)
                return;

            if (m_JoinBehavior == PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed)
            {
                // Make sure it's a button that was actuated.
                if (!(control is ButtonControl))
                    return;

                // Make sure it's a device that is usable by the player's actions. We don't want
                // to join a player who's then stranded and has no way to actually interact with the game.
                if (!IsDeviceUsableWithPlayerActions(control.device))
                    return;

                ////REVIEW: should we log a warning or error when the actions for the player do not have control schemes?

                JoinPlayer(pairWithDevice: control.device);
            }
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple PlayerInputManagers in the game. There should only be one PlayerInputManager", this);
                return;
            }

            // if the join action is a reference, clone it so we don't run into problems with the action being disabled by
            // PlayerInput when devices are assigned to individual players
            if (joinAction.reference != null && joinAction.action?.actionMap?.asset != null)
            {
                var inputActionAsset = Instantiate(joinAction.action.actionMap.asset);
                var inputActionReference = InputActionReference.Create(inputActionAsset.FindAction(joinAction.action.name));
                joinAction = new InputActionProperty(inputActionReference);
            }

            // Join all players already in the game.
            for (var i = 0; i < PlayerInput.s_AllActivePlayersCount; ++i)
                NotifyPlayerJoined(PlayerInput.s_AllActivePlayers[i]);

            if (m_AllowJoining)
                EnableJoining();
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;

            if (m_AllowJoining)
                DisableJoining();
        }

        /// <summary>
        /// If split-screen is enabled, then for each player in the game, adjust the player's <see cref="Camera.rect"/>
        /// to fit the player's split screen area according to the number of players currently in the game and the
        /// current split-screen configuration.
        /// </summary>
        private void UpdateSplitScreen()
        {
            // Nothing to do if split-screen is not enabled.
            if (!m_SplitScreen)
                return;

            // Determine number of split-screens to create based on highest player index we have.
            var minSplitScreenCount = 0;
            foreach (var player in PlayerInput.all)
            {
                if (player.playerIndex >= minSplitScreenCount)
                    minSplitScreenCount = player.playerIndex + 1;
            }

            // Adjust to fixed number if we have it.
            if (m_FixedNumberOfSplitScreens > 0)
            {
                if (m_FixedNumberOfSplitScreens < minSplitScreenCount)
                    Debug.LogWarning(
                        $"Highest playerIndex of {minSplitScreenCount} exceeds fixed number of split-screens of {m_FixedNumberOfSplitScreens}",
                        this);

                minSplitScreenCount = m_FixedNumberOfSplitScreens;
            }

            // Determine divisions along X and Y. Usually, we have a square grid of split-screens so all we need to
            // do is make it large enough to fit all players.
            var numDivisionsX = Mathf.CeilToInt(Mathf.Sqrt(minSplitScreenCount));
            var numDivisionsY = numDivisionsX;
            if (!m_MaintainAspectRatioInSplitScreen && numDivisionsX * (numDivisionsX - 1) >= minSplitScreenCount)
            {
                // We're allowed to produce split-screens with aspect ratios different from the screen meaning
                // that we always add one more column before finally adding an entirely new row.
                numDivisionsY -= 1;
            }

            // Assign split-screen area to each player.
            foreach (var player in PlayerInput.all)
            {
                // Make sure the player's splitScreenIndex isn't out of range.
                var splitScreenIndex = player.splitScreenIndex;
                if (splitScreenIndex >= numDivisionsX * numDivisionsY)
                {
                    Debug.LogError(
                        $"Split-screen index of {splitScreenIndex} on player is out of range (have {numDivisionsX * numDivisionsY} screens); resetting to playerIndex",
                        player);
                    player.m_SplitScreenIndex = player.playerIndex;
                }

                // Make sure we have a camera.
                var camera = player.camera;
                if (camera == null)
                {
                    Debug.LogError(
                        "Player has no camera associated with it. Cannot set up split-screen. Point PlayerInput.camera to camera for player.",
                        player);
                    continue;
                }

                // Assign split-screen area based on m_SplitScreenRect.
                var column = splitScreenIndex % numDivisionsX;
                var row = splitScreenIndex / numDivisionsX;
                var rect = new Rect
                {
                    width = m_SplitScreenRect.width / numDivisionsX,
                    height = m_SplitScreenRect.height / numDivisionsY
                };
                rect.x = m_SplitScreenRect.x + column * rect.width;
                // Y is bottom-to-top but we fill from top down.
                rect.y = m_SplitScreenRect.y + m_SplitScreenRect.height - (row + 1) * rect.height;
                camera.rect = rect;
            }
        }

        private bool IsDeviceUsableWithPlayerActions(InputDevice device)
        {
            Debug.Assert(device != null);

            if (m_PlayerPrefab == null)
                return true;

            var playerInput = m_PlayerPrefab.GetComponentInChildren<PlayerInput>();
            if (playerInput == null)
                return true;

            var actions = playerInput.actions;
            if (actions == null)
                return true;

            // If the asset has control schemes, see if there's one that works with the device plus
            // whatever unpaired devices we have left.
            if (actions.controlSchemes.Count > 0)
            {
                using (var unpairedDevices = InputUser.GetUnpairedInputDevices())
                {
                    if (InputControlScheme.FindControlSchemeForDevices(unpairedDevices, actions.controlSchemes,
                        mustIncludeDevice: device) == null)
                        return false;
                }
                return true;
            }

            // Otherwise just check whether any of the maps has bindings usable with the device.
            foreach (var actionMap in actions.actionMaps)
                if (actionMap.IsUsableWithDevice(device))
                    return true;

            return false;
        }

        private void ValidateInputActionAsset()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_PlayerPrefab == null || m_PlayerPrefab.GetComponentInChildren<PlayerInput>() == null)
                return;

            var actions = m_PlayerPrefab.GetComponentInChildren<PlayerInput>().actions;
            if (actions == null)
                return;

            var isValid = true;
            foreach (var controlScheme in actions.controlSchemes)
            {
                if (controlScheme.deviceRequirements.Count > 0)
                    break;

                isValid = false;
            }

            if (isValid) return;

            var assetInfo = actions.name;
#if UNITY_EDITOR
            assetInfo = AssetDatabase.GetAssetPath(actions);
#endif
            Debug.LogWarning($"The input action asset '{assetInfo}' in the player prefab assigned to PlayerInputManager has " +
                "no control schemes with required devices. The JoinPlayersWhenButtonIsPressed join behavior " +
                "will not work unless the expected input devices are listed as requirements in the input " +
                "action asset.", m_PlayerPrefab);
#endif
        }

        /// <summary>
        /// Called by <see cref="PlayerInput"/> when it is enabled.
        /// </summary>
        /// <param name="player"></param>
        internal void NotifyPlayerJoined(PlayerInput player)
        {
            Debug.Assert(player != null);

            UpdateSplitScreen();

            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(PlayerJoinedMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(PlayerJoinedMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_PlayerJoinedEvent?.Invoke(player);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_PlayerJoinedCallbacks, player, "onPlayerJoined");
                    break;
            }
        }

        /// <summary>
        /// Called by <see cref="PlayerInput"/> when it is disabled.
        /// </summary>
        /// <param name="player"></param>
        internal void NotifyPlayerLeft(PlayerInput player)
        {
            Debug.Assert(player != null);

            UpdateSplitScreen();

            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(PlayerLeftMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(PlayerLeftMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_PlayerLeftEvent?.Invoke(player);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_PlayerLeftCallbacks, player, "onPlayerLeft");
                    break;
            }
        }

        [Serializable]
        public class PlayerJoinedEvent : UnityEvent<PlayerInput>
        {
        }

        [Serializable]
        public class PlayerLeftEvent : UnityEvent<PlayerInput>
        {
        }
    }
}
