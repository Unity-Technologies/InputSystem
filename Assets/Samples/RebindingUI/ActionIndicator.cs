using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ActionUIIndicator : MonoBehaviour
{
    public InputActionReference action;
    private Image image;
    
    void Start()
    {
        image = GetComponent<Image>();
    }
    
    private void OnEnable()
    {
        action.action.performed += OnPerformed;
        action.action.Enable();
    }
    
    private void OnDisable()
    {
        action.action.Disable();
        action.action.performed -= OnPerformed;
    }

    private void OnPerformed(InputAction.CallbackContext obj)
    {
        image.color = ColorWithAlpha(1.0f);   
    }

    private Color ColorWithAlpha(float alpha)
    {
        var color = image.color;
        return new Color(color.r, color.g, color.b, alpha);
    }

    private void Update()
    {
        var color = image.color;
        if (color.a > 0.0f)
            image.color = ColorWithAlpha(Mathf.Max(image.color.a - Time.deltaTime * 1.0f, 0.0f));
    }
}
