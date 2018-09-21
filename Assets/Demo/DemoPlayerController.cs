using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Controller for a single player in the game.
/// </summary>
/// <remarks>
/// </remarks>
public class DemoPlayerController : MonoBehaviour
{
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
    private InputUser m_User;

    private Rigidbody m_Rigidbody;

    public InputUser user
    {
        get { return m_User; }
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
    public void Initialize()
    {
        m_User = InputUser.Add();

        // Each player gets a separate action setup. The first player simply uses
        // the actions as is but for any additional player, we need to duplicate
        // the original actions.
        if (m_User.index != 0)
            controls.DuplicateAndSwitchAsset();

        // By default, player starts out with gameplay actions active.
        m_User.SwitchActions(controls.gameplay);

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
    public InputControlScheme InferDefaultControlScheme()
    {
        //if we have VR devices, go with them by default regardless of platform

        #if UNITY_STANDALONE
        #elif UNITY_ANDROID || UNITY_IOS
        #endif

        throw new NotImplementedException();
    }

    public InputControlScheme SelectControlSchemeBasedOnDevice(InputDevice device)
    {
        throw new NotImplementedException();
    }

    public void ConnectInput()
    {
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

    public void OnFireStarted(InputAction.CallbackContext context)
    {
        if (context.interaction is SlowTapInteraction)
            m_Charging = true;
    }

    public void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (context.interaction is SlowTapInteraction)
        {
            StartCoroutine(ExecuteChargedFire((int)(context.duration * burstSpeed)));
        }
        else
        {
            FireProjectile();
        }
        m_Charging = false;
    }

    /// <summary>
    /// Called when the "gameplay/fire" action is cancelled.
    /// </summary>
    /// <param name="context"></param>
    public void OnFireCancelled(InputAction.CallbackContext context)
    {
        m_Charging = false;
    }

    /// <summary>
    /// Called when the "gameplay/jump" action is performed.
    /// </summary>
    /// <param name="context"></param>
    public void OnJumpPerformed(InputAction.CallbackContext context)
    {
        var jump = new Vector3(0.0f, jumpForce, 0.0f);
        if (m_IsGrounded)
        {
            m_Rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
            m_IsGrounded = false;
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
    }

    public void OnDevicesChanged()
    {
    }

    public void OnCollisionStay()
    {
        m_IsGrounded = true;
    }

    public void ShowMenu()
    {
        //pause haptics
        //disable game controls / switch to menu actions
    }

    public void Update()
    {
        Move(m_Move);
        Look(m_Look);
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

    /// <summary>
    /// Fire <paramref name="projectileCount"/> projectiles over time.
    /// </summary>
    /// <param name="projectileCount"></param>
    /// <param name="delayBetweenProjectiles"></param>
    /// <returns></returns>
    private IEnumerator ExecuteChargedFire(int projectileCount, float delayBetweenProjectiles = 0.1f)
    {
        for (var i = 0; i < projectileCount; ++i)
        {
            FireProjectile();
            yield return new WaitForSeconds(delayBetweenProjectiles);
        }
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

    private void OnGotoMenu()
    {
        // Pause haptics effects while we are in the menu.
        user.PauseHaptics();

        // Switch from gameplay actions to menu actions.
        user.SwitchActions(controls.menu);

        // Activate the UI.
        ui.gameObject.SetActive(true);
    }

    private void OnResumeGame()
    {
        // Deactivate the UI.
        ui.gameObject.SetActive(false);

        // Resume playback of haptics effects.
        user.ResumeHaptics();

        // Switch back to gameplay controls.
        user.SwitchActions(controls.gameplay);
    }
}
