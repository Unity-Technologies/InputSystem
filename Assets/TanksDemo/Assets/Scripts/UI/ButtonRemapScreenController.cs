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
        gasRemapButton.onClick.AddListener(GasRemapButtonClicked);
        brakeRemapButton.onClick.AddListener(BrakeRemapButtonClicked);
        fireRemapButton.onClick.AddListener(FireRemapButtonClicked);
        turretRemapButton.onClick.AddListener(TurretRemapButtonClicked);
        okButton.onClick.AddListener(OkButtonClicked);

        // Set the first button to be selected so that
        // gamepad navigation can be performed.
        gasRemapButton.Select();
    }

    private void OkButtonClicked()
    {
        SceneManager.LoadScene("NewInput");
    }

    private void GasRemapButtonClicked()
    {
        Debug.Log("Gas remap button clicked.");
    }

    private void BrakeRemapButtonClicked()
    {
        Debug.Log("Brake remap button clicked.");
    }

    private void FireRemapButtonClicked()
    {
        Debug.Log("Fire remap button clicked.");
    }

    private void TurretRemapButtonClicked()
    {
        Debug.Log("Turret remap button clicked.");
    }
}
