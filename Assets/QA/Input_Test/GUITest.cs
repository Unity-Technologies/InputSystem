using UnityEngine;
using UnityEngine.EventSystems;

public class GUITest : MonoBehaviour
{
    public GameObject initialSelectedGameObject;

    public void OnEnable()
    {
        if (initialSelectedGameObject != null)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(initialSelectedGameObject);
        }
    }

    public void OnButtonClick()
    {
        Debug.Log("Button Click Received");
    }
}
