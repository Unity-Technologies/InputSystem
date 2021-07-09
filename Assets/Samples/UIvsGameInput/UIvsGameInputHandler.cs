using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TODO
// [X] Test on phone
// [ ] Look into triggering Fire() immediately
// [ ] Navigation support

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIvsGameInputHandler : MonoBehaviour
{
    public Text statusBarText;
    public GameObject inGameUI;
    public GameObject mainMenuUI;
    public GameObject menuButton;
    public PlayerInput playerInput;
    public GameObject projectile;

    private bool m_OpenMenuActionTriggered;
    private bool m_ResetCameraActionTriggered;
    private bool m_FireActionTriggered;
    private bool m_CurrentControlSchemeUsesPointer;

    private Vector2 m_Rotation;
    private InputAction m_LookEngageAction;
    private InputAction m_LookAction;
    private InputAction m_CancelAction;

    private Mouse m_Mouse;
    private Vector2? m_MousePositionToWarpToAfterCursorUnlock;

    internal enum State
    {
        InGame,
        InGameControllingCamera,
        InMenu,
    }

    internal State m_State;

    public void OnEnable()
    {
        // By default, hide menu and show game UI.
        inGameUI.SetActive(true);
        mainMenuUI.SetActive(false);
        menuButton.SetActive(false);

        // Look up InputActions on the player so we don't have to do this over and over.
        m_LookEngageAction = playerInput.actions["LookEngage"];
        m_LookAction = playerInput.actions["Look"];
        m_CancelAction = playerInput.actions["UI/Cancel"];

        m_State = State.InGame;
    }

    // This is called when PlayerInput updates the controls bound to its InputActions.
    public void OnControlsChanged()
    {
        // Check if in the current control scheme uses a device that is a Pointer or TrackedDevice.
        // Both types of devices have the ability to point at elements in the UI and thus create
        // ambiguity between UI and game input.
        m_CurrentControlSchemeUsesPointer = playerInput.GetDevice<Pointer>() != null || playerInput.GetDevice<TrackedDevice>() != null;

        m_Mouse = default;
        m_MousePositionToWarpToAfterCursorUnlock = default;

        // Enable button for main menu depending on whether we use touch or not.
        // With kb&mouse and gamepad, not necessary but with touch, we have no "Cancel" control.
        menuButton.SetActive(playerInput.GetDevice<Touchscreen>() != null);
    }

    public void Update()
    {
        switch (m_State)
        {
            case State.InGame:
            {
                if (m_OpenMenuActionTriggered)
                {
                    m_State = State.InMenu;

                    // Bring up main menu.
                    inGameUI.SetActive(false);
                    mainMenuUI.SetActive(true);

                    // Disable gameplay inputs.
                    playerInput.DeactivateInput();
                }

                var pointerIsOverUI = IsPointerOverUI();
                if (pointerIsOverUI)
                    break;

                if (m_ResetCameraActionTriggered)
                    transform.rotation = default;

                if (m_LookEngageAction.WasPressedThisFrame())
                    EngageCameraLock();

                break;
            }

            case State.InGameControllingCamera:

                if (m_ResetCameraActionTriggered && !IsPointerOverUI())
                    transform.rotation = default;

                //this presents an ordering problem with resets
                if (m_FireActionTriggered && !IsPointerOverUI())
                    Fire();

                // Rotate camera.
                var rotate = m_LookAction.ReadValue<Vector2>();
                if (rotate.sqrMagnitude > 0.01)
                {
                    const float kSensitivity = 10;
                    var scaledRotateSpeed = kSensitivity * Time.deltaTime;
                    m_Rotation.y += rotate.x * scaledRotateSpeed;
                    m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
                    transform.localEulerAngles = m_Rotation;
                }

                // Keep track of distance we travel with the mouse while in mouse lock so
                // that when we unlock, we can jump to a position that feels "right".
                if (m_Mouse != null)
                    m_MousePositionToWarpToAfterCursorUnlock = m_MousePositionToWarpToAfterCursorUnlock.Value + m_Mouse.delta.ReadValue();

                if (m_CancelAction.WasPressedThisFrame() || !m_LookEngageAction.IsPressed())
                    DisengageCameraControl();

                break;

            case State.InMenu:

                if (m_CancelAction.WasPressedThisFrame())
                    OnContinueClicked();

                break;
        }

        m_ResetCameraActionTriggered = default;
        m_OpenMenuActionTriggered = default;
        m_FireActionTriggered = default;
    }

    private void EngageCameraLock()
    {
        // With a mouse, it's annoying to always end up with the pointer centered in the middle of
        // the screen after we come out of a cursor lock. So, what we do is we simply remember where
        // the cursor was when we locked and then warp the mouse back to that position after the cursor
        // lock is released.
        m_Mouse = playerInput.GetDevice<Mouse>();
        m_MousePositionToWarpToAfterCursorUnlock = m_Mouse?.position.ReadValue();

        Cursor.lockState = CursorLockMode.Locked;

        m_State = State.InGameControllingCamera;
    }

    private void DisengageCameraControl()
    {
        Cursor.lockState = CursorLockMode.None;

        if (m_MousePositionToWarpToAfterCursorUnlock != null)
            playerInput.GetDevice<Mouse>()?.WarpCursorPosition(m_MousePositionToWarpToAfterCursorUnlock.Value);

        m_State = State.InGame;
    }

    public void OnTopLeftClicked()
    {
        statusBarText.text = "'Top Left' button clicked";
    }

    public void OnBottomLeftClicked()
    {
        statusBarText.text = "'Bottom Left' button clicked";
    }

    public void OnTopRightClicked()
    {
        statusBarText.text = "'Top Right' button clicked";
    }

    public void OnBottomRightClicked()
    {
        statusBarText.text = "'Bottom Right' button clicked";
    }

    public void OnMenuClicked()
    {
        m_OpenMenuActionTriggered = true;
    }

    public void OnContinueClicked()
    {
        mainMenuUI.SetActive(false);
        inGameUI.SetActive(true);

        // Reenable gameplay inputs.
        playerInput.ActivateInput();

        m_State = State.InGame;
    }

    public void OnExitClicked()
    {
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #else
        Application.Quit();
        #endif
    }

    // When the 'Menu' and 'CameraReset' actions trigger, ...

    public void OnMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
            m_OpenMenuActionTriggered = true;
    }

    public void OnResetCamera(InputAction.CallbackContext context)
    {
        if (context.performed)
            m_ResetCameraActionTriggered = true;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
            m_FireActionTriggered = true;
    }

    /*
    public void OnFire(InputAction.CallbackContext context)
    {
        // For this action, let's try something different. Let's say we want to trigger a response
        // right away every time the "fire" action triggers. Theoretically, this would allows us
        // to correctly respond even if there is multiple activations in a single frame. In practice,
        // this will realistically only happen with low framerates (and even then it can be questionable
        // whether we want to respond this way).
        //
        // The problem is

        var device = playerInput.GetDevice<Pointer>();
        if (device != null && EventSystem.current.IsPositionOverUIObject(device.position.ReadValue())
        {
            return;
        }
    }

    private PointerEventData m_PointerData;
    private List<RaycastResult> m_RaycastResults;
    */

    private bool IsPointerOverUI()
    {
        // If we're not controlling the UI with a pointer, we can early out of this.
        if (!m_CurrentControlSchemeUsesPointer)
            return false;

        // Otherwise, check if the primary pointer is currently over a UI object.
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void Fire()
    {
        var transform = this.transform;
        var newProjectile = Instantiate(projectile);
        newProjectile.transform.position = transform.position + transform.forward * 0.6f;
        newProjectile.transform.rotation = transform.rotation;
        const int kSize = 1;
        newProjectile.transform.localScale *= kSize;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(kSize, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIvsGameInputHandler))]
internal class UIvsGameInputHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug");
            EditorGUILayout.Space();

            using (new EditorGUI.IndentLevelScope())
            {
                var state = ((UIvsGameInputHandler)target).m_State;
                EditorGUILayout.LabelField("State", state.ToString());
            }
        }
    }
}
#endif
