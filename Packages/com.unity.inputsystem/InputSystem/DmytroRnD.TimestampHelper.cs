namespace UnityEngine.InputSystem.DmytroRnD
{
    internal static class TimestampHelper
    {
        public static long ConvertToLong(double timeSinceStartupInSeconds)
        {
            // given long max value 9223372036854775807
            // meaning it will span ~292 years
            return (long) (timeSinceStartupInSeconds * 1000000000.0);
        }

        public static double ConvertToSeconds(long time)
        {
            return ((double)time) / 1000000000.0;
        }
    }
}