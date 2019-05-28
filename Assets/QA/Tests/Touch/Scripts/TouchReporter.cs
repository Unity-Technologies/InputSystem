using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TouchReporter : MonoBehaviour
{
    public Image image;
    public Text coordinateText;

    [Tooltip("This is the index of the touch that will be reported.  Appropriate values are between zero and TouchscreenState.kMaxTouches - 1")]
    public int touchIndex = 0;

    void Update()
    {
        var touchscreen = InputSystem.GetDevice<Touchscreen>();
        if (touchscreen == null)
            return;

        var touch = touchscreen.touches[touchIndex];
        if (touch.isInProgress)
        {
            var position = touch.position.ReadValue();
            coordinateText.text =
                position.x.ToString("0000") + ", " +
                position.y.ToString("0000");
            image.color = Color.red;
        }
        else
        {
            image.color = Color.white;
        }
    }
}
