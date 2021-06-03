using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

////REVIEW: add Vector2 and Vector3 as primitive value types?

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A union holding a primitive value.
    /// </summary>
    /// <remarks>
    /// This structure is used for storing things such as default states for controls
    /// (see <see cref="Layouts.InputControlLayout.ControlItem.defaultState"/>). It can
    /// store one value of any primitive, non-reference C# type (bool, char, int, float, etc).
    /// </remarks>
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

        /// <summary>
        /// Type of value stored in the struct. <see cref="TypeCode.Empty"/>
        /// if the struct does not hold a value (i.e. has been default-initialized).
        /// </summary>
        /// <value>Type of value stored in the struct.</value>
        public TypeCode type => m_Type;

        /// <summary>
        /// If true, the struct does not contain a primitive value (i.e. has <see cref="type"/>
        /// <see cref="TypeCode.Empty"/>).
        /// </summary>
        /// <value>Whether the struct is holding a value or not.</value>
        public bool isEmpty => type == TypeCode.Empty;

        /// <summary>
        /// Create a PrimitiveValue holding a bool.
        /// </summary>
        /// <param name="value">A boolean value.</param>
        public PrimitiveValue(bool value)
            : this()
        {
            m_Type = TypeCode.Boolean;
            m_BoolValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a character.
        /// </summary>
        /// <param name="value">A character.</param>
        public PrimitiveValue(char value)
            : this()
        {
            m_Type = TypeCode.Char;
            m_CharValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a byte.
        /// </summary>
        /// <param name="value">A byte value.</param>
        public PrimitiveValue(byte value)
            : this()
        {
            m_Type = TypeCode.Byte;
            m_ByteValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a signed byte.
        /// </summary>
        /// <param name="value">A signed byte value.</param>
        public PrimitiveValue(sbyte value)
            : this()
        {
            m_Type = TypeCode.SByte;
            m_SByteValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a short.
        /// </summary>
        /// <param name="value">A short value.</param>
        public PrimitiveValue(short value)
            : this()
        {
            m_Type = TypeCode.Int16;
            m_ShortValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding an unsigned short.
        /// </summary>
        /// <param name="value">An unsigned short value.</param>
        public PrimitiveValue(ushort value)
            : this()
        {
            m_Type = TypeCode.UInt16;
            m_UShortValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding an int.
        /// </summary>
        /// <param name="value">An int value.</param>
        public PrimitiveValue(int value)
            : this()
        {
            m_Type = TypeCode.Int32;
            m_IntValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding an unsigned int.
        /// </summary>
        /// <param name="value">An unsigned int value.</param>
        public PrimitiveValue(uint value)
            : this()
        {
            m_Type = TypeCode.UInt32;
            m_UIntValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a long.
        /// </summary>
        /// <param name="value">A long value.</param>
        public PrimitiveValue(long value)
            : this()
        {
            m_Type = TypeCode.Int64;
            m_LongValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a ulong.
        /// </summary>
        /// <param name="value">An unsigned long value.</param>
        public PrimitiveValue(ulong value)
            : this()
        {
            m_Type = TypeCode.UInt64;
            m_ULongValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a float.
        /// </summary>
        /// <param name="value">A float value.</param>
        public PrimitiveValue(float value)
            : this()
        {
            m_Type = TypeCode.Single;
            m_FloatValue = value;
        }

        /// <summary>
        /// Create a PrimitiveValue holding a double.
        /// </summary>
        /// <param name="value">A double value.</param>
        public PrimitiveValue(double value)
            : this()
        {
            m_Type = TypeCode.Double;
            m_DoubleValue = value;
        }

        /// <summary>
        /// Convert to another type of value.
        /// </summary>
        /// <param name="type">Type of value to convert to.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="ArgumentException">There is no conversion from the
        /// PrimitiveValue's current <see cref="PrimitiveValue.type"/> to
        /// <paramref name="type"/>.</exception>
        /// <remarks>
        /// This method simply calls the other conversion methods (<see cref="ToBoolean"/>,
        /// <see cref="ToChar"/>, etc) based on the current type of value. <c>ArgumentException</c>
        /// is thrown if there is no conversion from the current to the requested type.
        ///
        /// Every value can be converted to <c>TypeCode.Empty</c>.
        /// </remarks>
        /// <seealso cref="ToBoolean"/>
        /// <seealso cref="ToChar"/>
        /// <seealso cref="ToByte"/>
        /// <seealso cref="ToSByte"/>
        /// <seealso cref="ToInt16"/>
        /// <seealso cref="ToInt32"/>
        /// <seealso cref="ToInt64"/>
        /// <seealso cref="ToUInt16"/>
        /// <seealso cref="ToUInt32"/>
        /// <seealso cref="ToUInt64"/>
        /// <seealso cref="ToSingle"/>
        /// <seealso cref="ToDouble"/>
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
                case TypeCode.UInt64: return ToUInt64();
                case TypeCode.Single: return ToSingle();
                case TypeCode.Double: return ToDouble();
                case TypeCode.Empty: return new PrimitiveValue();
            }

            throw new ArgumentException($"Don't know how to convert PrimitiveValue to '{type}'", nameof(type));
        }

        /// <summary>
        /// Compare this value to <paramref name="other"/>.
        /// </summary>
        /// <param name="other">Another value.</param>
        /// <returns>True if the two values are equal.</returns>
        /// <remarks>
        /// Equality is based on type and contents. The types of both values
        /// must be identical and the memory contents of each value must be
        /// bit-wise identical (i.e. things such as floating-point epsilons
        /// are not taken into account).
        /// </remarks>
        public unsafe bool Equals(PrimitiveValue other)
        {
            if (m_Type != other.m_Type)
                return false;

            var thisValuePtr = UnsafeUtility.AddressOf(ref m_DoubleValue);
            var otherValuePtr = UnsafeUtility.AddressOf(ref other.m_DoubleValue);

            return UnsafeUtility.MemCmp(thisValuePtr, otherValuePtr, sizeof(double)) == 0;
        }

        /// <summary>
        /// Compare this value to the value of <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">Either another PrimitiveValue or a boxed primitive
        /// value such as a byte, bool, etc.</param>
        /// <returns>True if the two values are equal.</returns>
        /// <remarks>
        /// If <paramref name="obj"/> is a boxed primitive value, it is automatically
        /// converted to a PrimitiveValue.
        /// </remarks>
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

        /// <summary>
        /// Compare two PrimitiveValues for equality.
        /// </summary>
        /// <param name="left">First value.</param>
        /// <param name="right">Second value.</param>
        /// <returns>True if the two values are equal.</returns>
        /// <seealso cref="Equals(PrimitiveValue)"/>
        public static bool operator==(PrimitiveValue left, PrimitiveValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two PrimitiveValues for inequality.
        /// </summary>
        /// <param name="left">First value.</param>
        /// <param name="right">Second value.</param>
        /// <returns>True if the two values are not equal.</returns>
        /// <seealso cref="Equals(PrimitiveValue)"/>
        public static bool operator!=(PrimitiveValue left, PrimitiveValue right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compute a hash code for the value.
        /// </summary>
        /// <returns>A hash code.</returns>
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

        /// <summary>
        /// Return a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        /// <remarks>
        /// String versions of PrimitiveValues are always culture invariant. This means that
        /// floating-point values, for example, will <em>not</em> the decimal separator of
        /// the current culture.
        /// </remarks>
        /// <seealso cref="FromString"/>
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

        /// <summary>
        /// Parse the given string into a PrimitiveValue.
        /// </summary>
        /// <param name="value">A string containing a value.</param>
        /// <returns>The PrimitiveValue parsed from the string.</returns>
        /// <remarks>
        /// Integers are parsed as longs. Floating-point numbers are parsed as doubles.
        /// Hexadecimal notation is supported for integers.
        /// </remarks>
        /// <seealso cref="ToString()"/>
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

            // Long.
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
            {
                return new PrimitiveValue(longResult);
            }
            // Try hex format. For whatever reason, HexNumber does not allow a 0x prefix so we manually
            // get rid of it.
            if (value.IndexOf("0x", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                var hexDigits = value.TrimStart();
                if (hexDigits.StartsWith("0x"))
                    hexDigits = hexDigits.Substring(2);

                if (long.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexResult))
                    return new PrimitiveValue(hexResult);
            }

            ////TODO: allow trailing width specifier
            throw new NotImplementedException();
        }

        /// <summary>
        /// Equivalent to <see cref="type"/>.
        /// </summary>
        /// <returns>Type code for value stored in struct.</returns>
        public TypeCode GetTypeCode()
        {
            return type;
        }

        /// <summary>
        /// Convert the value to a boolean.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted boolean value.</returns>
        public bool ToBoolean(IFormatProvider provider = null)
        {
            switch (type)
            {
                case TypeCode.Boolean:
                    return m_BoolValue;
                case TypeCode.Char:
                    return m_CharValue != default;
                case TypeCode.Byte:
                    return m_ByteValue != default;
                case TypeCode.SByte:
                    return m_SByteValue != default;
                case TypeCode.Int16:
                    return m_ShortValue != default;
                case TypeCode.UInt16:
                    return m_UShortValue != default;
                case TypeCode.Int32:
                    return m_IntValue != default;
                case TypeCode.UInt32:
                    return m_UIntValue != default;
                case TypeCode.Int64:
                    return m_LongValue != default;
                case TypeCode.UInt64:
                    return m_ULongValue != default;
                case TypeCode.Single:
                    return !Mathf.Approximately(m_FloatValue, default);
                case TypeCode.Double:
                    return NumberHelpers.Approximately(m_DoubleValue, default);
                default:
                    return default;
            }
        }

        /// <summary>
        /// Convert the value to a byte.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted byte value.</returns>
        public byte ToByte(IFormatProvider provider = null)
        {
            return (byte)ToInt64(provider);
        }

        /// <summary>
        /// Convert the value to a char.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted char value.</returns>
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
                    return default;
            }
        }

        /// <summary>
        /// Not supported. Throws <c>NotSupportedException</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Does not return.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public DateTime ToDateTime(IFormatProvider provider = null)
        {
            throw new NotSupportedException("Converting PrimitiveValue to DateTime");
        }

        /// <summary>
        /// Convert the value to a decimal.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Value converted to decimal format.</returns>
        public decimal ToDecimal(IFormatProvider provider = null)
        {
            return new decimal(ToDouble(provider));
        }

        /// <summary>
        /// Convert the value to a double.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted double value.</returns>
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
                    return default;
            }
        }

        /// <summary>
        /// Convert the value to a <c>short</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>short</c> value.</returns>
        public short ToInt16(IFormatProvider provider = null)
        {
            return (short)ToInt64(provider);
        }

        /// <summary>
        /// Convert the value to an <c>int</c>
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>int</c> value.</returns>
        public int ToInt32(IFormatProvider provider = null)
        {
            return (int)ToInt64(provider);
        }

        /// <summary>
        /// Convert the value to a <c>long</c>
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>long</c> value.</returns>
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
                    return default;
            }
        }

        /// <summary>
        /// Convert the value to a <c>sbyte</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>sbyte</c> value.</returns>
        public sbyte ToSByte(IFormatProvider provider = null)
        {
            return (sbyte)ToInt64(provider);
        }

        /// <summary>
        /// Convert the value to a <c>float</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>float</c> value.</returns>
        public float ToSingle(IFormatProvider provider = null)
        {
            return (float)ToDouble(provider);
        }

        /// <summary>
        /// Convert the value to a <c>string</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>string</c> value.</returns>
        /// <remarks>
        /// Same as calling <see cref="ToString()"/>.
        /// </remarks>
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="conversionType">Ignored.</param>
        /// <param name="provider">Ignored.</param>
        /// <returns>Does not return.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Convert the value to a <c>ushort</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>ushort</c> value.</returns>
        public ushort ToUInt16(IFormatProvider provider = null)
        {
            return (ushort)ToUInt64();
        }

        /// <summary>
        /// Convert the value to a <c>uint</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>uint</c> value.</returns>
        public uint ToUInt32(IFormatProvider provider = null)
        {
            return (uint)ToUInt64();
        }

        /// <summary>
        /// Convert the value to a <c>ulong</c>.
        /// </summary>
        /// <param name="provider">Ignored.</param>
        /// <returns>Converted <c>ulong</c> value.</returns>
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
                    return default;
            }
        }

        /// <summary>
        /// Return a boxed version of the value.
        /// </summary>
        /// <returns>A boxed GC heap object.</returns>
        /// <remarks>
        /// This method always allocates GC heap memory.
        /// </remarks>
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

        /// <summary>
        /// Create a PrimitiveValue from the given "blittable"/struct value.
        /// </summary>
        /// <param name="value">A value.</param>
        /// <typeparam name="TValue">Type of value to convert. Must be either an <c>enum</c>
        /// or one of the C# primitive value types (<c>bool</c>, <c>int</c>, <c>float</c>, etc.).</typeparam>
        /// <returns>The PrimitiveValue converted from <paramref name="value"/>. If it is an
        /// <c>enum</c> type, the PrimitiveValue will hold a value of the enum's underlying
        /// type (i.e. <c>Type.GetEnumUnderlyingType</c>).</returns>
        /// <exception cref="ArgumentException">No conversion exists from the given <typeparamref name="TValue"/>
        /// type.</exception>
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
                case TypeCode.Single: return new PrimitiveValue(Convert.ToSingle(value));
                case TypeCode.Double: return new PrimitiveValue(Convert.ToDouble(value));
            }

            throw new ArgumentException(
                $"Cannot convert value '{value}' of type '{typeof(TValue).Name}' to PrimitiveValue", nameof(value));
        }

        /// <summary>
        /// Create a PrimitiveValue from a boxed value.
        /// </summary>
        /// <param name="value">A value. If <c>null</c>, the result will be <c>default(PrimitiveValue)</c>.
        /// If it is a <c>string</c>, <see cref="FromString"/> is used. Otherwise must be either an <c>enum</c>
        /// or one of the C# primitive value types (<c>bool</c>, <c>int</c>, <c>float</c>, etc.). If it is an
        /// <c>enum</c> type, the PrimitiveValue will hold a value of the enum's underlying
        /// type (i.e. <c>Type.GetEnumUnderlyingType</c>).</param>
        /// <exception cref="ArgumentException">No conversion exists from the type of <paramref name="value"/>.</exception>
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

        /// <summary>
        /// Create a PrimitiveValue holding a bool.
        /// </summary>
        /// <param name="value">A boolean value.</param>
        public static implicit operator PrimitiveValue(bool value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a character.
        /// </summary>
        /// <param name="value">A character.</param>
        public static implicit operator PrimitiveValue(char value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a byte.
        /// </summary>
        /// <param name="value">A byte value.</param>
        public static implicit operator PrimitiveValue(byte value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a signed byte.
        /// </summary>
        /// <param name="value">A signed byte value.</param>
        public static implicit operator PrimitiveValue(sbyte value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a short.
        /// </summary>
        /// <param name="value">A short value.</param>
        public static implicit operator PrimitiveValue(short value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding an unsigned short.
        /// </summary>
        /// <param name="value">An unsigned short value.</param>
        public static implicit operator PrimitiveValue(ushort value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding an int.
        /// </summary>
        /// <param name="value">An int value.</param>
        public static implicit operator PrimitiveValue(int value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding an unsigned int.
        /// </summary>
        /// <param name="value">An unsigned int value.</param>
        public static implicit operator PrimitiveValue(uint value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a long.
        /// </summary>
        /// <param name="value">A long value.</param>
        public static implicit operator PrimitiveValue(long value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a ulong.
        /// </summary>
        /// <param name="value">An unsigned long value.</param>
        public static implicit operator PrimitiveValue(ulong value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a float.
        /// </summary>
        /// <param name="value">A float value.</param>
        public static implicit operator PrimitiveValue(float value)
        {
            return new PrimitiveValue(value);
        }

        /// <summary>
        /// Create a PrimitiveValue holding a double.
        /// </summary>
        /// <param name="value">A double value.</param>
        public static implicit operator PrimitiveValue(double value)
        {
            return new PrimitiveValue(value);
        }

        // The following methods exist only to make the annoying Microsoft code analyzer happy.

        public static PrimitiveValue FromBoolean(bool value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromChar(char value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromByte(byte value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromSByte(sbyte value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromInt16(short value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromUInt16(ushort value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromInt32(int value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromUInt32(uint value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromInt64(long value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromUInt64(ulong value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromSingle(float value)
        {
            return new PrimitiveValue(value);
        }

        public static PrimitiveValue FromDouble(double value)
        {
            return new PrimitiveValue(value);
        }
    }
}
