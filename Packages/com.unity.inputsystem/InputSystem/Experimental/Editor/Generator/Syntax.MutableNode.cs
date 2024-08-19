using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public abstract class MutableNode : Node, INode
        {
            /// <summary>
            /// Adds a child node to this node and makes this node the its parent.
            /// </summary>
            /// <param name="node">The node to be added as a child.</param>
            /// <exception cref="Exception">If node has already been parented.</exception>
            public void Add(Node node)
            {
                if (node.m_Parent != null)
                    throw new Exception($"{node} already have a parent");
                
                Children.Add(node);
                node.m_Parent = this;
            }

            protected MutableNode(SourceContext context) 
                : base(context)
            {
            }
        }
    }
}