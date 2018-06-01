using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ArrowHighlight : MonoBehaviour
{
    private byte moveSpeed = 2;
    private byte fadeSpeed = 10;

    private Material mat;
    private bool isMouseMove = false;
    private bool isPlaying = false;

    // Use this for initialization
    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        SetAlpha(0f);
    }

    // The loop for effect
    private IEnumerator HighlightMovement()
    {
        float alpha = 0f;
        float offset = 0f;
        isPlaying = true;

        while (isMouseMove)
        {
            // Fade in
            while (alpha < 0.8f)
            {
                alpha = Mathf.Min(alpha + fadeSpeed * Time.deltaTime, 0.8f);
                SetAlpha(alpha);
                yield return new WaitForEndOfFrame();
            }
            //yield return new WaitForSeconds(0.1f);

            // Offset
            while (offset > -0.5f)
            {
                offset = Mathf.Max(offset - moveSpeed * Time.deltaTime, -0.5f);
                mat.SetTextureOffset("_MainTex", new Vector2(offset, 0));
                yield return new WaitForEndOfFrame();
            }

            // Fade out
            while (alpha > 0f)
            {
                alpha -= fadeSpeed * Time.deltaTime;
                SetAlpha(alpha);
                yield return new WaitForEndOfFrame();
            }

            // Reset
            offset = 0f;
            mat.SetTextureOffset("_MainTex", new Vector2(offset, 0));
            //yield return new WaitForSeconds(0.1f);
        }

        isPlaying = false;
    }

    // To stop movement
    public void Stop()
    {
        isMouseMove = false;
    }

    public void Play()
    {
        isMouseMove = true;
        if (!isPlaying)
            StartCoroutine("HighlightMovement");
    }

    // Change the alpha channel for the material
    private void SetAlpha(float alpha)
    {
        Color matColor = mat.color;
        matColor.a = alpha;
        mat.color = matColor;
    }
}
