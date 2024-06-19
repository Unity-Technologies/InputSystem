using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a node in a dependency graph.
    /// </summary>
    public interface IDependencyGraphNode : IEquatable<IDependencyGraphNode>
    {
        /// <summary>
        /// Returns a display name of this node.
        /// </summary>
        public string displayName { get; }
        
        /// <summary>
        /// Returns the number of child dependencies of this node.
        /// </summary>
        int childCount { get; }
        
        /// <summary>
        /// Returns the child dependency with the given index, where index is in interval [0, childCount).
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        /// <returns>IDependencyGraphNode instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">If <paramref name="index"/> is outside interval 0, childCount).</exception>
        IDependencyGraphNode GetChild(int index);
    }
    
    /// <summary>
    /// Provides extension methods for types implementing IDependencyGraphNode. 
    /// </summary>
    public static class DependencyGraphNodeExtensions
    {
        public static bool IsLeaf<TSource>(this TSource source) 
            where TSource : IDependencyGraphNode
        {
            return source.childCount == 0;
        }

        public static string Describe<TSource>(this TSource source)
            where TSource : IDependencyGraphNode
        {
            return Describe(source, new StringBuilder());
        }
        
        public static string Describe<TSource>(this TSource source, StringBuilder buffer)
            where TSource : IDependencyGraphNode
        {
            // TODO Consider if we want this or want to generate in reverse to reconstruct original expression
            buffer.Append(source.displayName);
            if (source.IsLeaf())
                return buffer.ToString();
            buffer.Append("( ");
            buffer.Append(source.GetChild(0).displayName);
            var n = source.childCount;
            for (var i = 1; i < n; ++i)
                buffer.Append(", ").Append(source.GetChild(i).displayName);
            buffer.Append(" )");
            return buffer.ToString();
        }
        
        public static void VisitDepthFirst<TSource>(this TSource root, Action<IDependencyGraphNode> visitor)
            where TSource : IDependencyGraphNode
        {
            // Non-recursive depth-first-search
            // TODO It would be good to optimize this, e.g. we could do with temporary allocator if nodes may be represented as structs in context, at least we can do better than this, especially if only main thread.
            // TODO One option is a represents tree as an array, 
            var stack = new Stack<TSource>();
            var visited = new HashSet<TSource>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (visited.Contains(node)) 
                    continue;
                visitor(node);
                visited.Add(node);
                var n = node.childCount;
                for (var i=0; i < n; ++i)
                    visitor(node.GetChild(i));
            }
        }

        // TODO Consider removing
        public static void VisitDepthFirstRecursive(this IDependencyGraphNode root, Action<IDependencyGraphNode> visitor)
        {
            visitor(root);
            var n = root.childCount;
            for (var i = 0; i < n; ++i)
                VisitDepthFirstRecursive(root.GetChild(i), visitor);
        }

        // TODO Consider removing
        public static IDependencyGraphNode[] ToArray(this IDependencyGraphNode root)
        {;
            var list = new List<IDependencyGraphNode>();
            VisitDepthFirstRecursive(root, (node) => list.Add(node));
            return list.ToArray();
        }

        public static bool CompareDependencyGraphs(this IDependencyGraphNode first, IDependencyGraphNode second)
        {
            if (ReferenceEquals(null, first) || ReferenceEquals(null, second))
                return false;
            // TODO Implement
            return false;
        }
    }
}