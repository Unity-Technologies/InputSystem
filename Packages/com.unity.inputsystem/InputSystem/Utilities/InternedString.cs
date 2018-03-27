using System;

////TODO: instead of using string.Intern, put them in a custom table and allow passing them around as indices
////      (this will probably also be useful for jobs)
////      when this is implemented, also allow interning directly from Substrings

namespace UnityEngine.Experimental.Input.Utilities
{
    /// <summary>
    /// Wraps around a string to allow for faster case-insensitive
    /// string comparisons while preserving original casing.
    /// </summary>
    public struct InternedString : IEquatable<InternedString>, IComparable<InternedString>
    {
        private readonly string m_StringOriginalCase;
        private readonly string m_StringLowerCase;

        public int length
        {
            get { return m_StringLowerCase != null ? m_StringLowerCase.Length : 0; }
        }

        public InternedString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                m_StringOriginalCase = null;
                m_StringLowerCase = null;
            }
            else
            {
                ////TODO: I think instead of string.Intern() this should use a custom weak-referenced intern table
                m_StringOriginalCase = string.Intern(text);
                m_StringLowerCase = string.Intern(text.ToLower());
            }
        }

        public bool IsEmpty()
        {
            return m_StringLowerCase == null;
        }

        public string ToLower()
        {
            return m_StringLowerCase;
        }

        public override bool Equals(object obj)
        {
            if (obj is InternedString)
                return Equals((InternedString)obj);

            var str = obj as string;
            if (str != null)
            {
                if (m_StringLowerCase == null)
                    return string.IsNullOrEmpty(str);
                return str.ToLower() == m_StringLowerCase;
            }

            return false;
        }

        public bool Equals(InternedString other)
        {
            return ReferenceEquals(m_StringLowerCase, other.m_StringLowerCase);
        }

        public int CompareTo(InternedString other)
        {
            return string.Compare(m_StringLowerCase, other.m_StringLowerCase);
        }

        public override int GetHashCode()
        {
            if (m_StringLowerCase == null)
                return 0;
            return m_StringLowerCase.GetHashCode();
        }

        public override string ToString()
        {
            return m_StringOriginalCase ?? string.Empty;
        }

        public static bool operator==(InternedString a, InternedString b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(InternedString a, InternedString b)
        {
            return !a.Equals(b);
        }

        public static bool operator==(InternedString a, string b)
        {
            return string.Compare(a.m_StringLowerCase, b, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool operator!=(InternedString a, string b)
        {
            return string.Compare(a.m_StringLowerCase, b, StringComparison.OrdinalIgnoreCase) != 0;
        }

        public static bool operator==(string a, InternedString b)
        {
            return string.Compare(a, b.m_StringLowerCase, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool operator!=(string a, InternedString b)
        {
            return string.Compare(a, b.m_StringLowerCase, StringComparison.OrdinalIgnoreCase) != 0;
        }

        public static implicit operator string(InternedString str)
        {
            return str.m_StringOriginalCase;
        }
    }
}
