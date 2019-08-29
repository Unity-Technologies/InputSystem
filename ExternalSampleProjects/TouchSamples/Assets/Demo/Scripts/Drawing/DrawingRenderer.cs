using UnityEngine;

////FIXME: Doesn't seem to work as of 2019.3a5; the lines just disappeared on press release; tested only in editor

namespace InputSamples.Demo.Drawing
{
    /// <summary>
    /// Component that manages the flattening completed lines
    /// </summary>
    public class DrawingRenderer : MonoBehaviour
    {
        // Renderer that renders flattened background.
        [SerializeField]
        private Renderer backgroundObject;

        // Reference to drawing controller.
        [SerializeField]
        private DrawingController controller;

        // Name of layer that completed line gets moved to to be captured by our RT.
        [SerializeField]
        private string completedLayerName;

        // Camera that captures lines onto our flattened layer.
        [SerializeField]
        private Camera captureCamera;

        // Render texture for flattened layer (double-buffered).
        private RenderTexture background1, background2;

        // Shader used by capture render (to capture premultiplied output to our RT).
        private Shader captureShader;

        private void Awake()
        {
            if (backgroundObject != null && captureCamera != null)
            {
                background1 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                background2 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

                captureCamera.enabled = false;
                SwapBuffers();

                float aspect = (float)Screen.width / Screen.height;
                Vector3 scale = backgroundObject.transform.localScale;
                scale = new Vector3(scale.y * aspect, scale.y, 1);
                backgroundObject.transform.localScale = scale;
            }

            captureShader = Shader.Find("Hidden/Drawing/Capture");

            ClearCanvas();
        }

        private void OnEnable()
        {
            if (backgroundObject != null && captureCamera != null && controller != null)
            {
                controller.LineDrawn += OnLineDrawn;
                controller.Cleared += ClearCanvas;
            }
        }

        private void OnDisable()
        {
            if (backgroundObject != null && captureCamera != null && controller != null)
            {
                controller.LineDrawn -= OnLineDrawn;
                controller.Cleared -= ClearCanvas;
            }
        }

        /// <summary>
        /// Clear the canvas.
        /// </summary>
        public void ClearCanvas()
        {
            // Camera clears to 0, 0, 0, 0, so we just use it to clear the RT by temporarily removing the camera's
            // layer mask
            int prevMask = captureCamera.cullingMask;
            captureCamera.cullingMask = 0;

            captureCamera.Render();
            SwapBuffers();

            captureCamera.cullingMask = prevMask;
        }

        /// <summary>
        /// Flatten the given line.
        /// </summary>
        private DrawingLine OnLineDrawn(DrawingLine drawnLine)
        {
            LineRenderer lineRenderer = drawnLine.RendererInstance;
            lineRenderer.gameObject.layer = LayerMask.NameToLayer(completedLayerName);

            // Camera clears to 0, 0, 0, 0
            captureCamera.RenderWithShader(captureShader, "Premultiplied");

            Destroy(lineRenderer.gameObject);
            SwapBuffers();

            // Tell controller that we've been disabled
            return null;
        }

        private void SwapBuffers()
        {
            RenderTexture temp = background1;
            background1 = background2;
            background2 = temp;

            captureCamera.targetTexture = background1;

            backgroundObject.material.mainTexture = background2;
        }
    }
}
