using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public sealed class TypeReference : IEquatable<TypeReference>
        {
            private readonly DeclaredType m_DeclaredType;
            private readonly Type m_Type;
            
            public TypeReference(Type type)
            {
                m_Type = type;
            }

            public TypeReference(DeclaredType type)
            {
                m_DeclaredType = type;
            }

            public static TypeReference For<T>()
            {
                return new TypeReference(typeof(T));
            }

            public bool isType => m_Type != null;
            public Type type => m_Type;
            public DeclaredType declaredType => m_DeclaredType;
            public string declaredNamespace => m_Type != null ? m_Type.Namespace : m_DeclaredType.declaredNamespace;
            public string value => isType ? type.Name : declaredType.name;
            
            public bool Equals(TypeReference other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(m_DeclaredType, other.m_DeclaredType) && m_Type == other.m_Type;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TypeReference)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_DeclaredType, m_Type);
            }

            public static bool operator ==(TypeReference left, TypeReference right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(TypeReference left, TypeReference right)
            {
                return !Equals(left, right);
            }
        }
    }
}