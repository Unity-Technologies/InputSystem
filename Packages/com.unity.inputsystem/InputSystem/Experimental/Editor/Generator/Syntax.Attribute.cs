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
        public sealed class Attribute : ITakeArgument
        {
            private readonly List<Argument> m_Arguments;
            
            /// <summary>
            /// Constructs a new <c>Attribute</c>.
            /// </summary>
            /// <param name="name">The attribute name.</param>
            public Attribute(string name)
            {
                this.name = name;
                m_Arguments = new List<Argument>();
            }

            /// <summary>
            /// Gets or sets the unique attribute identifier.
            /// </summary>
            public string name { get; set; }
            
            /// <summary>
            /// Returns the parameters associated with this attribute.
            /// </summary>
            public IReadOnlyList<Argument> arguments => m_Arguments;

            public void Format(SourceFormatter formatter)
            {
                formatter.WriteUnformatted('[');
                formatter.WriteUnformatted(name);
                if (m_Arguments.Count != 0)
                {
                    formatter.WriteUnformatted('(');
                    for (var i = 0; i < m_Arguments.Count; ++i)
                    {
                        if (i > 0)
                            formatter.WriteUnformatted(", ");
                        m_Arguments[i].Format(formatter);
                    }    
                    formatter.WriteUnformatted(')');
                }
                formatter.WriteUnformatted(']');
                formatter.Newline();
            }

            public void AddArgument(Argument argument)
            {
                m_Arguments.Add(argument);
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
        
        public static Syntax.Struct StructLayout(this Syntax.Struct @struct, LayoutKind layoutKind, 
            int pack = 0, int absoluteSizeBytes = -1)
        {
            var attribute = @struct.DeclareAttribute("StructLayout")
                .Argument("layoutKind", "LayoutKind." + layoutKind); // TODO Use type function
            if (pack > 0)
                attribute.Argument("pack", pack.ToString());
            if (absoluteSizeBytes > 0)
                attribute.Argument("size", absoluteSizeBytes.ToString());
            return @struct;
        }
    }
}