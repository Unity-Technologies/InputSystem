namespace ISX
{
    internal static class StringHelpers
    {
        public static int CountOccurrences(this string str, char ch)
        {
            if (str == null)
                return 0;

            var length = str.Length;
            var index = 0;
            var count = 0;

            while (index < length)
            {
                var nextIndex = str.IndexOf(ch, index);
                if (nextIndex == -1)
                    break;

                ++count;
                index = nextIndex + 1;
            }

            return count;
        }
    }
}
