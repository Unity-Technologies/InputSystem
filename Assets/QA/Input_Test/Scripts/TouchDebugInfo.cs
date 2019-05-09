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
        RectTransform infoSection;
        // Add a new section
        if (index + 1 > infoPool.childCount)
        {
            RectTransform template = infoPool.GetChild(0).GetComponent<RectTransform>();
            Vector3 pos = template.localPosition + new Vector3(template.rect.width, 0, 0);

            infoSection = Instantiate(template, infoPool);
            infoSection.localPosition = pos;
            infoSection.GetComponent<Text>().text = content;
        }
        //use an existing one
        else
        {
            infoSection = infoPool.GetChild(index).GetComponent<RectTransform>();
            infoSection.GetComponent<Text>().text = content;
        }

        // Remove the extra ones
        for (int i = index + 1; i < infoPool.childCount; i++)
            Destroy(infoPool.GetChild(i)?.gameObject);

        // Adjust UI width and position
        m_info.sizeDelta = new Vector2(infoPool.localPosition.x + infoSection.rect.width * infoPool.childCount, m_info.sizeDelta.y);
        
        if (m_isShowing)
            SetPositionByX(m_startPos.x - CalculateInfoContainerWidth());
    }
}
