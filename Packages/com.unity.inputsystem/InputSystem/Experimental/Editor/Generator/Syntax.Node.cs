using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public abstract class Node : INode
        {
            internal MutableNode m_Parent; // TODO Remove?
            public virtual void PreFormat(SourceContext context, SourceFormatter formatter) {}
            public virtual void Format(SourceContext context, SourceFormatter formatter) {}
            public virtual void PostFormat(SourceContext context, SourceFormatter formatter) {}
            protected readonly List<INode> Children;

            protected Node(SourceContext context)
            {
                Children = new List<INode>();
                this.context = context;
            }
            public DocSummary docSummary { get; set; }
            public SourceContext context { get; }
            public IReadOnlyList<INode> children => Children;
        }
    }
}