using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A union holding a primitive value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PrimitiveValue : IEquatable<PrimitiveValue>, IConvertible
    {
        [FieldOffset(0)] private TypeCode m_Type;
        [FieldOffset(4)] private bool m_BoolValue;
        [FieldOffset(4)] private char m_CharValue;
        [FieldOffset(4)] private byte m_ByteValue;
        [FieldOffset(4)] private sbyte m_SByteValue;
        [FieldOffset(4)] private short m_ShortValue;
        [FieldOffset(4)] private ushort m_UShortValue;
        [FieldOffset(4)] private int m_IntValue;
        [FieldOffset(4)] private uint m_UIntValue;
        [FieldOffset(4)] private long m_LongValue;
        [FieldOffset(4)] private ulong m_ULongValue;
        [FieldOffset(4)] private float m_FloatValue;
        [FieldOffset(4)] private double m_DoubleValue;

        public TypeCode type => m_Type;

        /// <summary>
        /// If true, the struct does not contain a primitive value.
        /// </summary>
        public bool isEmpty => type == TypeCode.Empty;

        public PrimitiveValue(bool value)
            : this()
        {
            m_Type = TypeCode.Boolean;
            m_BoolValue = value;
        }

        public PrimitiveValue(char value)
            : this()
        {
            m_Type = TypeCode.Char;
            m_CharValue = value;
        }

        public PrimitiveValue(byte value)
            : this()
        {
            m_Type = TypeCode.Byte;
            m_ByteValue = value;
        }

        public PrimitiveValue(sbyte value)
            : this()
        {
            m_Type = TypeCode.SByte;
            m_SByteValue = value;
        }

        public PrimitiveValue(short value)
            : this()
        {
            m_Type = TypeCode.Int16;
            m_ShortValue = value;
        }

        public PrimitiveValue(ushort value)
            : this()
        {
            m_Type = TypeCode.UInt16;
            m_UShortValue = value;
        }

        public PrimitiveValue(int value)
            : this()
        {
            m_Type = TypeCode.Int32;
            m_IntValue = value;
        }

        public PrimitiveValue(uint value)
            : this()
        {
            m_Type = TypeCode.UInt32;
            m_UIntValue = value;
        }

        public PrimitiveValue(long value)
            : this()
        {
            m_Type = TypeCode.Int64;
            m_LongValue = value;
        }

        public PrimitiveValue(ulong value)
            : this()
        {
            m_Type = TypeCode.UInt64;
            m_ULongValue = value;
        }

        public PrimitiveValue(float value)
            : this()
        {
            m_Type = TypeCode.Single;
            m_FloatValue = value;
        }

        public PrimitiveValue(double value)
            : this()
        {
            m_Type = TypeCode.Double;
            m_DoubleValue = value;
        }

        public PrimitiveValue ConvertTo(TypeCode type)
        {
            switch (type)
            {
                case TypeCode.Boolean: return ToBoolean();
                case TypeCode.Char: return ToChar();
                case TypeCode.Byte: return ToByte();
                case TypeCode.SByte: return ToSByte();
                case TypeCode.Int16: return ToInt16();
                case TypeCode.Int32: return ToInt32();
                case TypeCode.Int64: return ToInt64();
                case TypeCode.UInt16: return ToInt16();
                case TypeCode.UInt32: return ToInt32();
                case TypeCode.UInt64: return ToInt64();
                case TypeCode.Single: return ToSingle();
                case TypeCode.Double: return ToDouble();
                case TypeCode.Empty: return new PrimitiveValue();
            }

            throw new ArgumentException($"Don't know how to convert PrimitiveValue to '{type}'", nameof(type));
        }

        public unsafe bool Equals(PrimitiveValue other)
        {
            if (m_Type != other.m_Type)
                return false;

            var thisValuePtr = UnsafeUtility.AddressOf(ref m_DoubleValue);
            var otherValuePtr = UnsafeUtility.AddressOf(ref other.m_DoubleValue);

            return UnsafeUtility.MemCmp(thisValuePtr, otherValuePtr, sizeof(double)) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (obj is PrimitiveValue value)
                return Equals(value);
            if (obj is bool || obj is char || obj is byte || obj is sbyte || obj is short
                || obj is ushort || obj is int || obj is uint || obj is long || obj is ulong
                || obj is float || obj is double)
                return Equals(FromObject(obj));
            return false;
        }

        public static bool operator==(PrimitiveValue left, PrimitiveValue right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(PrimitiveValue left, PrimitiveValue right)
        {
            return !left.Equals(right);
        }

        public override unsafe int GetHashCode()
        {
            unchecked
            {
                fixed(double* valuePtr = &m_DoubleValue)
                {
                    var hashCode = m_Type.GetHashCode();
                    hashCode = (hashCode * 397) ^ valuePtr->GetHashCode();
                    return hashCode;
                }
            }
        }

        public override string ToString()
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    // Default ToString() uses "False" and "True". We want lowercase to match C# literals.
                    return m_BoolValue ? "true" : "false";
                case TypeCode.Char:
                    return $"'{m_CharValue.ToString()}'";
                case TypeCode.Byte:
                    return m_ByteValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.SByte:
                    return m_SByteValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.Int16:
                    return m_ShortValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.UInt16:
                    return m_UShortValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.Int32:
                    return m_IntValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.UInt32:
                    return m_UIntValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.Int64:
                    return m_LongValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.UInt64:
                    return m_ULongValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.Single:
                    return m_FloatValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case TypeCode.Double:
                    return m_DoubleValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
                default:
                    return string.Empty;
            }
        }

        public static PrimitiveValue FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new PrimitiveValue();

            // Bool.
            if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValue(true);
            if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValue(false);

            // Double.
            if (value.Contains('.') || value.Contains("e") || value.Contains("E") ||
                value.Contains("infinity", StringComparison.InvariantCultureIgnoreCase))
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleResult))
                    return new PrimitiveValue(doubleResult);
            }

            // Int.
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                return new PrimitiveValue(intResult);
            }
            // Try hex format. For whatever reason, HexNumber does not allow a 0x prefix so we manually
            // get rid of it.
            if (value.IndexOf("0x", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                var hexDigits = value.TrimStart();
                if (hexDigits.StartsWith("0x"))
                    hexDigits = hexDigits.Substring(2);

                if (int.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexResult))
                    return new PrimitiveValue(hexResult);
            }

            ////TODO: allow trailing width specifier
            throw new NotImplementedException();
        }

        public TypeCode GetTypeCode()
        {
            return type;
        }

        public bool ToBoolean(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    return m_BoolValue;
                case TypeCode.Char:
                    return m_CharValue != '\0';
                case TypeCode.Byte:
                    return m_ByteValue != 0;
                case TypeCode.SByte:
                    return m_SByteValue != 0;
                case TypeCode.Int16:
                    return m_ShortValue != 0;
                case TypeCode.UInt16:
                    return m_UShortValue != 0;
                case TypeCode.Int32:
                    return m_IntValue != 0;
                case TypeCode.UInt32:
                    return m_UIntValue != 0;
                case TypeCode.Int64:
                    return m_LongValue != 0;
                case TypeCode.UInt64:
                    return m_ULongValue != 0;
                case TypeCode.Single:
                    return !Mathf.Approximately(m_FloatValue, 0);
                case TypeCode.Double:
                    return NumberHelpers.Approximately(m_DoubleValue, 0);
                default:
                    return default(bool);
            }
        }

        public byte ToByte(IFormatProvider provider = null)
        {
            return (byte)ToInt64(provider);
        }

        public char ToChar(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Char:
                    return m_CharValue;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return (char)ToInt64(provider);
                default:
                    return default(char);
            }
        }

        public DateTime ToDateTime(IFormatProvider provider = null)
        {
            throw new NotSupportedException("Converting PrimitiveValue to DateTime");
        }

        public decimal ToDecimal(IFormatProvider provider = null)
        {
            return new decimal(ToDouble(provider));
        }

        public double ToDouble(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    if (m_BoolValue)
                        return 1;
                    return 0;
                case TypeCode.Char:
                    return m_CharValue;
                case TypeCode.Byte:
                    return m_ByteValue;
                case TypeCode.SByte:
                    return m_SByteValue;
                case TypeCode.Int16:
                    return m_ShortValue;
                case TypeCode.UInt16:
                    return m_UShortValue;
                case TypeCode.Int32:
                    return m_IntValue;
                case TypeCode.UInt32:
                    return m_UIntValue;
                case TypeCode.Int64:
                    return m_LongValue;
                case TypeCode.UInt64:
                    return m_ULongValue;
                case TypeCode.Single:
                    return m_FloatValue;
                case TypeCode.Double:
                    return m_DoubleValue;
                default:
                    return default(double);
            }
        }

        public short ToInt16(IFormatProvider provider = null)
        {
            return (short)ToInt64(provider);
        }

        public int ToInt32(IFormatProvider provider = null)
        {
            return (int)ToInt64(provider);
        }

        public long ToInt64(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    if (m_BoolValue)
                        return 1;
                    return 0;
                case TypeCode.Char:
                    return m_CharValue;
                case TypeCode.Byte:
                    return m_ByteValue;
                case TypeCode.SByte:
                    return m_SByteValue;
                case TypeCode.Int16:
                    return m_ShortValue;
                case TypeCode.UInt16:
                    return m_UShortValue;
                case TypeCode.Int32:
                    return m_IntValue;
                case TypeCode.UInt32:
                    return m_UIntValue;
                case TypeCode.Int64:
                    return m_LongValue;
                case TypeCode.UInt64:
                    return (long)m_ULongValue;
                case TypeCode.Single:
                    return (long)m_FloatValue;
                case TypeCode.Double:
                    return (long)m_DoubleValue;
                default:
                    return default(long);
            }
        }

        public sbyte ToSByte(IFormatProvider provider = null)
        {
            return (sbyte)ToInt64(provider);
        }

        public float ToSingle(IFormatProvider provider = null)
        {
            return (float)ToDouble(provider);
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider = null)
        {
            return (ushort)ToUInt64(provider);
        }

        public uint ToUInt32(IFormatProvider provider = null)
        {
            return (uint)ToUInt64(provider);
        }

        public ulong ToUInt64(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    if (m_BoolValue)
                        return 1;
                    return 0;
                case TypeCode.Char:
                    return m_CharValue;
                case TypeCode.Byte:
                    return m_ByteValue;
                case TypeCode.SByte:
                    return (ulong)m_SByteValue;
                case TypeCode.Int16:
                    return (ulong)m_ShortValue;
                case TypeCode.UInt16:
                    return m_UShortValue;
                case TypeCode.Int32:
                    return (ulong)m_IntValue;
                case TypeCode.UInt32:
                    return m_UIntValue;
                case TypeCode.Int64:
                    return (ulong)m_LongValue;
                case TypeCode.UInt64:
                    return m_ULongValue;
                case TypeCode.Single:
                    return (ulong)m_FloatValue;
                case TypeCode.Double:
                    return (ulong)m_DoubleValue;
                default:
                    return default(ulong);
            }
        }

        public object ToObject()
        {
            switch (m_Type)
            {
                case TypeCode.Boolean: return m_BoolValue;
                case TypeCode.Char: return m_CharValue;
                case TypeCode.Byte: return m_ByteValue;
                case TypeCode.SByte: return m_SByteValue;
                case TypeCode.Int16: return m_ShortValue;
                case TypeCode.UInt16: return m_UShortValue;
                case TypeCode.Int32: return m_IntValue;
                case TypeCode.UInt32: return m_UIntValue;
                case TypeCode.Int64: return m_LongValue;
                case TypeCode.UInt64: return m_ULongValue;
                case TypeCode.Single: return m_FloatValue;
                case TypeCode.Double: return m_DoubleValue;
                default: return null;
            }
        }

        public static PrimitiveValue From<TValue>(TValue value)
            where TValue : struct
        {
            var type = typeof(TValue);
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Boolean: return new PrimitiveValue(Convert.ToBoolean(value));
                case TypeCode.Char: return new PrimitiveValue(Convert.ToChar(value));
                case TypeCode.Byte: return new PrimitiveValue(Convert.ToByte(value));
                case TypeCode.SByte: return new PrimitiveValue(Convert.ToSByte(value));
                case TypeCode.Int16: return new PrimitiveValue(Convert.ToInt16(value));
                case TypeCode.Int32: return new PrimitiveValue(Convert.ToInt32(value));
                case TypeCode.Int64: return new PrimitiveValue(Convert.ToInt64(value));
                case TypeCode.UInt16: return new PrimitiveValue(Convert.ToUInt16(value));
                case TypeCode.UInt32: return new PrimitiveValue(Convert.ToUInt32(value));
                case TypeCode.UInt64: return new PrimitiveValue(Convert.ToUInt64(value));
            }

            throw new ArgumentException(
                $"Cannot convert value '{value}' of type '{typeof(TValue).Name}' to PrimitiveValue", nameof(value));
        }

        public static PrimitiveValue FromObject(object value)
        {
            if (value == null)
                return new PrimitiveValue();

            if (value is string stringValue)
                return FromString(stringValue);

            if (value is bool b)
                return new PrimitiveValue(b);
            if (value is char ch)
                return new PrimitiveValue(ch);
            if (value is byte bt)
                return new PrimitiveValue(bt);
            if (value is sbyte sbt)
                return new PrimitiveValue(sbt);
            if (value is short s)
                return new PrimitiveValue(s);
            if (value is ushort us)
                return new PrimitiveValue(us);
            if (value is int i)
                return new PrimitiveValue(i);
            if (value is uint ui)
                return new PrimitiveValue(ui);
            if (value is long l)
                return new PrimitiveValue(l);
            if (value is ulong ul)
                return new PrimitiveValue(ul);
            if (value is float f)
                return new PrimitiveValue(f);
            if (value is double d)
                return new PrimitiveValue(d);

            // Enum.
            if (value is Enum)
            {
                var underlyingType = value.GetType().GetEnumUnderlyingType();
                var underlyingTypeCode = Type.GetTypeCode(underlyingType);
                switch (underlyingTypeCode)
                {
                    case TypeCode.Byte: return new PrimitiveValue((byte)value);
                    case TypeCode.SByte: return new PrimitiveValue((sbyte)value);
                    case TypeCode.Int16: return new PrimitiveValue((short)value);
                    case TypeCode.Int32: return new PrimitiveValue((int)value);
                    case TypeCode.Int64: return new PrimitiveValue((long)value);
                    case TypeCode.UInt16: return new PrimitiveValue((ushort)value);
                    case TypeCode.UInt32: return new PrimitiveValue((uint)value);
                    case TypeCode.UInt64: return new PrimitiveValue((ulong)value);
                }
            }

            throw new ArgumentException($"Cannot convert '{value}' to primitive value", nameof(value));
        }

        public static implicit operator PrimitiveValue(bool value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(char value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(byte value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(sbyte value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(short value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(ushort value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(int value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(uint value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(long value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(ulong value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(float value)
        {
            return new PrimitiveValue(value);
        }

        public static implicit operator PrimitiveValue(double value)
        {
            return new PrimitiveValue(value);
        }
    }
}
