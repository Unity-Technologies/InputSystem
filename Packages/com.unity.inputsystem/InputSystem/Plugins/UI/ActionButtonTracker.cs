using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.UI;

public class ActionButtonTracker : MonoBehaviour
{
    [SerializeField]
    InputActionProperty m_ButtonAction;

    [SerializeField]
    Image m_Image;

    [SerializeField]
    Color m_EnabledColor;

    [SerializeField]
    Color m_DisabledColor;

    void OnEnable()
    {
        var buttonAction = m_ButtonAction.action;
        if (buttonAction != null && !buttonAction.enabled)
        {
            buttonAction.performed += OnTriggered;
            buttonAction.Enable();
        }
    }

    void OnDisable()
    {
        var buttonAction = m_ButtonAction.action;
        if (buttonAction != null && buttonAction.enabled)
        {
            buttonAction.Disable();
            buttonAction.performed -= OnTriggered;
        }
    }

    // Update is called once per frame
    void OnTriggered(InputAction.CallbackContext context)
    {
        if(m_Image != null)
        {
            bool newState = context.ReadValue<float>() != 0.0f;
            m_Image.color = newState ? m_EnabledColor : m_DisabledColor;
        }
       
    }
}
