using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        /// <summary>
        /// Interface for a syntax node that may be formatted to source code.
        /// </summary>
        public interface IFormattableSyntaxNode
        {
            /// <summary>
            /// Pre-order visitor traversal pattern.
            /// </summary>
            /// <param name="context">The associated source context.</param>
            /// <param name="formatter">The associated formatter.</param>
            public void PreFormat([NotNull] SourceContext context, [NotNull] SourceFormatter formatter);
            
            /// <summary>
            /// In-order visitor traversal pattern.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="formatter">The associated formatter.</param>
            public void Format([NotNull] SourceContext context, [NotNull] SourceFormatter formatter);
            
            /// <summary>
            /// Post-order visitor traversal pattern.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="formatter">The associated formatter.</param>
            public void PostFormat([NotNull] SourceContext context, [NotNull] SourceFormatter formatter);
        }

        public enum VisitOrder
        {
            PreOrder,
            InOrder,
            PostOrder
        }
        
        public interface IVisitor
        {
            public void Visit(SourceContext context, SourceFormatter formatter, VisitOrder order);
        }

        public interface INodeContext
        {
            public SourceContext context { get; }
        }
        
        /// <summary>
        /// Interface representing operations on an arbitrary syntax tree node.
        /// </summary>
        public interface INode : IFormattableSyntaxNode, IEnumerable<INode>, INodeContext
        {
            //public SourceContext context { get; }
            
            /// <summary>
            /// Returns a read-only view of all child nodes associated with this node.
            /// </summary>
            //public IEnumerable<INode> children { get; }
        }
    }
}