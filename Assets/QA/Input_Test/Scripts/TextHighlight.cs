using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class TextHighlight : MonoBehaviour
{
    private TextMesh text_mesh;

    private bool is_playing = false;        // if a highligh is current in play
    private bool is_needed = false;         // if the highligh should keep playing

    private byte fade_speed = 10;

    // Use this for initialization
    void Start()
    {
        text_mesh = GetComponent<TextMesh>();
        SetAlpha(0f);
    }

    // The display sequence
    private IEnumerator Highlight()
    {
        is_playing = true;
        float alpha = text_mesh.color.a;

        while (alpha < 1f)
        {
            alpha = Mathf.Min(alpha + fade_speed * Time.deltaTime, 1f);
            SetAlpha(alpha);
            yield return new WaitForEndOfFrame();
        }

        while (is_needed)
            yield return new WaitForEndOfFrame();

        while (alpha > 0f)
        {
            alpha = Mathf.Max(alpha - fade_speed * Time.deltaTime * 0.1f, 0f);
            SetAlpha(alpha);
            yield return new WaitForEndOfFrame();
        }
        is_playing = false;
    }

    // Display the text
    public void Play(string txt)
    {
        text_mesh.text = txt;
        is_needed = true;

        if (is_playing)
            StopCoroutine("Highlight");
        StartCoroutine("Highlight");
    }

    public void Stop()
    {
        is_needed = false;
    }

    // Set alpha for the text
    private void SetAlpha(float alpha)
    {
        Color txtColor = text_mesh.color;
        txtColor.a = alpha;
        text_mesh.color = txtColor;
    }
}
