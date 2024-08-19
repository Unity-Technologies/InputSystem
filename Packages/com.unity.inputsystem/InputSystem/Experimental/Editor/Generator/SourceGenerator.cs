using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public sealed class SourceGenerator 
    {
        public SourceGenerator(SourceContext context)
        {
            this.context = context;
        }

        public SourceContext context { get; private set; }

        public static void Format(SourceContext context, SourceFormatter formatter)
        {
            //foreach (var item in context.root.usingStatements)
            //    item.Format(formatter);
            
            //formatter.NeedsNewline();
            
            /*if (context.root.usingStatements.Count != 0 && context.root.declaredTypes.Count != 0)
            {
                formatter.Newline();
                formatter.Newline();
            }*/
            
            foreach (var declaredType in context.root)
                VisitDepthFirstRecursive(declaredType, 
                    (node) => node.PreFormat(context, formatter), 
                    (node) => node.Format(context, formatter), 
                    (node) => node.PostFormat(context, formatter));
        }
        
        public void Format(SourceFormatter formatter)
        {
            Format(this.context, formatter);
        }

        /*public static void VisitDepthFirst(Syntax.INode root, Action<Syntax.INode> visitor)
        {
            var stack = new Stack<Syntax.INode>();
            var visited = new HashSet<Syntax.INode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (visited.Contains(node)) 
                    continue; // TODO Throw?
                
                visitor(node);
                visited.Add(node);
                
                var n = node.children.Count;
                for (var i=0; i < n; ++i)
                    stack.Push(node.children[i]);
            }
        }*/
        
        public static void VisitDepthFirstRecursive<TNode>(TNode root, Action<Syntax.INode> preVisitor, 
            Action<Syntax.INode> visitor, Action<Syntax.INode> postVisitor)
            where TNode : Syntax.INode
        {
            preVisitor(root); // TODO Combine pre and visitor?
            visitor(root);
            foreach (var child in root)
                VisitDepthFirstRecursive(child, preVisitor, visitor, postVisitor);
            postVisitor(root);
        }
        
        public override string ToString()
        {
            var formatter = new SourceFormatter();
            Format(formatter);
            return formatter.ToString();
        }
    }
}