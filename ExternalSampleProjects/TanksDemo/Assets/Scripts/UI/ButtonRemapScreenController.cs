using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonRemapScreenController : MonoBehaviour
{
    public Button okButton;
    public InputActionAsset tanksInputActions;
    private InputActionMap playerActionMap;

    void Start()
    {
        playerActionMap = tanksInputActions.FindActionMap("Player");
        playerActionMap.Disable();
        okButton.onClick.AddListener(OkButtonClicked);
    }

    private void OkButtonClicked()
    {
        playerActionMap.Enable();
        SceneManager.LoadScene("NewInput");
    }
}
