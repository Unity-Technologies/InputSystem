using System.Text;
using UnityEngine;

namespace UnityEditor.InputSystem.Experimental.Generator
{
    internal sealed class SourceBuilder
    {
        private const char kNewline = '\n';
        
        private readonly StringBuilder m_Builder;
        private int m_Indent;
            
        public SourceBuilder(string path)
        {
            m_Builder = new StringBuilder();
            this.path = path;
        }

        public int length => m_Builder.Length;

        public override string ToString() => m_Builder.ToString();
            
        public string path { get; }

        public void IncreaseIndent() => ++m_Indent;
        public void WriteIndent() => m_Builder.Append('\t', m_Indent);

        public void DecreaseIndent()
        {
            Debug.Assert(m_Indent > 0);
            --m_Indent;
        }

        public void NewLine() => m_Builder.Append(kNewline);

        public void WriteLine(string value)
        {
            WriteIndent();
            m_Builder.Append(value);
            NewLine();
        }

        public void WriteLine(char value)
        {
            WriteIndent();
            m_Builder.Append(value);
            NewLine();
        }

        public void BeginScope()
        {
            WriteLine('{');
            IncreaseIndent();
        }

        public void EndScope()
        {
            DecreaseIndent();
            WriteLine('}');
        }
    }
}