using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

// Should warn about not having a canvas or parent canvas assigned.
// Should warn about ray-casted UI consuming events.


[ExecuteInEditMode]
public class OnScreenControlUI : MonoBehaviour
{
    public Canvas canvas;
    public Camera camera;
    public CustomOnScreenControl control;
    public RectTransform area;
    public RectTransform bounds;
    public RectTransform knob;

    private void OnDrawGizmosSelected()
    {
        // This will not produce meaningful results unless we have a rect transform (ISXB-915, ISXB-916).
        var parentRectTransform = transform.parent as RectTransform;
        if (parentRectTransform == null)
            return;

        Gizmos.matrix = parentRectTransform.localToWorldMatrix;

        var startPos = parentRectTransform.anchoredPosition;

        /*var startPos = parentRectTransform.anchoredPosition;
        if (Application.isPlaying)
            startPos = m_StartPos;
*/
        Gizmos.color = new Color32(84, 173, 219, 255);

        var center = startPos;
        /*if (Application.isPlaying && m_Behaviour == Behaviour.ExactPositionWithDynamicOrigin)
            center = m_PointerDownPos;*/

        var radius = UnitConverter.MillimetersToPixels(control.stickRadiusMillimeters);
        ScreenGizmos.DrawGizmoCircle(center, radius);

        //if (m_Behaviour != Behaviour.ExactPositionWithDynamicOrigin) return;

        //Gizmos.color = new Color32(158, 84, 219, 255);
        //DrawGizmoCircle(startPos, m_DynamicOriginRange);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void OnEnable()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (camera == null)
            camera = Camera.main;

        if (bounds != null)
        {
            var rawImage = knob.GetComponent<RawImage>();
            if (rawImage != null /*&& rawImage.texture == null*/)
            {
                var g = new Gradient
                {
                    colorKeys = new GradientColorKey[]
                    {
                        new(new Color(0.1f, 0.1f, 0.1f, 1.0f), 0.0f),
                        new(new Color(0.1f, 0.1f, 0.1f, 1.0f), 0.8f),
                        new(new Color(0.2f, 0.2f, 0.2f, 1.0f), 0.9f),
                        new(new Color(0.2f, 0.2f, 0.2f, 1.0f), 1.0f)
                    },
                    mode = GradientMode.PerceptualBlend
                };

                var texture = GenerateCircleTexture(128, 64.0f, g);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                bounds.GetComponent<RawImage>().texture = texture;
            }
        }

        if (knob != null)
        {
            var rawImage = knob.GetComponent<RawImage>();
            if (rawImage != null /*&& rawImage.texture == null*/)
            {
                var g = new Gradient
                {
                    colorKeys = new GradientColorKey[]
                    {
                        new(new Color(0.3f, 0.3f, 0.3f, 1.0f), 0.0f),
                        new(new Color(0.3f, 0.3f, 0.3f, 1.0f), 0.5f),
                        new(new Color(0.5f, 0.5f, 0.5f, 1.0f), 0.9f),
                        new(new Color(0.2f, 0.2f, 0.2f, 1.0f), 1.0f)
                    },
                    mode = GradientMode.PerceptualBlend
                };

                var texture = GenerateCircleTexture(128, 64.0f, g);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                knob.GetComponent<RawImage>().texture = texture;
            }
        }
    }

    void Update()
    {
        // If we do not have a control nor a camera, there is nothing we can visualize via UI.
        if (control == null || camera == null)
            return;

        var normalizedBounds = control.bounds;
        var stickRadiusPixels = UnitConverter.MillimetersToPixels(control.stickRadiusMillimeters);
        var stickViewport = camera.ScreenToViewportPoint(new Vector2(stickRadiusPixels, stickRadiusPixels));
        var stickRect = new Rect(normalizedBounds.center.x - stickViewport.x,
            normalizedBounds.center.y - stickViewport.y, stickViewport.x * 2, stickViewport.y * 2);

        // Optionally transform a UI object to represent the interactable area in viewport space.
        if (area != null)
            PlaceAtViewportRect(area, canvas, normalizedBounds, camera);

        // Optionally transform a UI object to represent the stick bounds.
        if (bounds != null)
            PlaceAtViewportRect(bounds, canvas, stickRect, camera);

        // Optionally transform a UI object represent the stick knob.
        if (knob != null)
        {
            var viewportPosition = stickRect.center;
            if (control.control is StickControl stick)
                viewportPosition += stick.ReadValue() * stickViewport;
            PlaceAtViewport(knob, canvas, viewportPosition, camera);
        }
    }

    /// <summary>
    /// Places a RectTransform at given viewport coordinates inside a Canvas.
    /// </summary>
    /// <param name="child">The UI element to move.</param>
    /// <param name="canvas">The parent canvas.</param>
    /// <param name="viewportPos">Normalized viewport coordinates.</param>
    /// <param name="camera">The camera that defines the viewport.</param>
    private static void PlaceAtViewport(RectTransform child, Canvas canvas, Vector2 viewportPos, Camera camera)
    {
        // Convert viewport coordinates to screen coordinates (pixels).
        Vector2 screenPos = camera.ViewportToScreenPoint(viewportPos);

        // Convert screen coordinates to local point in canvas.
        var canvasRect = canvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out var localPos);

        // Apply position to child.
        child.anchoredPosition = localPos;
    }

    /// <summary>
    /// Places and sizes a RectTransform to exactly match a viewport-space rect.
    /// Works regardless of pivot or anchors.
    /// </summary>
    /// <param name="rect">The UI RectTransform to place.</param>
    /// <param name="canvas">The parent canvas.</param>
    /// <param name="viewportRect">Rect in viewport coords (x,y,width,height) with 0–1 range.</param>
    /// <param name="camera">The camera defining the viewport.</param>
    public static void PlaceAtViewportRect(RectTransform rect, Canvas canvas, Rect viewportRect, Camera camera)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Convert bottom-left and top-right corners from viewport to screen
        Vector2 screenBL = camera.ViewportToScreenPoint(new Vector2(viewportRect.xMin, viewportRect.yMin));
        Vector2 screenTR = camera.ViewportToScreenPoint(new Vector2(viewportRect.xMax, viewportRect.yMax));

        // Convert to local canvas space
        Vector2 localBL, localTR;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenBL, canvas.worldCamera, out localBL);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTR, canvas.worldCamera, out localTR);

        // Compute size and center
        Vector2 size = localTR - localBL;
        Vector2 center = (localTR + localBL) * 0.5f;

        // Apply to RectTransform, ignoring pivot/anchors
        rect.localPosition = center;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Abs(size.x));
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(size.y));
    }

    private static Texture2D GenerateCircleTexture(int textureSize, float radius, Gradient gradient)
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        // Transparent background
        Color32[] pixels = new Color32[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Circle parameters
        var center = new Vector2(textureSize / 2f, textureSize / 2f);
        var squareRadius = radius * radius;

        // Draw circle
        for (var y = 0; y < textureSize; y++)
        {
            for (var x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                var magnitude = (pos - center).magnitude;
                if (magnitude <= radius)
                {
                    float t = magnitude / radius;
                    pixels[y * textureSize + x] = gradient.Evaluate(t);
                }
                // TODO Should be partial
            }
        }

        // Apply to texture
        tex.SetPixels32(pixels);
        tex.Apply();

        return tex;
    }
}
