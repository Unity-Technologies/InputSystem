using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ISX;

public class ProblematicKeysDebugLog : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Keyboard KB = Keyboard.current;

        if (KB == null) { return; }

        if (KB.leftMetaKey.value != 0) { Debug.Log("LeftMeta"); }
        if (KB.rightMetaKey.value != 0) { Debug.Log("RightMeta"); }

        if (KB.contextMenuKey.value != 0) { Debug.Log("ContextMenu"); }

        if (KB.digit9Key.value != 0) { Debug.Log("Digit9"); }
        if (KB.backslashKey.value != 0) { Debug.Log("Backslash"); }

        if (KB.numpadEqualsKey.value != 0) { Debug.Log("NumpadEquals"); }
        if (KB.numpadPlusKey.value != 0) { Debug.Log("NumpadPlus"); }
        if (KB.numpadMinusKey.value != 0) { Debug.Log("NumpadMinus"); }
        if (KB.numpadMultiplyKey.value != 0) { Debug.Log("NumpadMultiply"); }
        if (KB.numpadDivideKey.value != 0) { Debug.Log("NumpadDivide"); }
    }
}
