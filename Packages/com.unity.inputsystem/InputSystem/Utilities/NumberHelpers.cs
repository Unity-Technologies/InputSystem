using System;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static class NumberHelpers
    {
        public static int AlignToMultiple(int number, int alignment)
        {
            var remainder = number % alignment;
            if (remainder == 0)
                return number;

            return number + alignment - remainder;
        }

        public static uint AlignToMultiple(uint number, uint alignment)
        {
            var remainder = number % alignment;
            if (remainder == 0)
                return number;

            return number + alignment - remainder;
        }

        public static bool Approximately(double a, double b)
        {
            return Math.Abs(b - a) <  Math.Max(1E-06 * Math.Max(Math.Abs(a), Math.Abs(b)), double.Epsilon * 8);
        }
    }
}
