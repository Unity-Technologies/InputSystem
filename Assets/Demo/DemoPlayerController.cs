using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;
using Random = UnityEngine.Random;

////WIP; not functional ATM

/// <summary>
/// Controller for a single player in the game.
/// </summary>
public class DemoPlayerController : MonoBehaviour, DemoControls.IGameplayActions
{
    public const float DelayBetweenBurstProjectiles = 0.1f;

    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;

    /// <summary>
    /// Prefab to spawn for projectiles fired by the player.
    /// </summary>
    public GameObject projectilePrefab;

    /// <summary>
    /// Controls used by this player.
    /// </summary>
    [Tooltip("The input actions containing bindings and control schemes to use for player input.")]
    public DemoControls controls;

    /// <summary>
    /// UI input module specific to the player.
    /// </summary>
    /// <remarks>
    /// We feed input from <see cref="controls"/> into this module thus making the player's UI responsive
    /// to the player's devices only.
    /// </remarks>
    public UIActionInputModule uiActions;

    /// <summary>
    /// GameObject hierarchy inside <see cref="ui"/> that represents the menu UI.
    /// </summary>
    [Tooltip("Root object the per-player menu UI.")]
    public GameObject menuUI;

    /// <summary>
    /// GameObject hierarchy inside <see cref="ui"/> that represents the in-game UI.
    /// </summary>
    [Tooltip("Root object of the per-player in-game UI.")]
    public GameObject inGameUI;

    /// <summary>
    /// In-game UI that displays control hints.
    /// </summary>
    [Tooltip("In-game UI to display control hints.")]
    public Text controlHintsUI;

    public GameObject controllerLostUI;

    public Text lobbyUserNameUI;

    public Text lobbyDevicesUI;

    /// <summary>
    /// In-game UI to show while the player is charging the fire button.
    /// </summary>
    [Tooltip("In-game UI to show while the player is charging the fire button.")]
    public GameObject chargingUI;

    private string m_Name;
    private State m_State;
    private int m_Score;
    private bool m_ShowHints;
    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool m_IsGrounded;
    private bool m_Charging;
    private bool m_DeviceLost;
    private Vector2 m_Rotation;
    private InputUser m_User;

    private int m_BurstProjectileCountRemaining;
    private float m_LastBurstProjectileTime;

    public enum State
    {
        /// <summary>
        /// Player is not currently used.
        /// </summary>
        Inactive,

        /// <summary>
        /// Player has joined the game.
        /// </summary>
        Joined,

        /// <summary>
        /// Player has joined the game and brought up the menu.
        /// </summary>
        /// <remarks>
        /// In this state, the player can customize controls, switch accounts, or exit the game.
        /// To indicate readiness, the player needs to first exit the menu again.
        /// </remarks>
        JoinedInMenu,

        /// <summary>
        /// Player is ready to start the game.
        /// </summary>
        Ready,

        /// <summary>
        /// Game has started and player is actively playing.
        /// </summary>
        InGame,

        /// <summary>
        /// Game has started and player is in menu.
        /// </summary>
        InMenu,
    }

    public State state
    {
        get { return m_State; }
    }

    /// <summary>
    /// Current score of the player.
    /// </summary>
    public int score
    {
        get { return m_Score; }
    }

    /// <summary>
    /// If true, the player currently has the in-game menu up.
    /// </summary>
    /// <remarks>
    /// If all players have the in-game menu up, the game is automatically paused. This will
    /// always pause the game in single-player but in multi-player will only pause the game
    /// if all players go into the menu.
    /// </remarks>
    public bool isInMenu
    {
        get { return state == State.InMenu || state == State.JoinedInMenu; }
    }

    /// <summary>
    /// Whether the player has indicated to be ready for the game to start.
    /// </summary>
    /// <remarks>
    /// This is only used in multi-player in the initial phase when we wait for all players to
    /// join and indicate they are ready. Once all players are ready, the game starts.
    /// </remarks>
    public bool isReady
    {
        get { return state == State.Ready; }
    }

    /// <summary>
    /// Whether the player has lost a controller (e.g. ran out battery) and we're waiting for the player
    /// to come back online.
    /// </summary>
    public bool hasLostDevice
    {
        get { return m_DeviceLost; }
    }

    /// <summary>
    /// The input user associated with the player.
    /// </summary>
    /// <remarks>
    /// No two players will be assigned the same input user.
    ///
    /// The input user tracks the devices paired to the player.
    /// </remarks>
    public InputUser user
    {
        get { return m_User; }
    }

    /// <summary>
    /// One-time initialization for a player controller.
    /// </summary>
    /// <remarks>
    /// Once spawned, we are reusing player instances over and over. The setup we perform in here,
    /// however, is done only once.
    /// </remarks>
    public void PerformOneTimeInitialization()
    {
        // Each player gets a separate action setup. This makes the state of actions and bindings
        // local to each player and also ensures we're not stepping on the action setup used by
        // DemoGame itself for the main menu (where we are not using control schemes and just blindly
        // bind to whatever devices are available locally).
        controls = new DemoControls();

        Debug.Assert(uiActions != null);
        Debug.Assert(projectilePrefab != null);
        Debug.Assert(controls != null);

        // Wire our callbacks into gameplay actions. We don't need to do the same
        // for menu actions as it's the UI using those and not us.
        controls.gameplay.SetCallbacks(this);

        // Wire our input actions into the UI. Doing this manually here instead of setting it up
        // in the inspector ensure that when we duplicate DemoControls.inputactions above, we
        // end up with the UI using the right actions.
        //
        // NOTE: Our bindings will be effective on the devices assigned to the user which in turn
        //       means that the UI will react only to input from that same user.
        uiActions.BindUIActions(controls.menu);
    }

    /// <summary>
    /// Based on the choice of the given device, select an appropriate control scheme.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    /// <remarks>
    /// The chosen control scheme may depend also on what other devices are already in use by other
    /// players.
    /// </remarks>
    public InputControlScheme? SelectControlSchemeBasedOnDevice(InputDevice device)
    {
        return InputControlScheme.FindControlSchemeForDevice(device, controls.controlSchemes);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        m_Look = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// The <see cref="DemoControls.GameplayActions.fire"/> action got triggered.
    /// </summary>
    /// <param name="context"></param>
    public void OnFire(InputAction.CallbackContext context)
    {
        Debug.Assert(!isInMenu, "Shouldn't trigger gameplay/fire when in menu");

        // If we had lost our devices, looks like it has come back.
        if (m_DeviceLost)
        {
            ////TODO: need to check whether we really have our devices back (may not have been the one with the fire
            ////      button on it that we lost)
            ////TODO: hide device lost UI
            m_DeviceLost = false;
            return;
        }

        // While we're in the phase where players can join the game, the fire button toggles
        // the player's readiness state. In the game, it obviously fires.
        switch (m_State)
        {
            case State.Joined:
                // This player is ready to start the game.
                if (!hasLostDevice)
                    ChangeState(State.Ready);
                break;

            case State.Ready:
                // This player is no longer ready to start the game.
                ChangeState(State.Joined);
                break;

            case State.InGame:
                switch (context.phase)
                {
                    case InputActionPhase.Started:
                        if (context.interaction is SlowTapInteraction)
                            m_Charging = true;
                        break;

                    case InputActionPhase.Performed:
                        ////TODO: handle case where we're already running a burst fire
                        if (context.interaction is SlowTapInteraction)
                        {
                            m_BurstProjectileCountRemaining = (int)(context.duration * burstSpeed);
                            m_LastBurstProjectileTime = -1;
                        }
                        else
                        {
                            FireProjectile();
                        }
                        m_Charging = false;
                        break;

                    case InputActionPhase.Cancelled:
                        m_Charging = false;
                        break;
                }

                break;
        }
    }

    /// <summary>
    /// Called when the <see cref="DemoControls.GameplayActions.menu"/> action is triggered.
    /// </summary>
    /// <param name="context"></param>
    /// <remarks>
    /// This is not directly used on Steam where we have dedicated <see cref="DemoControls.GameplayActions.steamEnterMenu"/>
    /// and <see cref="DemoControls.MenuActions.steamExitMenu"/> actions instead.
    /// </remarks>
    public void OnMenu(InputAction.CallbackContext context)
    {
        if (isInMenu)
        {
            // Leave menu.

            user.ResumeHaptics();

            controls.gameplay.Enable();
            controls.menu.Disable();///REVIEW: this should likely be left to the UI input module

            menuUI.SetActive(false);
        }
        else
        {
            // Enter menu.

            user.PauseHaptics();

            controls.gameplay.Disable();
            controls.menu.Enable();///REVIEW: this should likely be left to the UI input module

            // We do want the menu toggle to remain active. Rather than moving the action to its
            // own separate action map, we just go and enable that one single action from the
            // gameplay actions.
            // NOTE: This will cause gameplay.enabled to remain true.
            // NOTE: This setup won't work on Steam where we can only have a single action set active
            //       at any time. We ignore the gameplay/menu action on Steam and instead handle
            //       menu toggling via the two separate actions gameplay/steamEnterMenu and menu/steamExitMenu
            //       that we use only for Steam.
            controls.gameplay.menu.Enable();

            menuUI.SetActive(true);
        }
    }

    public void OnSteamEnterMenu(InputAction.CallbackContext context)
    {
        OnMenu(context);
    }

    public void OnSteamExitMenu(InputAction.CallbackContext context)
    {
        OnMenu(context);
    }

    ////TODO: this is also where we should look for whether we have custom bindings for the user that we should activate
    public bool OnJoin(InputUser user)
    {
        Debug.Assert(user.valid);
        Debug.Assert(user.pairedDevices.Count == 1, "Players should join on exactly one input device");

        // Associate our InputUser with the actions we're using.
        user.AssociateActionsWithUser(controls);

        // Find out what control scheme to use and whether we have all the devices needed for it.
        var controlScheme = SelectControlSchemeBasedOnDevice(user.pairedDevices[0]);
        Debug.Assert(controlScheme.HasValue, "Must not join player on devices that we have no control scheme for");

        // Try to activate control scheme. The scheme may require additional devices which we
        // also need to pair to the user. This process may fail and we may end up a player missing
        // devices to start playing.
        user.ActivateControlScheme(controlScheme.Value).AndPairRemainingDevices();
        if (user.hasMissingRequiredDevices)
            return false;

        // Put the player in joined state.
        m_User = user;
        ChangeState(State.Joined);

        return true;
    }

    public void OnGameStarted()
    {
        Debug.Assert(!menuUI.activeSelf, "Should not start game with player still being in menu");

        // Activate gameplay controls.
        controls.gameplay.Enable();
    }

    public void OnDeviceLost()
    {
        m_DeviceLost = true;

        ////TODO
        //show UI
        //Next OnFire resolves the situation
        //When in lobby, player is unjoined (?)
    }

    public void OnControlSchemeChanged()
    {
    }

    /// <summary>
    /// Called when the set of devices assigned the player has changed or when the player has
    /// customized controls.
    /// </summary>
    /// <remarks>
    /// Updates UI help texts with information based on the bindings in the currently
    /// active control scheme. This makes sure we display relevant information in the UI
    /// (e.g. gamepad hints instead of keyboard hints when the user is playing with a
    /// gamepad).
    /// </remarks>
    public void OnDevicesOrBindingsHaveChanged()
    {
        var devices = user.pairedDevices;
        controlHintsUI.text = GetOrCreateUIHint(controls.gameplay.fire, "Tap {0} to fire, hold to charge", devices);
    }

    public void OnUserAccountChanged()
    {
        //update name
    }

    public void Update()
    {
        Move(m_Move);
        Look(m_Look);

        // Execute charged fire.
        if (m_BurstProjectileCountRemaining > 0 &&
            (m_LastBurstProjectileTime < 0 ||
             Time.time - m_LastBurstProjectileTime >
             DelayBetweenBurstProjectiles))
        {
            FireProjectile();
            m_LastBurstProjectileTime = Time.time;
            --m_BurstProjectileCountRemaining;
        }
    }

    private void Move(Vector2 direction)
    {
        var scaledMoveSpeed = moveSpeed * Time.deltaTime;
        var move = transform.TransformDirection(direction.x, 0, direction.y);
        transform.localPosition += move * scaledMoveSpeed;
    }

    private void Look(Vector2 rotate)
    {
        const float kClampAngle = 80.0f;

        m_Rotation.y += rotate.x * rotateSpeed * Time.deltaTime;
        m_Rotation.x -= rotate.y * rotateSpeed * Time.deltaTime;

        m_Rotation.x = Mathf.Clamp(m_Rotation.x, -kClampAngle, kClampAngle);

        var localRotation = Quaternion.Euler(m_Rotation.x, m_Rotation.y, 0.0f);
        transform.rotation = localRotation;
    }

    private void FireProjectile()
    {
        var transform = this.transform;
        var newProjectile = Instantiate(projectilePrefab);
        newProjectile.transform.position = transform.position + transform.forward * 0.6f;
        newProjectile.transform.rotation = transform.rotation;
        var size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }

    private void Reset()
    {
        m_Score = 0;
        m_Move = Vector2.zero;
        m_Look = Vector2.zero;

        if (m_User.valid)
            m_User.UnpairDevicesAndRemoveUser();
    }

    private void ChangeState(State newState)
    {
        var oldState = m_State;
        switch (newState)
        {
            case State.Joined:
                Debug.Assert(oldState == State.Inactive || oldState == State.Ready);
                ////TODO: UI feedback
                break;

            case State.Ready:
                Debug.Assert(oldState == State.Joined);
                Debug.Assert(!hasLostDevice, "Cannot start game with player having lost devices");
                ////TODO: UI feedback
                DemoGame.instance.OnPlayerIsReady(this);
                break;

            case State.Inactive:
                Reset();
                break;
        }

        m_State = newState;
    }

    ////TODO: flush out cached UI hints when a device is removed (for good)

    private struct CachedUIHint
    {
        public InputAction action;
        public InputControl control;
        public string format;
        public string text;
    }

    private static List<CachedUIHint> s_CachedUIHints;

    public static void ClearUIHintsCache()
    {
        if (s_CachedUIHints != null)
            s_CachedUIHints.Clear();
    }

    /// <summary>
    /// Create a textual hint to show for the given action based on the devices we are currently using.
    /// </summary>
    /// <param name="action">Action to generate a hint for.</param>
    /// <param name="format">Format string. Use {0} where the active control name should be inserted.</param>
    /// <param name="devices">Set of currently assigned devices. The action will be searched for a bound control that sits
    /// on one of the devices. If none is found, an empty string is returned.</param>
    /// <returns>Text containing a hint for the given action or an empty string.</returns>
    private static string GetOrCreateUIHint(InputAction action, string format, ReadOnlyArray<InputDevice> devices)
    {
        InputControl control = null;

        // Find the first bound control that sits on one of the given devices.
        var controls = action.controls;
        foreach (var element in controls)
            if (devices.ContainsReference(element.device))
            {
                control = element;
                break;
            }

        if (control == null)
            return string.Empty;

        // See if we have an existing hint.
        if (s_CachedUIHints != null)
        {
            foreach (var hint in s_CachedUIHints)
            {
                if (hint.action == action && hint.control == control && hint.format == format)
                    return hint.text;
            }
        }

        // No, so create a new hint and cache it.
        var controlName = control.shortDisplayName;
        if (string.IsNullOrEmpty(controlName))
            controlName = control.displayName;
        var text = string.Format(format, controlName);
        if (s_CachedUIHints == null)
            s_CachedUIHints = new List<CachedUIHint>();
        s_CachedUIHints.Add(new CachedUIHint {action = action, control = control, format = format, text = text});

        return text;
    }
}
