using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_MAGIC_LEAP
using UnityEngine.XR.MagicLeap;
#endif
public class MLReportEnumValues : MonoBehaviour
{
    public InputAction controllerTypeAction;
    public InputAction controllerDoFAction;
    public InputAction controllerCalibrationAccuracyAction;
    public InputAction lightWearCalibrationStatusAction;

    public Text controllerTypeText;
    public Text controllerDoFText;
    public Text controllerCalibrationAccuracyText;
    public Text lightWearCalibrationStatusText;

#if UNITY_MAGIC_LEAP
    void OnEnable()
    {
        if (controllerTypeAction != null)
        {
            controllerTypeAction.started += UpdateType;
            controllerTypeAction.performed += UpdateType;
            controllerTypeAction.canceled += UpdateType;
            controllerTypeAction.Enable();
        }

        if (controllerDoFAction != null)
        {
            controllerDoFAction.started += UpdateDoF;
            controllerDoFAction.performed += UpdateDoF;
            controllerDoFAction.canceled += UpdateDoF;
            controllerDoFAction.Enable();
        }

        if (controllerCalibrationAccuracyAction != null)
        {
            controllerCalibrationAccuracyAction.started += UpdateCalibrationAccuracy;
            controllerCalibrationAccuracyAction.performed += UpdateCalibrationAccuracy;
            controllerCalibrationAccuracyAction.canceled += UpdateCalibrationAccuracy;
            controllerCalibrationAccuracyAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (controllerTypeAction != null)
        {
            controllerTypeAction.Disable();
            controllerTypeAction.performed -= UpdateType;
            controllerTypeAction.started -= UpdateType;
            controllerTypeAction.canceled -= UpdateType;
        }

        if (controllerDoFAction != null)
        {
            controllerDoFAction.Disable();
            controllerDoFAction.performed -= UpdateDoF;
            controllerDoFAction.started -= UpdateDoF;
            controllerDoFAction.canceled -= UpdateDoF;
        }

        if (controllerCalibrationAccuracyAction != null)
        {
            controllerCalibrationAccuracyAction.Disable();
            controllerCalibrationAccuracyAction.performed -= UpdateCalibrationAccuracy;
            controllerCalibrationAccuracyAction.started -= UpdateCalibrationAccuracy;
            controllerCalibrationAccuracyAction.canceled -= UpdateCalibrationAccuracy;
        }
    }

    void UpdateType(InputAction.CallbackContext context)
    {
        if (controllerTypeText != null)
        {
            ControllerType type = (ControllerType)context.ReadValue<int>();
            controllerTypeText.text = $"Type: {type}";
        }
    }

    void UpdateDoF(InputAction.CallbackContext context)
    {
        if (controllerDoFText != null)
        {
            ControllerDoF dof = (ControllerDoF)context.ReadValue<int>();
            controllerDoFText.text = $"DoF: {dof}";
        }
    }

    void UpdateCalibrationAccuracy(InputAction.CallbackContext context)
    {
        if (controllerCalibrationAccuracyText != null)
        {
            ControllerCalibrationAccuracy accuracy = (ControllerCalibrationAccuracy)context.ReadValue<int>();
            controllerCalibrationAccuracyText.text = $"Calibration Accuracy: {accuracy}";
        }
    }

#endif
}
