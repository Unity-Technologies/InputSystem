using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class ScreenKeyboardTestCameraScript : MonoBehaviour
{
    public Toggle m_KeyboardShowOccludingArea;
    public Material m_Material;
    public GameObject m_Controller;

    private void GLVertex(float x, float y)
    {
        GL.Vertex(new Vector3(x / Screen.width, 1.0f - y / Screen.height, 0));
    }

    void OnPostRender()
    {
        if (!m_KeyboardShowOccludingArea.isOn)
            return;
        GL.PushMatrix();

        var rc = m_Controller.GetComponent<ScreenKeyboardTestScript>().GetOccludingArea();
        m_Material.SetPass(0);
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        GL.Color(Color.red);
        GLVertex(rc.x, rc.y);
        GLVertex(rc.x + rc.width, rc.y);
        GLVertex(rc.x + rc.width, rc.y + rc.height);
        GLVertex(rc.x, rc.y + rc.height);
        GL.End();
        GL.PopMatrix();
    }
}
