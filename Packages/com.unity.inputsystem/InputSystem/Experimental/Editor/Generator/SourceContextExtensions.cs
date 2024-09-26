namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static class SourceContextExtensions
    {
        public static void Format(this SourceContext context, SourceFormatter formatter)
        {
            VisitDepthFirstRecursive(context.root, context, formatter);
        }
        
        public static string ToSource(this SourceContext context, SourceFormatter formatter = null)
        {
            formatter ??= new SourceFormatter();
            Format(context, formatter);
            return formatter.ToString();
        }
        
        private static void VisitDepthFirstRecursive<TNode>(TNode root, SourceContext context, SourceFormatter formatter)
            where TNode : Syntax.INode
        {
            root.PreFormat(context, formatter);
            root.Format(context, formatter);
            foreach (var child in root)
                VisitDepthFirstRecursive(child, context, formatter);
            root.PostFormat(context, formatter);
        }
    }
}