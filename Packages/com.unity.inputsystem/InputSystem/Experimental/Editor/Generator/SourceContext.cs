using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    /// <summary>
    /// Represents a source generation context.
    /// </summary>
    public sealed class SourceContext 
    {
        /// <summary>
        /// The root node of a source file.
        /// </summary>
        public class Root : Syntax.Node, Syntax.IDeclareClass, Syntax.IDeclareStruct, Syntax.IDeclareEnum, 
            Syntax.IDeclareUsing
        {
            private readonly HashSet<string> m_Namespaces;
            private readonly List<Syntax.UsingStatement> m_UsingStatements;
            private readonly List<Syntax.Class> m_Classes;
            private readonly List<Syntax.Struct> m_Structs;
            private readonly List<Syntax.Enum> m_Enums;
            
            internal Root(SourceContext context, string defaultNamespace = null) : base(context)
            {
                m_Namespaces = new HashSet<string>();
                m_UsingStatements = new List<Syntax.UsingStatement>();
                m_Classes = new List<Syntax.Class>();
                m_Enums = new List<Syntax.Enum>();
                m_Structs = new List<Syntax.Struct>();
                this.defaultNamespace = defaultNamespace;
            
                SetChildren(m_UsingStatements, m_Enums, m_Classes, m_Structs);
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
        }

        /// <summary>
        /// Constructs a new <c>SourceContext</c>.
        /// </summary>
        /// <param name="defaultNamespace">The default namespace to use if nodes are not added to an explicit namespace.</param>
        public SourceContext(string defaultNamespace = null)
        {
            root = new Root(this, defaultNamespace);
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
            foreach (var declaredType in context.root)
                VisitDepthFirstRecursive(declaredType, 
                    (node) => node.PreFormat(context, formatter), 
                    (node) => node.Format(context, formatter), 
                    (node) => node.PostFormat(context, formatter));
        }
        
        public static string ToSource(this SourceContext context, SourceFormatter formatter = null)
        {
            formatter ??= new SourceFormatter();
            Format(context, formatter);
            return formatter.ToString();
        }
        
        private static void VisitDepthFirstRecursive<TNode>(TNode root, Action<Syntax.INode> preVisitor, 
            Action<Syntax.INode> visitor, Action<Syntax.INode> postVisitor)
            where TNode : Syntax.INode
        {
            preVisitor(root); // TODO Combine pre and visitor?
            visitor(root);
            foreach (var child in root)
                VisitDepthFirstRecursive(child, preVisitor, visitor, postVisitor);
            postVisitor(root);
        }
    }
}