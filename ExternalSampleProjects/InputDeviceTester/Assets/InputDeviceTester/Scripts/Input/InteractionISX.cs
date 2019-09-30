using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Reflection;

public class InteractionISX : MonoBehaviour
{
    [Header("The Input Action with Interactions")]
    public InputAction m_inputAction;

    [Header("UI element to choose Phases")]
    public Toggle m_startedToggle;
    public Toggle m_performedToggle;
    public Toggle m_cancelledToggle;

    [Header("UI element for more info")]
    public Text m_phaseText;
    public Text m_ctxInfoText;
    public Text m_interactionItems;
    public Text m_interactionValues;

    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.started += ctx => OnInputStarted(ctx);
        m_inputAction.performed += ctx => OnInputPerformed(ctx);
        m_inputAction.canceled += ctx => OnInputCancelled(ctx);
    }

    private void OnEnable()
    {
        m_inputAction.Enable();
    }

    private void OnDisable()
    {
        m_inputAction?.Disable();
    }

    private void OnInputStarted(InputAction.CallbackContext ctx)
    {
        m_phaseText.text = "Started";

        if (m_startedToggle.isOn)
        {
            ShowCTXInfo(ctx);
            ShowInteractionFields(ctx);
        }
        else
        {
            // Clear all info from Performed, so it doesn't mix up with the previous result
            m_ctxInfoText.text = "";
            m_interactionItems.text = "";
            m_interactionValues.text = "";
        }
    }

    private void OnInputCancelled(InputAction.CallbackContext ctx)
    {
        m_phaseText.text += "\nCancelled";

        if (m_cancelledToggle.isOn)
        {
            ShowCTXInfo(ctx);
            ShowInteractionFields(ctx);
        }
    }

    private void OnInputPerformed(InputAction.CallbackContext ctx)
    {
        m_phaseText.text += string.IsNullOrEmpty(m_phaseText.text) ? "Performed" : "\nPerformed";

        if (m_performedToggle.isOn)
        {
            ShowCTXInfo(ctx);
            ShowInteractionFields(ctx);
        }
    }

    private void ShowCTXInfo(InputAction.CallbackContext ctx)
    {
        ButtonControl control = ctx.control as ButtonControl;
        m_ctxInfoText.text = ctx.action.name + "\n"
            + ctx.phase.ToString() + "\n"
            + ctx.startTime.ToString("F2") + "\n"
            + ctx.duration.ToString("F2") + "\n"
            + ctx.time.ToString("F2") + "\n"
            + control?.pressPoint.ToString("F1");
    }

    private void ShowInteractionFields(InputAction.CallbackContext ctx)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        FieldInfo[] fields = ctx.interaction.GetType().GetFields(bindingFlags);

        m_interactionItems.text = "";
        m_interactionValues.text = "";
        for (int i = 0; i < fields.Length; i++)
        {
            m_interactionItems.text += fields[i].Name + "\n";
            m_interactionValues.text += fields[i].GetValue(ctx.interaction).ToString() + "\n";
        }
    }
}
