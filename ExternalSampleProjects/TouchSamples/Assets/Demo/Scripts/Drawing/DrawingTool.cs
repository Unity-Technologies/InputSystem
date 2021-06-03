using System;
using UnityEngine;

namespace InputSamples.Demo.Drawing
{
    /// <summary>
    /// A configurable tool for drawing.
    /// </summary>
    [Serializable]
    public class DrawingTool
    {
        [SerializeField]
        private AnimationCurve sizePressureCurve;

        [SerializeField]
        private float minVertexDistance = 0.1f;

        [SerializeField]
        private LineRenderer linePrefab;

        [SerializeField]
        private float lineAlpha;

        /// <summary>
        /// Curve that maps input pressure to line size.
        /// </summary>
        public AnimationCurve SizePressureCurve => sizePressureCurve;

        /// <summary>
        /// Minimum distance the pointer must move before we extend the line.
        /// </summary>
        public float MinVertexDistance => minVertexDistance;

        /// <summary>
        /// Prefab to use for this tool.
        /// </summary>
        public LineRenderer LinePrefab => linePrefab;

        /// <summary>
        /// Alpha of this line.
        /// </summary>
        public float LineAlpha => lineAlpha;
    }
}
