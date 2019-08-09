using System;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A four-character code.
    /// </summary>
    /// <remarks>
    /// A four-character code is a struct containing four byte characters. FourCCs are frequently used in the input system
    /// to identify the format of data sent to or from the native backend representing events, input device state or
    /// commands sent to input devices.
    /// </remarks>
    public struct FourCC : IEquatable<FourCC>
    {
        int m_Code;

        public FourCC(int code)
        {
            m_Code = code;
        }

        public FourCC(char a, char b = ' ', char c = ' ', char d = ' ')
        {
            m_Code = (a << 24) | (b << 16) | (c << 8) | d;
        }

        public FourCC(string str)
            : this()
        {
            if (str == null)
                throw new System.ArgumentNullException(nameof(str));

            var length = str.Length;
            Debug.Assert(length >= 1 && length <= 4, "FourCC string must be one to four characters long!");

            var a = str[0];
            var b = length > 1 ? str[1] : ' ';
            var c = length > 2 ? str[2] : ' ';
            var d = length > 3 ? str[3] : ' ';

            m_Code = (a << 24) | (b << 16) | (c << 8) | d;
        }

        public static implicit operator int(FourCC fourCC)
        {
            return fourCC.m_Code;
        }

        public static int ToInt32(FourCC fourCC)
        {
            return fourCC.m_Code;
        }

        public static implicit operator FourCC(int i)
        {
            var fourCC = new FourCC {m_Code = i};
            return fourCC;
        }

        public static FourCC FromInt32(int i)
        {
            return i;
        }

        public override string ToString()
        {
            return
                string.Format("{0}{1}{2}{3}", (char)(m_Code >> 24), (char)((m_Code & 0xff0000) >> 16),
                (char)((m_Code & 0xff00) >> 8), (char)(m_Code & 0xff));
        }

        public bool Equals(FourCC other)
        {
            return m_Code == other.m_Code;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is FourCC && Equals((FourCC)obj);
        }

        public override int GetHashCode()
        {
            return m_Code;
        }

        public static bool operator==(FourCC left, FourCC right)
        {
            return left.m_Code == right.m_Code;
        }

        public static bool operator!=(FourCC left, FourCC right)
        {
            return left.m_Code != right.m_Code;
        }
    }
}
