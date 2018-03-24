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
    }
}
