using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscapeMenu : MonoBehaviour
{

    public InputAction openMenu;
    private GameObject menuObject;
    private bool isMenuOpened;
    private int sceneIDPrevious;



    public void Start()
    {
        menuObject = GameObject.Find("Escape Menu");
        menuObject.SetActive(false);
        sceneIDPrevious = SceneManager.GetActiveScene().buildIndex;
        // assign a callback for the "OpenMenu" action.
        openMenu.performed += ctx => { OnEsc(ctx); };
        
       
    }

    public void OnEsc(InputAction.CallbackContext context)
    {
        isMenuOpened = !isMenuOpened;

        if (isMenuOpened)
        {
            SceneManager.UnloadSceneAsync(sceneIDPrevious);
            ActivateMenu();

        }
        else
        {
            SceneManager.LoadScene(sceneIDPrevious, LoadSceneMode.Additive);
            DeactivateMenu();
        }
        
        Debug.Log("Esc pressed");
    }

    public void ActivateMenu()
    {
        menuObject.SetActive(true);
    }
    public void DeactivateMenu()
    {
        menuObject.SetActive(false);
    }
    public void OnUIButtonPress()
    {
        Debug.Log($"Button pressed!");
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
