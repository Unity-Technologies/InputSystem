using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using ISX;

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

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null) { return; }

        SetImageColor(rightMouseButtonIndicator, !(mouse.rightButton.value == 0));
        SetImageColor(leftMouseButtonIndicator, !(mouse.leftButton.value == 0));
        SetImageColor(middleMouseButtonIndicator, !(mouse.middleButton.value == 0));

        positionX.text = mouse.position.x.value.ToString();
        positionY.text = mouse.position.y.value.ToString();

        deltaX.text = mouse.delta.x.value.ToString();
        deltaY.text = mouse.delta.y.value.ToString();

        scrollX.text = mouse.scroll.x.value.ToString();
        scrollY.text = mouse.scroll.y.value.ToString();
    }

    void SetImageColor(Image img, bool condition)
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
