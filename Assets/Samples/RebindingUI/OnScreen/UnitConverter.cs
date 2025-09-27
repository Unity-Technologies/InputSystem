namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal static class UnitConverter
    {
        private const float kMillimetersPerInch = 25.4f;
        private const string kWarningMessage = "Screen.dpi not available, assuming 96 DPI.";

        private static float EffectivePpi(float ppi) => ppi <= 0.0f ? 96.0f : ppi;
        private static float PixelsToMillimetersFactor(float ppi) => kMillimetersPerInch / EffectivePpi(ppi);
        private static float MillimetersToPixelsFactor(float ppi) => EffectivePpi(ppi) / kMillimetersPerInch;

        #region PixelsToMillimeters

        public static float PixelsToMillimeters(float pixels, float ppi) => pixels * PixelsToMillimetersFactor(ppi);

        public static float PixelsToMillimeters(float millimeters) => PixelsToMillimeters(millimeters, Screen.dpi);

        /// <summary>
        /// Converts a pixel size to millimeters, based on Screen.dpi.
        /// </summary>
        /// <remarks>Screen.dpi may not be available on some platforms. In such cases, 96 DPI is assumed.</remarks>
        public static Vector2 PixelsToMillimeters(Vector2 pixels) => PixelsToMillimeters(pixels, Screen.dpi);


        /// <summary>
        /// Converts a size in pixels to millimeters based on the given PPI (Pixels-per-inch).
        /// </summary>
        /// <param name="pixels">The size in pixels (screen-space).</param>
        /// <param name="ppi">Pixels-per-inch (PPI). If this is zero or negative DefaultPixelsPerInch will be used.</param>
        /// <returns></returns>
        public static Vector2 PixelsToMillimeters(Vector2 pixels, float ppi) => pixels * PixelsToMillimetersFactor(ppi);

        #endregion

        #region MillimetersToPixels

        public static float MillimetersToPixels(float millimeters, float ppi) => millimeters * MillimetersToPixelsFactor(ppi);

        public static float MillimetersToPixels(float millimeters) => MillimetersToPixels(millimeters, Screen.dpi);

        /// <summary>
        /// Converts a size in millimeters to pixels based on the given PPI (Pixels-per-inch).
        /// </summary>
        /// <param name="millimeters">The size in millimeters</param>
        /// <param name="ppi">Pixels-per-inch (PPI). If this is zero or negative kDefaultPixelsPerInch will be used.</param>
        /// <returns>Size expressed in pixels.</returns>
        public static Vector2 MillimetersToPixels(Vector2 millimeters, float ppi) => millimeters * MillimetersToPixelsFactor(ppi);

        /// <summary>
        /// Converts a size in millimeters to pixels, based on Screen.dpi.
        /// </summary>
        /// <remarks>Screen.dpi may not be available on some platforms. In such cases, 96 DPI is assumed.</remarks>
        public static Vector2 MillimetersToPixels(Vector2 millimeters) => MillimetersToPixels(millimeters, Screen.dpi);

        #endregion
    }
}
