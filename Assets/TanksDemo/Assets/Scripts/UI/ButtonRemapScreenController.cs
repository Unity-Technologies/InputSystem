using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonRemapScreenController : MonoBehaviour
{
    public Button gasRemapButton;
    public Button brakeRemapButton;
    public Button fireRemapButton;
    public Button turretRemapButton;
    public Button okButton;

    public Text gasMappingValueText;
    public Text brakeMappingValueText;
    public Text fireMappingValueText;
    public Text turretMappingValueText;
    public Text okButtonMessageText;

    public InputActionAsset tanksInputActions;

    private const int gasMapIndex = 0;
    private const int brakeMapIndex = 1;
    private const int turretMapIndex = 2;
    private const int fireMapIndex = 3;

    private InputActionMap playerActionMap;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    void Start()
    {
        gasRemapButton.onClick.AddListener(delegate { RemapButtonClicked("Gas", gasRemapButton, gasMapIndex); });
        brakeRemapButton.onClick.AddListener(delegate { RemapButtonClicked("Brake", brakeRemapButton, brakeMapIndex); });
        fireRemapButton.onClick.AddListener(delegate { RemapButtonClicked("Fire", fireRemapButton, fireMapIndex); });
        turretRemapButton.onClick.AddListener(delegate { RemapButtonClicked("Turret", turretRemapButton, turretMapIndex); });
        okButton.onClick.AddListener(OkButtonClicked);

        // Set the first button to be selected so that
        // gamepad navigation can be performed.
        gasRemapButton.Select();

        playerActionMap = tanksInputActions.GetActionMap("Player");
        playerActionMap.Disable();

        ResetButtonMappingTextValues();
    }

    private void OkButtonClicked()
    {
        playerActionMap.Enable();
        SceneManager.LoadScene("NewInput");
    }

    private void RemapButtonClicked(string name, Button uiButton, int mapIndex)
    {
        uiButton.enabled = false;
        okButtonMessageText.text = "Press button/stick for " + name;

        var action = playerActionMap.actions[mapIndex];
        rebindOperation = action.PerformInteractiveRebinding().OnComplete(operation => ButtonRebindCompleted(uiButton)).Start();
    }

    private void ResetButtonMappingTextValues()
    {
        gasMappingValueText.text = InputControlPath.ToHumanReadableString(playerActionMap.actions[gasMapIndex].bindings[0].ToString());
        brakeMappingValueText.text = InputControlPath.ToHumanReadableString(playerActionMap.actions[brakeMapIndex].bindings[0].ToString());
        fireMappingValueText.text = InputControlPath.ToHumanReadableString(playerActionMap.actions[fireMapIndex].bindings[0].ToString());
        turretMappingValueText.text = InputControlPath.ToHumanReadableString(playerActionMap.actions[turretMapIndex].bindings[0].ToString());
    }

    private void ButtonRebindCompleted(Button uiButton)
    {
        okButtonMessageText.text = "OK";
        rebindOperation.Dispose();
        ResetButtonMappingTextValues();
        uiButton.enabled = true;
    }
}
