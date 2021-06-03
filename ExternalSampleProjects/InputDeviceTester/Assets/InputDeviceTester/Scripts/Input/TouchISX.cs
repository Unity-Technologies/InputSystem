using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TouchISX : MonoBehaviour
{
    [Tooltip("The Gameobject holds all the highlight objects for Input System")]
    public Transform m_HighlightPool;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    private InputAction m_touchAction;

    [Header("Script to Show More Info")]
    public TouchDebugInfo m_touchInfo;

    // Use this for initialization
    void Start()
    {
        m_touchAction = new InputAction(name: "TouchAction", binding: "<touchscreen>/touch*"); // Not using <touch> in order to not get primaryTouch, too.
        m_touchAction.performed += callbackContext => TouchInput(callbackContext.control as TouchControl);
        m_touchAction.canceled += callbackContext => EndTouchInput(callbackContext.control as TouchControl);
        m_touchAction.Enable();
    }

    void OnEnable()
    {
        m_touchAction?.Enable();
    }

    void OnDisable()
    {
        m_touchAction?.Disable();
    }

    void Update()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && m_touchInfo != null)
        {
            m_touchInfo.MaxISXCount = touchscreen.touches.Count;

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                TouchControl touch = touchscreen.touches[i];
                string touchInfo = touch.touchId.ReadValue() + "\n"
                    + touch.phase.ReadValue().ToString() + "\n"
                    + touch.position.ReadValue().ToString() + "\n"
                    + touch.pressure.ReadValue().ToString() + "\n"
                    + touch.radius.ReadValue().ToString() + "\n"
                    + touch.delta.ReadValue().ToString();
                m_touchInfo.AddNewInputInfo(touchInfo, i);
            }
        }
    }

    private void TouchInput(TouchControl control)
    {
        switch (control.phase.ReadValue())
        {
            case TouchPhase.Began:
                NewTouchInput(control);
                break;
            case TouchPhase.Moved:
                UpdateTouchInput(control);
                break;
            case TouchPhase.Canceled:
            case TouchPhase.Ended:
                EndTouchInput(control);
                break;
            case TouchPhase.Stationary:
                break;
            default:
                break;
        }
    }

    // When a new touch starts, use the first inactive highlight object
    private void NewTouchInput(TouchControl control)
    {
        int id = control.touchId.ReadValue();

        // Sometimes the Began phase is detected twice. The redundant one needs to be filtered out
        if (m_HighlightPool.Find(id.ToString()) != null) return;

        Vector2 pos = Camera.main.ScreenToWorldPoint(control.position.ReadValue());

        for (int i = 0; i < m_HighlightPool.childCount; i++)
        {
            if (!m_HighlightPool.GetChild(i).gameObject.activeSelf)
            {
                Transform highlight = m_HighlightPool.GetChild(i);
                highlight.name = id.ToString();
                highlight.position = new Vector3(pos.x, pos.y, 0.5f);
                highlight.gameObject.SetActive(true);

                // Change ID text
                Transform idText = highlight.Find("ID");
                if (idText != null)
                    idText.GetComponent<TextMesh>().text = "ID: " + id;

                return;
            }
        }
        ShowMessage("Touch " + id + " Detected.");
    }

    private void UpdateTouchInput(TouchControl control)
    {
        int id = control.touchId.ReadValue();
        Vector2 pos = Camera.main.ScreenToWorldPoint(control.position.ReadValue());

        Transform highlight = m_HighlightPool.Find(id.ToString());
        if (highlight != null)
        {
            highlight.position = new Vector3(pos.x, pos.y, 0.5f);

            // Update position text
            Transform posText = highlight.Find("Pos");
            if (posText != null)
                posText.GetComponent<TextMesh>().text = control.position.ReadValue().ToString("F0");
        }
    }

    // When a touch input ends, set the highlight inactive.
    private void EndTouchInput(TouchControl control)
    {
        int id = control.touchId.ReadValue();
        Transform highlight = m_HighlightPool.Find(id.ToString());

        if (highlight != null)
            highlight.gameObject.SetActive(false);
        else
            ShowMessage("Touch " + id + " Stopped.");
    }

    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=brown>" + msg + "</color>\n";
    }
}
