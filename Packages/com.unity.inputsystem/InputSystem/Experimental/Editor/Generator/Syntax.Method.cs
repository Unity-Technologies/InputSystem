using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDeclareMethod
        {
            public void AddMethod(Method method);
        }
        
        public class Method : Node, IDefineStatement
        {
            public Method(SourceContext context, string name, Visibility visibility, TypeReference returnType) 
                : base(context)
            {
                this.name = name;
                this.returnType = returnType;
                this.visibility = visibility;
                
                SetChildren(m_Statements);
            }

            private readonly List<Statement> m_Statements = new List<Statement>();
            
            public string name { get; set; }
            public TypeReference returnType { get; set; }
            public Visibility visibility { get; set; }

            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.Write(formatter.Format(visibility));
                formatter.Write(returnType == null ? "void" : context.GetTypeName(returnType.type));

                formatter.Write(name);
                formatter.WriteUnformatted('(');
                formatter.WriteUnformatted(')');
                formatter.BeginScope();
            }
            
            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.EndScope();
            }

            public void AddStatement(Statement statement)
            {
                m_Statements.Add(statement);
            }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Method DeclareMethod<TTarget>(this TTarget target, string name, 
            Syntax.Visibility visibility = Syntax.Visibility.Default, Syntax.TypeReference returnType = null)
            where TTarget : Syntax.IDeclareMethod, Syntax.INode
        {
            var method = new Syntax.Method(target.context, name, visibility, returnType);
            target.AddMethod(method);
            return method;
        }
    }
}