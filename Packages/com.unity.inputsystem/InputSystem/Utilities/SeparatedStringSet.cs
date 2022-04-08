using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal sealed class SeparatedStringSet
    {
        private readonly string separator;

        public string Value { get; private set; }

        public SeparatedStringSet(char separator)
            : this(null, Char.ToString(separator))
        {}

        public SeparatedStringSet(string separator)
            : this(null, separator)
        {}

        public SeparatedStringSet(string value, char separator)
            : this(value, Char.ToString(separator))
        {}

        public SeparatedStringSet(string value, string separator)
        {
            this.Value = value;
            this.separator = separator;
        }

        public bool IsEmpty => Value == null;

        public void Add(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException(nameof(value));
            if (Value == null)
            {
                Value = value;
                return;
            }
            if (FindExistingElement(value) >= 0)
                return; // Already in set
            Value += separator;
            Value += value;
        }

        public void Remove(string value)
        {
            if (Value == null) return;
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException(nameof(value));
            var index = FindExistingElement(value);
            if (index < 0)
                return;
            if (index == 0)
                Value = value.Length == Value.Length ? null : Value.Substring(value.Length + separator.Length);
            else if (index + value.Length == Value.Length)
                Value = Value.Substring(0, index - separator.Length);
            else
                Value = Value.Substring(0, index) + Value.Substring(index + value.Length + separator.Length);
        }

        private bool IsExistingElement(int index, int length)
        {   // Only valid if bounded by edge or separators, otherwise its a substring.
            return IsLeftValid(index) && IsRightValid(index + length);
        }

        private bool IsLeftValid(int index)
        {
            if (index == 0)
                return true;
            if (index > separator.Length &&
                0 == string.Compare(Value, index - separator.Length, separator, 0, separator.Length))
                return true;
            return false;
        }

        private bool IsRightValid(int index)
        {
            if (index == Value.Length)
                return true;
            if (index + separator.Length < Value.Length &&
                0 == string.Compare(Value, index, separator, 0, separator.Length))
                return true;
            return false;
        }

        private int FindExistingElement(string value)
        {
            for (var index = Value.IndexOf(value, 0); index != -1; index = Value.IndexOf(value, index + 1))
            {
                if (IsExistingElement(index, value.Length))
                    return index; // Already in set
            }
            return -1;
        }
    }
}
