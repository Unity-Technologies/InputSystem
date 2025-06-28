using System;
using UnityEngine;
using UnityEngine.UI;

public interface IShowMessages
{
    public void ShowMessage(string message);
    public void ShowMessage(string message, TimeSpan duration, Action timeoutCallback = null);
    public void HideMessage();
}

public class Message : MonoBehaviour, IShowMessages
{
    public Text text;
    private double m_Timeout;
    private Action m_TimeoutCallback;

    public void HideMessage()
    {
        gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (text)
            text.text = message;
    }

    public void ShowMessage(string message, TimeSpan duration, Action timeoutCallback = null)
    {
        ShowMessage(message);
        m_TimeoutCallback = timeoutCallback;
        m_Timeout = Time.timeAsDouble + duration.TotalSeconds;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!(Time.timeAsDouble >= m_Timeout))
            return;

        HideMessage();

        m_TimeoutCallback?.Invoke();
        m_TimeoutCallback = null;
    }
}
