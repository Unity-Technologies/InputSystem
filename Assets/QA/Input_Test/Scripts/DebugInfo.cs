using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DebugInfo : MonoBehaviour
{
    public static bool IsOn = false;
    private bool m_isPlaying = false;       // if the menu is in the process of sliding in/out

    public Transform m_arrowUI;
    public RectTransform m_infoContainer;

    private int m_moveTime = 500;     // millinsecond

    void Start()
    {
        ToggleDebugInfo();
    }

    public void ToggleDebugInfo()
    {
        if (m_isPlaying) return;

        IsOn = !IsOn;
        StartCoroutine("SlideToPositionX");
    }

    // Slide the debug info menu window in/out from view
    // Ratote the arrow UI 180 degrees
    private IEnumerator SlideToPositionX()
    {
        m_isPlaying = true;

        float posDifference = IsOn ? -1f * CalculateInfoContainerWidth() : CalculateInfoContainerWidth();
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
        return m_infoContainer.rect.width * GetComponentInParent<Canvas>().scaleFactor;
    }

    private void SetPositionByX(float posX)
    {
        Vector3 pos = transform.position;
        pos.x = posX;
        transform.position = pos;
    }
}
