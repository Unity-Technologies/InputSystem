using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using ISX;

public class MouseOutputs : MonoBehaviour {
    
    public Image LeftMouseButtonIndicator;
    public Image RightMouseButtonIndicator;
    public Image MiddleMouseButtonIndicator;
    public Text PositionX;
    public Text PositionY;
    public Text DeltaX;
    public Text DeltaY;
    public Text ScrollX;
    public Text ScrollY;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Mouse mouse = Mouse.current;

        if (mouse == null) { return; }

        SetImageColor(RightMouseButtonIndicator, !(mouse.rightButton.value == 0));
        SetImageColor(LeftMouseButtonIndicator, !(mouse.leftButton.value == 0));
        SetImageColor(MiddleMouseButtonIndicator, !(mouse.middleButton.value == 0));

        PositionX.text = mouse.position.x.value.ToString();
        PositionY.text = mouse.position.y.value.ToString();

        DeltaX.text = mouse.delta.x.value.ToString();
        DeltaY.text = mouse.delta.y.value.ToString();

        ScrollX.text = mouse.scroll.x.value.ToString();
        ScrollY.text = mouse.scroll.y.value.ToString();
    }

    void SetImageColor(Image img, bool Condition)
    {
        if (Condition)
        {
            img.color = Color.red;
        }
        else
        {
            img.color = Color.white;
        }
    }
}
