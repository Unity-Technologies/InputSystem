using UnityEngine;
using UnityEngine.Experimental.Input;

// This script specifically prints out debug statements for
// a handful of keys that have been identified as functioning
// incorrectly.
//
public class ProblematicKeysDebugLog : MonoBehaviour
{
    void Update()
    {
        Keyboard keyboard = InputSystem.GetDevice<Keyboard>();

        if (keyboard == null) { return; }

        if (keyboard.leftMetaKey.ReadValue() != 0) { Debug.Log("LeftMeta"); }
        if (keyboard.rightMetaKey.ReadValue() != 0) { Debug.Log("RightMeta"); }

        if (keyboard.contextMenuKey.ReadValue() != 0) { Debug.Log("ContextMenu"); }

        if (keyboard.digit9Key.ReadValue() != 0) { Debug.Log("Digit9"); }
        if (keyboard.backslashKey.ReadValue() != 0) { Debug.Log("Backslash"); }

        if (keyboard.numpadEqualsKey.ReadValue() != 0) { Debug.Log("NumpadEquals"); }
        if (keyboard.numpadPlusKey.ReadValue() != 0) { Debug.Log("NumpadPlus"); }
        if (keyboard.numpadMinusKey.ReadValue() != 0) { Debug.Log("NumpadMinus"); }
        if (keyboard.numpadMultiplyKey.ReadValue() != 0) { Debug.Log("NumpadMultiply"); }
        if (keyboard.numpadDivideKey.ReadValue() != 0) { Debug.Log("NumpadDivide"); }
    }
}
