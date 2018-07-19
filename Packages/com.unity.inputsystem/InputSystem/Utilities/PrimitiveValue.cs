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
        public PrimitiveValue primitiveValue;
        public object arrayValue;

        public PrimitiveValueType valueType
        {
            get { return primitiveValue.valueType; }
        }

        public bool isArray
        {
            get { throw new NotImplementedException(); }
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
            throw new NotImplementedException();
        }

        public TValue GetPrimitiveValue<TValue>()
        {
            if (arrayValue != null)
                throw new NotImplementedException();

            if (typeof(TValue) == typeof(double))
                throw new NotImplementedException();

            throw new NotImplementedException();
        }

        public TValue[] GetArrayValue<TValue>()
        {
            throw new NotImplementedException();
        }

        public static PrimitiveValueOrArray FromString(string value)
        {
            if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValueOrArray(true);
            if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                return new PrimitiveValueOrArray(false);

            if (value.IndexOf('.') != -1 || value.Contains("e") || value.Contains("E"))
            {
                double doubleResult;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleResult))
                    return new PrimitiveValueOrArray(doubleResult);
            }

            long longResult;
            if (long.TryParse(value, NumberStyles.Integer | NumberStyles.HexNumber, CultureInfo.InvariantCulture, out longResult))
            {
                return new PrimitiveValueOrArray(longResult);
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

            throw new NotImplementedException();
        }
    }
}
