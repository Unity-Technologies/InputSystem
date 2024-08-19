namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class DocSummary
        {
            public DocSummary(string text)
            {
                this.text = text;
            }
            
            public static implicit operator DocSummary(string text) => new DocSummary(text);
            
            public string text { get; set; }

            public void Format(SourceFormatter formatter)
            {
                formatter.WriteSummary(text);
                formatter.Newline();
            }
        }
    }
}