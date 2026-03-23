#if UNITY_EDITOR
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ExpressionUtils
    {
        public static PropertyInfo GetProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
        {
            var member = GetMemberExpression(expression).Member;
            var property = member as PropertyInfo;
            if (property == null)
                throw new InvalidOperationException($"Member with Name '{member.Name}' is not a property.");

            return property;
        }

        private static MemberExpression GetMemberExpression<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
        {
            MemberExpression memberExpression = null;
            switch (expression.Body.NodeType)
            {
                case ExpressionType.Convert:
                {
                    var body = (UnaryExpression)expression.Body;
                    memberExpression = body.Operand as MemberExpression;
                    break;
                }
                case ExpressionType.MemberAccess:
                    memberExpression = expression.Body as MemberExpression;
                    break;
            }

            if (memberExpression == null)
                throw new ArgumentException("Not a member access", nameof(expression));

            return memberExpression;
        }

        public static Func<TEntity, TProperty> CreateGetter<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
        {
            var propertyInfo = GetProperty(property);

            var instance = Expression.Parameter(typeof(TEntity), "instance");

            var body = Expression.Call(instance, propertyInfo.GetGetMethod());
            var parameters = new[] { instance };

            return Expression.Lambda<Func<TEntity, TProperty>>(body, parameters).Compile();
        }
    }
}

#endif
