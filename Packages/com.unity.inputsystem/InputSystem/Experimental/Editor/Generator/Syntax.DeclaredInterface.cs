namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface ISupportInterface
        {
            public void AddInterface(DeclaredInterface @interface);
        }
        
        public class DeclaredInterface : DeclaredType
        {
            public DeclaredInterface(SourceContext context, string name) 
                : base(context, "interface", name)
            {
            }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.DeclaredInterface DeclareInterface<TTarget>(this TTarget target, string name, 
            Syntax.TypeReference returnType = null)
            where TTarget : Syntax.INode, Syntax.ISupportInterface
        {
            var x = new Syntax.DeclaredInterface(target.context, name);
            target.AddInterface(x);
            return x;
        }
    }
}