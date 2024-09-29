using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                formatter.Write('['); // TODO This is a problem, not taking indent into account
                Format(formatter, this, 0);
                formatter.WriteUnformatted(']');
                formatter.Newline();
            }

            private static void Format(SourceFormatter formatter, Attribute attribute, int index)
            {
                if (index > 0)
                    formatter.WriteUnformatted(", ");
                formatter.WriteUnformatted(attribute.name);
                if (attribute.m_Arguments.Count == 0)
                    return;
                formatter.WriteUnformatted('(');
                for (var i = 0; i < attribute.m_Arguments.Count; ++i)
                {
                    if (i > 0)
                        formatter.WriteUnformatted(", ");
                    attribute.m_Arguments[i].Format(formatter);
                }    
                formatter.WriteUnformatted(')');
            }

            public static bool Format(SourceFormatter formatter, IReadOnlyList<Attribute> attributes)
            {
                if (attributes.Count == 0) 
                    return false;
                formatter.Write('[');
                for (var i=0; i < attributes.Count; ++i)
                    Format(formatter, attributes[i], i);
                formatter.WriteUnformatted(']');
                //formatter.WriteUnformatted(' ');
                return true;
            }

            public void AddArgument(Argument argument)
            {
                m_Arguments.Add(argument);
            }
        }
    }
    
    public static class AttributeExtensions
    {
        public static Syntax.Attribute DeclareAttribute<TTarget>(this TTarget target, string name) // TODO Consider not supporting string
            where TTarget : Syntax.IDeclareAttribute
        {
            var attribute = new Syntax.Attribute(name);
            target.AddAttribute(attribute);
            return attribute;
        }
        
        public static Syntax.Attribute DeclareAttribute<TTarget>(this TTarget target, System.Type attribute)
            where TTarget : Syntax.IDeclareAttribute, Syntax.INode
        {
            target.context.root.Using(attribute.Namespace);
            var name = attribute.Name;
            if (name.EndsWith("Attribute"))
                name = name.Substring(0, name.Length - 9);
            return DeclareAttribute(target, name);
        }
        
        public static Syntax.Attribute NotNull<TTarget>(this TTarget target)
            where TTarget : Syntax.IDeclareAttribute, Syntax.INode
        {
            return target.DeclareAttribute(typeof(NotNullAttribute));
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