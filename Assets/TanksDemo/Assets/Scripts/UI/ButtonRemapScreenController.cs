using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonRemapScreenController : MonoBehaviour
{
    public Button gasRemapButton;
    public Button brakeRemapButton;
    public Button fireRemapButton;
    public Button turretRemapButton;
    public Button turretRemapButtonUp;
    public Button turretRemapButtonDown;
    public Button turretRemapButtonLeft;
    public Button turretRemapButtonRight;
    public Button okButton;

    public Text gasMappingValueText;
    public Text brakeMappingValueText;
    public Text fireMappingValueText;
    public Text turretMappingValueText;
    public Text turretMappingValueTextUp;
    public Text turretMappingValueTextDown;
    public Text turretMappingValueTextLeft;
    public Text turretMappingValueTextRight;

    public InputActionAsset tanksInputActions;

    private const int gasMapIndex = 0;
    private const int brakeMapIndex = 1;
    private const int turretMapIndex = 2;
    private const int fireMapIndex = 3;

    private InputActionMap playerActionMap;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private RemapUI gasUI, brakeUI, fireUI, turrretUI;

    public class RemapUI
    {
        private Button m_Button;
        private Text m_Text;
        private InputAction m_Action;
        private int m_DefaultBindingIndex;
        private InputActionRebindingExtensions.RebindingOperation rebindOperation;
        private Button[] m_CompositeButtons;
        private Text[] m_CompositeTexts;
        private int[] m_CompositeBindingIndices;
        private Type m_ExpectedControlType;
        bool m_IsUsingComposite = false;

        public RemapUI(Button button, Text text, InputAction action, string name, int bindingIndex = 0, Type expectedControlType = null, Button[] compositeButtons = null, Text[] compositeTexts = null)
        {
            m_Button = button;
            m_Text = text;
            m_Action = action;
            m_DefaultBindingIndex = bindingIndex;
            m_Button.onClick.AddListener(delegate { RemapButtonClicked(name, m_DefaultBindingIndex); });
            m_ExpectedControlType = expectedControlType;
            m_CompositeButtons = compositeButtons;
            if (m_CompositeButtons != null)
            {
                m_CompositeTexts = compositeTexts;
                m_CompositeBindingIndices = Enumerable.Range(0, m_Action.bindings.Count)
                    .Where(x => m_Action.bindings[x].isPartOfComposite).ToArray();
                for (int i = 0; i < m_CompositeButtons.Length && i < m_CompositeBindingIndices.Length; i++)
                {
                    int compositeBindingIndex = m_CompositeBindingIndices[i];
                    m_CompositeButtons[i].onClick.AddListener(delegate { RemapButtonClicked(name, compositeBindingIndex); });
                }
            }
            ResetButtonMappingTextValue();
        }

        private float ScoreFunc(System.Type expectedControl, InputControl control, InputEventPtr eventPtr)
        {
            if (control.synthetic)
                return -1;

            // Give preference to controls which match the expected type (ie get the Vector2 for a Stick,
            // rather than individual axes), but allow other types to let us construct the control as a
            // composite.
            if (expectedControl != null && !expectedControl.IsInstanceOfType(control))
                return 0.1f;

            return 1;
        }

        void RemapButtonClicked(string name, int bindingIndex = 0)
        {
            m_Button.enabled = false;
            m_Text.text = "Press button/stick for " + name;
            rebindOperation = m_Action.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => ButtonRebindCompleted());
            if (m_CompositeBindingIndices != null)
            {
                rebindOperation = rebindOperation
                    .OnComputeScore((x, y) => ScoreFunc(m_ExpectedControlType, x, y))
                    .OnGeneratePath(x =>
                    {
                        if (m_ExpectedControlType != null && m_CompositeBindingIndices != null &&
                            !m_ExpectedControlType.IsInstanceOfType(x))
                            m_IsUsingComposite = true;
                        else
                            m_IsUsingComposite = false;
                        return null;
                    })
                    .OnApplyBinding((x, path) =>
                    {
                        if (m_IsUsingComposite)
                        {
                            m_Action.ApplyBindingOverride(m_DefaultBindingIndex, null);
                            m_Action.ApplyBindingOverride(
                                bindingIndex != m_DefaultBindingIndex ? bindingIndex : m_CompositeBindingIndices[0],
                                path);
                        }
                        else
                        {
                            m_Action.ApplyBindingOverride(m_DefaultBindingIndex, path);
                            foreach (var i in m_CompositeBindingIndices)
                                m_Action.ApplyBindingOverride(i, null);
                        }
                    });
            }
            rebindOperation.Start();
        }

        void ResetButtonMappingTextValue()
        {
            m_Text.text = InputControlPath.ToHumanReadableString(m_Action.bindings[0].effectivePath);
            m_Button.gameObject.SetActive(!m_IsUsingComposite);
            if (m_CompositeTexts != null)
                for (int i = 0; i < m_CompositeTexts.Length; i++)
                {
                    m_CompositeTexts[i].text = InputControlPath.ToHumanReadableString(m_Action.bindings[i + 2].effectivePath);
                    m_CompositeButtons[i].gameObject.SetActive(m_IsUsingComposite);
                }
        }

        void ButtonRebindCompleted()
        {
            rebindOperation.Dispose();
            ResetButtonMappingTextValue();
            m_Button.enabled = true;
        }
    }

    void Start()
    {
        playerActionMap = tanksInputActions.GetActionMap("Player");
        playerActionMap.Disable();

        gasUI = new RemapUI(gasRemapButton, gasMappingValueText, playerActionMap.actions[gasMapIndex], "Gas");
        brakeUI = new RemapUI(brakeRemapButton, brakeMappingValueText, playerActionMap.actions[brakeMapIndex], "Brake");
        fireUI = new RemapUI(fireRemapButton, fireMappingValueText, playerActionMap.actions[fireMapIndex], "Fire");
        turrretUI = new RemapUI(turretRemapButton, turretMappingValueText, playerActionMap.actions[turretMapIndex], "Turret",
            expectedControlType: typeof(InputControl<Vector2>),
            compositeButtons: new[] {turretRemapButtonUp, turretRemapButtonDown, turretRemapButtonLeft, turretRemapButtonRight},
            compositeTexts: new[] {turretMappingValueTextUp, turretMappingValueTextDown, turretMappingValueTextLeft, turretMappingValueTextRight});
        okButton.onClick.AddListener(OkButtonClicked);

        // Set the first button to be selected so that
        // gamepad navigation can be performed.
        gasRemapButton.Select();
    }

    private void OkButtonClicked()
    {
        playerActionMap.Enable();
        SceneManager.LoadScene("NewInput");
    }
}
