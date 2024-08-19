namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class NamedParameter
        {
            public NamedParameter(string name, string value)
            {
                this.type = type;
                this.name = name;
                this.value = value;
            }
            
            public string type { get; set; }
            public string name { get; set; }
            public string value { get; set; }

            public void Format(SourceFormatter formatter)
            {
                formatter.Write(type);
                formatter.Write(name);
            }
        }
    }
}