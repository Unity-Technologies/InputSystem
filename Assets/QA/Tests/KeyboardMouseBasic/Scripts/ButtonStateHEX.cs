using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using ISX;


[RequireComponent (typeof(Text))]
public class ButtonStateHEX : MonoBehaviour {

    [Header("This text box should have a monospace font")]
    Text OutputText;

    private void Start()
    {
        OutputText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update() {
        if (Keyboard.current != null) {
            Keyboard KB = Keyboard.current;
            OutputText.text = "0x" + _GetStringForKBButtonState(Keyboard.current);

            /*int workingInt = 0;
            int offset = 96;
            //for (int i = 31; i >= 0; i--)
            //{
            //    workingInt |= _BoolHelper(KB[(Key)(offset + i)].value != 0f) << (i + offset);
            //}
            OutputText.text += workingInt.ToString("X8");
            workingInt = 0;
            offset = 64;
            for (int i = 31; i >= 0; i--)
            {
                workingInt |= _BoolHelper(KB[(Key)(offset + i)].value != 0f) << (i + offset);
            }
            OutputText.text += workingInt.ToString("X8");
            workingInt = 0;
            offset = 32;
            for (int i = 31; i >= 0; i--)
            {
                workingInt |= _BoolHelper(KB[(Key)(offset + i)].value != 0f) << (i + offset);
            }
            OutputText.text += workingInt.ToString("X8");
            workingInt = 0;
            offset = 0;
            for (int i = 31; i >= 0; i--)
            {
                if (i == 0) { break; } // Don't check zero - it is invalid
                workingInt |= _BoolHelper(KB[(Key)(offset + i)].value != 0f) << (i + offset);
            }
            OutputText.text += workingInt.ToString("X8");*/
        }
	}
    
    int _BoolHelper(bool BoolIn)
    {
        if (!BoolIn) { return 0; }
        else { return 1; }
    }

    string _GetStringForKBButtonState(Keyboard KB)
    {
        int workingInt = 0;
        int offset = 0;
        string retVal = "";
        for (int j = ((int)(Key.Count))/16; j >= 0; j--)
        {
            workingInt = 0;
            offset = j * 16;
            for (int i = 15; i >= 0; i--)
            {
                if (i + offset != 0 && i + offset < ((int)(Key.Count)))
                {
                    workingInt |= _BoolHelper(KB[(Key)(offset + i)].value != 0f) << (i);
                }
            }

            if (j != ((int)(Key.Count)) / 16) { retVal += "."; } // Don't put a trailing period because it looks dumb
            retVal += workingInt.ToString("X4");
        }

        return retVal;
    }
}
