using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ISX;
using UnityEngine.UI;

public class KeyboardPress : MonoBehaviour {

    public Key ReportKey;

    [Header("If left empty, will try to auto populate with GetComponent<Image>()")]
    public Image ReportImage;

    private Keyboard KB;
    private char LastKeyPressed;

    private Color redTransparent;
    private Color whiteTransparent;

    private void Start()
    {
        redTransparent = new Color(1, 0, 0, 0.5f);
        whiteTransparent = new Color(1, 1, 1, 0.1f);

        if (ReportImage == null) { ReportImage = GetComponent<Image>(); }
    }

    void Update () {
        KB = Keyboard.current;

        if (KB == null) { return; }

		if (KB[ReportKey].value != 0)
        {
            ReportImage.color = redTransparent;
        }
        else
        {
            ReportImage.color = whiteTransparent;
        }
	}
}
