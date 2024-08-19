using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareClass
        {
            public void AddClass(Class @class);
        }
        
        /// <summary>
        /// A class syntax node.
        /// </summary>
        public sealed class Class : DeclaredType
        {
            /// <summary>
            /// Constructs a new class syntax node.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="name">The unique class identifier.</param>
            internal Class([NotNull] SourceContext context, [NotNull] string name)
                : base(context, "class", name)
            { }
        }
    }

    public static partial class SyntaxExtensions
    {
        public static Syntax.Class DeclareClass<TSource>(this TSource target, string name)
            where TSource : Syntax.IDeclareClass, Syntax.INode
        {
            var node = new Syntax.Class(target.context, name);
            target.AddClass(node);
            return node;
        }
    }
}