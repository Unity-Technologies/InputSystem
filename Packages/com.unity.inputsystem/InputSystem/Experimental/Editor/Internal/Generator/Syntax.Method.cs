using System.Collections.Generic;
using UnityEditor.InputSystem.Experimental.Generator;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDeclareMethod
        {
            public void AddMethod(MethodDeclaration method);
        }
        
        public interface IDefineMethod
        {
            public void AddMethod(Method method);
        }

        public class MethodDeclaration : Node, IDefineParameter
        {
            private readonly List<Parameter> m_Parameters = new List<Parameter>();
            
            public MethodDeclaration(SourceContext context, string name, Visibility visibility, TypeReference returnType) 
                : base(context)
            {
                this.name = name;
                this.returnType = returnType;
                this.visibility = visibility;
            }

            public string name { get; set; }
            public TypeReference returnType { get; set; }
            public Visibility visibility { get; set; }
            public IReadOnlyList<Parameter> parameters => m_Parameters;
            
            public void AddParameter(Parameter parameter) => m_Parameters.Add(parameter);
        }
        
        public sealed class Method : Node, IDefineStatement, IDefineParameter, IDefineTypeArgument
        {
            private readonly List<Parameter> m_Parameters = new();
            private readonly List<Statement> m_Statements = new();
            private readonly List<TypeArgument> m_TypeArguments = new();
            
            public Method(SourceContext context, string name, Visibility visibility, TypeReference returnType) 
                : base(context)
            {
                this.name = name;
                this.returnType = returnType;
                this.visibility = visibility;
                
                SetChildren(m_Statements);
            }
            
            public string name { get; set; }
            public bool isConstructor { get; set; }
            public bool isExtensionMethod { get; set; }
            public TypeReference returnType { get; set; }
            public Visibility visibility { get; set; }
            public bool isAbstract { get; set; }
            public bool isStatic { get; set; }

            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.Paragraph();
                formatter.Write(formatter.Format(visibility));
                
                if (isStatic)
                    formatter.Write("static");
                if (isAbstract)
                    formatter.Write("abstract");

                if (!isConstructor)
                    formatter.Write(returnType == null ? "void" : returnType.value);

                formatter.Write(name);
                TypeArgument.Format(formatter, m_TypeArguments);
                formatter.WriteUnformatted('(');
                Parameter.Format(formatter, m_Parameters);
                /*for (var i = 0; i < m_Parameters.Count; ++i)
                {
                    if (i > 0)
                        formatter.WriteUnformatted(", ");
                    else if (isExtensionMethod)
                        formatter.WriteUnformatted("this ");
                    m_Parameters[i].Format(context, formatter);
                }*/
                formatter.WriteUnformatted(')');
                
                TypeArgument.FormatConstraints(formatter, m_TypeArguments);
                formatter.BeginScope();
            }
            
            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.EndScope();
            }

            public void AddStatement(Statement statement) => m_Statements.Add(statement);
            public void AddParameter(Parameter parameter) => m_Parameters.Add(parameter);
            public void AddTypeArgument(TypeArgument arg) => m_TypeArguments.Add(arg);

            public IReadOnlyList<Parameter> parameters => m_Parameters;
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.MethodDeclaration DeclareMethod<TTarget>(this TTarget target, string name, 
            Syntax.Visibility visibility = Syntax.Visibility.Default, Syntax.TypeReference returnType = null)
            where TTarget : Syntax.IDeclareMethod, Syntax.INode
        {
            var method = new Syntax.MethodDeclaration(target.context, name, visibility, returnType);
            target.AddMethod(method);
            return method;
        }
        
        public static Syntax.Method DefineMethod<TTarget>(this TTarget target, string name, 
            Syntax.Visibility visibility = Syntax.Visibility.Default, Syntax.TypeReference returnType = null)
            where TTarget : Syntax.IDefineMethod, Syntax.INode
        {
            var method = new Syntax.Method(target.context, name, visibility, returnType);
            target.AddMethod(method);
            return method;
        }
    }
}