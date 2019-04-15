using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;
#if ENABLE_VR
using UnityEngine.XR;
#endif
using InputDevice = UnityEngine.Experimental.Input.InputDevice;

#if UNITY_EDITOR
using UnityEditor;
#endif

////WIP; not functional ATM

/// <summary>
/// Main controller for the demo game.
/// </summary>
/// <remarks>
/// This is a lobby style local multi-player game that can be played with one or more players.
/// When a game is started, players can join by pressing buttons on devices. As more players join,
/// the screen is increasingly subdivided in classic split-screen fashion. The number of players
/// is not limited but the screen area for players may get very small.
///
/// The game is started when all players that joined indicate readiness.
///
/// While in the main menu, all local devices supported by the game can be used regardless of who
/// they are paired to at the platform (if at all). However, the device that clicks the "Start Game"
/// button will automatically perform a join on the first player.
/// </remarks>
public class DemoGame : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject fishPrefab;

    public Canvas mainMenuCanvas;
    public Camera mainMenuCamera;
    public GameObject startGameButton;
    public UIActionInputModule uiInputModule;

    /// <summary>
    /// The possible states that the game can be in as a whole.
    /// </summary>
    public enum State
    {
        Invalid,

        /// <summary>
        /// In main menu and thus outside the game.
        /// </summary>
        InMainMenu,

        /// <summary>
        /// In game but not yet started. Waiting for players to join and indicate readiness.
        /// </summary>
        /// <remarks>
        /// At this point, we still don't yet know whether it's going to be a multi-player or single-player
        /// game. If only a single player joins and indicates readiness, we start a single-player game.
        /// If multiple players join and indicate readiness, we start a multi-player game.
        /// </remarks>
        InLobby,

        /// <summary>
        /// A single-player game is in progress.
        /// </summary>
        /// <remarks>
        /// This state is chosen if the game is started and only a single player has joined.
        /// </remarks>
        InSinglePlayerGame,

        /// <summary>
        /// A multi-player game is in progress.
        /// </summary>
        /// <remarks>
        /// This state is chosen if the game is started and multiple players have joined.
        ///
        /// Note that if players leave during a multi-player game, we may be left with only a single player
        /// in the end. However, the game will still operate as a multi-player game where players cannot freely
        /// switch between input devices.
        /// </remarks>
        InMultiPlayerGame,

        /// <summary>
        /// A game has finished and we are displaying the final game summary screen.
        /// </summary>
        InGameOver,
    }

    /// <summary>
    /// The current state of the game as a whole.
    /// </summary>
    /// <remarks>
    /// Indicates whether we're the menu, the lobby, or the game.
    /// </remarks>
    public State state
    {
        get { return m_State; }
    }

    /// <summary>
    /// Whether a game is currently in progress.
    /// </summary>
    /// <remarks>
    /// True if either a single-player or a multi-player game is ongoing.
    /// </remarks>
    public bool isInGame
    {
        get { return isInSinglePlayerGame || isInMultiPlayerGame; }
    }

    /// <summary>
    /// Whether we're currently in a single-player game.
    /// </summary>
    public bool isInSinglePlayerGame
    {
        get { return m_State == State.InSinglePlayerGame; }
    }

    /// <summary>
    /// Whether we're currently in a multi-player game.
    /// </summary>
    public bool isInMultiPlayerGame
    {
        get { return m_State == State.InMultiPlayerGame; }
    }

    /// <summary>
    /// Whether we're currently in a game and it has been paused.
    /// </summary>
    /// <remarks>
    /// We pause the game if a player loses a device paired to the player. We also pause if all
    /// players are in the menu.
    /// </remarks>
    public bool isPaused
    {
        get { return isInGame && m_Paused; }
    }

    /// <summary>
    /// List of players currently joined in the game.
    /// </summary>
    public ReadOnlyArray<DemoPlayerController> players
    {
        get { return new ReadOnlyArray<DemoPlayerController>(m_Players, 0, m_ActivePlayerCount); }
    }

    public DemoFishController fish
    {
        get { return m_Fish; }
    }

    /// <summary>
    /// Platform we are running on.
    /// </summary>
    /// <remarks>
    /// In actual deployment, this will always be the same as <see cref="Application.platform"/>.
    /// During tests, we may set this to a platform other than the one we are actually running on.
    ///
    /// The game supports Windows, Mac, Linux, WebGL, Android, iOS, Xbox, PS4, Switch, and UWP.
    /// On each platform, the game can be played in both single-player and multi-player setup.
    /// VR is supported where applicable (<see cref="vrSupported"/>).
    /// </remarks>
    public static RuntimePlatform platform
    {
        get
        {
            if (!s_Platform.HasValue)
                return Application.platform;
            return s_Platform.Value;
        }
        set { s_Platform = value; }
    }

    public static bool vrSupported
    {
        get
        {
            if (!s_VRSupported.HasValue)
#if ENABLE_VR
                return XRSettings.enabled;
#else
                return false;
#endif
            return s_VRSupported.Value;
        }
        set { s_VRSupported = value; }
    }

    /// <summary>
    /// The game singleton.
    /// </summary>
    public static DemoGame instance
    {
        get { return s_Instance; }
    }

    private static DemoGame s_Instance;

    private static RuntimePlatform? s_Platform;
    private static bool? s_VRSupported;

    private int m_ActivePlayerCount;
    private DemoPlayerController[] m_Players;
    private DemoFishController m_Fish;
    private State m_State;
    private bool m_Paused;

    public void Awake()
    {
        s_Instance = this;

        // Have it let us know then the user setup in the system changes.
        InputUser.onChange += OnUserChange;

        // And have it tell us when someone uses a device not paired to any user. Note that
        // we do not yet turn on InputUserSupport.listenForUnpairedDeviceActivity. We do only
        // during certain times.
        InputUser.onUnpairedDeviceUsed += OnUnpairedInputDeviceUsed;
    }

    public void OnDestroy()
    {
        InputUser.onChange -= OnUserChange;
        InputUser.onUnpairedDeviceUsed -= OnUnpairedInputDeviceUsed;
        s_Instance = null;
    }

    /// <summary>
    /// Bring up the main menu after the game has been launched.
    /// </summary>
    public void Start()
    {
        ChangeState(State.InMainMenu);
    }

    /// <summary>
    /// Invoked from the "Quit" button in the main menu.
    /// </summary>
    public void OnQuitButton()
    {
        Debug.Assert(state == State.InMainMenu);

        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Invoked from the "Start Game" button in the main menu.
    /// </summary>
    /// <remarks>
    /// To start a game, we always go through the lobby. If the user simply presses the fire button
    /// again, we launch right into the game.
    /// </remarks>
    public void OnStartGameButton()
    {
        Debug.Assert(state == State.InMainMenu);

        ////TODO: this should be something that UIActionInputModule can give us without us querying the action directly
        // Find out which device clicked the "Start Game" button. We use this to
        // automatically join the player instead of requiring the player who clicked to
        // then issue an explicit join.
        // NOTE: The button can be triggered either by the "click" (e.g. mouse) action or by the "submit" (e.g. gamepad)
        //       button so we look at both.
        var clickAction = uiInputModule.leftClick.action;
        var submitAction = uiInputModule.submit.action;
        var startGameClickedByControl = clickAction.lastTriggerTime > submitAction.lastTriggerTime
            ? clickAction.lastTriggerControl
            : submitAction.lastTriggerControl;
        Debug.Assert(startGameClickedByControl != null);
        var playerDevice = startGameClickedByControl.device;

        // Perform pairing. This will either succeed instantly or it will bring up an account
        // picker. If the user cancels, we do nothing and just stay in the main menu. If the user
        // picks an account, we end up in OnUserChange().
        InputUser.PerformPairingWithDevice(playerDevice);
    }

    /// <summary>
    /// Called when a device is used that isn't currently paired to any user.
    /// </summary>
    /// <param name="control">Control that was actuated.</param>
    /// <remarks>
    ///
    ///
    /// Note that we get here even if, at the platform level, devices are always paired to users. This is
    /// the case on PS4, for example. However, as long as we have not called <see cref="InputUser.PerformPairingWithDevice"/>,
    /// we consider an <see cref="InputDevice"/> unpaired.
    ///
    ///
    ///
    /// In single-player mode, we concurrently enable all our bindings from all available control schemes.
    /// This means that the actions will bind to whatever devices are available. However, we only assign
    /// the devices actively used with the current control scheme to the user. This means that we always know
    /// how the player is currently controlling the game.
    ///
    /// When the player uses a binding that isn't part of the current control scheme, this method will
    /// be called. In here we automatically switch control schemes by looking at which control scheme is
    /// suited to the unassigned device.
    ///
    /// Note that the logic here also covers the case where there are multiple devices meant to be used
    /// with the same control scheme. For example, there may be two gamepads and the player is free to
    /// switch from one or the other. In that case, while we will stay on the Gamepad control scheme,
    /// we will still unassign the previously used gamepad from the player and assign the newly used one.
    /// </remarks>
    private void OnUnpairedInputDeviceUsed(InputControl control)
    {
        // We should only listen for unpaired device activity when we're either in the lobby
        // or in single-player mode. In a multi-player game, we turn it off and simply ignore whatever
        // input is coming in on devices that no one has joined the game with. We don't support joining
        // a game in progress.
        Debug.Assert(state == State.InLobby || state == State.InSinglePlayerGame);

        switch (state)
        {
            case State.InLobby:
            {
                // Joins can only be initiated from buttons, not from things like wiggling the sticks
                // on a gamepad.
                if (!(control is ButtonControl))
                    return;

                // NOTE: If we supported having players select additional devices necessary for a control scheme
                //       by pressing a button on them, this is where we'd do it. However, we keep things simpler
                //       and pair additional devices automatically from what's available on the system.

                // A new player is joining on a device that currently isn't paired to a user.
                // Initiate pairing. When we have an associated user, we join the player to the
                // game.
                //
                // On consoles, this may actually bring up an account picker. On other platforms,
                // we will generally go straight into receiving an OnUserChange() callback.
                InputUser.PerformPairingWithDevice(control.device);
                break;
            }

            ////REVIEW: for single-player, it would be cheaper to enable bindings such that they bind to everything
            ////        available in the system and then detect when a binding is used that isn't part of the current
            ////        control scheme (i.e. the way we were doing it before InputUser got refactored)
            case State.InSinglePlayerGame:
            {
                // In single-player, we allow the player to switch between whatever devices
                // are present locally so we may be looking at a situation where the player has
                // just switched to a different device (e.g. moved from keyboard&mouse to gamepad).
                // See if we support the device the player is using and if so, switch to it.
                //
                // NOTE: As we do not have a dedicated single-player mode (it wouldn't really increase the value
                //       of the demo, we have the slightly odd situation that we allow a single player to
                //       freely switch devices while in the game but we do not allow it while in the lobby -- as
                //       we can't tell whether it's a join or just the first player switching devices).

                Debug.Assert(players.Count == 1);
                var player = players[0];

                // See if we actually have a control scheme for the device that was used. No point
                // switching to the device if we can't do much with it.
                var device = control.device;
                if (player.SelectControlSchemeBasedOnDevice(device) == null)
                {
                    // No, we don't have a control scheme for this device. Ignore it.
                    ////REVIEW: flash a warning in the UI for a moment?
                    return;
                }

                // Pair the device to the user after unpairing the devices currently used.
                // NOTE: If we're on a platform where devices need a valid user account associated with them,
                //       we may bring up the account picker here,
                InputUser.PerformPairingWithDevice(device, user: player.user,
                    options: InputUserPairingOptions.UnpairCurrentDevicesFromUser);

                break;
            }
        }
    }

    /// <summary>
    /// Called when there's a change in the input user setup in the system.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="change"></param>
    /// <param name="device"></param>
    private void OnUserChange(InputUser user, InputUserChange change, InputDevice device)
    {
        var player = FindPlayerControllerForUser(user);
        switch (change)
        {
            // A player has switched accounts. This will only happen on platforms that have user account
            // management (PS4, Xbox, Switch). On PS4, for example, this can happen at any time by the
            // player pressing the PS4 button and switching accounts. We simply update the information
            // we display for the player's active user account.
            case InputUserChange.AccountChanged:
            {
                if (player != null)
                    player.OnUserAccountChanged();
                break;
            }

            // If the user has cancelled account selection, we remove the user if there's no devices
            // already paired to it. This usually happens when a player initiates a join on a device on
            // Xbox or Switch, has the account picker come up, but then cancels instead of making an
            // account selection. In this case, we want to cancel the join.
            // NOTE: We are only adding DemoPlayerControllers once device pairing is complete
            case InputUserChange.AccountSelectionCancelled:
            {
                if (user.pairedDevices.Count == 0)
                {
                    Debug.Assert(FindPlayerControllerForUser(user) == null);
                    user.UnpairDevicesAndRemoveUser();
                }
                break;
            }

            // An InputUser gained a new device. If we're in the lobby and don't yet have a player
            // for the user, it means a new player has joined. We don't join players until they have
            // a device paired to them which is why we ignore InputUserChange.Added and only react
            // to InputUserChange.DevicePaired instead.
            case InputUserChange.DevicePaired:
            {
                if (state == State.InLobby && player == null)
                {
                    OnPlayerJoins(user);
                }
                else if (player != null)
                {
                    player.OnDevicesOrBindingsHaveChanged();
                }
                break;
            }

            // Some player ran out of battery or unplugged a wired device.
            case InputUserChange.DeviceLost:
            {
                Debug.Assert(player != null);
                player.OnDeviceLost();

                ////REVIEW: should we unjoin a user when losing devices in the lobby?
                ////TODO: we need a way for other players to be able to resolve the situation

                // If we're currently in-game, we pause the game until the player has re-gained control.
                if (isInGame)
                    PauseGame();

                break;
            }

            // Some player has customized controls or had previously customized controls loaded.
            case InputUserChange.BindingsChanged:
            {
                player.OnDevicesOrBindingsHaveChanged();
                break;
            }
        }
    }

    private DemoPlayerController FindPlayerControllerForDevice(InputDevice device)
    {
        var user = InputUser.FindUserPairedToDevice(device);
        if (user == null)
            return null;

        return FindPlayerControllerForUser(user.Value);
    }

    private DemoPlayerController FindPlayerControllerForUser(InputUser user)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Called when a new player enters the lobby.
    /// </summary>
    /// <param name="user">Device the player is entering the lobby with. We always require device
    /// activity for a join.</param>
    /// <remarks>
    /// This should only be called while in the lobby.
    /// </remarks>
    public void OnPlayerJoins(InputUser user)
    {
        Debug.Assert(state == State.InLobby, "Joining can only happen while in the lobby");
        Debug.Assert(FindPlayerControllerForUser(user) == null, "There should not be two different players associated with the same InputUser");
        Debug.Assert(user.pairedDevices.Count > 0, "User should have at least one device that the join was initiated with");

        // If we still have inactive player objects, use those and bring an inactive
        // player back to life.
        DemoPlayerController player;
        if (m_Players != null && m_ActivePlayerCount < m_Players.Length && m_Players[m_ActivePlayerCount] != null)
        {
            // Reuse a player we've previously created. Just reactivate it and wipe its state.
            player = m_Players[m_ActivePlayerCount];
            player.gameObject.SetActive(true);
        }
        else
        {
            // Otherwise create a new player.
            var playerObject = Instantiate(playerPrefab);
            player = playerObject.GetComponent<DemoPlayerController>();
            if (player == null)
                throw new Exception("Missing DemoPlayerController component on " + playerObject);
            player.PerformOneTimeInitialization();

            // Add to list.
            if (m_Players == null || m_Players.Length == m_ActivePlayerCount)
                Array.Resize(ref m_Players, m_ActivePlayerCount + 10);
            m_Players[m_ActivePlayerCount] = player;
        }

        // Attempt to join the player based on the devices we have. If the device the player
        // joins on is part of a control scheme that requires additional devices, we may not
        // have all required devices.
        Debug.Assert(player.state == DemoPlayerController.State.Inactive);
        if (!player.OnJoin(user))
        {
            ////TODO: display feedback about missing devices
            user.UnpairDevicesAndRemoveUser();
            return;
        }

        ++m_ActivePlayerCount;

        // Whenever we add or remove players, we need to update the split-screen configuration.
        UpdateSplitScreen();
    }

    /// <summary>
    /// Called when a player that has joined the game indicates that he/she is ready to
    /// start the game. When all players have indicated readiness, the game starts.
    /// </summary>
    /// <param name="player"></param>
    /// <seealso cref="DemoPlayerController.State.Ready"/>
    public void OnPlayerIsReady(DemoPlayerController player)
    {
        Debug.Assert(m_ActivePlayerCount >= 1, "Must have at least one player");
        Debug.Assert(player != null);
        Debug.Assert(player.state == DemoPlayerController.State.Ready, "Player has not indicated readiness");
        Debug.Assert(state == State.InLobby, "Can only launch into the game from the lobby");

        // See if all players have indicated they're ready.
        for (var i = 0; i < m_ActivePlayerCount; ++i)
        {
            if (!m_Players[i].isReady)
            {
                // No, still some players that have joined but are not ready yet.
                return;
            }
        }

        // Yes, all players are ready. Start the game.
        if (m_ActivePlayerCount == 1)
            ChangeState(State.InSinglePlayerGame);
        else
            ChangeState(State.InMultiPlayerGame);
    }

    /// <summary>
    /// Called when a player selects the "Exit" menu item in the player's own menu.
    /// </summary>
    /// <param name="player">Player that chose to leave the game.</param>
    /// <remarks>
    /// If the last player that's in the game chooses to leave, the game ends without an end-game
    /// screen. Instead, it goes straight back to the main menu.
    /// </remarks>
    public void OnPlayerLeaves(DemoPlayerController player)
    {
        if (state == State.InLobby)
        {
            ////TODO
        }
        else
        {
            Debug.Assert(isInGame, "Must be in game or lobby for players to leave");
            ////TODO
        }
    }

    /// <summary>
    /// Assign screen areas to each player based on the number of currently active players.
    /// </summary>
    /// <remarks>
    /// The position within the player list determines the screen area that the player gets.
    /// If only a single player has joined, the player is assigned the full screen area.
    /// </remarks>
    private void UpdateSplitScreen()
    {
        ////TODO
    }

    /// <summary>
    /// Show or hide the main menu.
    /// </summary>
    /// <param name="value"></param>
    private void ShowMainMenu(bool value = true)
    {
        // Enabling or disabling the main menu canvas automatically enables or disables
        // the actions referenced by the UIActionInputModule sitting on the canvas. We've not
        // restricted them by a set of devices or set a binding mask on them so the actions will
        // go and grab whatever devices are present and matching the bindings we have. This means
        // that every local device can be used to drive the main menu.
        //
        // NOTE: On consoles, we do not require sign-in at this point. Even if the device is not
        //       paired to a user, we allow it to drive the UI. When it comes to starting an actual
        //       game, that's when we will care about pairing devices to users.

        mainMenuCamera.gameObject.SetActive(value);
        mainMenuCanvas.gameObject.SetActive(value);

        // If we enable the main menu, automatically select the start button.
        if (value)
            EventSystem.current.SetSelectedGameObject(startGameButton);

        ////TODO: move this into UI module doing it automatically
        if (value)
            uiInputModule.EnableAllActions();
        else
            uiInputModule.DisableAllActions();
    }

    private void StartGame()
    {
        // Create fish, if need be.
        if (m_Fish == null)
        {
            var fishObject = Instantiate(fishPrefab);
            m_Fish = fishObject.GetComponent<DemoFishController>();
            if (m_Fish == null)
                throw new Exception("Cannot find 'DemoFishController' on " + fishObject);
        }

        // Wipe state from last game.
        m_Fish.Reset();

        //start timer

        // Let players know the game is on.
        for (var i = 0; i < m_ActivePlayerCount; ++i)
            m_Players[i].OnGameStarted();
    }

    private void EndGame()
    {
    }

    private void PauseGame()
    {
    }

    /// <summary>
    /// Perform state transitions that affect the game globally.
    /// </summary>
    /// <param name="newState"></param>
    private void ChangeState(State newState)
    {
        var oldState = state;
        switch (newState)
        {
            // Go into main menu.
            case State.InMainMenu:
            {
                ShowMainMenu();
                break;
            }

            // Go to lobby.
            case State.InLobby:
            {
                Debug.Assert(oldState == State.InMainMenu);
                Debug.Assert(InputUser.all.Count == 0);

                // Start listening for device activity on devices not currently paired to a user.
                // This is how we detect when a player presses a button to join the game with a specific
                // device.
                ++InputUser.listenForUnpairedDeviceActivity;

                ////TODO: show "Press button to join" text
                break;
            }

            // Go from lobby to single-player game.
            case State.InSinglePlayerGame:
            {
                Debug.Assert(oldState == State.InLobby);
                Debug.Assert(m_ActivePlayerCount == 1);
                Debug.Assert(InputUser.all.Count == 1);

                // In single-player we need to keep listening for device activity that isn't coming
                // from devices currently assigned to the player. This is how we detect when the player
                // is switching from one device to another (and potentially from one control scheme
                // to another).
                ++InputUser.listenForUnpairedDeviceActivity;

                break;
            }

            // Go from lobby to multi-player game.
            case State.InMultiPlayerGame:
            {
                Debug.Assert(oldState == State.InLobby);
                Debug.Assert(m_ActivePlayerCount > 1);
                Debug.Assert(InputUser.all.Count > 1);

                break;
            }

            // Go from single- or multi-player game to end-game screen.
            case State.InGameOver:
            {
                Debug.Assert(oldState == State.InSinglePlayerGame || oldState == State.InMultiPlayerGame);
                break;
            }
        }

        // Only in the lobby and in single-player games do we listen for device activity
        // on devices not currently paired to a user.
        if (newState != State.InLobby && newState != State.InSinglePlayerGame)
            --InputUser.listenForUnpairedDeviceActivity;

        m_State = newState;
    }
}
