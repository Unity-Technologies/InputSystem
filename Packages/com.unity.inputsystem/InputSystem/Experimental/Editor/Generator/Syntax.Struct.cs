namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        /// <summary>
        /// A struct syntax node.
        /// </summary>
        public class Struct : DeclaredType
        {
            /// <summary>
            /// Constructs a new <c>Struct</c> syntax node.
            /// </summary>
            /// <param name="name">The unique struct identifier.</param>
            internal Struct(SourceContext context, string name)
                : base(context, "struct", name)
            { }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Struct DeclareStruct(this SourceContext target, string name)
        {
            var node = new Syntax.Struct(target, name);
            target.Add(node);
            return node;
        }
        
        public static Syntax.Struct DeclareStruct(this Syntax.MutableNode target, string name)
        {
            var node = new Syntax.Struct(target.context, name);
            target.Add(node);
            return node;
        }
    }
}