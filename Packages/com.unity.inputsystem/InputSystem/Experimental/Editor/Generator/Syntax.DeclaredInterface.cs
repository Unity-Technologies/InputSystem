namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDeclareInterface
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

        public interface IImplementInterface
        {
            public void AddImplementedInterface(ImplementedInterface @interface);
        }

        public class ImplementedInterface
        {
            public ImplementedInterface(SourceContext context, string name)
            {
                this.name = name;
            }
            
            public string name { get; set; }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.DeclaredInterface DeclareInterface<TTarget>(this TTarget target, string name, 
            Syntax.TypeReference returnType = null)
            where TTarget : Syntax.INode, Syntax.IDeclareInterface
        {
            var x = new Syntax.DeclaredInterface(target.context, name);
            target.AddInterface(x);
            return x;
        }

        public static Syntax.IImplementInterface ImplementInterface<TTarget>(this TTarget target, string @interface)
            where TTarget : Syntax.INode, Syntax.IImplementInterface
        {
            var x = new Syntax.ImplementedInterface(target.context, @interface);
            target.AddImplementedInterface(x);
            return target;
        }
    }
}