using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    // TODO Consider only using child lists, left(pre), center(inorder), right(post)
    //
    // E.g. for class
    // Attribute, Declaration, Body
    // where Attribute have children
    
    /// <summary>
    /// Represents a source generation context.
    /// </summary>
    public sealed class SourceContext 
    {
        /// <summary>
        /// The root node of a source file.
        /// </summary>
        public class Root : Syntax.Node, Syntax.IDeclareClass, Syntax.IDeclareStruct, Syntax.IDeclareEnum, 
            Syntax.IDeclareUsing, Syntax.IDeclareInterface, Syntax.IDefineSnippet, Syntax.IDeclareNamespace
        {
            private readonly List<Syntax.UsingStatement> m_UsingStatements;
            private readonly List<Syntax.Class> m_Classes;
            private readonly List<Syntax.Struct> m_Structs;
            private readonly List<Syntax.Enum> m_Enums;
            private readonly List<Syntax.DeclaredInterface> m_Interfaces;
            private readonly List<Syntax.Snippet> m_Snippets;
            private readonly List<Syntax.Namespace> m_Namespaces;
            
            internal Root(SourceContext context) : base(context)
            {
                m_UsingStatements = new List<Syntax.UsingStatement>();
                m_Classes = new List<Syntax.Class>();
                m_Enums = new List<Syntax.Enum>();
                m_Structs = new List<Syntax.Struct>();
                m_Interfaces = new List<Syntax.DeclaredInterface>();
                m_Snippets = new List<Syntax.Snippet>();
                m_Namespaces = new List<Syntax.Namespace>();
                
                SetChildren(m_UsingStatements, m_Namespaces, m_Interfaces, m_Enums, m_Classes, m_Structs, m_Snippets);
            }
            
            public string defaultNamespace { get; set; }
            /// <inheritDoc />
            public void AddClass(Syntax.Class @class) => m_Classes.Add(@class);
            /// <inheritDoc />
            public void AddStruct(Syntax.Struct @struct) => m_Structs.Add(@struct);
            /// <inheritDoc />
            public void AddEnum(Syntax.Enum @enum) => m_Enums.Add(@enum);
            /// <inheritDoc />
            public void AddUsing(Syntax.UsingStatement statement)
            {
                if (!m_UsingStatements.Contains(statement))
                    m_UsingStatements.Add(statement);
            }
            /// <inheritDoc />
            public void AddInterface(Syntax.DeclaredInterface @interface) => m_Interfaces.Add(@interface);

            public void AddSnippet(Syntax.Snippet snippet) => m_Snippets.Add(@snippet);

            public void AddNamespace(Syntax.Namespace @namespace)
            {
                if (!m_Namespaces.Contains(@namespace))
                    m_Namespaces.Add(@namespace);
            }
        }

        /// <summary>
        /// Constructs a new <c>SourceContext</c>.
        /// </summary>
        public SourceContext()
        {
            root = new Root(this);
        }

        /// <summary>
        /// Returns the root node of the source file to which all other nodes are attached.
        /// </summary>
        public Root root { get; }

        internal string GetTypeName(Type type)
        {
            // Custom handling of primitive types that doesn't require namespace access
            if (type.IsPrimitive)
            {
                if (type == typeof(bool)) return "bool";
                if (type == typeof(int)) return "int";
                if (type == typeof(uint)) return "uint";
                if (type == typeof(float)) return "float";
                if (type == typeof(double)) return "double";
                if (type == typeof(char)) return "char";
                if (type == typeof(byte)) return "byte";
                if (type == typeof(short)) return "short";
                if (type == typeof(ushort)) return "ushort";
                if (type == typeof(nint)) return "nint";
                if (type == typeof(nuint)) return "nuint";
            }
            
            return type.Name;
        }
    }
    
    public static class SourceContextExtensions
    {
        public static void Format(this SourceContext context, SourceFormatter formatter)
        {
            VisitDepthFirstRecursive(context.root, context, formatter);
        }
        
        public static string ToSource(this SourceContext context, SourceFormatter formatter = null)
        {
            formatter ??= new SourceFormatter();
            Format(context, formatter);
            return formatter.ToString();
        }
        
        private static void VisitDepthFirstRecursive<TNode>(TNode root, SourceContext context, SourceFormatter formatter)
            where TNode : Syntax.INode
        {
            root.PreFormat(context, formatter);
            root.Format(context, formatter);
            foreach (var child in root)
                VisitDepthFirstRecursive(child, context, formatter);
            root.PostFormat(context, formatter);
        }
    }
}