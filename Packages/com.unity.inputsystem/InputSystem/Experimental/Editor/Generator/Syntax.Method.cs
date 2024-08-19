namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public class Method : Node
        {
            public Method(SourceContext context, string name, TypeReference returnType = null) 
                : base(context)
            {
                
            }

            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                base.PreFormat(context, formatter);
            }
            
            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                base.PreFormat(context, formatter);
            }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Method DeclareMethod(this Syntax.MutableNode target, string name, Syntax.TypeReference returnType = null)
        {
            var method = new Syntax.Method(target.context, name, returnType);
            target.Add(method);
            return method;
        }
    }
}