using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareStruct
        {
            public void AddStruct(Struct @struct);
        }
        
        /// <summary>
        /// A struct syntax node.
        /// </summary>
        public class Struct : DeclaredType
        {
            /// <summary>
            /// Constructs a new <c>Struct</c> syntax node.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="name">The unique struct identifier.</param>
            internal Struct([NotNull] SourceContext context, [NotNull] string name)
                : base(context, "struct", name)
            { }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Struct DeclareStruct<TTarget>(this TTarget target, string name, Syntax.Visibility visibility = Syntax.Visibility.Default)
            where TTarget : Syntax.IDeclareStruct, Syntax.INode
        {
            var node = new Syntax.Struct(target.context, name);
            node.visibility = visibility;
            target.AddStruct(node);
            return node;
        }
    }
}