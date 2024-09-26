using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    /// <summary>
    /// A basic C# source formatter.
    /// </summary>
    /// <remarks>Note that this formatter is not thread-safe.</remarks>
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
        private int m_Indent;
        private readonly int m_TabLength;
        private int m_LineLength;
        private bool m_NeedsParagraph;
        private bool m_NeedsNewLine;
        private bool m_NeedsSpace;
        private readonly bool m_UseTabs;

        private const string kSummary = "summary";
        private const string kDocPrefix = "///";
        
        public SourceFormatter(bool useTabs = false, int tabLength = 4)
        {
            m_Buffer = new StringBuilder();
            m_UseTabs = useTabs;
            m_TabLength = tabLength;
        }

        public void Comment(string content)
        {
            Write("//");
            Write(content);
        }
        
        public void WriteSummary(string content)
        {
            Write(kDocPrefix);
            WriteXmlOpenElement(kSummary);
            Newline();
            
            Write(kDocPrefix);
            Write(content);
            Newline();
            
            Write(kDocPrefix);
            WriteXmlCloseElement(kSummary);
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

        public string Format(Syntax.Visibility visibility)
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

            if (m_NeedsNewLine)
                Newline();
            
            // If there is already text on this line append space between tokens.
            // Otherwise make sure the line is indented.
            if (m_LineLength > 0)
            {
                WriteUnformatted(kSpace);
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
            WriteUnformatted(value);
            m_LineLength += length;
        }

        public void WriteUnformatted(string value)
        {
            if (m_LineLength == 0) 
                Indent();
            m_Buffer.Append(value);
        }
        
        public void WriteUnformatted(char c)
        {
            if (m_LineLength == 0) 
                Indent();
            m_Buffer.Append(c);
        }

        public void WriteLine(string text)
        {
            Write(text);
        }

        public void WriteLine(ReadOnlySpan<char> span)
        {
            Write(span.ToString()); // TODO FIX, inefficient
        }

        public void Write(char c)
        {
            BeginLine(1);
            WriteUnformatted(c);
            ++m_LineLength;
        }

        public void Assign(string name, string value)
        {
            Write(name);
            Write(kAssign);
            Write(value);
        }

        public void EndStatement()
        {
            WriteUnformatted(kSemicolon);
            m_NeedsNewLine = true;
        }

        private void NamedArgument(string argument, string value)
        {
            WriteUnformatted(argument);
            WriteUnformatted(':');
            WriteUnformatted(kSpace);
            WriteUnformatted(value);
        }

        public void EndClass()
        {
            EndScope();
        }

        public void BeginScope()
        {
            if (m_NeedsNewLine || m_LineLength > 0)
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

        private void Indent()
        {
            for (var i = 0; i < m_Indent; ++i)
            {
                if (m_UseTabs)
                {
                    WriteUnformatted(kTab);
                }
                else
                {
                    for (var j = m_TabLength; j != 0; --j)
                        WriteUnformatted(kSpace);
                }    
            }
        }

        public void Newline()
        {
            WriteUnformatted(kNewline);
            m_NeedsNewLine = false;
            m_LineLength = 0;
        }

        public void Paragraph()
        {
            if (!m_NeedsParagraph)
                return;
            
            WriteUnformatted(kNewline);
            WriteUnformatted(kNewline);
            m_NeedsParagraph = false;
            m_NeedsNewLine = false;
            m_LineLength = 0;
        }

        public override string ToString()
        {
            return m_Buffer.ToString();
        }

        public void NeedsNewline()
        {
            m_NeedsNewLine = true;
        }

        public void NeedsParagraph()
        {
            m_NeedsParagraph = true;
        }
    }
}