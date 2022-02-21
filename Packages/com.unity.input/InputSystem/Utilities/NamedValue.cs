using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A combination of a name and a value assignment for it.
    /// </summary>
    public struct NamedValue : IEquatable<NamedValue>
    {
        public const string Separator = ",";

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Value of the parameter.
        /// </summary>
        public PrimitiveValue value { get; set; }

        public TypeCode type => value.type;

        public NamedValue ConvertTo(TypeCode type)
        {
            return new NamedValue
            {
                name = name,
                value = value.ConvertTo(type)
            };
        }

        public static NamedValue From<TValue>(string name, TValue value)
            where TValue : struct
        {
            return new NamedValue
            {
                name = name,
                value = PrimitiveValue.From(value)
            };
        }

        public override string ToString()
        {
            return $"{name}={value}";
        }

        public bool Equals(NamedValue other)
        {
            return string.Equals(name, other.name, StringComparison.InvariantCultureIgnoreCase)
                && value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is NamedValue parameterValue && Equals(parameterValue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ value.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(NamedValue left, NamedValue right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(NamedValue left, NamedValue right)
        {
            return !left.Equals(right);
        }

        public static NamedValue[] ParseMultiple(string parameterString)
        {
            if (parameterString == null)
                throw new ArgumentNullException(nameof(parameterString));

            parameterString = parameterString.Trim();
            if (string.IsNullOrEmpty(parameterString))
                return null;

            var parameterCount = parameterString.CountOccurrences(Separator[0]) + 1;
            var parameters = new NamedValue[parameterCount];

            var index = 0;
            for (var i = 0; i < parameterCount; ++i)
            {
                var parameter = ParseParameter(parameterString, ref index);
                parameters[i] = parameter;
            }

            return parameters;
        }

        public static NamedValue Parse(string str)
        {
            var index = 0;
            return ParseParameter(str, ref index);
        }

        private static NamedValue ParseParameter(string parameterString, ref int index)
        {
            var parameter = new NamedValue();
            var parameterStringLength = parameterString.Length;

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            // Parse name.
            var nameStart = index;
            while (index < parameterStringLength)
            {
                var nextChar = parameterString[index];
                if (nextChar == '=' || nextChar == Separator[0] || char.IsWhiteSpace(nextChar))
                    break;
                ++index;
            }
            parameter.name = parameterString.Substring(nameStart, index - nameStart);

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            if (index == parameterStringLength || parameterString[index] != '=')
            {
                // No value given so take "=true" as implied.
                parameter.value = true;
            }
            else
            {
                ++index; // Skip over '='.

                // Skip whitespace.
                while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                    ++index;

                // Parse value.
                var valueStart = index;
                while (index < parameterStringLength &&
                       !(parameterString[index] == Separator[0] || char.IsWhiteSpace(parameterString[index])))
                    ++index;

                ////TODO: use Substring struct here so that we don't allocate lots of useless strings

                var value = parameterString.Substring(valueStart, index - valueStart);
                parameter.value = PrimitiveValue.FromString(value);
            }

            if (index < parameterStringLength && parameterString[index] == Separator[0])
                ++index;

            return parameter;
        }

        public void ApplyToObject(object instance)
        {
            if (instance == null)
                throw new System.ArgumentNullException(nameof(instance));

            var instanceType = instance.GetType();

            ////REVIEW: what about properties?
            var field = instanceType.GetField(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new ArgumentException(
                    $"Cannot find public field '{name}' in '{instanceType.Name}' (while trying to apply parameter)", nameof(instance));

            ////REVIEW: would be awesome to be able to do this without boxing
            var fieldTypeCode = Type.GetTypeCode(field.FieldType);
            field.SetValue(instance, value.ConvertTo(fieldTypeCode).ToObject());
        }

        public static void ApplyAllToObject<TParameterList>(object instance, TParameterList parameters)
            where TParameterList : IEnumerable<NamedValue>
        {
            foreach (var parameter in parameters)
                parameter.ApplyToObject(instance);
        }
    }
}
