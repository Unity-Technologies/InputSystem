using UnityEngine;
using UnityEngine.EventSystems;

public class GUITest : MonoBehaviour
{
    public GameObject initialSelectedGameObject;
    
    public void OnEnable()
    {
        if (initialSelectedGameObject != null)
        {
            EventSystem current = EventSystem.current;
            if (current != null)
                EventSystem.current.SetSelectedGameObject(initialSelectedGameObject);
        }      
    }

    public void OnButtonClick()
    {
        Debug.Log("Button Click Recieved");
    }
}
