using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Input;

public class InputDebugInfo : MonoBehaviour
{
    private bool m_isShowing = false;
    private bool m_isPlaying = false;       // if the menu is in the process of sliding in/out
    private bool m_isDragging = false;

    public Transform m_arrowUI;
    public RectTransform m_info;

    private int m_moveTime = 500;     // millinsecond

    private float m_startMouseY;
    private float m_startY;

    private InputAction m_toggleAction;

    void Start()
    {
        //m_toggleAction = new InputAction(name: "ToggleInfoDisplay");
        //m_toggleAction.AddBinding("<keyboard>/leftCtrl");

        //m_toggleAction.performed += _ => OnToggleDebugInfo();
        //m_toggleAction.Enable();
    }

    void OnEnable()
    {
        //if (m_toggleAction != null)
        //    m_toggleAction.Enable();
    }

    void OnDisable()
    {
        //m_toggleAction.Disable();
    }

    void Update()
    {
        if (InputSystem.GetDevice<Keyboard>() == null) return;

        Keyboard currentKeyboard = InputSystem.GetDevice<Keyboard>();
        if (currentKeyboard.leftCtrlKey.isPressed || currentKeyboard.rightCtrlKey.isPressed)
        {
            if (currentKeyboard.iKey.isPressed)
                OnToggleDebugInfo();
        }
    }

    public void OnToggleDebugInfo()
    {
        if (m_isPlaying || m_isDragging) return;

        m_isShowing = !m_isShowing;
        StartCoroutine("SlideToPositionX");
    }

    public void OnDragStart()
    {
        if (m_isPlaying) return;

        m_isDragging = true;
        m_startMouseY = Input.mousePosition.y;
        m_startY = transform.position.y;
    }

    public void OnDrag()
    {
        if (m_isPlaying) return;

        float delta = Input.mousePosition.y - m_startMouseY;
        Vector3 pos = transform.position;
        pos.y = Mathf.Min(Mathf.Max(m_startY + delta, m_info.rect.height * GetComponentInParent<Canvas>().scaleFactor), Screen.height);
        transform.position = pos;
    }

    public void OnDragEnd()
    {
        m_isDragging = false;
    }

    // Slide the debug info menu window in/out from view
    // Ratote the arrow UI 180 degrees
    private IEnumerator SlideToPositionX()
    {
        m_isPlaying = true;

        float posDifference = m_isShowing ? -1f * CalculateInfoContainerWidth() : CalculateInfoContainerWidth();
        float currentX = transform.position.x;
        float targetX = currentX + posDifference;

        Quaternion targetAngle = m_arrowUI.rotation * Quaternion.Euler(0f, 0f, 180f);

        while (Mathf.Abs(currentX - targetX) > 10f)
        {
            // Calculate the position in current frame
            currentX += Time.deltaTime * posDifference * 1000 / m_moveTime;
            SetPositionByX(currentX);

            // Rotate Arrow
            m_arrowUI.Rotate(Vector3.back, Time.deltaTime * 180 * 1000 / m_moveTime);

            yield return new WaitForEndOfFrame();
        }

        SetPositionByX(targetX);
        m_arrowUI.rotation = targetAngle;
        m_isPlaying = false;
    }

    private float CalculateInfoContainerWidth()
    {
        if (m_info != null)
            return m_info.rect.width * GetComponentInParent<Canvas>().scaleFactor;
        else
            throw new Exception("Need assign \"info\" Transform Rect.");
    }

    private void SetPositionByX(float posX)
    {
        Vector3 pos = transform.position;
        pos.x = posX;
        transform.position = pos;
    }
}
