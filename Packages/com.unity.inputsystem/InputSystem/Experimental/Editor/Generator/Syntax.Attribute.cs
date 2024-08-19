using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareAttribute
        {
            void AddAttribute(Attribute attribute);
        }
        
        /// <summary>
        /// Represents an attribute annotation.
        /// </summary>
        public sealed class Attribute
        {
            private readonly List<NamedParameter> m_Parameters;
            
            /// <summary>
            /// Constructs a new <c>Attribute</c>.
            /// </summary>
            /// <param name="name">The attribute name.</param>
            public Attribute(string name)
            {
                this.name = name;
                m_Parameters = new List<NamedParameter>();
            }

            /// <summary>
            /// Gets or sets the unique attribute identifier.
            /// </summary>
            public string name { get; set; }
            
            /// <summary>
            /// Returns the parameters associated with this attribute.
            /// </summary>
            public IReadOnlyList<NamedParameter> parameters => m_Parameters;

            /// <summary>
            /// Adds a parameter to the attribute.
            /// </summary>
            /// <param name="parameter">The parameter to be added.</param>
            public void AddParameter(NamedParameter parameter)
            {
                m_Parameters.Add(parameter);
            }
            
            public void Format(SourceFormatter formatter)
            {
                formatter.Write($"[{name}");
                if (m_Parameters.Count != 0)
                {
                    formatter.WriteUnformatted('(');
                    for (var i = 0; i < m_Parameters.Count; ++i)
                    {
                        if (i > 0)
                        {
                            formatter.WriteUnformatted(", ");
                        }
                        formatter.WriteUnformatted(m_Parameters[i].name);
                        formatter.WriteUnformatted(':');
                        formatter.Write(m_Parameters[i].value);
                    }    
                    formatter.WriteUnformatted(')');
                }
                formatter.WriteUnformatted(']');
                formatter.Newline();
            }
            
            public static Attribute StructLayout(LayoutKind layoutKind, int pack = 0, int absoluteSizeBytes = -1)
            {
                var annotation = new Attribute("StructLayout");
                annotation.AddParameter(new NamedParameter("layoutKind", "LayoutKind." + layoutKind.ToString())); // TODO Use type function
                if (pack > 0)
                    annotation.AddParameter(new NamedParameter("pack", pack.ToString()));
                if (absoluteSizeBytes > 0)
                    annotation.AddParameter(new NamedParameter("size", absoluteSizeBytes.ToString()));
                return annotation;
            }
        }
    }
    
    public static class AttributeExtensions
    {
        public static Syntax.Attribute DeclareAttribute<TTarget>(this TTarget target, string name)
            where TTarget : Syntax.IDeclareAttribute
        {
            var attribute = new Syntax.Attribute(name);
            target.AddAttribute(attribute);
            return attribute;
        }
    }
}