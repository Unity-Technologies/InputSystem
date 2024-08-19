using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public abstract class Node : INode
        {
            protected Node(SourceContext context)
            {
                this.context = context;
            }
            public SourceContext context { get; }
            
            public virtual void PreFormat(SourceContext context, SourceFormatter formatter) {}
            public virtual void Format(SourceContext context, SourceFormatter formatter) {}
            public virtual void PostFormat(SourceContext context, SourceFormatter formatter) {}
            IEnumerator IEnumerable.GetEnumerator() => GetChildren().GetEnumerator();

            private IEnumerable<INode> GetChildren()
            {
                if (m_ChildCollections == null)
                    yield break;
                
                foreach (var enumerable in m_ChildCollections)
                {
                    foreach (var item in enumerable)
                    {
                        yield return item;
                    }
                }
            }
            public IEnumerable<INode> children => GetChildren(); 
            private IReadOnlyList<INode>[] m_ChildCollections;
            public IEnumerator<INode> GetEnumerator() => children.GetEnumerator();
            protected void SetChildren(params IReadOnlyList<INode>[] childCollections)
            {
                m_ChildCollections = childCollections;
            }
        }
    }
}