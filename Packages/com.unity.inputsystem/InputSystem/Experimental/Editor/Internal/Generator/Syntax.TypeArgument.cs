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

            public static void Format(SourceFormatter formatter, IReadOnlyList<TypeArgument> typeArguments)
            {
                if (typeArguments.Count == 0) return;
                formatter.WriteUnformatted('<');
                formatter.WriteUnformatted(typeArguments[0].name);
                for (var i = 1; i < typeArguments.Count; ++i)
                {
                    formatter.WriteUnformatted(", ");
                    formatter.WriteUnformatted(typeArguments[i].name);
                }
                formatter.WriteUnformatted('>');
            }

            public static void FormatConstraints(SourceFormatter formatter, IReadOnlyList<TypeArgument> typeArguments)
            {
                if (typeArguments.Count <= 0) return;
                for (var i = 0; i < typeArguments.Count; ++i)
                {
                    var typeArgument = typeArguments[i];
                    if (typeArgument.constraints.Count == 0)
                        continue;
                        
                    formatter.Newline();
                    formatter.IncreaseIndent();
                    if (typeArgument.constraints.Count > 0)
                    {
                        formatter.Write("where ");
                        formatter.WriteUnformatted(typeArgument.name);
                        formatter.WriteUnformatted(" : ");
                        for (var j = 0; j < typeArgument.constraints.Count; ++j)
                        {
                            if (j > 0)
                                formatter.WriteUnformatted(", ");
                            formatter.WriteUnformatted(typeArgument.constraints[j]);   
                        }
                    }
                    formatter.DecreaseIndent();
                }
            }

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