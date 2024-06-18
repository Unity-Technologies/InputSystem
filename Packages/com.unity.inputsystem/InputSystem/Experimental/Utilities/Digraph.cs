using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.InputSystem.Experimental
{
    internal sealed class Digraph
    {
        private readonly IDependencyGraphNode m_Root;
        private int m_Indent;
        private int m_TabSpace;
        private int m_FontSize;
        private string m_Font;
        private bool m_PropertyScope;
        private bool m_NeedsSeparator;
        
        public Digraph(IDependencyGraphNode root)
        {
            m_Root = root;
            m_Indent = 0;
            m_TabSpace = 3;
            m_FontSize = 12;
            m_Font = "Source Code Pro";
            m_PropertyScope = false;
            m_NeedsSeparator = false;
        }

        public string name { get; set; }
        public string title { get; set; }
        public StringBuilder buffer { get; set; }

        public int tabSpace
        {
            get => m_TabSpace;
            set
            {
                if (value < 0)
                    throw new ArgumentException($"{nameof(tabSpace)} must be zero or positive");
                m_TabSpace = value;
            }
        }

        public int fontSize
        {
            get => m_FontSize;
            set
            {
                if (value < 0)
                    throw new ArgumentException($"{nameof(fontSize)} must be zero or positive");
                m_FontSize = value;
            }
        }

        public string font
        {
            get => m_Font;
            set
            {
                if (value == null)
                    throw new ArgumentNullException($"{nameof(font)} may not be null.");
                m_Font = value;
            }
        }

        private void NewLine()
        {
            Write('\n');
            m_NeedsSeparator = false;
        }

        private void Space()
        {
            if (!m_NeedsSeparator)
                return;
            Write(' ');
            m_NeedsSeparator = false;
        }

        private void Indent()
        {
            for (var i = 0; i < m_Indent; ++i)
            {
                for (var j = 0;  j < tabSpace; ++j)
                    Write(' ');
            }
        }

        private void IncreaseIndent()
        {
            ++m_Indent;
        }

        private void DecreaseIndent()
        {
            --m_Indent;
        }

        private void BeginLine()
        {
            if (m_PropertyScope)
            {
                if (m_NeedsSeparator)
                {
                    Write(' ');
                    m_NeedsSeparator = false;
                }
            }
            else
            {
                if (m_NeedsSeparator)
                {
                    NewLine();
                }
                Indent();    
            }
        }

        private void EndLine()
        {
            m_NeedsSeparator = true;
            /*if (m_PropertyScope)
            {
                //Space();
                m_NeedsSeparator = true;
            }
            else
            {
                NewLine();
            }*/
        }
        
        private void Line(string text)
        {
            BeginLine();
            Write(text);
            EndLine();
        }

        private void BeginScope(string text, string id = null, char c = '{')
        {
            BeginLine();
            Write(text);
            if (!string.IsNullOrEmpty(id))
            {
                Write(' ');
                Write(id);
            }
            Write(' ');
            Write(c);
            EndLine();
            IncreaseIndent();
        }

        private void EndScope(char c = '}')
        {
            DecreaseIndent();
            BeginLine();
            Write(c);
            EndLine();
        }

        private void BeginPropertyScope(string text)
        {
            //BeginScope(text, '[');
            BeginLine();
            Write(text);
            Write(' ');
            Write('[');
            m_PropertyScope = true;
        }
        
        private void EndPropertyScope()
        {
            //EndScope(']');
            m_PropertyScope = false;
            Write(']');
            EndLine();
        }

        private void Write(char c)
        {
            buffer.Append(c);
        }
        
        private void Write(string text)
        {
            buffer.Append(text);
        }

        private void ValueProperty(string identifier, string value)
        {
            BeginLine();
            Write(identifier);
            Write('=');
            Write(value);
            EndLine();
        }
        
        private void StringProperty(string identifier, string value)
        {
            BeginLine();
            Write(identifier);
            Write('=');
            Write('"');
            Write(value);
            Write('"');
            EndLine();
        }

        private void Font(string fontName, string fontSizeStringValue)
        {
            StringProperty("fontname", m_Font);
            ValueProperty("fontsize", fontSizeStringValue);
        }

        private void Edge(string from, string to)
        {
            BeginLine();
            Write(from);
            Write(" -> ");
            Write(to);
            EndLine();
        }
        
        public string Build()
        {
            if (buffer == null)
                buffer ??= new StringBuilder();
            
            var fontSizeString = m_FontSize.ToString();
            
            if (!string.IsNullOrEmpty(name))
                BeginScope("digraph " + name);
            else
                BeginScope("digraph");
            
            if (!string.IsNullOrEmpty(title))
                StringProperty("label", title);
            StringProperty("rankdir", "LR");
            
            BeginPropertyScope("node");
            ValueProperty("shape", "rect");
            EndPropertyScope();
            
            BeginPropertyScope("graph");
            Font(m_Font, fontSizeString);
            EndPropertyScope();
            
            BeginPropertyScope("node");
            Font(m_Font, fontSizeString);
            EndPropertyScope();

            BeginPropertyScope("edge");
            Font(m_Font, fontSizeString);
            EndPropertyScope();

            // Generate nodes with generated DOT identifiers.
            //var nodes = m_Root.ToArray();
            //var nodeNames = new string[nodes.Length];
            var nodeIdentifiers = new Dictionary<IDependencyGraphNode, string>();
            m_Root.VisitDepthFirst((node) =>
            {
                var id = $"node{nodeIdentifiers.Count}";
                nodeIdentifiers.Add(node, id);
                
                BeginPropertyScope(id);
                StringProperty("label", node.displayName);
                EndPropertyScope();
            });
            
            // Generate edges
            m_Root.VisitDepthFirst((node) =>
            {
                var from = nodeIdentifiers[node];
                var n = node.childCount;
                for (var i = 0; i < n; ++i)
                {
                    var to = node.GetChild(i);
                    Edge(from, nodeIdentifiers[to]);
                }
            });
            
            EndScope();
            
            return buffer.ToString();
        }
    }
    
    // TODO Consider making digraph interface separate since not used unless included

    internal static class DigraphExtensionMethods
    {
        public static string ToDot<TSource>(this TSource source, StringBuilder buffer = null)
            where TSource : IDependencyGraphNode
        {
            return new Digraph(source).Build();
        }
    }
}