using System.Collections.Generic;
using UnityEditor.InputSystem.Experimental.Generator;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineTypeArgument
        {
            public void AddTypeArgument(TypeArgument arg);
        }
        
        public class TypeArgument : Node
        {
            private readonly List<string> m_Constraints;
            
            public TypeArgument(SourceContext context, string name) 
                : base(context)
            {
                this.name = name;
                this.m_Constraints = new List<string>();
            }
            
            public string name { get; set; }

            public void AddConstraint(string constraint)
            {
                m_Constraints.Add(constraint);
            }

            public void AddStructConstraint()
            {
                AddConstraint("struct");
            }

            public void AddConstraint(System.Type type)
            {
                AddConstraint(SourceUtils.GetTypeName(type));
            }

            public IReadOnlyList<string> constraints => m_Constraints;
        }
    }

    public static partial class SyntaxExtensions
    {
        public static Syntax.TypeArgument TypeArgument<TTarget>(this TTarget target, string value)
            where TTarget : Syntax.INode, Syntax.IDefineTypeArgument
        {
            var arg = new Syntax.TypeArgument(target.context, value);
            target.AddTypeArgument(arg);
            return arg;
        }
    }
}