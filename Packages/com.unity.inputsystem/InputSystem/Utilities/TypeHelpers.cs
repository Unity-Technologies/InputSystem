using System;
using System.Reflection;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class TypeHelpers
    {
        public static TObject As<TObject>(this object obj)
        {
            // This avoid NREs for value types. For example, trying to do "(Vector3)null" will
            // result in an NRE. Doing "null.As<Vector3>()" will result in "default(Vector3)".
            if (obj == null)
                return default;
            return (TObject)obj;
        }

        public static bool IsInt(this TypeCode type)
        {
            switch (type)
            {
                case TypeCode.Byte: return true;
                case TypeCode.SByte: return true;
                case TypeCode.Int16: return true;
                case TypeCode.Int32: return true;
                case TypeCode.Int64: return true;
                case TypeCode.UInt16: return true;
                case TypeCode.UInt32: return true;
                case TypeCode.UInt64: return true;
            }
            return false;
        }

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

        public static string GetNiceTypeName(this Type type)
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

        public static Type GetGenericTypeArgumentFromHierarchy(Type type, Type genericTypeDefinition, int argumentIndex)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition));
            if (argumentIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(argumentIndex));

            if (genericTypeDefinition.IsInterface)
            {
                // Walk up the chain until we find the generic type def as an interface on a type.
                while (true)
                {
                    var interfaces = type.GetInterfaces();
                    var haveFoundInterface = false;
                    foreach (var element in interfaces)
                    {
                        if (element.IsConstructedGenericType &&
                            element.GetGenericTypeDefinition() == genericTypeDefinition)
                        {
                            type = element;
                            haveFoundInterface = true;
                            break;
                        }

                        // Recurse into interface in case we're looking for a base interface.
                        var typeArgument =
                            GetGenericTypeArgumentFromHierarchy(element, genericTypeDefinition, argumentIndex);
                        if (typeArgument != null)
                            return typeArgument;
                    }

                    if (haveFoundInterface)
                        break;

                    type = type.BaseType;
                    if (type == null || type == typeof(object))
                        return null;
                }
            }
            else
            {
                // Walk up the chain until we find the generic type def.
                while (!type.IsConstructedGenericType || type.GetGenericTypeDefinition() != genericTypeDefinition)
                {
                    type = type.BaseType;
                    if (type == typeof(object))
                        return null;
                }
            }

            return type.GenericTypeArguments[argumentIndex];
        }
    }
}
