namespace UnityEngine.InputSystem.Experimental.Generator
{
    public sealed class SourceFile
    {
        public SourceFile(string path = null)
        {
            path = null;
            context = new SourceContext();
        }

        public SourceContext context { get; }
        public string path { get; set; }
        public string content { get; set; }
    }
}