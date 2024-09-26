namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineStatement
        {
            public void AddStatement(Statement statement);
        }
        
        public class Statement : Node
        {
            public Statement(SourceContext context, string value) 
                : base(context)
            {
                this.value = value;
            }
            
            public string value { get; set; }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                formatter.Write(value);
                formatter.EndStatement();
            }
        }
    }

    public static class StatementExtensions
    {
        public static TTarget Statement<TTarget>(this TTarget target, string value)
            where TTarget : Syntax.INode, Syntax.IDefineStatement
        {
            var statement = new Syntax.Statement(target.context, value);
            target.AddStatement(statement);
            return target;
        }
        
        public static TTarget Assignment<TTarget>(this TTarget target, string variable, string value)
            where TTarget : Syntax.INode, Syntax.IDefineStatement
        {
            return Statement(target, $"{variable} = {value}");
        }
    }
}