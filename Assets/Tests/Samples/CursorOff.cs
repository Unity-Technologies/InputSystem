using UnityEngine;

public class CursorOff : MonoBehaviour
{
    // Start is called before the first frame update

    bool toggle = false;
    void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (toggle)
                Cursor.visible = true;
            else
                Cursor.visible = false;
            toggle = !toggle;
        }
            
    }
}
