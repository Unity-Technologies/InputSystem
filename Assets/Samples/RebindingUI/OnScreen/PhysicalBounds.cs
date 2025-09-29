namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Defines a 2D rect transform suitable for device-specific bounds.
    /// </summary>
    /// <remarks>
    /// This is useful to anchor interactive regions based on physical aspects of a device, in
    /// particular touchscreen. When designing on-screen-controls, you want e.g. an on screen stick
    /// to stay consistent in size across devices. This mimics the behavior of a physical device
    /// where the mechanical offset of a gamepad stick wouldn't change depending on the screen size
    /// or resolution. This is relevant since our physical ability as humans is limited to our limbs, e.g.
    /// our number of fingers and their reaching length is fixed.
    ///
    /// When using a UI framework to visualize on-screen controls, you likely want to combine screen-based (pixels)
    /// and physical layout (millimeters or inches).
    /// </remarks>
    public struct PhysicalBounds
    {
        /// <summary>
        /// Evaluates whether this transform bounding rectangle contains the associated viewport coordinate.
        /// </summary>
        /// <param name="point">Viewport coordinate (normalized).</param>
        /// <returns>true if bounds contains point, else false.</returns>
        public bool ContainsViewportPoint(Vector2 point)
        {
            return ContainsPhysicalPoint(new Vector2(point.x * rect.width, point.y * rect.height));
        }

        /// <summary>
        /// Evaluates whether this transform bounding rectangle contains the associated screen point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="pixelsPerInch">Pixels-per-inch (PPI) pixel density of the associated display.</param>
        /// <remarks>Due to limited support for multiple displays, this may return incorrect results
        /// since it will use DPI (rather PPI) reported by Screen and not individual displays.</remarks>
        /// <returns>true if bounds contains point, else false.</returns>
        public bool ContainsScreenPoint(Vector2 screenPoint, float pixelsPerInch)
        {
            return ContainsPhysicalPoint(screenPoint * UnitConverter.PixelsToMillimetersConversionFactor(pixelsPerInch));
        }

        /// <summary>
        /// Returns whether this transform bounding rectangle contains the associated physical point.
        /// </summary>
        /// <param name="point">Physical space point in millimeters.</param>
        /// <returns></returns>
        public bool ContainsPhysicalPoint(Vector2 point)
        {
            switch (shape)
            {
                case AreaShape.Rectangle:
                    return rect.Contains(point);
                case AreaShape.Ellipse:
                {
                    var delta = point - rect.center;
                    var radius = rect.size / 2;
                    var value = (delta.x * delta.x) / (radius.x * radius.x) +
                        (delta.y * delta.y) / (radius.y * radius.y);
                    return value <= 1f;
                }
                default:
                    return false;
            }
        }

        private RectTransform rectTransform;

        /// <summary>
        /// A bounding rectangle in millimeters.
        /// </summary>
        [SerializeField]
        private Rect rect;

        /// <summary>
        /// The axis-aligned clipping shape.
        /// </summary>
        [SerializeField]
        private AreaShape shape;
    }
}
