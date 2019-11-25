using System;
using System.Globalization;

////TODO: goal should be to end up with this being internal

////TODO: instead of using string.Intern, put them in a custom table and allow passing them around as indices
////      (this will probably also be useful for jobs)
////      when this is implemented, also allow interning directly from Substrings

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Wraps around a string to allow for faster case-insensitive string comparisons while
    /// preserving original casing.
    /// </summary>
    /// <remarks>
    /// Unlike <c>string</c>, InternedStrings can be compared with a quick <c>Object.ReferenceEquals</c>
    /// comparison and without actually comparing string contents.
    ///
    /// Also, unlike <c>string</c>, the representation of an empty and a <c>null</c> string is identical.
    ///
    /// Note that all string comparisons using InternedStrings are both case-insensitive and culture-insensitive.
    ///
    /// There is a non-zero cost to creating an InternedString. The first time a new unique InternedString
    /// is encountered, there may also be a GC heap allocation.
    /// </remarks>
    public struct InternedString : IEquatable<InternedString>, IComparable<InternedString>
    {
        private readonly string m_StringOriginalCase;
        private readonly string m_StringLowerCase;

        /// <summary>
        /// Length of the string in characters. Equivalent to <c>string.Length</c>.
        /// </summary>
        /// <value>Length of the string.</value>
        public int length => m_StringLowerCase?.Length ?? 0;

        /// <summary>
        /// Initialize the InternedString with the given string. Except if the string is <c>null</c>
        /// or empty, this requires an internal lookup (this is the reason the conversion from <c>string</c>
        /// to InternedString is not implicit).
        /// </summary>
        /// <param name="text">A string. Can be null.</param>
        /// <remarks>
        /// The InternedString preserves the original casing. Meaning that <see cref="ToString()"/> will
        /// return the string as it was supplied through <paramref name="text"/>. However, comparison
        /// between two InternedStrings is still always just a reference comparisons regardless of case
        /// and culture.
        ///
        /// <example>
        /// <code>
        /// var lowerCase = new InternedString("text");
        /// var upperCase = new InternedString("TEXT");
        ///
        /// // This is still just a quick reference comparison:
        /// if (lowerCase == upperCase)
        ///     Debug.Log("True");
        ///
        /// // But this prints the strings in their original casing.
        /// Debug.Log(lowerCase);
        /// Debug.Log(upperCase);
        /// </code>
        /// </example>
        /// </remarks>
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
                ////      (this way we can also avoid the garbage from ToLower())
                m_StringOriginalCase = string.Intern(text);
                m_StringLowerCase = string.Intern(text.ToLower(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Whether the string is empty, i.e. has a <see cref="length"/> of zero. If so, the
        /// InternedString corresponds to <c>default(InternedString)</c>.
        /// </summary>
        /// <returns>True if the string is empty.</returns>
        public bool IsEmpty()
        {
            return m_StringLowerCase == null;
        }

        /// <summary>
        /// Return a lower-case version of the string.
        /// </summary>
        /// <returns>A lower-case version of the string.</returns>
        /// <remarks>
        /// InternedStrings internally always store a lower-case version which means that this
        /// method does not incur a GC heap allocation cost.
        /// </remarks>
        public string ToLower()
        {
            return m_StringLowerCase;
        }

        /// <summary>
        /// Compare the InternedString to given object.
        /// </summary>
        /// <param name="obj">An object. If it is a <c>string</c>, performs a string comparison. If
        /// it is an InternedString, performs an InternedString-comparison. Otherwise returns false.</param>
        /// <returns>True if the InternedString is equal to <paramref name="obj"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is InternedString other)
                return Equals(other);

            if (obj is string str)
            {
                if (m_StringLowerCase == null)
                    return string.IsNullOrEmpty(str);
                return string.Equals(m_StringLowerCase, str.ToLower(CultureInfo.InvariantCulture));
            }

            return false;
        }

        /// <summary>
        /// Compare two InternedStrings for equality. They are equal if, ignoring case and culture,
        /// their text is equal.
        /// </summary>
        /// <param name="other">Another InternedString.</param>
        /// <returns>True if the two InternedStrings are equal.</returns>
        /// <remarks>
        /// This operation is cheap and does not involve an actual string comparison. Instead,
        /// a simple <c>Object.ReferenceEquals</c> comparison is performed.
        /// </remarks>
        public bool Equals(InternedString other)
        {
            return ReferenceEquals(m_StringLowerCase, other.m_StringLowerCase);
        }

        public int CompareTo(InternedString other)
        {
            return string.Compare(m_StringLowerCase, other.m_StringLowerCase,
                StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Compute a hash code for the string. Equivalent to <c>string.GetHashCode</c>.
        /// </summary>
        /// <returns>A hash code.</returns>
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
            return string.Compare(a.m_StringLowerCase, b.ToLower(CultureInfo.InvariantCulture),
                StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static bool operator!=(InternedString a, string b)
        {
            return string.Compare(a.m_StringLowerCase, b.ToLower(CultureInfo.InvariantCulture),
                StringComparison.InvariantCultureIgnoreCase) != 0;
        }

        public static bool operator==(string a, InternedString b)
        {
            return string.Compare(a.ToLower(CultureInfo.InvariantCulture), b.m_StringLowerCase,
                StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static bool operator!=(string a, InternedString b)
        {
            return string.Compare(a.ToLower(CultureInfo.InvariantCulture), b.m_StringLowerCase,
                StringComparison.InvariantCultureIgnoreCase) != 0;
        }

        public static bool operator<(InternedString left, InternedString right)
        {
            return string.Compare(left.m_StringLowerCase, right.m_StringLowerCase,
                StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        public static bool operator>(InternedString left, InternedString right)
        {
            return string.Compare(left.m_StringLowerCase, right.m_StringLowerCase,
                StringComparison.InvariantCultureIgnoreCase) > 0;
        }

        /// <summary>
        /// Convert the given InternedString back to a <c>string</c>. Equivalent to <see cref="ToString()"/>.
        /// </summary>
        /// <param name="str">An InternedString.</param>
        /// <returns>A string.</returns>
        public static implicit operator string(InternedString str)
        {
            return str.ToString();
        }
    }
}
