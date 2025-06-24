using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Simple game manager that manages enabling/disabling in-game and UI actions.
/// </summary>
public class GameManager : MonoBehaviour
{
    // TODO Its still an issue if we assign UI cancel button in rebinding,
    //      same goes for keyboard ESC

    public GameObject menu;
    public InputActionAsset gameplayActions;
    public InputActionReference menuAction;
    public InputActionReference exitMenuAction;
    public GameObject initiallySelectedGameObject;

    void Start()
    {
        // Let menu initially be disabled
        menu.SetActive(false);

        // Let gameplay actions be initially enabled
        gameplayActions.Enable();
    }

    private void OnEnable()
    {
        menuAction.action.performed += OnMenu;
        exitMenuAction.action.performed += OnExitMenu;
    }

    private void OnDisable()
    {
        menuAction.action.performed -= OnMenu;
        exitMenuAction.action.performed -= OnExitMenu;
    }

    private void OnMenu(InputAction.CallbackContext obj)
    {
        // Disable gameplay actions while in menu
        gameplayActions.Disable();

        // Enable menu if currently not active
        menu.SetActive(true);

        // Make sure EventSystem has a selection to allow gamepad navigation
        if (EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);
    }

    private void OnExitMenu(InputAction.CallbackContext obj)
    {
        if (!menu.activeInHierarchy)
            return;

        // Hide menu
        menu.SetActive(false);

        // Reenable gameplay actions
        gameplayActions.Enable();
    }
}
