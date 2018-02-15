using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using ISX;

// Print the last keyboard button state as a hex string.
//
[RequireComponent(typeof(Text))]
public class ButtonStateHex : MonoBehaviour
{
    [Header("This text box should have a monospace font.")]
    Text m_OutputText;

    private void Start()
    {
        m_OutputText = GetComponent<Text>();
    }

    // Update is called once per frame
    //
    void Update()
    {
        if (Keyboard.current != null)
        {
            m_OutputText.text = "0x" + _GetStringForKBButtonState(Keyboard.current);
        }
    }

    // _BoolHelper
    // true  => 1
    // false => 0
    //
    int _BoolHelper(bool boolIn)
    {
        if (!boolIn) { return 0; }
        else { return 1; }
    }

    // Procedurally generate a hex representation of a keyboard state.
    // Every 4 hex digits are period delimited
    // This representation does NOT start with "0x"
    //
    string GetStringForKBButtonState(Keyboard keyboard)
    {
        int workingInt = 0;
        int offset = 0;
        string retVal = "";

        for (int j = ((int)(Key.Count)) / 16; j >= 0; j--)
        {
            workingInt = 0;
            offset = j * 16;
            for (int i = 15; i >= 0; i--)
            {
                if (i + offset != 0 && i + offset < ((int)(Key.Count)))
                {
                    workingInt |= _BoolHelper(keyboard[(Key)(offset + i)].value != 0f) << (i);
                }
            }

            if (j != ((int)(Key.Count)) / 16) { retVal += "."; } // Don't put a trailing period because it looks dumb
            retVal += workingInt.ToString("X4");
        }

        return retVal;
    }
}
