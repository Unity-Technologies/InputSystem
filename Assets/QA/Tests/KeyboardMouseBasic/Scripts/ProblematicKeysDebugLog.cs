using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;

// This script specifically prints out debug statements for
// a handful of keys that have been identified as functioning
// incorrectly.
//
public class ProblematicKeysDebugLog : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null) { return; }

        if (keyboard.leftMetaKey.value != 0) { Debug.Log("LeftMeta"); }
        if (keyboard.rightMetaKey.value != 0) { Debug.Log("RightMeta"); }

        if (keyboard.contextMenuKey.value != 0) { Debug.Log("ContextMenu"); }

        if (keyboard.digit9Key.value != 0) { Debug.Log("Digit9"); }
        if (keyboard.backslashKey.value != 0) { Debug.Log("Backslash"); }

        if (keyboard.numpadEqualsKey.value != 0) { Debug.Log("NumpadEquals"); }
        if (keyboard.numpadPlusKey.value != 0) { Debug.Log("NumpadPlus"); }
        if (keyboard.numpadMinusKey.value != 0) { Debug.Log("NumpadMinus"); }
        if (keyboard.numpadMultiplyKey.value != 0) { Debug.Log("NumpadMultiply"); }
        if (keyboard.numpadDivideKey.value != 0) { Debug.Log("NumpadDivide"); }
    }
}
