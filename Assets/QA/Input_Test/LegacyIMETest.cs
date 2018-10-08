using UnityEngine;

public class LegacyIMETest : MonoBehaviour
{
    public IMECompositionMode mode;
    public Vector2 cursorPosition;

    public bool isSelected;
    public string outputString;
    public string compositionString;

    // Update is called once per frame
    void Update()
    {
        if (enabled)
        {
            Input.imeCompositionMode = mode;
            Input.compositionCursorPos = cursorPosition;

            isSelected = Input.imeIsSelected;
            string newChars =  Input.inputString;
            if (newChars.Length > 0)
                outputString += Input.inputString;

            compositionString = Input.compositionString;
        }
    }
}
