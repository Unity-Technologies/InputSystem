using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A four-character code.
    /// </summary>
    /// <remarks>
    /// A four-character code is a struct containing four byte characters totalling a single <c>int</c>.
    /// FourCCs are frequently used in the input system to identify the format of data sent to or from
    /// the native backend representing events, input device state or commands sent to input devices.
    /// </remarks>
    public struct FourCC : IEquatable<FourCC>
    {
        private int m_Code;

        /// <summary>
        /// Create a FourCC from the given integer.
        /// </summary>
        /// <param name="code">FourCC code represented as an <c>int</c>. Character order is
        /// little endian. "ABCD" is stored with A in the highest order 8 bits and D in the
        /// lowest order 8 bits.</param>
        /// <remarks>
        /// This method does not actually verify whether the four characters in the code
        /// are printable.
        /// </remarks>
        public FourCC(int code)
        {
            m_Code = code;
        }

        /// <summary>
        /// Create a FourCC from the given four characters.
        /// </summary>
        /// <param name="a">First character.</param>
        /// <param name="b">Second character.</param>
        /// <param name="c">Third character.</param>
        /// <param name="d">Fourth character.</param>
        public FourCC(char a, char b = ' ', char c = ' ', char d = ' ')
        {
            m_Code = (a << 24) | (b << 16) | (c << 8) | d;
        }

        /// <summary>
        /// Create a FourCC from the given string.
        /// </summary>
        /// <param name="str">A string with four characters or less but with at least one character.</param>
        /// <exception cref="ArgumentException"><paramref name="str"/> is empty or has more than four characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        public FourCC(string str)
            : this()
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            var length = str.Length;
            if (length < 1 || length > 4)
                throw new ArgumentException("FourCC string must be one to four characters long!", nameof(str));

            var a = str[0];
            var b = length > 1 ? str[1] : ' ';
            var c = length > 2 ? str[2] : ' ';
            var d = length > 3 ? str[3] : ' ';

            m_Code = (a << 24) | (b << 16) | (c << 8) | d;
        }

        /// <summary>
        /// Convert the given FourCC into an <c>int</c>.
        /// </summary>
        /// <param name="fourCC">A FourCC.</param>
        /// <returns>The four characters of the code packed into one <c>int</c>. Character order is
        /// little endian. "ABCD" is stored with A in the highest order 8 bits and D in the
        /// lowest order 8 bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(FourCC fourCC)
        {
            return fourCC.m_Code;
        }

        /// <summary>
        /// Convert the given <c>int</c> into a FourCC.
        /// </summary>
        /// <param name="i">FourCC code represented as an <c>int</c>. Character order is
        /// little endian. "ABCD" is stored with A in the highest order 8 bits and D in the
        /// lowest order 8 bits.</param>
        /// <returns>The FourCC converted from <paramref name="i"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FourCC(int i)
        {
            return new FourCC(i);
        }

        /// <summary>
        /// Convert the FourCC into a string in the form of "ABCD".
        /// </summary>
        /// <returns>String representation of the FourCC.</returns>
        public override string ToString()
        {
            return
                $"{(char) (m_Code >> 24)}{(char) ((m_Code & 0xff0000) >> 16)}{(char) ((m_Code & 0xff00) >> 8)}{(char) (m_Code & 0xff)}";
        }

        /// <summary>
        /// Compare two FourCCs for equality.
        /// </summary>
        /// <param name="other">Another FourCC.</param>
        /// <returns>True if the two FourCCs are equal, i.e. have the same exact
        /// character codes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FourCC other)
        {
            return m_Code == other.m_Code;
        }

        /// <summary>
        /// Compare the FourCC to the given object.
        /// </summary>
        /// <param name="obj">An object. Can be null.</param>
        /// <returns>True if <paramref name="obj"/> is a FourCC that has the same
        /// character code sequence.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is FourCC cc && Equals(cc);
        }

        /// <summary>
        /// Compute a hash code for the FourCC.
        /// </summary>
        /// <returns>Simply returns the FourCC converted to an <c>int</c>.</returns>
        public override int GetHashCode()
        {
            return m_Code;
        }

        /// <summary>
        /// Compare two FourCCs for equality.
        /// </summary>
        /// <param name="left">First FourCC.</param>
        /// <param name="right">Second FourCC.</param>
        /// <returns>True if the two FourCCs are equal, i.e. have the same exact
        /// character codes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(FourCC left, FourCC right)
        {
            return left.m_Code == right.m_Code;
        }

        /// <summary>
        /// Compare two FourCCs for inequality.
        /// </summary>
        /// <param name="left">First FourCC.</param>
        /// <param name="right">Second FourCC.</param>
        /// <returns>True if the two FourCCs are not equal, i.e. do not have the same exact
        /// character codes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(FourCC left, FourCC right)
        {
            return left.m_Code != right.m_Code;
        }

        // Make annoying Microsoft code analyzer happy.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FourCC FromInt32(int i)
        {
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(FourCC fourCC)
        {
            return fourCC.m_Code;
        }
    }
}
