using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Interactions;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;

/// <summary>
/// Controller for a single player in the game.
/// </summary>
public class DemoPlayerController : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float burstSpeed;
    public float jumpForce = 2.0f;

    public GameObject projectilePrefab;
    public DemoControls controls;

    /// <summary>
    /// UI specific to the player.
    /// </summary>
    /// <remarks>
    /// We feed input from <see cref="controls"/> into this UI thus making the UI responsive
    /// to the player's devices only.
    /// </remarks>
    public Canvas ui;

    private Vector2 m_Move;
    private Vector2 m_Look;
    private bool m_IsGrounded;
    private bool m_Charging;
    private Vector2 m_Rotation;
    private InputUser m_User;

    private Rigidbody m_Rigidbody;

    private int m_Score;

    private void Start()
    {
        Debug.Assert(ui != null);
        Debug.Assert(projectilePrefab != null);
        Debug.Assert(controls != null);

        m_Rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(InputUser user)
    {
        // Set up input.
        m_User = user;
        if (user.index != 0)
        {
            ////REVIEW: may want to put this code into a helper method in the auto-generated file
            // We're not the first player so give us our own private duplicate of the controls.
            controls = new DemoControls(Instantiate(controls.asset));//this only needs to set asset; can keep DemoControls
        }
        m_User.actions = controls.gameplay;

        // Wire up UI actions.
        // NOTE: Our bindings will be effective on the devices assigned to the user which in turn
        //       means that the UI will react only to input from that same user.
        var uiInput = ui.GetComponent<UIActionInputModule>();
        Debug.Assert(uiInput != null);
        uiInput.moveAction.Set(controls.menu.navigate);
        uiInput.leftClickAction.Set(controls.menu.click);
    }

    public void Reset()
    {
        m_Score = 0;
        m_Move = Vector2.zero;
        m_Look = Vector2.zero;
    }

    public void OnEnable()
    {
        controls.Enable();
    }

    public void OnDisable()
    {
        controls.Disable();
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
            StartCoroutine(BurstFire((int)(context.duration * burstSpeed)));
        }
        else
        {
            Fire();
        }
        m_Charging = false;
    }

    public void OnFireCancelled(InputAction.CallbackContext context)
    {
        m_Charging = false;
    }

    public void OnJumpPerformed(InputAction.CallbackContext context)
    {
        var jump = new Vector3(0.0f, jumpForce, 0.0f);
        if (m_IsGrounded)
        {
            m_Rigidbody.AddForce(jump * jumpForce, ForceMode.Impulse);
            m_IsGrounded = false;
        }
    }

    public void OnGUI()
    {
        if (m_Charging)
            GUI.Label(new Rect(100, 100, 200, 100), "Charging...");
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

    private IEnumerator BurstFire(int burstAmount)
    {
        for (var i = 0; i < burstAmount; ++i)
        {
            Fire();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Fire()
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

    private void Menu()
    {
    }
}
