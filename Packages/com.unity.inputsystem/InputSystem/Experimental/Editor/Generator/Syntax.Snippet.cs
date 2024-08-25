using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineSnippet
        {
            public void AddSnippet(Snippet snippet);
        }
        
        public sealed class Snippet : Node
        {
            public Snippet([NotNull] SourceContext context, [NotNull] string text) 
                : base(context)
            {
                if (context == null) throw new ArgumentNullException(nameof(context));
                this.text = text ?? throw new ArgumentNullException(nameof(text));
            }

            public string text { get; set; }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                var startIndex = 0;
                while (startIndex < text.Length)
                {
                    var stopIndex = text.IndexOf('\n', startIndex);
                    for (var i = startIndex; i < stopIndex; ++i)
                        formatter.WriteUnformatted(text[i]);
                    formatter.Newline();
                }
            }
        }
    }

    public static class SnippetExtensions
    {
        public static TTarget Snippet<TTarget>(this TTarget target, string text)
            where TTarget : Syntax.IDefineSnippet, Syntax.INode
        {
            var snippet = new Syntax.Snippet(target.context, text);
            target.AddSnippet(snippet);
            return target;
        }
    }
}