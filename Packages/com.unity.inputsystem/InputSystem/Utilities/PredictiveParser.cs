using System;

namespace UnityEngine.InputSystem.Utilities
{
    // this parser uses Span<T> so it's only available from later unity versions
#if UNITY_2021_2_OR_NEWER
    internal struct PredictiveParser
    {
        public void ExpectSingleChar(ReadOnlySpan<char> str, char c)
        {
            if (str[m_Position] != c)
                throw new InvalidOperationException($"Expected a '{c}' character at position {m_Position} in : {str.ToString()}");

            ++m_Position;
        }

        public int ExpectInt(ReadOnlySpan<char> str)
        {
            int pos = m_Position;

            int sign = 1;
            if (str[pos] == '-')
            {
                sign = -1;
                ++pos;
            }

            int value = 0;
            for (;;)
            {
                var n = str[pos];
                if (n >= '0' && n <= '9')
                {
                    value *= 10;
                    value += n - '0';
                    ++pos;
                }
                else
                    break;
            }

            if (m_Position == pos)
                throw new InvalidOperationException($"Expected an int at position {m_Position} in {str.ToString()}");

            m_Position = pos;
            return value * sign;
        }

        public ReadOnlySpan<char> ExpectString(ReadOnlySpan<char> str)
        {
            var startPos = m_Position;
            if (str[startPos] != '\"')
                throw new InvalidOperationException($"Expected a '\"' character at position {m_Position} in {str.ToString()}");

            ++m_Position;

            for (;;)
            {
                var c = str[m_Position];
                c |= ' ';
                if (c >= 'a' && c <= 'z')
                {
                    ++m_Position;
                    continue;
                }

                break;
            }

            // if the first non-alpha character is not a quote, throw
            if (str[m_Position] != '\"')
                throw new InvalidOperationException($"Expected a closing '\"' character at position {m_Position} in string: {str.ToString()}");

            if (m_Position - startPos == 1)
                return ReadOnlySpan<char>.Empty;

            var output = str.Slice(startPos + 1, m_Position - startPos - 1);

            ++m_Position;
            return output;
        }

        public bool AcceptSingleChar(ReadOnlySpan<char> str, char c)
        {
            if (str[m_Position] != c)
                return false;

            m_Position++;
            return true;
        }

        public bool AcceptString(ReadOnlySpan<char> input, out ReadOnlySpan<char> output)
        {
            output = default;
            var startPos = m_Position;
            var endPos = startPos;
            if (input[endPos] != '\"')
                return false;

            ++endPos;

            for (;;)
            {
                var c = input[endPos];
                c |= ' ';
                if (c >= 'a' && c <= 'z')
                {
                    ++endPos;
                    continue;
                }

                break;
            }

            // if the first non-alpha character is not a quote, throw
            if (input[endPos] != '\"')
                return false;

            // empty string?
            if (m_Position - startPos == 1)
                output = ReadOnlySpan<char>.Empty;
            else
                output = input.Slice(startPos + 1, endPos - startPos - 1);

            m_Position = endPos + 1;
            return true;
        }

        public void AcceptInt(ReadOnlySpan<char> str)
        {
            // skip negative sign
            if (str[m_Position] == '-')
                ++m_Position;

            for (;;)
            {
                var n = str[m_Position];
                if (n >= '0' && n <= '9')
                    ++m_Position;
                else
                    break;
            }
        }

        private int m_Position;
    }
#endif
}
