using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static class StringHelpers
    {
        public static string GetPlural(this string str)
        {
            switch (str)
            {
                case "Mouse": return "Mice";
                case "mouse": return "mice";
                case "Axis": return "Axes";
                case "axis": return "axis";
            }

            return str + 's';
        }

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

        public static IEnumerable<string> Split(this string str, Func<char, bool> predicate)
        {
            if (string.IsNullOrEmpty(str))
                yield break;

            var length = str.Length;
            var position = 0;

            while (position < length)
            {
                // Skip separator.
                var ch = str[position];
                if (predicate(ch))
                {
                    ++position;
                    continue;
                }

                // Skip to next separator.
                var startPosition = position;
                ++position;
                while (position < length)
                {
                    ch = str[position];
                    if (predicate(ch))
                        break;
                    ++position;
                }
                var endPosition = position;

                yield return str.Substring(startPosition, endPosition - startPosition);
            }
        }

        public static string Join<TValue>(IEnumerable<TValue> values, string separator)
        {
            // Optimize for there not being any values or only a single one
            // that needs no concatenation.
            var firstValue = default(TValue);
            var valueCount = 0;
            StringBuilder result = null;

            foreach (var value in values)
            {
                ++valueCount;
                if (valueCount == 1)
                {
                    firstValue = value;
                    continue;
                }

                if (valueCount == 2)
                {
                    result = new StringBuilder();
                    result.Append(firstValue.ToString());
                }

                result.Append(separator);
                result.Append(value.ToString());
            }

            if (valueCount == 0)
                return null;
            if (valueCount == 1)
                return firstValue.ToString();

            return result.ToString();
        }

        public static string MakeUniqueName<TExisting>(string baseName, IEnumerable<TExisting> existingSet,
            Func<TExisting, string> getNameFunc)
        {
            var name = baseName;
            var nameLowerCase = name.ToLower();
            var nameIsUnique = false;
            var namesTried = 1;

            // Find unique name.
            while (!nameIsUnique)
            {
                nameIsUnique = true;
                foreach (var existing in existingSet)
                {
                    var existingName = getNameFunc(existing);
                    if (existingName.ToLower() == nameLowerCase)
                    {
                        name = string.Format("{0}{1}", baseName, namesTried);
                        nameLowerCase = name.ToLower();
                        nameIsUnique = false;
                        ++namesTried;
                        break;
                    }
                }
            }

            return name;
        }

        ////REVIEW: should we allow whitespace and skip automatically?
        public static bool CharacterSeparatedListsHaveAtLeastOneCommonElement(string firstList, string secondList,
            char separator)
        {
            if (firstList == null)
                throw new ArgumentNullException("firstList");
            if (secondList == null)
                throw new ArgumentNullException("secondList");

            // Go element by element through firstList and try to find a matching
            // element in secondList.
            var indexInFirst = 0;
            var lengthOfFirst = firstList.Length;
            var lengthOfSecond = secondList.Length;
            while (indexInFirst < lengthOfFirst)
            {
                // Find end of current element.
                var endIndexInFirst = indexInFirst + 1;
                while (endIndexInFirst < lengthOfFirst && firstList[endIndexInFirst] != separator)
                    ++endIndexInFirst;
                var lengthOfCurrentInFirst = endIndexInFirst - indexInFirst;

                // Go through element in secondList and match it to the current
                // element.
                var indexInSecond = 0;
                while (indexInSecond < lengthOfSecond)
                {
                    // Find end of current element.
                    var endIndexInSecond = indexInSecond + 1;
                    while (endIndexInSecond < lengthOfSecond && secondList[endIndexInSecond] != separator)
                        ++endIndexInSecond;
                    var lengthOfCurrentInSecond = endIndexInSecond - indexInSecond;

                    // If length matches, do character-by-character comparison.
                    if (lengthOfCurrentInFirst == lengthOfCurrentInSecond)
                    {
                        var startIndexInFirst = indexInFirst;
                        var startIndexInSecond = indexInSecond;

                        var isMatch = true;
                        for (var i = 0; i < lengthOfCurrentInFirst; ++i)
                        {
                            var first = firstList[startIndexInFirst + i];
                            var second = secondList[startIndexInSecond + i];

                            if (char.ToLower(first) != char.ToLower(second))
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                            return true;
                    }

                    // Not a match so go to next.
                    indexInSecond = endIndexInSecond + 1;
                }

                // Go to next element.
                indexInFirst = endIndexInFirst + 1;
            }

            return false;
        }

        // Parse an int at the given position in the string.
        // Unlike int.Parse(), does not require allocating a new string containing only
        // the substring with the number.
        public static int ParseInt(string str, int pos)
        {
            var multiply = 1;
            var result = 0;
            var length = str.Length;

            while (pos < length)
            {
                var ch = str[pos];
                var digit = ch - '0';
                if (digit < 0 || digit > 9)
                    break;

                result = result * multiply + digit;

                multiply *= 10;
                ++pos;
            }

            return result;
        }

        ////TODO: this should use UTF-8 and not UTF-16

        public static bool WriteStringToBuffer(string text, IntPtr buffer, int bufferSize)
        {
            uint offset = 0;
            return WriteStringToBuffer(text, buffer, bufferSize, ref offset);
        }

        public static unsafe bool WriteStringToBuffer(string text, IntPtr buffer, int bufferSize, ref uint offset)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException("buffer");

            var length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            if (length > ushort.MaxValue)
                throw new ArgumentException(string.Format("String exceeds max size of {0} characters", ushort.MaxValue), "text");

            var endOffset = offset + sizeof(char) * length + sizeof(int);
            if (endOffset > bufferSize)
                return false;

            var ptr = ((byte*)buffer) + offset;
            *((ushort*)ptr) = (ushort)length;
            ptr += sizeof(ushort);

            for (var i = 0; i < length; ++i, ptr += sizeof(char))
                *((char*)ptr) = text[i];

            offset = (uint)endOffset;
            return true;
        }

        public static string ReadStringFromBuffer(IntPtr buffer, int bufferSize)
        {
            uint offset = 0;
            return ReadStringFromBuffer(buffer, bufferSize, ref offset);
        }

        public static unsafe string ReadStringFromBuffer(IntPtr buffer, int bufferSize, ref uint offset)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException("buffer");

            if (offset + sizeof(int) > bufferSize)
                return null;

            var ptr = ((byte*)buffer) + offset;
            var length = *((ushort*)ptr);
            ptr += sizeof(ushort);

            if (length == 0)
                return null;

            var endOffset = offset + sizeof(char) * length + sizeof(int);
            if (endOffset > bufferSize)
                return null;

            var text = Marshal.PtrToStringUni(new IntPtr(ptr), length);

            offset = (uint)endOffset;
            return text;
        }
    }
}
