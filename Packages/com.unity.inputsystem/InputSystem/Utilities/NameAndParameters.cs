using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

////TODO: add array support

////TODO: switch parsing to use to Substring

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A combination of a name and an optional list of named parameter values. For example, "Clamp(min=1,max=2)".
    /// </summary>
    public struct NameAndParameters
    {
        public string name { get; set; }
        public ReadOnlyArray<NamedValue> parameters { get; set; }

        public override string ToString()
        {
            if (parameters.Count == 0)
                return name;
            var parameterString = string.Join(NamedValue.Separator, parameters.Select(x => x.ToString()).ToArray());
            return $"{name}({parameterString})";
        }

        public static IEnumerable<NameAndParameters> ParseMultiple(string text)
        {
            List<NameAndParameters> list = null;
            if (!ParseMultiple(text, ref list))
                return Enumerable.Empty<NameAndParameters>();
            return list;
        }

        internal static bool ParseMultiple(string text, ref List<NameAndParameters> list)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            if (list == null)
                list = new List<NameAndParameters>();
            else
                list.Clear();

            var index = 0;
            var textLength = text.Length;

            while (index < textLength)
                list.Add(ParseNameAndParameters(text, ref index));

            return true;
        }

        public static NameAndParameters Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            var index = 0;
            return ParseNameAndParameters(text, ref index);
        }

        private static NameAndParameters ParseNameAndParameters(string text, ref int index)
        {
            var textLength = text.Length;

            // Skip whitespace.
            while (index < textLength && char.IsWhiteSpace(text[index]))
                ++index;

            // Parse name.
            var nameStart = index;
            while (index < textLength)
            {
                var nextChar = text[index];
                if (nextChar == '(' || nextChar == NamedValue.Separator[0] || char.IsWhiteSpace(nextChar))
                    break;
                ++index;
            }
            if (index - nameStart == 0)
                throw new ArgumentException($"Expecting name at position {nameStart} in '{text}'", nameof(text));
            var name = text.Substring(nameStart, index - nameStart);

            // Skip whitespace.
            while (index < textLength && char.IsWhiteSpace(text[index]))
                ++index;

            // Parse parameters.
            NamedValue[] parameters = null;
            if (index < textLength && text[index] == '(')
            {
                ++index;
                var closeParenIndex = text.IndexOf(')', index);
                if (closeParenIndex == -1)
                    throw new ArgumentException($"Expecting ')' after '(' at position {index} in '{text}'", nameof(text));

                var parameterString = text.Substring(index, closeParenIndex - index);
                parameters = NamedValue.ParseMultiple(parameterString);
                index = closeParenIndex + 1;
            }

            if (index < textLength && (text[index] == ',' || text[index] == InputBinding.Separator))
                ++index;

            return new NameAndParameters {name = name, parameters = new ReadOnlyArray<NamedValue>(parameters)};
        }
    }
}
