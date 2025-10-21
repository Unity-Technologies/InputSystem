using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class EditModeGhostCam : MonoBehaviour
{
    [SerializeField, Range(0.1f, 10f)]
    float m_MoveSpeed;

    [SerializeField, Range(1f, 10f)]
    float m_SpeedMultiplier;

    [SerializeField, Range(0.01f, 1f)]
    float m_RotateSpeed;

    [SerializeField]
    InputActionReference m_MoveActionReference;

    [SerializeField]
    InputActionReference m_LookActionReference;

    [SerializeField]
    InputActionReference m_ResetActionReference;

    [SerializeField]
    InputActionReference m_SpeedActionReference;

    InputAction m_MoveAction;
    InputAction m_LookAction;
    InputAction m_ResetAction;
    InputAction m_SpeedAction;
    void OnEnable()
    {
        EnableAction(ref m_MoveActionReference, ref m_MoveAction);
        EnableAction(ref m_LookActionReference, ref m_LookAction);
        EnableAction(ref m_ResetActionReference, ref m_ResetAction, ResetRequested);
        EnableAction(ref m_SpeedActionReference, ref m_SpeedAction);
    }

    void OnDisable()
    {
        DisableAction(ref m_MoveAction);
        DisableAction(ref m_LookAction);
        DisableAction(ref m_ResetAction, ResetRequested);
        DisableAction(ref m_SpeedAction);
    }

    void EnableAction(ref InputActionReference actionReference, ref InputAction action, Action<InputAction.CallbackContext> performedCallback = null)
    {
        DisableAction(ref action);

        if (actionReference != null)
            action = actionReference.action;

        if (action != null)
        {
            if (performedCallback != null)
                action.performed += performedCallback;

            action.Enable();
        }
    }

    void DisableAction(ref InputAction action, Action<InputAction.CallbackContext> performedCallback = null)
    {
        if (action != null)
        {
            action.Disable();

            if (performedCallback != null)
                action.performed -= performedCallback;
        }

        action = null;
    }

    void Update()
    {
        if (m_LookAction != null)
        {
            var look = m_LookAction.ReadValue<Vector2>();
            look *= m_RotateSpeed;

            var myTransform = transform;
            myTransform.localEulerAngles = myTransform.localEulerAngles + new Vector3(-look.y, look.x, 0f);
        }

        if (m_MoveAction != null)
        {
            var move = m_MoveAction.ReadValue<Vector2>();
            move *= Time.deltaTime * m_MoveSpeed;
            if ((m_SpeedAction?.ReadValue<float>() ?? 0f) > 0.5f)
                move *= m_SpeedMultiplier;
            transform.Translate(move.x, 0f, move.y);
        }
    }

    void ResetRequested(InputAction.CallbackContext ctx)
    {
        var myTransform = transform;
        myTransform.localPosition = Vector3.zero;
        myTransform.localRotation = Quaternion.identity;
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        DisableAction(ref m_MoveAction);
        DisableAction(ref m_LookAction);
        DisableAction(ref m_ResetAction, ResetRequested);
        DisableAction(ref m_SpeedAction);

        EnableAction(ref m_MoveActionReference, ref m_MoveAction);
        EnableAction(ref m_LookActionReference, ref m_LookAction);
        EnableAction(ref m_ResetActionReference, ref m_ResetAction, ResetRequested);
        EnableAction(ref m_SpeedActionReference, ref m_SpeedAction);
    }
}
