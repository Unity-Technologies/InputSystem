using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchDebugInfo : InputDebugInfo
{
    [Header("Input Info Text")]
    public Transform m_oldInputInfoPool;
    public Transform m_ISXInfoPool;

    private Vector3 m_startPos;
    private int m_maxISXCount = 0;
    private int m_maxOldCount = 0;

    public int MaxOldInputCount
    {
        get { return m_maxOldCount; }
        set
        {
            m_maxOldCount = value;
            ChangeUIPanelSize(m_oldInputInfoPool, value);
        }
    }

    public int MaxISXCount
    {
        get { return m_maxISXCount; }
        set
        {
            m_maxISXCount = value;
            ChangeUIPanelSize(m_ISXInfoPool, value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        CheckShortcut();
    }

    public void AddOldInputInfo(string content, int index = 0)
    {
        AddInputInfo(content, m_oldInputInfoPool, index);
    }

    public void AddNewInputInfo(string content, int index = 0)
    {
        AddInputInfo(content, m_ISXInfoPool, index);
    }

    private void AddInputInfo(string content, Transform infoPool, int index = 0)
    {
        Transform infoSection = infoPool.GetChild(index);

        // Enable new section to display info
        if (infoSection != null)
        {
            if (!infoSection.gameObject.activeSelf)
                infoSection.gameObject.SetActive(true);
            infoSection.GetComponent<Text>().text = content;
        }

        // Disable the ones not used
        for (int i = index + 1; i < infoPool.childCount; i++)
            infoPool.GetChild(i)?.gameObject.SetActive(false);
    }

    // Adjust UI width and position
    private void ChangeUIPanelSize(Transform infoPool, int count = 0)
    {
        int max = Mathf.Max(m_maxISXCount, m_maxOldCount);
        max = Mathf.Max(max, 1);                    // The first section always shows
        max = Mathf.Min(max, infoPool.childCount);  // No need to be bigger than the child objects need

        // Clear the content in the first section when there is no touch input
        if (count == 0)
            infoPool.GetChild(0).GetComponent<Text>().text = "";

        m_info.sizeDelta = new Vector2(infoPool.localPosition.x + infoPool.GetChild(0).GetComponent<RectTransform>().rect.width * max, m_info.sizeDelta.y);
        if (m_isShowing)
            SetPositionByX(m_startPos.x - CalculateInfoContainerWidth());
    }
}
