using System.Text;
using UnityEngine;

namespace UnityEditor.InputSystem.Experimental.Generator
{
    internal class BasicSourceFormatter
    {
        private const char kNewline = '\n';
        
        private readonly StringBuilder m_Builder;
        private int m_Indent;
        private string m_Value;
            
        protected BasicSourceFormatter(string path)
        {
            m_Builder = new StringBuilder();
            this.path = path;
        }

        protected void Complete()
        {
            m_Value = m_Builder.ToString();
            m_Builder.Clear();
        }

        public int length => m_Builder.Length;

        public override string ToString() => m_Value;
            
        public string path { get; }
            
        protected void IncreaseIndent() => ++m_Indent;
        protected void WriteIndent() => m_Builder.Append('\t', m_Indent);
        protected void DecreaseIndent()
        {
            Debug.Assert(m_Indent > 0);
            --m_Indent;
        }
            
        protected void NewLine() => m_Builder.Append(kNewline);
            
        protected void WriteLine(string value)
        {
            WriteIndent();
            m_Builder.Append(value);
            NewLine();
        }
            
        protected void WriteLine(char value)
        {
            WriteIndent();
            m_Builder.Append(value);
            NewLine();
        }
    }
}