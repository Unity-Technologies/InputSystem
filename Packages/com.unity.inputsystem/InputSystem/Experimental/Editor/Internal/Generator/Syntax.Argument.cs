namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface ITakeArgument
        {
            public void AddArgument(Argument argument);
        }
        
        public class Argument
        {
            public Argument(string name, string value)
            {
                this.name = name;
                this.value = value;
            }
            
            public string name { get; set; }
            public string value { get; set; }

            public void Format(SourceFormatter formatter)
            {
                if (name != null)
                {
                    formatter.WriteUnformatted(name);
                    formatter.WriteUnformatted(':');
                    formatter.WriteUnformatted(' ');
                }
                formatter.Write(value);
            }
        }
    }

    public static class ArgumentExtensions
    {
        public static TTarget Argument<TTarget>(this TTarget target, string name, string value)
            where TTarget : Syntax.ITakeArgument
        {
            var argument = new Syntax.Argument(name, value);
            target.AddArgument(argument);
            return target;
        }
        
        public static TTarget Argument<TTarget>(this TTarget target, string value)
            where TTarget : Syntax.ITakeArgument
        {
            var argument = new Syntax.Argument(null, value);
            target.AddArgument(argument);
            return target;
        }
    }
}