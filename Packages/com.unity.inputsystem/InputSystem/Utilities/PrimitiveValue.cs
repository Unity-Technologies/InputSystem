using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Input.Utilities
{
    public enum PrimitiveValueType
    {
        None,
        Bool,
        Char,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
    }

    /// <summary>
    /// A union holding a primitive value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PrimitiveValue
    {
        [FieldOffset(0)] public PrimitiveValueType valueType;
        [FieldOffset(4)] public bool boolValue;
        [FieldOffset(4)] public char charValue;
        [FieldOffset(4)] public byte byteValue;
        [FieldOffset(4)] public sbyte sbyteValue;
        [FieldOffset(4)] public short shortValue;
        [FieldOffset(4)] public ushort ushortValue;
        [FieldOffset(4)] public int intValue;
        [FieldOffset(4)] public uint uintValue;
        [FieldOffset(4)] public long longValue;
        [FieldOffset(4)] public ulong ulongValue;
        [FieldOffset(4)] public float floatValue;
        [FieldOffset(4)] public double doubleValue;

        /// <summary>
        /// If true, the struct contains a primitive value.
        /// </summary>
        public bool isEmpty
        {
            get { return valueType == PrimitiveValueType.None; }
        }

        public PrimitiveValue(bool value)
            : this()
        {
            valueType = PrimitiveValueType.Bool;
            boolValue = value;
        }

        public PrimitiveValue(char value)
            : this()
        {
            valueType = PrimitiveValueType.Char;
            charValue = value;
        }

        public PrimitiveValue(byte value)
            : this()
        {
            valueType = PrimitiveValueType.Byte;
            byteValue = value;
        }

        public PrimitiveValue(sbyte value)
            : this()
        {
            valueType = PrimitiveValueType.SByte;
            sbyteValue = value;
        }

        public PrimitiveValue(short value)
            : this()
        {
            valueType = PrimitiveValueType.Short;
            shortValue = value;
        }

        public PrimitiveValue(ushort value)
            : this()
        {
            valueType = PrimitiveValueType.UShort;
            ushortValue = value;
        }

        public PrimitiveValue(int value)
            : this()
        {
            valueType = PrimitiveValueType.Int;
            intValue = value;
        }

        public PrimitiveValue(uint value)
            : this()
        {
            valueType = PrimitiveValueType.UInt;
            uintValue = value;
        }

        public PrimitiveValue(long value)
            : this()
        {
            valueType = PrimitiveValueType.Long;
            longValue = value;
        }

        public PrimitiveValue(ulong value)
            : this()
        {
            valueType = PrimitiveValueType.ULong;
            ulongValue = value;
        }

        public PrimitiveValue(float value)
            : this()
        {
            valueType = PrimitiveValueType.Float;
            floatValue = value;
        }

        public PrimitiveValue(double value)
            : this()
        {
            valueType = PrimitiveValueType.Double;
            doubleValue = value;
        }

        public override string ToString()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    return boolValue.ToString();
                case PrimitiveValueType.Char:
                    return charValue.ToString();
                case PrimitiveValueType.Byte:
                    return byteValue.ToString();
                case PrimitiveValueType.SByte:
                    return sbyteValue.ToString();
                case PrimitiveValueType.Short:
                    return shortValue.ToString();
                case PrimitiveValueType.UShort:
                    return ushortValue.ToString();
                case PrimitiveValueType.Int:
                    return intValue.ToString();
                case PrimitiveValueType.UInt:
                    return uintValue.ToString();
                case PrimitiveValueType.Long:
                    return longValue.ToString();
                case PrimitiveValueType.ULong:
                    return ulongValue.ToString();
                case PrimitiveValueType.Float:
                    return floatValue.ToString();
                case PrimitiveValueType.Double:
                    return doubleValue.ToString();
                default:
                    return string.Empty;
            }
        }

        public bool ToBool()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    return boolValue;
                case PrimitiveValueType.Char:
                    return charValue != '\0';
                case PrimitiveValueType.Byte:
                    return byteValue != 0;
                case PrimitiveValueType.SByte:
                    return sbyteValue != 0;
                case PrimitiveValueType.Short:
                    return shortValue != 0;
                case PrimitiveValueType.UShort:
                    return ushortValue != 0;
                case PrimitiveValueType.Int:
                    return intValue != 0;
                case PrimitiveValueType.UInt:
                    return uintValue != 0;
                case PrimitiveValueType.Long:
                    return longValue != 0;
                case PrimitiveValueType.ULong:
                    return ulongValue != 0;
                case PrimitiveValueType.Float:
                    return !Mathf.Approximately(floatValue, 0);
                case PrimitiveValueType.Double:
                    return NumberHelpers.Approximately(doubleValue, 0);
                default:
                    return default(bool);
            }
        }

        public int ToInt()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    if (boolValue)
                        return 1;
                    return 0;
                case PrimitiveValueType.Char:
                    return charValue;
                case PrimitiveValueType.Byte:
                    return byteValue;
                case PrimitiveValueType.SByte:
                    return sbyteValue;
                case PrimitiveValueType.Short:
                    return shortValue;
                case PrimitiveValueType.UShort:
                    return ushortValue;
                case PrimitiveValueType.Int:
                    return intValue;
                case PrimitiveValueType.UInt:
                    return (int)uintValue;
                case PrimitiveValueType.Long:
                    return (int)longValue;
                case PrimitiveValueType.ULong:
                    return (int)ulongValue;
                case PrimitiveValueType.Float:
                    return (int)floatValue;
                case PrimitiveValueType.Double:
                    return (int)doubleValue;
                default:
                    return default(int);
            }
        }

        public long ToLong()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    if (boolValue)
                        return 1;
                    return 0;
                case PrimitiveValueType.Char:
                    return charValue;
                case PrimitiveValueType.Byte:
                    return byteValue;
                case PrimitiveValueType.SByte:
                    return sbyteValue;
                case PrimitiveValueType.Short:
                    return shortValue;
                case PrimitiveValueType.UShort:
                    return ushortValue;
                case PrimitiveValueType.Int:
                    return intValue;
                case PrimitiveValueType.UInt:
                    return uintValue;
                case PrimitiveValueType.Long:
                    return longValue;
                case PrimitiveValueType.ULong:
                    return (long)ulongValue;
                case PrimitiveValueType.Float:
                    return (long)floatValue;
                case PrimitiveValueType.Double:
                    return (long)doubleValue;
                default:
                    return default(long);
            }
        }

        public float ToFloat()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    if (boolValue)
                        return 1;
                    return 0;
                case PrimitiveValueType.Char:
                    return charValue;
                case PrimitiveValueType.Byte:
                    return byteValue;
                case PrimitiveValueType.SByte:
                    return sbyteValue;
                case PrimitiveValueType.Short:
                    return shortValue;
                case PrimitiveValueType.UShort:
                    return ushortValue;
                case PrimitiveValueType.Int:
                    return intValue;
                case PrimitiveValueType.UInt:
                    return uintValue;
                case PrimitiveValueType.Long:
                    return longValue;
                case PrimitiveValueType.ULong:
                    return ulongValue;
                case PrimitiveValueType.Float:
                    return floatValue;
                case PrimitiveValueType.Double:
                    return (float)doubleValue;
                default:
                    return default(float);
            }
        }

        public double ToDouble()
        {
            switch (valueType)
            {
                case PrimitiveValueType.Bool:
                    if (boolValue)
                        return 1;
                    return 0;
                case PrimitiveValueType.Char:
                    return charValue;
                case PrimitiveValueType.Byte:
                    return byteValue;
                case PrimitiveValueType.SByte:
                    return sbyteValue;
                case PrimitiveValueType.Short:
                    return shortValue;
                case PrimitiveValueType.UShort:
                    return ushortValue;
                case PrimitiveValueType.Int:
                    return intValue;
                case PrimitiveValueType.UInt:
                    return uintValue;
                case PrimitiveValueType.Long:
                    return longValue;
                case PrimitiveValueType.ULong:
                    return ulongValue;
                case PrimitiveValueType.Float:
                    return floatValue;
                case PrimitiveValueType.Double:
                    return doubleValue;
                default:
                    return default(double);
            }
        }
    }

    public struct PrimitiveValueOrArray
    {
        ////REVIEW: use InlinedArray<PrimitiveValue>?
        public PrimitiveValue primitiveValue;
        public object arrayValue;

        public PrimitiveValueType valueType
        {
            get { return primitiveValue.valueType; }
        }

        public bool isArray
        {
            get { return arrayValue != null; }
        }

        public bool isEmpty
        {
            get { return valueType == PrimitiveValueType.None; }
        }

        public PrimitiveValueOrArray(PrimitiveValue value)
        {
            primitiveValue = value;
            arrayValue = null;
        }

        public PrimitiveValueOrArray(bool value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(char value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(byte value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(sbyte value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(short value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(ushort value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(int value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(uint value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(long value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(ulong value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(float value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public PrimitiveValueOrArray(double value)
        {
            primitiveValue = new PrimitiveValue(value);
            arrayValue = null;
        }

        public override string ToString()
        {
            if (!isArray)
                return primitiveValue.ToString();

            throw new NotImplementedException();
        }

        public static PrimitiveValueOrArray FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new PrimitiveValueOrArray();

            // Bool.
            if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValueOrArray(true);
            if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValueOrArray(false);

            // Double.
            if (value.IndexOf('.') != -1 || value.Contains("e") || value.Contains("E"))
            {
                double doubleResult;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleResult))
                    return new PrimitiveValueOrArray(doubleResult);
            }

            // Long.
            long longResult;
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out longResult))
            {
                return new PrimitiveValueOrArray(longResult);
            }
            // Try hex format. For whatever reason, HexNumber does not allow a 0x prefix so we manually
            // get rid of it.
            if (value.IndexOf("0x", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                var hexDigits = value.TrimStart();
                if (hexDigits.StartsWith("0x"))
                    hexDigits = hexDigits.Substring(2);
                if (long.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out longResult))
                {
                    return new PrimitiveValueOrArray(longResult);
                }
            }

            ////TODO: allow trailing width specifier
            throw new NotImplementedException();
        }

        public static PrimitiveValueOrArray FromObject(object value)
        {
            if (value == null)
                return new PrimitiveValueOrArray();

            var stringValue = value as string;
            if (stringValue != null)
                return FromString(stringValue);

            if (value is bool)
                return new PrimitiveValueOrArray((bool)value);
            if (value is char)
                return new PrimitiveValueOrArray((char)value);
            if (value is byte)
                return new PrimitiveValueOrArray((byte)value);
            if (value is sbyte)
                return new PrimitiveValueOrArray((sbyte)value);
            if (value is short)
                return new PrimitiveValueOrArray((short)value);
            if (value is ushort)
                return new PrimitiveValueOrArray((ushort)value);
            if (value is int)
                return new PrimitiveValueOrArray((int)value);
            if (value is uint)
                return new PrimitiveValueOrArray((uint)value);
            if (value is long)
                return new PrimitiveValueOrArray((long)value);
            if (value is ulong)
                return new PrimitiveValueOrArray((ulong)value);
            if (value is float)
                return new PrimitiveValueOrArray((float)value);
            if (value is double)
                return new PrimitiveValueOrArray((double)value);

            ////TODO: arrays

            throw new ArgumentException(string.Format("Cannot convert '{0}' to primitive value or value array", value), "value");
        }
    }
}
