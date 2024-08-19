using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public sealed class UsingStatement : IEquatable<UsingStatement>
        {
            private readonly string m_Namespace;
            
            public UsingStatement(string ns)
            {
                m_Namespace = ns;
            }
            
            //public override void PreFormat(NewSourceFormatter formatter) { }
            public void Format(SourceFormatter formatter)
            {
                formatter.Write("using");
                formatter.Write(m_Namespace);
                formatter.EndStatement();
            }
            //public override void PostFormat(NewSourceFormatter formatter) { }
            public bool Equals(UsingStatement other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return m_Namespace == other.m_Namespace;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is UsingStatement other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (m_Namespace != null ? m_Namespace.GetHashCode() : 0);
            }

            public static bool operator ==(UsingStatement left, UsingStatement right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(UsingStatement left, UsingStatement right)
            {
                return !Equals(left, right);
            }
        }
    }
}