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
                    // "Split" snippet into rows and copy over via formatter
                    var stopIndex = text.IndexOf('\n', startIndex);
                    if (stopIndex == -1)
                        stopIndex = text.Length;
                    formatter.WriteLine(text.AsSpan(startIndex, stopIndex - startIndex));
                    //for (var i = startIndex; i < stopIndex; ++i)
                    //    formatter.WriteUnformatted(text[i]);
                    formatter.Newline();
                    startIndex = stopIndex + 1;
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