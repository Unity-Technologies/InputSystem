using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public class SourceContext
    {
        private readonly HashSet<string> m_Namespaces;
        private readonly List<Syntax.DeclaredType> m_DeclaredTypes;
        private readonly List<Syntax.UsingStatement> m_UsingStatements;

        public SourceContext(string defaultNamespace = null)
        {
            m_Namespaces = new HashSet<string>();
            m_UsingStatements = new List<Syntax.UsingStatement>();
            m_DeclaredTypes = new List<Syntax.DeclaredType>();
            this.defaultNamespace = defaultNamespace;
        }
        
        public IReadOnlyList<Syntax.UsingStatement> usingStatements => m_UsingStatements;
        public IReadOnlyList<Syntax.DeclaredType> declaredTypes => m_DeclaredTypes;
        public string defaultNamespace { get; set; }

        public void AddUsing(Syntax.UsingStatement statement)
        {
            if (!m_UsingStatements.Contains(statement))
                m_UsingStatements.Add(statement);
        }

        public void AddUsing(string usingNamespace)
        {
            if (!m_Namespaces.Add(usingNamespace))
                return;
            m_UsingStatements.Add(new Syntax.UsingStatement(usingNamespace));
        }
        
        public void Add(Syntax.DeclaredType type)
        {
            if (type.context != this)
                throw new ArgumentException();
            
            if (m_DeclaredTypes.Contains(type))
                throw new Exception();
            m_DeclaredTypes.Add(type);
        }
        
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
}