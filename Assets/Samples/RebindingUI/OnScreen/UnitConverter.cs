using System;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Conversion utility class for physical coordinate transforms.
    /// </summary>
    internal static class UnitConverter
    {
        /// <summary>
        /// Conversion factor for millimeters per inch.
        /// </summary>
        /// <example>
        /// var millimeters = 10.0f;
        /// Debug.Log($"{millimeters} mm is equal to {millimeters / MillimetersPerInch} inches.");
        /// </example>
        public const float MillimetersPerInch = 25.4f;

        /// <summary>
        /// Returns a conversion factor for converting pixels to millimeters using the given pixel density.
        /// </summary>
        /// <param name="ppi">Pixel density expressed as pixels-per-inch (PPI).</param>
        /// <returns>Conversion ratio for converting pixels to millimeters.</returns>
        public static float PixelsToMillimetersConversionFactor(float ppi)
        {
            if (ppi <= 0.0f)
                throw new ArgumentOutOfRangeException($"Invalid pixel density: {ppi}");
            return MillimetersPerInch / ppi;
        }

        /// <summary>
        /// Returns a conversion factor for converting millimeters to pixels using the given pixel density.
        /// </summary>
        /// <param name="ppi">Pixel density expressed as pixels-per-inch (PPI).</param>
        /// <returns>Conversion ratio for converting millimeters to pixels.</returns>
        public static float MillimetersToPixelsConversionFactor(float ppi)
        {
            if (ppi <= 0.0f)
                throw new ArgumentOutOfRangeException($"Invalid pixel density: {ppi}");
            return ppi / MillimetersPerInch;
        }

        /// <summary>
        /// Given a pixel density, returns the same pixel density (if positive), or returns a fallback PPI of 96.
        /// </summary>
        /// <param name="ppi">The candidate pixel density expressed as pixels-per-inch (PPI).</param>
        /// <returns>Returns <paramref name="ppi"/> if positive, otherwise 96 PPI.</returns>
        public static float EffectivePixelDensity(float ppi) => ppi <= 0.0f ? 96.0f : ppi;
    }
}
