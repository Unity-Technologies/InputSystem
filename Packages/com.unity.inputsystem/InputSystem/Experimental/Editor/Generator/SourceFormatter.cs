using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public interface ISourceFormatter
    {
        
    }
    
    /// <summary>
    /// A basic C# source formatter.
    /// </summary>
    public sealed class SourceFormatter
    {
        private const string kPublic = "public";
        private const string kInternal = "internal";
        private const string kPrivate = "private";
        
        private const char kNewline = '\n';
        private const char kOpenStatement = '{';
        private const char kCloseStatement = '}';
        private const char kTab = '\t';
        private const char kSpace = ' ';
        private const char kSemicolon = ';';
        private const char kAssign = '=';
        
        private readonly StringBuilder m_Buffer;
        private readonly List<string> m_Namespaces;
        private int m_Indent;
        private readonly int m_TabLength;
        private int m_LineLength;
        private bool m_NeedsNewLine;
        private bool m_NeedsSpace;
        private readonly bool m_UseTabs;

        private const string kSummary = "summary";
        
        public SourceFormatter(bool useTabs = false, int tabLength = 4)
        {
            m_Buffer = new StringBuilder();
            m_Namespaces = new List<string>();
            m_UseTabs = useTabs;
            m_TabLength = tabLength;
        }
        
        public string header { get; set; }

        public void Comment(string content)
        {
            Write("//");
            Write(content);
        }
        
        public void WriteSummary(string content)
        {
            WriteDocPrefix();
            WriteXmlOpenElement(kSummary);
            Newline();
            
            WriteDocPrefix();
            Write(content);
            Newline();
            
            WriteDocPrefix();
            WriteXmlCloseElement(kSummary);
        }

        private void WriteDocPrefix()
        {
            Write("///");
        }
        
        private void WriteXmlOpenElement(string name)
        {
            Write('<');
            WriteUnformatted(name);
            WriteUnformatted('>');
        }
        
        private void WriteXmlCloseElement(string name)
        {
            Write('<');
            WriteUnformatted('/');
            WriteUnformatted(name);
            WriteUnformatted('>');
        }

        public string ToString(Syntax.Visibility visibility)
        {
            switch (visibility)
            {
                case Syntax.Visibility.Public:
                    return kPublic;
                case Syntax.Visibility.Internal:
                    return kInternal;
                case Syntax.Visibility.Private:
                    return kPrivate;
                case Syntax.Visibility.Default:
                    return string.Empty;
                default: 
                    throw new ArgumentException($"{nameof(visibility)}");
            }
        }

        private void BeginLine(int length)
        {
            // If there is no previous text return immediately
            if (length == 0)
                return;
            
            // If we need to move to the next line to begin a new line
            if (m_NeedsNewLine)
                Newline();
            
            // If there is already text on this line append space between tokens.
            // Otherwise make sure the line is indented.
            if (m_LineLength > 0)
            {
                m_Buffer.Append(kSpace);
                ++m_LineLength;
            }
            else
            {
                Indent();
            }   
        }
        
        public void Write([NotNull] string value)
        {
            var length = value.Length;
            BeginLine(length);
            m_Buffer.Append(value);
            m_LineLength += length;
        }

        public void WriteUnformatted(string value)
        {
            m_Buffer.Append(value);
        }
        
        public void WriteUnformatted(char c)
        {
            m_Buffer.Append(c);
        }

        public void Write(char c)
        {
            BeginLine(1);
            m_Buffer.Append(c);
            ++m_LineLength;
        }

        public void WriteType(string type)
        {
            var index = type.LastIndexOf('.');
            if (index < 0)
            {
                Write(type);
            }
            else
            {
                var offset = index + 1;
                Write(type.Substring(offset, type.Length - offset));
                var ns = type.Substring(0, index);
                if (!m_Namespaces.Contains(ns))
                    m_Namespaces.Add(ns);
            }
        }

        public void Assign(string name, string value)
        {
            Write(name);
            Write(kAssign);
            Write(value);
        }
        
        public void DeclareField(string type, string identifier, Syntax.Visibility visibility = Syntax.Visibility.Internal, string value = null, int fieldOffset = -1)
        {
            if (fieldOffset >= 0)
                Write($"[FieldOffset({fieldOffset})]");
            Write(ToString(visibility));
            WriteType(type);
            Write(identifier);
            if (value != null)
            {
                Write(kAssign);
                Write(value);
            }
            EndStatement();
        }

        public void EndStatement()
        {
            WriteUnformatted(kSemicolon);
            m_NeedsNewLine = true;
        }

        private void BeginAttribute(string name)
        {
            Write($"[StructLayout(");
        }

        private void EndAttribute()
        {
            WriteUnformatted(')');
            WriteUnformatted(']');
        }

        private void NamedArgument(string argument, string value)
        {
            WriteUnformatted(argument);
            WriteUnformatted(':');
            WriteUnformatted(kSpace);
            WriteUnformatted(value);
        }

        public void Using([NotNull] string ns)
        {
            Write("using");
            WriteUnformatted(kSpace);
            WriteUnformatted(ns);
        }
        
        public SourceFormatter BeginStruct(string identifier, Syntax.Visibility visibility = Syntax.Visibility.Internal, StructLayoutAttribute layout = null)
        {
            if (layout != null)
            {
                // TODO Add Using namespace to file using System.Runtime.InteropServices;
                var value = layout.Value;
                var type = layout.Value.GetType();
                var ns = type.Namespace;
                var fullName = type.FullName;
                var v = fullName.Substring(ns.Length + 1, fullName.Length - ns.Length - 1);
                
                BeginAttribute("StructLayout");
                NamedArgument("layoutKind", $"{layout.Value.GetType().Name}.{layout.Value.ToString()}");
                //NamedArgument("layoutKind", v + "." + value);
                //Write($"[StructLayout({layout.Value.GetType()}.{layout.Value.ToString()})]");
                EndAttribute();
                Newline();
            }
            
            Write(ToString(visibility));
            Write("struct");
            Write(identifier);
            Newline();
            BeginScope();

            return this;
        }

        public SourceFormatter EndStruct()
        {
            EndScope();
            return this;
        }
        
        public void BeginClass(string identifier, Syntax.Visibility visibility = Syntax.Visibility.Internal)
        {
            Write(ToString(visibility));
            Write("class");
            m_Buffer.Append(identifier);
            Newline();
            BeginScope();
        }

        public void EndClass()
        {
            EndScope();
        }

        public void BeginScope()
        {
            if (m_NeedsNewLine)
                Newline();
            Write(kOpenStatement);
            Newline();
            IncreaseIndent();
        }

        public void EndScope()
        {
            DecreaseIndent();
            Write(kCloseStatement);
            m_NeedsNewLine = true;
        }
        
        public void IncreaseIndent()
        {
            ++m_Indent;
        }

        public void DecreaseIndent()
        {
            if (m_Indent > 0) --m_Indent;
        }

        public void Indent()
        {
            for (var i = 0; i < m_Indent; ++i)
            {
                if (m_UseTabs)
                {
                    m_Buffer.Append(kTab);
                }
                else
                {
                    for (var j = m_TabLength; j != 0; --j)
                        m_Buffer.Append(kSpace);    
                }    
            }
        }

        public void Newline()
        {
            m_Buffer.Append(kNewline);
            m_NeedsNewLine = false;
            m_LineLength = 0;
        }

        public override string ToString()
        {
            var temp = new StringBuilder();
            if (!string.IsNullOrEmpty(header))
                temp.Append(header);
            foreach (var ns in m_Namespaces)
                temp.Append(ns);
            if (temp.Length > 0)
            {
                if (m_Buffer.Length == 0)
                    return temp.ToString(); 
                temp.Append(kNewline).Append(kNewline);
                m_Buffer.Insert(0, temp);
            }
            
            return m_Buffer.ToString();
        }

        public void NeedsNewline()
        {
            m_NeedsNewLine = true;
        }
    }
}