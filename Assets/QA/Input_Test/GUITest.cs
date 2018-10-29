using UnityEngine;
using UnityEngine.EventSystems;

public class GUITest : UIBehaviour, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
{
    protected override void OnEnable()
    {
        base.OnEnable();

        EventSystem current = EventSystem.current;
        if (current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        EventSystem current = EventSystem.current;
        if (current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnButtonClick()
    {
        Debug.Log("Button Click Recieved");
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected Event Recieved");
        eventData.Use();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("Deselected Event Recieved");
        eventData.Use();
    }

    public void OnMove(AxisEventData eventData)
    {
        string directionStr = "Unknown";
        switch (eventData.moveDir)
        {
            case MoveDirection.Up:
                directionStr = "Up";
                break;
            case MoveDirection.Down:
                directionStr = "Down";
                break;
            case MoveDirection.Left:
                directionStr = "Left";
                break;
            case MoveDirection.Right:
                directionStr = "Right";
                break;
            case MoveDirection.None:
                directionStr = "None";
                break;
        }
        Debug.Log(string.Format("Move Event Recieved: [{0}] [{1}]", directionStr, eventData.moveVector));
        eventData.Use();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Debug.Log("Submit Event Recieved");
        eventData.Use();
    }

    public void OnCancel(BaseEventData eventData)
    {
        Debug.Log("Cancel Event Recieved");
        eventData.Use();
    }
}
