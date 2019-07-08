using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public abstract class ProcessorISX : MonoBehaviour
{
    [Header("The Input Action with Processors")]
    public InputAction m_inputAction;

    [Header("UI element for more info")]
    public Text m_originalText;
    public Text m_resultText;
    public Image m_stickImage = null;

    protected int m_stickImageOffsetFactor = 12;

    protected abstract void UpdateResult();
}
