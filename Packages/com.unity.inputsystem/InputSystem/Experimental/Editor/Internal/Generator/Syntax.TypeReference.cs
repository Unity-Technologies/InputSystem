using System;
using System.Text;
using UnityEditor.InputSystem.Experimental.Generator;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    // TODO Needs context to transform types, but why would we when we can have it here?!
    public static partial class Syntax
    {
        public sealed class TypeReference : IEquatable<TypeReference>
        {
            private readonly DeclaredType m_DeclaredType;
            private readonly Type m_Type;
            private readonly TypeArgument m_TypeArgument;
            
            public TypeReference(Type type)
            {
                m_Type = type;
            }

            public TypeReference(DeclaredType type)
            {
                m_DeclaredType = type;
            }

            public TypeReference(TypeArgument typeArgument)
            {
                m_TypeArgument = typeArgument;
            }

            public static TypeReference For<T>()
            {
                return new TypeReference(typeof(T));
            }
            
            public static implicit operator TypeReference(System.Type type) => new TypeReference(type);
            public static implicit operator TypeReference(TypeArgument type) => new TypeReference(type);
            public static implicit operator TypeReference(DeclaredType type) => new TypeReference(type);

            public bool isType => m_Type != null;
            public bool isGeneric => m_TypeArgument != null;
            public Type type => m_Type;
            public DeclaredType declaredType => m_DeclaredType;
            public string declaredNamespace => m_Type != null ? m_Type.Namespace : m_DeclaredType.declaredNamespace;

            public string value
            {
                get
                {
                    if (m_Type != null)
                        return SourceUtils.GetTypeName(m_Type);    
                    if (isGeneric)
                        return m_TypeArgument.name;
                    return declaredType.name;
                }
            }

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