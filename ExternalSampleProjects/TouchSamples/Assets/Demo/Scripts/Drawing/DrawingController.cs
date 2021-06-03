using System;
using System.Collections.Generic;
using System.Text;
using InputSamples.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InputSamples.Demo.Drawing
{
    /// <summary>
    /// Controller that listens to input from <see cref="PointerInputManager"/> and uses it to draw lines.
    /// </summary>
    public class DrawingController : MonoBehaviour
    {
        // Depth of lines from camera.
        private const float LineZ = 1;

        // Pen tool.
        [SerializeField]
        private DrawingTool penTool;

        // Highlighter tool.
        [SerializeField]
        private DrawingTool highlighterTool;

        // Camera with which our screen projections are performed.
        [SerializeField]
        private Camera renderCamera;

        // Reference to input manager.
        [SerializeField]
        private PointerInputManager inputManager;

        // Debug label.
        [SerializeField]
        private Text label;

        /// <summary>
        /// Event fired when a line is completed.
        /// </summary>
        public event Func<DrawingLine, DrawingLine> LineDrawn;

        /// <summary>
        /// Event fired when canvas is cleared.
        /// </summary>
        public event Action Cleared;

        // Mapping of active lines to input ID.
        private readonly Dictionary<int, DrawingLine> activeLines = new Dictionary<int, DrawingLine>();

        // Collection of old lines.
        private readonly List<DrawingLine> existingLines = new List<DrawingLine>();

        // Currently active tool.
        private DrawingTool activeTool;

        // Currently active colour.
        private Color activeColor = Color.black;

        protected virtual void Awake()
        {
            activeTool = penTool;
        }

        protected virtual void OnEnable()
        {
            inputManager.Pressed += OnPressed;
            inputManager.Dragged += OnDragged;
            inputManager.Released += OnReleased;
        }

        protected virtual void OnDisable()
        {
            inputManager.Pressed -= OnPressed;
            inputManager.Dragged -= OnDragged;
            inputManager.Released -= OnReleased;
        }

        /// <summary>
        /// Sets the controller to a new color.
        /// </summary>
        public void SetColor(Color newColor)
        {
            activeColor = newColor;
        }

        /// <summary>
        /// Sets the currently active tool.
        /// </summary>
        public void SetTool(int toolIndex)
        {
            switch (toolIndex)
            {
                default:
                case 0:
                    activeTool = penTool;
                    break;
                case 1:
                    activeTool = highlighterTool;
                    break;
            }
        }

        /// <summary>
        /// Clear all existing lines.
        /// </summary>
        public void ClearCanvas()
        {
            foreach (DrawingLine existingLine in existingLines)
            {
                if (existingLine != null &&
                    existingLine.RendererInstance != null)
                {
                    Destroy(existingLine.RendererInstance.gameObject);
                }
            }
            existingLines.Clear();

            if (Cleared != null)
            {
                Cleared();
            }
        }

        private Vector3 GetWorldPosFromInput(PointerInput input, float z)
        {
            return renderCamera.ScreenToWorldPoint(new Vector3(input.Position.x, input.Position.y, z));
        }

        private void OnPressed(PointerInput input, double time)
        {
            DebugInfo(input);

            if (!EventSystem.current.IsPointerOverGameObject(input.InputId))
            {
                CreateNewLine(input);
            }
        }

        private void OnDragged(PointerInput input, double time)
        {
            DebugInfo(input);

            DrawingLine activeLine;
            if (!activeLines.TryGetValue(input.InputId, out activeLine))
            {
                // Probably the press started on a UI element
                return;
            }

            activeLine.SubmitPoint(input, GetWorldPosFromInput(input, LineZ));
        }

        private void OnReleased(PointerInput input, double time)
        {
            DebugInfo(input);

            DrawingLine activeLine;
            if (!activeLines.TryGetValue(input.InputId, out activeLine))
            {
                // Probably the press started on a UI element
                return;
            }

            activeLine.SubmitPoint(input, GetWorldPosFromInput(input, LineZ));

            // Remove from dictionary
            activeLines.Remove(input.InputId);

            DrawingLine finalLine = activeLine;
            if (LineDrawn != null)
            {
                finalLine = LineDrawn(activeLine);
            }
            if (finalLine != null)
            {
                existingLines.Add(finalLine);
            }
        }

        private void CreateNewLine(PointerInput input)
        {
            Vector3 worldPos = GetWorldPosFromInput(input, LineZ);
            var newLine = new DrawingLine(activeTool, input, worldPos, activeColor);

            activeLines[input.InputId] = newLine;

            newLine.RendererInstance.transform.parent = transform;
        }

        // Temporary, for visualizing input from new input system.
        private void DebugInfo(PointerInput input)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("ID: {0}", input.InputId);
            builder.AppendLine();
            builder.AppendFormat("Position: {0}", input.Position);
            builder.AppendLine();

            if (input.Tilt.HasValue)
            {
                builder.AppendFormat("Tilt: {0}", input.Tilt);
                builder.AppendLine();
            }
            if (input.Pressure.HasValue)
            {
                builder.AppendFormat("Pressure: {0}", input.Pressure);
                builder.AppendLine();
            }
            if (input.Tilt.HasValue)
            {
                builder.AppendFormat("Tilt: {0}", input.Tilt);
                builder.AppendLine();
            }
            if (input.Radius.HasValue)
            {
                builder.AppendFormat("Radius: {0}", input.Radius);
                builder.AppendLine();
            }
            if (input.Twist.HasValue)
            {
                builder.AppendFormat("Twist: {0}", input.Twist);
                builder.AppendLine();
            }

            label.text = builder.ToString();
        }
    }
}
