using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;

public class TouchForInputSystem : MonoBehaviour
{
    [Tooltip("The Gameobject holds all the highlight objects for Input System")]
    public Transform m_HighlightPool;

    [Tooltip("Where all the messages go")]
    public InputField m_MessageWindow;

    private InputAction touch_action;

    // This is to store the touch id each highlight gameobject is showing for.
    // Since there are 10 highlight objects in the highlight_pool
    // the index number for the array is also the index for the child object in the pool
    private int[] touch_id_order = new int[10];

    // Use this for initialization
    void Start()
    {
        touch_action = new InputAction(name: "TouchAction", binding: "<touchscreen>/<touch>");
        touch_action.performed += callbackContext => TouchInput(callbackContext.control as TouchControl);
        touch_action.Enable();
    }

    private void TouchInput(TouchControl control)
    {
        switch (control.phase.ReadValue())
        {
            case PointerPhase.Began:
                NewTouchInput(control);
                UpdateTouchInput(control);
                break;
            case PointerPhase.Moved:
                UpdateTouchInput(control);
                break;
            case PointerPhase.Cancelled:
            case PointerPhase.Ended:
                EndTouchInput(control);
                break;
            default:
                break;
        }
    }

    private void NewTouchInput(TouchControl control)
    {
        int id = control.touchId.ReadValue();
        foreach (Transform highlight in m_HighlightPool)
        {
            if (!highlight.gameObject.activeSelf)
            {
                touch_id_order[highlight.GetSiblingIndex()] = id;
                highlight.gameObject.SetActive(true);

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

        for (int i = 0; i < 10; i++)
        {
            if (touch_id_order[i] == id)
            {
                Transform highlight = m_HighlightPool.GetChild(i);
                highlight.position = new Vector3(pos.x, pos.y, 0f);

                Transform posText = highlight.Find("Pos");
                if (posText != null)
                    posText.GetComponent<TextMesh>().text = control.position.ReadValue().ToString("F0");
            }
        }
    }

    private void EndTouchInput(TouchControl control)
    {
        int id = control.touchId.ReadValue();
        for (int i = 0; i < 10; i++)
        {
            if (touch_id_order[i] == id)
            {
                m_HighlightPool.GetChild(i).gameObject.SetActive(false);
                return;
            }
        }
        ShowMessage("Touch " + id + " Stopped.");
    }

    private void ShowMessage(string msg)
    {
        m_MessageWindow.text += "<color=brown>" + msg + "</color>\n";
    }
}
