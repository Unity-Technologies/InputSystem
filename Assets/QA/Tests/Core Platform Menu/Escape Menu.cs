using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscapeMenu : MonoBehaviour
{
    public InputAction openMenu;
    private GameObject menuObject;
    private bool isMenuOpened;
    private Scene currentScene;

    public void Start()
    {
        menuObject = GameObject.Find("Escape Menu");
        menuObject.SetActive(false);
        currentScene = SceneManager.GetActiveScene();

        // Assign a callback for the "OpenMenu" action.
        openMenu.performed += ctx => { OnEsc(ctx); };
    }

    public void OnEsc(InputAction.CallbackContext context)
    {
        isMenuOpened = !isMenuOpened;

        if (isMenuOpened)
        {
            DeactivateScene(currentScene);
            ActivateMenu();
        }
        else
        {
            ActivateScene(currentScene);
            DeactivateMenu();
        }
    }

    public void ActivateMenu()
    {
        menuObject.SetActive(true);
    }

    public void DeactivateMenu()
    {
        menuObject.SetActive(false);
    }

    public void ActivateScene(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            obj.SetActive(true);
        }
    }

    public void DeactivateScene(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            obj.SetActive(false);
        }
    }

    public void OnUIButtonPress()
    {
        SceneManager.LoadScene("Core Platforms Menu");
    }

    public void OnEnable()
    {
        openMenu.Enable();
    }

    public void OnDisable()
    {
        openMenu.Disable();
    }
}
