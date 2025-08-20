using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class UIvsGameInputHandler : MonoBehaviour
{
    public Text statusBarText;
    public GameObject inGameUI;
    public GameObject mainMenuUI;
    public GameObject menuButton;
    public GameObject firstButtonInMainMenu;
    public GameObject firstNavigationSelection;
    [Space]
    public PlayerInput playerInput;
    public GameObject projectile;

    [Space]
    [Tooltip("Multiplier for Pointer.delta values when adding to rotation.")]
    public float m_MouseLookSensitivity = 0.1f;
    [Tooltip("Rotation per second with fully actuated Gamepad/joystick stick.")]
    public float m_GamepadLookSpeed = 10f;

    private bool m_OpenMenuActionTriggered;
    private bool m_ResetCameraActionTriggered;
    private bool m_FireActionTriggered;
    internal bool m_UIEngaged;

    private Vector2 m_Rotation;
    private InputAction m_LookEngageAction;
    private InputAction m_LookAction;
    private InputAction m_CancelAction;
    private InputAction m_UIEngageAction;
    private GameObject m_LastNavigationSelection;

    private Mouse m_Mouse;
    private Vector2? m_MousePositionToWarpToAfterCursorUnlock;

    internal enum State
    {
        InGame,
        InGameControllingCamera,
        InMenu,
    }

    internal State m_State;

    internal enum ControlStyle
    {
        None,
        KeyboardMouse,
        Touch,
        GamepadJoystick,
    }

    internal ControlStyle m_ControlStyle;

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
        m_UIEngageAction = playerInput.actions["UIEngage"];

        m_State = State.InGame;
    }

    // This is called when PlayerInput updates the controls bound to its InputActions.
    public void OnControlsChanged()
    {
        // We could determine the types of controls we have from the names of the control schemes or their
        // contents. However, a way that is both easier and more robust is to simply look at the kind of
        // devices we have assigned to us. We do not support mixed models this way but this does correspond
        // to the limitations of the current control code.

        if (playerInput.GetDevice<Touchscreen>() != null) // Note that Touchscreen is also a Pointer so check this first.
            m_ControlStyle = ControlStyle.Touch;
        else if (playerInput.GetDevice<Pointer>() != null)
            m_ControlStyle = ControlStyle.KeyboardMouse;
        else if (playerInput.GetDevice<Gamepad>() != null || playerInput.GetDevice<Joystick>() != null)
            m_ControlStyle = ControlStyle.GamepadJoystick;
        else
            Debug.LogError("Control scheme not recognized: " + playerInput.currentControlScheme);

        m_Mouse = default;
        m_MousePositionToWarpToAfterCursorUnlock = default;

        // Enable button for main menu depending on whether we use touch or not.
        // With kb&mouse and gamepad, not necessary but with touch, we have no "Cancel" control.
        menuButton.SetActive(m_ControlStyle == ControlStyle.Touch);

        // If we're using navigation-style input, start with UI control disengaged.
        if (m_ControlStyle == ControlStyle.GamepadJoystick)
            SetUIEngaged(false);

        RepaintInspector();
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

                    // Select topmost button.
                    EventSystem.current.SetSelectedGameObject(firstButtonInMainMenu);
                }

                var pointerIsOverUI = IsPointerOverUI();
                if (pointerIsOverUI)
                    break;

                if (m_ResetCameraActionTriggered)
                    transform.rotation = default;

                // When using a pointer-based control scheme, we engage camera look explicitly.
                if (m_ControlStyle != ControlStyle.GamepadJoystick && m_LookEngageAction.WasPressedThisDynamicUpdate() && IsPointerInsideScreen())
                    EngageCameraControl();

                // With gamepad/joystick, we can freely rotate the camera at any time.
                if (m_ControlStyle == ControlStyle.GamepadJoystick)
                    ProcessCameraLook();

                if (m_FireActionTriggered)
                    Fire();

                break;
            }

            case State.InGameControllingCamera:

                if (m_ResetCameraActionTriggered && !IsPointerOverUI())
                    transform.rotation = default;

                if (m_FireActionTriggered && !IsPointerOverUI())
                    Fire();

                // Rotate camera.
                ProcessCameraLook();

                // Keep track of distance we travel with the mouse while in mouse lock so
                // that when we unlock, we can jump to a position that feels "right".
                if (m_Mouse != null)
                    m_MousePositionToWarpToAfterCursorUnlock = m_MousePositionToWarpToAfterCursorUnlock.Value + m_Mouse.delta.ReadValue();

                if (m_CancelAction.WasPressedThisDynamicUpdate() || !m_LookEngageAction.IsPressed())
                    DisengageCameraControl();

                break;

            case State.InMenu:

                if (m_CancelAction.WasPressedThisDynamicUpdate())
                    OnContinueClicked();

                break;
        }

        m_ResetCameraActionTriggered = default;
        m_OpenMenuActionTriggered = default;
        m_FireActionTriggered = default;
    }

    private void ProcessCameraLook()
    {
        var rotate = m_LookAction.ReadValue<Vector2>();
        if (!(rotate.sqrMagnitude > 0.01))
            return;

        // For gamepad and joystick, we rotate continuously based on stick actuation.
        float rotateScaleFactor;
        if (m_ControlStyle == ControlStyle.GamepadJoystick)
            rotateScaleFactor = m_GamepadLookSpeed * Time.deltaTime;
        else
            rotateScaleFactor = m_MouseLookSensitivity;

        m_Rotation.y += rotate.x * rotateScaleFactor;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * rotateScaleFactor, -89, 89);
        transform.localEulerAngles = m_Rotation;
    }

    private void EngageCameraControl()
    {
        // With a mouse, it's annoying to always end up with the pointer centered in the middle of
        // the screen after we come out of a cursor lock. So, what we do is we simply remember where
        // the cursor was when we locked and then warp the mouse back to that position after the cursor
        // lock is released.
        m_Mouse = playerInput.GetDevice<Mouse>();
        m_MousePositionToWarpToAfterCursorUnlock = m_Mouse?.position.ReadValue();

        Cursor.lockState = CursorLockMode.Locked;

        m_State = State.InGameControllingCamera;

        RepaintInspector();
    }

    private void DisengageCameraControl()
    {
        Cursor.lockState = CursorLockMode.None;

        if (m_MousePositionToWarpToAfterCursorUnlock != null)
            m_Mouse?.WarpCursorPosition(m_MousePositionToWarpToAfterCursorUnlock.Value);

        m_State = State.InGame;

        RepaintInspector();
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

        RepaintInspector();
    }

    public void OnExitClicked()
    {
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #else
        Application.Quit();
        #endif
    }

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

    public void OnUIEngage(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // From here, we could also do things such as showing UI that we only
        // have up while the UI is engaged. For example, the same approach as
        // here could be used to display a radial selection dials for items.

        SetUIEngaged(!m_UIEngaged);
    }

    private void SetUIEngaged(bool value)
    {
        if (value)
        {
            playerInput.actions.FindActionMap("UI").Enable();
            SetPlayerActionsEnabled(false);

            // Select the GO that was selected last time.
            if (m_LastNavigationSelection == null)
                m_LastNavigationSelection = firstNavigationSelection;
            EventSystem.current.SetSelectedGameObject(m_LastNavigationSelection);
        }
        else
        {
            m_LastNavigationSelection = EventSystem.current.currentSelectedGameObject; // If this happens to be null, we will automatically pick up firstNavigationSelection again.
            EventSystem.current.SetSelectedGameObject(null);

            playerInput.actions.FindActionMap("UI").Disable();
            SetPlayerActionsEnabled(true);
        }

        m_UIEngaged = value;

        RepaintInspector();
    }

    // Enable/disable every in-game action other than the UI toggle.
    private void SetPlayerActionsEnabled(bool value)
    {
        var actions = playerInput.actions.FindActionMap("Player");
        foreach (var action in actions)
        {
            if (action == m_UIEngageAction)
                continue;

            if (value)
                action.Enable();
            else
                action.Disable();
        }
    }

    // There's two different approaches taken here. The first OnFire() just does the same as the action
    // callbacks above and just sets some state to leave action responses to Update().
    // The second OnFire() puts the response logic directly inside the callback.

    #if false

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
            m_FireActionTriggered = true;
    }

    #else

    public void OnFire(InputAction.CallbackContext context)
    {
        // For this action, let's try something different. Let's say we want to trigger a response
        // right away every time the "fire" action triggers. Theoretically, this would allow us
        // to correctly respond even if there is multiple activations in a single frame. In practice,
        // this will realistically only happen with low framerates (and even then it can be questionable
        // whether we want to respond this way).

        if (!context.performed)
            return;

        var device = playerInput.GetDevice<Pointer>();
        if (device != null && IsRaycastHittingUIObject(device.position.ReadValue()))
            return;

        Fire();
    }

    // Can't use IsPointerOverGameObject() from within InputAction callbacks as the UI won't update
    // until after input processing is complete. So, need to explicitly raycast here.
    // NOTE: This is not something we'd want to do from a high-frequency action. If, for example, this
    //       is called from an action bound to `<Mouse>/position`, there will be an immense amount of
    //       raycasts performed per frame.
    private bool IsRaycastHittingUIObject(Vector2 position)
    {
        if (m_PointerData == null)
            m_PointerData = new PointerEventData(EventSystem.current);
        m_PointerData.position = position;
        EventSystem.current.RaycastAll(m_PointerData, m_RaycastResults);
        return m_RaycastResults.Count > 0;
    }

    private PointerEventData m_PointerData;
    private List<RaycastResult> m_RaycastResults = new List<RaycastResult>();

    #endif

    private bool IsPointerOverUI()
    {
        // If we're not controlling the UI with a pointer, we can early out of this.
        if (m_ControlStyle == ControlStyle.GamepadJoystick)
            return false;

        // Otherwise, check if the primary pointer is currently over a UI object.
        return EventSystem.current.IsPointerOverGameObject();
    }

    ////REVIEW: check this together with the focus PR; ideally, the code here should not be necessary
    private bool IsPointerInsideScreen()
    {
        var pointer = playerInput.GetDevice<Pointer>();
        if (pointer == null)
            return true;

        return Screen.safeArea.Contains(pointer.position.ReadValue());
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

    private void RepaintInspector()
    {
        // We have a custom inspector below that prints some debugging information for internal state.
        // When we change state, this will not result in an automatic repaint of the inspector as Unity
        // doesn't know about the change.
        //
        // We thus manually force a refresh. There's more elegant ways to do this but the easiest by
        // far is to just globally force a repaint of the entire editor window.

        #if UNITY_EDITOR
        InternalEditorUtility.RepaintAllViews();
        #endif
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
                var style = ((UIvsGameInputHandler)target).m_ControlStyle;
                EditorGUILayout.LabelField("Controls", style.ToString());
                if (style == UIvsGameInputHandler.ControlStyle.GamepadJoystick)
                {
                    var uiEngaged = ((UIvsGameInputHandler)target).m_UIEngaged;
                    EditorGUILayout.LabelField("UI Engaged?", uiEngaged ? "Yes" : "No");
                }
            }
        }
    }
}
#endif
