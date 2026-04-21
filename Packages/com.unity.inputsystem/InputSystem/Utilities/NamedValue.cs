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
        /// <summary>The separator character used between name and value in a named value string.</summary>
        public const string Separator = ",";

        /// <summary>
        /// Name of the parameter.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Value of the parameter.
        /// </summary>
        public PrimitiveValue value { get; set; }

        /// <summary>The <see cref="TypeCode"/> of the value stored in this named value.</summary>
        public TypeCode type => value.type;

        /// <summary>Returns a copy of this named value with the value converted to the given type.</summary>
        public NamedValue ConvertTo(TypeCode type)
        {
            return new NamedValue
            {
                name = name,
                value = value.ConvertTo(type)
            };
        }

        /// <summary>Creates a <see cref="NamedValue"/> from the given name and typed value.</summary>
        public static NamedValue From<TValue>(string name, TValue value)
            where TValue : struct
        {
            return new NamedValue
            {
                name = name,
                value = PrimitiveValue.From(value)
            };
        }

        /// <summary>Returns a string representation of this named value.</summary>
        public override string ToString()
        {
            return $"{name}={value}";
        }

        /// <summary>Returns true if this named value is equal to the given one.</summary>
        public bool Equals(NamedValue other)
        {
            return string.Equals(name, other.name, StringComparison.InvariantCultureIgnoreCase)
                && value == other.value;
        }

        /// <summary>Returns true if the given object is a <see cref="NamedValue"/> equal to this one.</summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is NamedValue parameterValue && Equals(parameterValue);
        }

        /// <summary>Returns a hash code for this named value.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ value.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>Returns true if both named values are equal.</summary>
        public static bool operator==(NamedValue left, NamedValue right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true if the two named values are not equal.</summary>
        public static bool operator!=(NamedValue left, NamedValue right)
        {
            return !left.Equals(right);
        }

        /// <summary>Parses a comma-separated list of named value strings and returns them as an array.</summary>
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

        /// <summary>Parses a single named value string.</summary>
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

        /// <summary>Applies this named value to the property or field with the matching name on the given object.</summary>
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

        /// <summary>Applies all named values in the given list to the matching properties or fields on the given object.</summary>
        public static void ApplyAllToObject<TParameterList>(object instance, TParameterList parameters)
            where TParameterList : IEnumerable<NamedValue>
        {
            foreach (var parameter in parameters)
                parameter.ApplyToObject(instance);
        }
    }
}
