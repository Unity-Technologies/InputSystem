using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using ISX;

public class TouchReporter : MonoBehaviour {

    public Image image;
    public Text coordinateText;

    [Tooltip("This is the index of the touch that will be reported.  Appropriate values are between zero and TouchscreenState.kMaxTouches - 1")]
    public int touchIndex = 0;

	void Update () {
		Touchscreen touchscreen = ISX.Touchscreen.current;

        coordinateText.text = touchscreen.touches[touchIndex].value.position.x.ToString("0000") + ", " +
                touchscreen.touches[touchIndex].value.position.y.ToString("0000");

        if (touchscreen.touches[touchIndex].value.phase != PointerPhase.None &&
            touchscreen.touches[touchIndex].value.phase != PointerPhase.Ended)
        {
            image.color = Color.red;
        }
        else
        {
            image.color = Color.white;
        }
	}
}
