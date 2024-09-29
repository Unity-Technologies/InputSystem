using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareUsing
        {
            public void AddUsing(UsingStatement statement);
        }
        
        public sealed class UsingStatement : Node, IEquatable<UsingStatement>
        {
            private readonly string m_Namespace;
            
            public UsingStatement(SourceContext context, string ns) 
                : base(context)
            {
                m_Namespace = ns;
            }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                formatter.Write("using");
                formatter.Write(m_Namespace);
                formatter.EndStatement();
                formatter.NeedsParagraph();
            }

            //public override void PreFormat(NewSourceFormatter formatter) { }
            /*public void Format(SourceFormatter formatter)
            {
                
            }*/
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
    
    public static class UsingExtensions
    {
        public static void Using<TTarget>(this TTarget target, string @namespace)
            where TTarget : Syntax.IDeclareUsing, Syntax.INode
        {
            target.AddUsing(new Syntax.UsingStatement(target.context, @namespace));
        }
    }
}