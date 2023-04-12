using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class StringHelpers
    {
        /// <summary>
        /// For every character in <paramref name="str"/> that is contained in <paramref name="chars"/>, replace it
        /// by the corresponding character in <paramref name="replacements"/> preceded by a backslash.
        /// </summary>
        public static string Escape(this string str, string chars = "\n\t\r\\\"", string replacements = "ntr\\\"")
        {
            if (str == null)
                return null;

            // Scan for characters that need escaping. If there's none, just return
            // string as is.
            var hasCharacterThatNeedsEscaping = false;
            foreach (var ch in str)
            {
                if (chars.Contains(ch))
                {
                    hasCharacterThatNeedsEscaping = true;
                    break;
                }
            }
            if (!hasCharacterThatNeedsEscaping)
                return str;

            var builder = new StringBuilder();
            foreach (var ch in str)
            {
                var index = chars.IndexOf(ch);
                if (index == -1)
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('\\');
                    builder.Append(replacements[index]);
                }
            }
            return builder.ToString();
        }

        public static string Unescape(this string str, string chars = "ntr\\\"", string replacements = "\n\t\r\\\"")
        {
            if (str == null)
                return str;

            // If there's no backslashes in the string, there's nothing to unescape.
            if (!str.Contains('\\'))
                return str;

            var builder = new StringBuilder();
            for (var i = 0; i < str.Length; ++i)
            {
                var ch = str[i];
                if (ch == '\\' && i < str.Length - 2)
                {
                    ++i;
                    ch = str[i];
                    var index = chars.IndexOf(ch);
                    if (index != -1)
                        builder.Append(replacements[index]);
                    else
                        builder.Append(ch);
                }
                else
                {
                    builder.Append(ch);
                }
            }
            return builder.ToString();
        }

        public static bool Contains(this string str, char ch)
        {
            if (str == null)
                return false;
            return str.IndexOf(ch) != -1;
        }

        public static bool Contains(this string str, string text, StringComparison comparison)
        {
            if (str == null)
                return false;
            return str.IndexOf(text, comparison) != -1;
        }

        public static string GetPlural(this string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            switch (str)
            {
                case "Mouse": return "Mice";
                case "mouse": return "mice";
                case "Axis": return "Axes";
                case "axis": return "axes";
            }

            return str + 's';
        }

        public static string NicifyMemorySize(long numBytes)
        {
            // Gigabytes.
            if (numBytes > 1024 * 1024 * 1024)
            {
                var gb = numBytes / (1024 * 1024 * 1024);
                var remainder = (numBytes % (1024 * 1024 * 1024)) / 1.0f;

                return $"{gb + remainder} GB";
            }

            // Megabytes.
            if (numBytes > 1024 * 1024)
            {
                var mb = numBytes / (1024 * 1024);
                var remainder = (numBytes % (1024 * 1024)) / 1.0f;

                return $"{mb + remainder} MB";
            }

            // Kilobytes.
            if (numBytes > 1024)
            {
                var kb = numBytes / 1024;
                var remainder = (numBytes % 1024) / 1.0f;

                return $"{kb + remainder} KB";
            }

            // Bytes.
            return $"{numBytes} Bytes";
        }

        public static bool FromNicifiedMemorySize(string text, out long result, long defaultMultiplier = 1)
        {
            text = text.Trim();

            var multiplier = defaultMultiplier;
            if (text.EndsWith("MB", StringComparison.InvariantCultureIgnoreCase))
            {
                multiplier = 1024 * 1024;
                text = text.Substring(0, text.Length - 2);
            }
            else if (text.EndsWith("GB", StringComparison.InvariantCultureIgnoreCase))
            {
                multiplier = 1024 * 1024 * 1024;
                text = text.Substring(0, text.Length - 2);
            }
            else if (text.EndsWith("KB", StringComparison.InvariantCultureIgnoreCase))
            {
                multiplier = 1024;
                text = text.Substring(0, text.Length - 2);
            }
            else if (text.EndsWith("Bytes", StringComparison.InvariantCultureIgnoreCase))
            {
                multiplier = 1;
                text = text.Substring(0, text.Length - "Bytes".Length);
            }

            if (!long.TryParse(text, out var num))
            {
                result = default;
                return false;
            }

            result = num * multiplier;
            return true;
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

        public static IEnumerable<Substring> Tokenize(this string str)
        {
            var pos = 0;
            var length = str.Length;

            while (pos < length)
            {
                while (pos < length && char.IsWhiteSpace(str[pos]))
                    ++pos;

                if (pos == length)
                    break;

                if (str[pos] == '"')
                {
                    ++pos;
                    var endPos = pos;
                    while (endPos < length && str[endPos] != '\"')
                    {
                        // Doesn't recognize control sequences but allows escaping double quotes.
                        if (str[endPos] == '\\' && endPos < length - 1)
                            ++endPos;
                        ++endPos;
                    }
                    yield return new Substring(str, pos, endPos - pos);
                    pos = endPos + 1;
                }
                else
                {
                    var endPos = pos;
                    while (endPos < length && !char.IsWhiteSpace(str[endPos]))
                        ++endPos;
                    yield return new Substring(str, pos, endPos - pos);
                    pos = endPos;
                }
            }
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

        public static string Join<TValue>(string separator, params TValue[] values)
        {
            return Join(values, separator);
        }

        public static string Join<TValue>(IEnumerable<TValue> values, string separator)
        {
            // Optimize for there not being any values or only a single one
            // that needs no concatenation.
            var firstValue = default(string);
            var valueCount = 0;
            StringBuilder result = null;

            foreach (var value in values)
            {
                if (value == null)
                    continue;
                var str = value.ToString();
                if (string.IsNullOrEmpty(str))
                    continue;

                ++valueCount;
                if (valueCount == 1)
                {
                    firstValue = str;
                    continue;
                }

                if (valueCount == 2)
                {
                    result = new StringBuilder();
                    result.Append(firstValue);
                }

                result.Append(separator);
                result.Append(str);
            }

            if (valueCount == 0)
                return null;
            if (valueCount == 1)
                return firstValue;

            return result.ToString();
        }

        public static string MakeUniqueName<TExisting>(string baseName, IEnumerable<TExisting> existingSet,
            Func<TExisting, string> getNameFunc)
        {
            if (getNameFunc == null)
                throw new ArgumentNullException(nameof(getNameFunc));

            if (existingSet == null)
                return baseName;

            var name = baseName;
            var nameLowerCase = name.ToLower();
            var nameIsUnique = false;
            var namesTried = 1;

            // If the name ends in digits, start counting from the given number.
            if (baseName.Length > 0)
            {
                var lastDigit = baseName.Length;
                while (lastDigit > 0 && char.IsDigit(baseName[lastDigit - 1]))
                    --lastDigit;
                if (lastDigit != baseName.Length)
                {
                    namesTried = int.Parse(baseName.Substring(lastDigit)) + 1;
                    baseName = baseName.Substring(0, lastDigit);
                }
            }

            // Find unique name.
            while (!nameIsUnique)
            {
                nameIsUnique = true;
                foreach (var existing in existingSet)
                {
                    var existingName = getNameFunc(existing);
                    if (existingName.ToLower() == nameLowerCase)
                    {
                        name = $"{baseName}{namesTried}";
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
                throw new ArgumentNullException(nameof(firstList));
            if (secondList == null)
                throw new ArgumentNullException(nameof(secondList));

            // Go element by element through firstList and try to find a matching
            // element in secondList.
            var indexInFirst = 0;
            var lengthOfFirst = firstList.Length;
            var lengthOfSecond = secondList.Length;
            while (indexInFirst < lengthOfFirst)
            {
                // Skip empty elements.
                if (firstList[indexInFirst] == separator)
                    ++indexInFirst;

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
                    // Skip empty elements.
                    if (secondList[indexInSecond] == separator)
                        ++indexInSecond;

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

                            if (char.ToLowerInvariant(first) != char.ToLowerInvariant(second))
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

        public static bool WriteStringToBuffer(string text, IntPtr buffer, int bufferSizeInCharacters)
        {
            uint offset = 0;
            return WriteStringToBuffer(text, buffer, bufferSizeInCharacters, ref offset);
        }

        public static unsafe bool WriteStringToBuffer(string text, IntPtr buffer, int bufferSizeInCharacters, ref uint offset)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException("buffer");

            var length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            if (length > ushort.MaxValue)
                throw new ArgumentException(string.Format("String exceeds max size of {0} characters", ushort.MaxValue), "text");

            var endOffset = offset + sizeof(char) * length + sizeof(int);
            if (endOffset > bufferSizeInCharacters)
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
                throw new ArgumentNullException(nameof(buffer));

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

        public static bool IsPrintable(this char ch)
        {
            // This is crude and far from how Unicode defines printable but it should serve as a good enough approximation.
            return !char.IsControl(ch) && !char.IsWhiteSpace(ch);
        }

        public static string WithAllWhitespaceStripped(this string str)
        {
            var buffer = new StringBuilder();
            foreach (var ch in str)
                if (!char.IsWhiteSpace(ch))
                    buffer.Append(ch);
            return buffer.ToString();
        }

        public static bool InvariantEqualsIgnoreCase(this string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return string.IsNullOrEmpty(right);
            return string.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string ExpandTemplateString(string template, Func<string, string> mapFunc)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentNullException(nameof(template));
            if (mapFunc == null)
                throw new ArgumentNullException(nameof(mapFunc));

            var buffer = new StringBuilder();

            var length = template.Length;
            for (var i = 0; i < length; ++i)
            {
                var ch = template[i];
                if (ch != '{')
                {
                    buffer.Append(ch);
                    continue;
                }

                ++i;
                var tokenStartPos = i;
                while (i < length && template[i] != '}')
                    ++i;
                var token = template.Substring(tokenStartPos, i - tokenStartPos);
                // Loop increment will skip closing '}'.

                var mapped = mapFunc(token);
                buffer.Append(mapped);
            }

            return buffer.ToString();
        }
    }
}
