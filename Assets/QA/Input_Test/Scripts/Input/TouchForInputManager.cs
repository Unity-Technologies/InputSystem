using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchForInputManager : MonoBehaviour
{
    // This is the object contains all the highlight for touch inputs
    // There should be 10 highlight gameobjects in the pool for 10 touches at the same time
    // They are assigned to a touch input accord to the fingerId
    [Tooltip("The Gameobject holds all the highlight objects for Input Manager")]
    public Transform m_HighlightPool;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    // The old input manager does not support touch input for Standalone build, even when the device does.
#if !UNITY_STANDALONE
    // Use this for initialization
    void Start()
    {
        if (!Input.touchSupported)
            throw new Exception("Current device does not support touch input for old Input Manager.");

        if (!Input.touchPressureSupported)
            ShowMessage("Touch Pressue is not supported.");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        NewTouchInput(touch);
                        UpdateTouchInput(touch);
                        break;
                    case TouchPhase.Moved:
                        UpdateTouchInput(touch);
                        break;
                    case TouchPhase.Canceled:
                    case TouchPhase.Ended:
                        RemoveTouchInput(touch);
                        break;
                    case TouchPhase.Stationary:
                    default:
                        break;
                }
            }
        }
    }

    private void UpdateTouchInput(Touch touch)
    {
        if (touch.fingerId < 10)
        {
            Transform highlight = m_HighlightPool.GetChild(touch.fingerId);
            Vector2 pos = Camera.main.ScreenToWorldPoint(touch.position);
            highlight.position = new Vector3(pos.x, pos.y, 0f);

            Transform posText = highlight.Find("Pos");
            if (posText != null)
                posText.GetComponent<TextMesh>().text = touch.position.ToString("F0");
        }
    }

    private void NewTouchInput(Touch touch)
    {
        if (touch.fingerId < 10)
        {
            Transform highlight = m_HighlightPool.GetChild(touch.fingerId);
            highlight.gameObject.SetActive(true);

            Transform idText = highlight.Find("ID");
            if (idText != null)
                idText.GetComponent<TextMesh>().text = "ID: " + touch.fingerId;
        }
        else
            ShowMessage("Touch " + touch.fingerId + " Detected.");
    }

    private void RemoveTouchInput(Touch touch)
    {
        if (touch.fingerId < 10)
        {
            Transform highlight = m_HighlightPool.GetChild(touch.fingerId);
            highlight.gameObject.SetActive(false);
        }
        else
            ShowMessage("Touch " + touch.fingerId + " Stopped.");
    }

    // Show the unmapped key name in the text field
    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=blue>" + msg + "</color>\n";
    }

#endif
}
