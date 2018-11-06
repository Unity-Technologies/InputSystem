using System;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.UI;
using Random = UnityEngine.Random;

////WIP

/// <summary>
/// Controller for a single player in the game.
/// </summary>
public class DemoPlayerController : MonoBehaviour, IInputUser, IGameplayActions
{
    public const float DelayBetweenBurstProjectiles = 0.1f;

    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;
    public float jumpForce = 2.0f;

    /// <summary>
    /// Prefab to spawn for projectiles fired by the player.
    /// </summary>
    public GameObject projectilePrefab;

    /// <summary>
    /// Controls used by this player.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public DemoControls controls;

    /// <summary>
    /// UI specific to the player.
    /// </summary>
    /// <remarks>
    /// We feed input from <see cref="controls"/> into this UI thus making the UI responsive
    /// to the player's devices only.
    /// </remarks>
    public Canvas ui;

    /// <summary>
    /// GameObject hierarchy inside <see cref="ui"/> that represents the menu UI.
    /// </summary>
    public GameObject menuUI;

    /// <summary>
    /// GameObject hierarchy inside <see cref="ui"/> that represents the in-game UI.
    /// </summary>
    public GameObject inGameUI;

    public Text fireHintsUI;
    public Text moveHintsUI;
    public Text lookHintsUI;
    public GameObject chargingUI;

    public Action<DemoPlayerController> onLeaveGame;

    private int m_Score;
    private bool m_ShowHints;
    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool m_IsGrounded;
    private bool m_Charging;
    private Vector2 m_Rotation;

    private int m_BurstProjectileCountRemaining;
    private float m_LastBurstProjectileTime;

    private Rigidbody m_Rigidbody;

    public int score
    {
        get { return m_Score; }
    }

    public bool isInMenu
    {
        get { return menuUI.activeSelf; }
    }

    public void Start()
    {
        Debug.Assert(ui != null);
        Debug.Assert(projectilePrefab != null);
        Debug.Assert(controls != null);

        m_Rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// One-time initialization for a player controller.
    /// </summary>
    /// <remarks>
    /// Once spawned, we are reusing player instances over and over. The setup we perform in here,
    /// however, is done only once.
    /// </remarks>
    public void PerformOneTimeInitialization(bool isFirstPlayer)
    {
        // Each player gets a separate action setup. The first player simply uses
        // the actions as is but for any additional player, we need to duplicate
        // the original actions.
        if (!isFirstPlayer)
            controls.MakePrivateCopyOfActions();

        // Wire our callbacks into gameplay actions. We don't need to do the same
        // for menu actions as it's the UI using those and not us.
        controls.gameplay.SetCallbacks(this);

        ////REVIEW: we have to figure out who controls the enabling/disabling of actions used by UIs
        // Wire our input actions into the UI. Doing this manually here instead of setting it up
        // in the inspector ensure that when we duplicate DemoControls.inputactions above, we
        // end up with the UI using the right actions.
        //
        // NOTE: Our bindings will be effective on the devices assigned to the user which in turn
        //       means that the UI will react only to input from that same user.
        var uiInput = ui.GetComponent<UIActionInputModule>();
        Debug.Assert(uiInput != null);
        uiInput.move = new InputActionProperty(controls.menu.navigate);
        uiInput.leftClick = new InputActionProperty(controls.menu.click);
    }

    /// <summary>
    /// Called when the player has entered a single-player game.
    /// </summary>
    public void StartSinglePlayerGame()
    {
        // Associate our InputUser with the actions we're using.
        this.AssignInputActions(controls);

        // Even without the user having picked up any device, we want to be able to display UI hints and have
        // them make sense for the current platform. So we dynamically decide on a default control scheme.
        // If necessary, the user's first input will switch to a different scheme automatically.
        var defaultScheme = InferDefaultControlSchemeForSinglePlayer();

        // Switch to default control scheme and give the player whatever devices it needs.
        // NOTE: We're not calling AndMaskBindingsFromOtherControlSchemes() here. We want the player to be
        //       able to freely switch between control schemes so we keep all the bindings we have alive and
        //       don't mask anything away.
        if (!this.AssignControlScheme(defaultScheme).AndAssignDevices())
        {
            // We couldn't successfully switch to the scheme we decided on as a default.
            // Fall back to just trying one scheme after the other until we have one that
            // we can set successfully.

            var controlSchemes = controls.asset.controlSchemes;
            for (var i = 0; i < controlSchemes.Count; ++i)
            {
                if (this.AssignControlScheme(controlSchemes[i]).AndAssignDevices())
                    break;
            }
        }

        StartGame();
    }

    /// <summary>
    /// Called when the player has joined a multi-player game.
    /// </summary>
    public void StartMultiPlayerGame()
    {
        // In multi-player, we always join players through specific devices. These should get
        // assigned to the player right away.
        Debug.Assert(this.GetAssignedInputDevices().Count > 0);

        // In multi-player, we don't want players to be able to switch between devices
        // so we restrict players to just their assigned devices when binding actions.
        this.BindOnlyToAssignedInputDevices();

        // Associate our InputUser with the actions we're using.
        this.AssignInputActions(controls);

        // Find which control scheme to use based on the device we have.
        var controlScheme = SelectControlSchemeBasedOnDevice(this.GetAssignedInputDevices()[0]);

        // Activate the control scheme and automatically assign whatever other devices we need
        // which aren't already assigned to someone else.
        // NOTE: We also make sure to disable any other control scheme so that the user cannot
        //       switch between devices.
        if (!this.AssignControlScheme(controlScheme)
            .AndAssignMissingDevices()
            .AndMaskBindingsFromOtherControlSchemes())
        {
            ////TODO: what to do here?
        }

        StartGame();
    }

    private void StartGame()
    {
        // Start with the gameplay actions being active.
        controls.gameplay.Enable();

        // And menu not being active.
        menuUI.SetActive(false);
    }

    /// <summary>
    /// Return the control scheme that makes a good default.
    /// </summary>
    /// <returns>Control scheme from <see cref="controls"/> to use by default.</returns>
    /// <remarks>
    /// In a single-player setup, the player can freely switch between control schemes according to
    /// whatever devices are available. However, we have to start the player out on *some* control
    /// scheme in order to be able to display UI hints. We don't want to wait until the user has actually
    /// used any device so that we'd know what actual devices to use.
    ///
    /// So, based on what platform we are on and what devices we have available locally, we select
    /// one of the control schemes to start out with.
    /// </remarks>
    private InputControlScheme InferDefaultControlSchemeForSinglePlayer()
    {
        ////TODO: check if we have VR devices; if so, use VR control scheme by default

        var platform = DemoGame.platform;

        if (platform.IsDesktopPlatform())
        {
            // If we have a gamepad, default to gamepad. Otherwise default to keyboard&mouse.
            if (InputSystem.GetDevice<Gamepad>() != null)
                return controls.GamepadScheme;
            return controls.KeyboardMouseScheme;
        }

        throw new NotImplementedException();
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
    public InputControlScheme SelectControlSchemeBasedOnDevice(InputDevice device)
    {
        var scheme = InputControlScheme.FindControlSchemeForControl(device, controls.asset.controlSchemes);
        if (scheme.HasValue)
            return scheme.Value;

        throw new NotImplementedException();
    }

    public void Reset()
    {
        m_Score = 0;
        m_Move = Vector2.zero;
        m_Look = Vector2.zero;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        m_Look = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
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
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var jump = new Vector3(0.0f, jumpForce, 0.0f);
            if (m_IsGrounded)
            {
                m_Rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
                m_IsGrounded = false;
            }
        }
    }

    public void OnMenu(InputAction.CallbackContext context)
    {
        if (isInMenu)
        {
            // Leave menu.

            this.ResumeHaptics();

            controls.gameplay.Enable();
            controls.menu.Disable();///REVIEW: this should likely be left to the UI input module

            menuUI.SetActive(false);
        }
        else
        {
            // Enter menu.

            this.PauseHaptics();

            controls.gameplay.Disable();
            controls.menu.Enable();///REVIEW: this should likely be left to the UI input module

            // We do want the menu toggle to remain active. Rather than moving the action to its
            // own separate action map, we just go and enable that one single action from the
            // gameplay actions.
            // NOTE: This will cause gameplay.enabled to remain true.
            controls.gameplay.menu.Enable();

            menuUI.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the user switches to a different control scheme.
    /// </summary>
    /// <remarks>
    /// Updates UI help texts with information based on the bindings in the currently
    /// active control scheme. This makes sure we display relevant information in the UI
    /// (e.g. gamepad hints instead of keyboard hints when the user is playing with a
    /// gamepad).
    /// </remarks>
    public void OnControlSchemeChanged()
    {
        //cache UI hints per device
    }

    public void OnDevicesChanged()
    {
    }

    public void OnCollisionStay()
    {
        m_IsGrounded = true;
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
}
