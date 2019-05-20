using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonRemapScreenController : MonoBehaviour
{
    public Button gasRemapButton;
    public Button brakeRemapButton;
    public Button fireRemapButton;
    public Button turretRemapButton;
    public Button okButton;

    void Start()
    {
        okButton.onClick.AddListener(OkButtonClicked);
        gasRemapButton.Select();
    }

    private void OkButtonClicked()
    {
        SceneManager.LoadScene("NewInput");
    }
}
