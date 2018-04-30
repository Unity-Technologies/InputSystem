using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.UI;

public class Vector2ControlActionStatus : MonoBehaviour
{
    public InputAction vector2Action;

    public Slider status1Slider;
    public Slider status2Slider;

    public Text status1Text;
    public Text status2Text;

    // Use this for initialization
    void Start()
    {
        vector2Action.Enable();
        vector2Action.performed += UpdateVector2;
        vector2Action.started += UpdateVector2;
        vector2Action.cancelled += UpdateVector2;
    }

    private void UpdateVector2(InputAction.CallbackContext context)
    {
        Vector2 value = ((Vector2Control)(context.control)).ReadValue();
        status1Slider.value = value.x;
        status2Slider.value = value.y;

        // 2018-04-30 Jack Pritz
        // This is commented out because it causes an error due to
        // https://github.com/StayTalm/InputSystem/issues/9
        //status1Text.text = value.x.ToString();
        //status2Text.text = value.y.ToString();
    }
}
