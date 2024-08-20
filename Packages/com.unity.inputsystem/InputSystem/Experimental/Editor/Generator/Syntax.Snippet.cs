namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public sealed class Snippet : Node
        {
            public Snippet(SourceContext context, string text) 
                : base(context)
            {
                this.text = text;
            }

            public string text { get; set; }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                var lines = this.text.Split('\n');
                for (var i = 0; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    formatter.WriteLine(line);
                }
            }
        }
    }
}