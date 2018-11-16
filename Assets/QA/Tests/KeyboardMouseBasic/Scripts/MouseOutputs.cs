using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Experimental.Input;

// Updates images and text based on the state of the most current mouse.
//
public class MouseOutputs : MonoBehaviour
{
    public Image leftMouseButtonIndicator;
    public Image rightMouseButtonIndicator;
    public Image middleMouseButtonIndicator;
    public Text positionX;
    public Text positionY;
    public Text deltaX;
    public Text deltaY;
    public Text scrollX;
    public Text scrollY;

    public void Update()
    {
        var mouse = InputSystem.GetDevice<Mouse>();
        if (mouse == null)
            return;

        SetImageColor(rightMouseButtonIndicator, !(mouse.rightButton.ReadValue() == 0));
        SetImageColor(leftMouseButtonIndicator, !(mouse.leftButton.ReadValue() == 0));
        SetImageColor(middleMouseButtonIndicator, !(mouse.middleButton.ReadValue() == 0));

        positionX.text = mouse.position.x.ReadValue().ToString();
        positionY.text = mouse.position.y.ReadValue().ToString();

        deltaX.text = mouse.delta.x.ReadValue().ToString();
        deltaY.text = mouse.delta.y.ReadValue().ToString();

        scrollX.text = mouse.scroll.x.ReadValue().ToString();
        scrollY.text = mouse.scroll.y.ReadValue().ToString();
    }

    private static void SetImageColor(Image img, bool condition)
    {
        if (condition)
        {
            img.color = Color.red;
        }
        else
        {
            img.color = Color.white;
        }
    }
}
