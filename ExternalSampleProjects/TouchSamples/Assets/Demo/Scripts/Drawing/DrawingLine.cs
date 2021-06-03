using InputSamples.Drawing;
using UnityEngine;

namespace InputSamples.Demo.Drawing
{
    /// <summary>
    /// An instance of a drawn line in the project.
    /// </summary>
    public class DrawingLine
    {
        private float lineLength;
        private Vector3 lastPosition;

        private readonly LineRenderer rendererInstance;
        private readonly DrawingTool tool;

        public LineRenderer RendererInstance => rendererInstance;

        /// <summary>
        /// Create a new instance of the DrawingLine. Will instantiate a new prefab.
        /// </summary>
        public DrawingLine(DrawingTool tool, PointerInput input, Vector3 worldPoint, Color color)
        {
            this.tool = tool;

            rendererInstance = Object.Instantiate(tool.LinePrefab, worldPoint, Quaternion.identity);

            float pressure = input.Pressure ?? 1.0f;
            float curveWidth = tool.SizePressureCurve.Evaluate(pressure);
            AnimationCurve curve = AnimationCurve.Constant(0.0f, 1.0f, curveWidth);
            rendererInstance.widthCurve = curve;

            rendererInstance.positionCount = 1;
            rendererInstance.SetPosition(0, worldPoint);

            color.a = tool.LineAlpha;
            rendererInstance.startColor = color;
            rendererInstance.endColor = color;

            lastPosition = worldPoint;
            lineLength = 0;
        }

        /// <summary>
        /// Submit a new point to be added to the line.
        /// </summary>
        /// <remarks>
        /// Will only add a new point if it's beyond the <see cref="DrawingTool.MinVertexDistance"/> defined in
        /// the corresponding <see cref="DrawingTool"/> .
        /// </remarks>
        public void SubmitPoint(PointerInput input, Vector3 worldPoint)
        {
            float distance = Vector3.Distance(worldPoint, lastPosition);
            if (!(distance > tool.MinVertexDistance))
            {
                return;
            }

            rendererInstance.positionCount++;
            rendererInstance.SetPosition(rendererInstance.positionCount - 1,
                worldPoint);

            lastPosition = worldPoint;

            float previousLength = lineLength;
            lineLength += distance;

            float curveRescaleValue = previousLength / lineLength;

            float pressure = input.Pressure ?? 1.0f;
            float curveWidth = tool.SizePressureCurve.Evaluate(pressure);

            // Rescale width curve
            AnimationCurve curve = rendererInstance.widthCurve;
            for (var i = 0; i < curve.length; ++i)
            {
                Keyframe key = curve[i];
                key.time *= curveRescaleValue;
                curve.MoveKey(i, key);
            }

            curve.AddKey(1.0f, curveWidth);

            rendererInstance.widthCurve = curve;
        }
    }
}
