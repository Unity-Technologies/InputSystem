using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        /// <summary>
        /// A class syntax node.
        /// </summary>
        public sealed class Class : DeclaredType, ISupportInterface
        {
            private List<DeclaredInterface> m_Interfaces;
            
            /// <summary>
            /// Constructs a new class syntax node.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="name">The unique class identifier.</param>
            internal Class([NotNull] SourceContext context, [NotNull] string name)
                : base(context, "class", name)
            { }

            public void AddInterface(DeclaredInterface @interface) => m_Interfaces.Add(@interface);
        }
    }

    public static partial class SyntaxExtensions
    {
        public static Syntax.Class DeclareClass(this SourceContext target, string name)
        {
            var node = new Syntax.Class(target, name);
            target.Add(node);
            return node;
        }
        
        public static Syntax.Class DeclareClass(this Syntax.MutableNode target, string name)
        {
            var node = new Syntax.Class(target.context, name);
            target.Add(node);
            return node;
        }
    }
}