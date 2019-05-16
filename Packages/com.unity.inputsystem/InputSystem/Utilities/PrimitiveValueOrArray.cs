using System;

namespace UnityEngine.InputSystem.Utilities
{
    public struct PrimitiveValueOrArray
    {
        ////REVIEW: use InlinedArray<PrimitiveValue>?
        public PrimitiveValue primitiveValue { get; set; }
        public object arrayValue { get; set; }

        public TypeCode valueType => primitiveValue.type;

        public bool isArray => arrayValue != null;

        public bool isEmpty => valueType == TypeCode.Empty;

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

            ////TODO: array support

            return new PrimitiveValueOrArray
            {
                primitiveValue = PrimitiveValue.FromString(value)
            };
        }

        public static PrimitiveValueOrArray FromObject(object value)
        {
            if (value == null)
                return new PrimitiveValueOrArray();

            if (value is string stringValue)
                return FromString(stringValue);

            if (value is bool b)
                return new PrimitiveValueOrArray(b);
            if (value is char c)
                return new PrimitiveValueOrArray(c);
            if (value is byte bt)
                return new PrimitiveValueOrArray(bt);
            if (value is sbyte sbt)
                return new PrimitiveValueOrArray(sbt);
            if (value is short s)
                return new PrimitiveValueOrArray(s);
            if (value is ushort us)
                return new PrimitiveValueOrArray(us);
            if (value is int i)
                return new PrimitiveValueOrArray(i);
            if (value is uint ui)
                return new PrimitiveValueOrArray(ui);
            if (value is long l)
                return new PrimitiveValueOrArray(l);
            if (value is ulong ul)
                return new PrimitiveValueOrArray(ul);
            if (value is float f)
                return new PrimitiveValueOrArray(f);
            if (value is double d)
                return new PrimitiveValueOrArray(d);

            ////TODO: arrays

            throw new ArgumentException($"Cannot convert '{value}' to primitive value or value array", nameof(value));
        }
    }
}
