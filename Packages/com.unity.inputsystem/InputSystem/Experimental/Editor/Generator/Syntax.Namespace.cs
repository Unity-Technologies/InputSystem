using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDeclareNamespace
        {
            public void AddNamespace(Namespace @namespace);
        }
        
        public class Namespace : Node, IDeclareClass, IDeclareStruct, IDeclareEnum, 
            IDeclareInterface, IDefineSnippet
        {
            private readonly List<UsingStatement> m_UsingStatements;
            private readonly List<DeclaredType> m_Types;
            //private readonly List<Struct> m_Structs;
            private readonly List<Enum> m_Enums;
            private readonly List<DeclaredInterface> m_Interfaces;
            private readonly List<Snippet> m_Snippets;
            
            internal Namespace(SourceContext context, string name = null) : base(context)
            {
                this.name = name;
                
                m_Types = new List<DeclaredType>();
                m_Enums = new List<Enum>();
                m_Interfaces = new List<DeclaredInterface>();
                m_Snippets = new List<Snippet>();
            
                SetChildren(m_Interfaces, m_Enums, m_Types, m_Snippets);
            }
            
            public string name { get; set; }
            
            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                if (name == null)
                    return;
                
                formatter.Write("namespace");
                formatter.Write(name);
                formatter.BeginScope();
            }

            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                if (name == null)
                    return;
                
                formatter.EndScope();
            }

            /// <inheritDoc />
            public void AddClass(Class @class) => m_Types.Add(@class);
            /// <inheritDoc />
            public void AddStruct(Struct @struct) => m_Types.Add(@struct);
            /// <inheritDoc />
            public void AddEnum(Enum @enum) => m_Enums.Add(@enum);
            /// <inheritDoc />
            public void AddUsing(UsingStatement statement)
            {
                if (!m_UsingStatements.Contains(statement))
                    m_UsingStatements.Add(statement);
            }
            /// <inheritDoc />
            public void AddInterface(DeclaredInterface @interface) => m_Interfaces.Add(@interface);

            public void AddSnippet(Snippet snippet) => m_Snippets.Add(snippet);
        }
    }

    public static class NamespaceExtensions
    {
        public static Syntax.Namespace Namespace<TTarget>(this TTarget target, string name)
            where TTarget : Syntax.IDeclareNamespace, Syntax.INode
        {
            var method = new Syntax.Namespace(target.context, name);
            target.AddNamespace(method);
            return method;
        }
    }
}