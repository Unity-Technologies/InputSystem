using System;
using System.Reflection;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static class TypeHelpers
    {
        public static Type GetValueType(MemberInfo member)
        {
            var field = member as FieldInfo;
            if (field != null)
                return field.FieldType;

            var property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType;

            var method = member as MethodInfo;
            if (method != null)
                return method.ReturnType;

            return null;
        }

        public static string GetNiceTypeName(Type type)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(int))
                    return "int";
                if (type == typeof(float))
                    return "float";
                if (type == typeof(char))
                    return "char";
                if (type == typeof(byte))
                    return "byte";
                if (type == typeof(short))
                    return "short";
                if (type == typeof(long))
                    return "long";
                if (type == typeof(double))
                    return "double";
                if (type == typeof(uint))
                    return "uint";
                if (type == typeof(sbyte))
                    return "sbyte";
                if (type == typeof(ushort))
                    return "ushort";
                if (type == typeof(ulong))
                    return "ulong";
            }

            return type.Name;
        }
    }
}
