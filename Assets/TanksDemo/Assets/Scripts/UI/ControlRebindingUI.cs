using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Linq;
using System;
using UnityEngine.InputSystem.Controls;

public class ControlRebindingUI : MonoBehaviour
{
    public Button m_Button;
    public Text m_Text;
    public InputActionReference m_ActionReference;
    public int m_DefaultBindingIndex;
    public Button[] m_CompositeButtons;
    public Text[] m_CompositeTexts;

    private InputAction m_Action;
    private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;
    private int[] m_CompositeBindingIndices;
    private string m_CompositeType;
    private bool m_IsUsingComposite;

    public void Start()
    {
        m_Action = m_ActionReference.action;
        if (m_Button == null)
            m_Button = GetComponentInChildren<Button>();
        if (m_Text == null)
            m_Text = m_Button.GetComponentInChildren<Text>();

        m_Button.onClick.AddListener(delegate { RemapButtonClicked(name, m_DefaultBindingIndex); });
        if (m_CompositeButtons != null && m_CompositeButtons.Length > 0)
        {
            if (m_CompositeTexts == null || m_CompositeTexts.Length != m_CompositeButtons.Length)
                m_CompositeTexts = new Text[m_CompositeButtons.Length];
            m_CompositeBindingIndices = Enumerable.Range(0, m_Action.bindings.Count)
                .Where(x => m_Action.bindings[x].isPartOfComposite).ToArray();
            var compositeBinding = m_Action.bindings.First(x => x.isComposite);
            m_CompositeType = compositeBinding.name;
            for (int i = 0; i < m_CompositeButtons.Length && i < m_CompositeBindingIndices.Length; i++)
            {
                int compositeBindingIndex = m_CompositeBindingIndices[i];
                m_CompositeButtons[i].onClick.AddListener(delegate { RemapButtonClicked(name, compositeBindingIndex); });
                if (m_CompositeTexts[i] == null)
                    m_CompositeTexts[i] = m_CompositeButtons[i].GetComponentInChildren<Text>();
            }
        }
        ResetButtonMappingTextValue();
    }

    private void OnDestroy()
    {
        m_RebindOperation?.Dispose();
    }

    private bool ControlMatchesCompositeType(InputControl control, string compositeType)
    {
        if (compositeType == null)
            return true;

        if (compositeType == "2D Vector")
            return typeof(InputControl<Vector2>).IsInstanceOfType(control);

        if (compositeType == "1D Axis")
            return typeof(AxisControl).IsInstanceOfType(control) && !typeof(ButtonControl).IsInstanceOfType(control);

        throw new ArgumentException($"{compositeType} is not a known composite type", nameof(compositeType));
    }

    private unsafe float ScoreFunc(string compositeType, InputControl control, InputEventPtr eventPtr)
    {
        var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
        var magnitude = control.EvaluateMagnitude(statePtr);

        if (control.synthetic)
            return magnitude - 1;

        // Give preference to controls which match the expected type (ie get the Vector2 for a Stick,
        // rather than individual axes), but allow other types to let us construct the control as a
        // composite.
        if (ControlMatchesCompositeType(control, m_CompositeType))
            return magnitude + 1;

        return magnitude;
    }

    void RemapButtonClicked(string name, int bindingIndex = 0)
    {
        m_Button.enabled = false;
        m_Text.text = "Press button/stick for " + name;
        m_RebindOperation?.Dispose();
        m_RebindOperation = m_Action.PerformInteractiveRebinding()
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => ButtonRebindCompleted());
        if (m_CompositeBindingIndices != null)
        {
            m_RebindOperation = m_RebindOperation
                .OnComputeScore((x, y) => ScoreFunc(m_CompositeType, x, y))
                .OnGeneratePath(x =>
                {
                    if (!ControlMatchesCompositeType(x, m_CompositeType))
                        m_IsUsingComposite = true;
                    else
                        m_IsUsingComposite = false;
                    return null;
                })
                .OnApplyBinding((x, path) =>
                {
                    if (m_IsUsingComposite)
                    {
                        m_Action.ApplyBindingOverride(m_DefaultBindingIndex, "");
                        m_Action.ApplyBindingOverride(
                            bindingIndex != m_DefaultBindingIndex ? bindingIndex : m_CompositeBindingIndices[0],
                            path);
                    }
                    else
                    {
                        m_Action.ApplyBindingOverride(m_DefaultBindingIndex, path);
                        foreach (var i in m_CompositeBindingIndices)
                            m_Action.ApplyBindingOverride(i, "");
                    }
                });
        }
        m_RebindOperation.Start();
    }

    void ResetButtonMappingTextValue()
    {
        m_Text.text = InputControlPath.ToHumanReadableString(m_Action.bindings[0].effectivePath);
        m_Button.gameObject.SetActive(!m_IsUsingComposite);
        if (m_CompositeTexts != null)
            for (int i = 0; i < m_CompositeTexts.Length; i++)
            {
                m_CompositeTexts[i].text = InputControlPath.ToHumanReadableString(m_Action.bindings[m_CompositeBindingIndices[i]].effectivePath);
                m_CompositeButtons[i].gameObject.SetActive(m_IsUsingComposite);
            }
    }

    void ButtonRebindCompleted()
    {
        m_RebindOperation.Dispose();
        m_RebindOperation = null;
        ResetButtonMappingTextValue();
        m_Button.enabled = true;
    }
}
