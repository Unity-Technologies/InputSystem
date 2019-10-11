using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowHighlight : MonoBehaviour
{
    private byte moveSpeed = 16;
    private byte fadeSpeed = 10;
    private float move_offset = 4f;

    private SpriteRenderer sp_render;
    private Vector3 origin_pos;

    private bool isMouseMove = false;
    private bool isPlaying = false;

    // Use this for initialization
    void Start()
    {
        sp_render = GetComponent<SpriteRenderer>();
        origin_pos = transform.localPosition;
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        isMouseMove = false;
    }

    private void OnDisable()
    {
        if (isPlaying)
        {
            SetAlpha(0f);
            StopCoroutine("HighlightMovement");
            isPlaying = false;
        }
    }

    // The loop for effect
    private IEnumerator HighlightMovement()
    {
        isPlaying = true;

        while (isMouseMove)
        {
            // Reset
            float offset = 0f;
            float alpha = 0f;
            transform.localPosition = origin_pos;

            // Fade in
            while (alpha < 0.8f)
            {
                alpha = Mathf.Min(alpha + fadeSpeed * Time.deltaTime, 0.9f);
                SetAlpha(alpha);
                yield return new WaitForEndOfFrame();
            }

            // Offset
            while (offset < move_offset)
            {
                offset = Mathf.Min(offset + moveSpeed * Time.deltaTime, move_offset);
                transform.localPosition = origin_pos + new Vector3(offset, 0f, 0f);
                yield return new WaitForEndOfFrame();
            }

            // Fade out
            while (alpha > 0f)
            {
                alpha -= fadeSpeed * Time.deltaTime;
                SetAlpha(alpha);
                yield return new WaitForEndOfFrame();
            }
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
        Color matColor = sp_render.color;
        matColor.a = alpha;
        sp_render.color = matColor;
    }
}
