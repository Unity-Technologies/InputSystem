using System;
using UnityEditor.InputSystem.Experimental.Generator;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDeclareInterface
        {
            public void AddInterface(DeclaredInterface @interface);
        }
        
        public class DeclaredInterface : DeclaredType
        {
            public DeclaredInterface(SourceContext context, string name) 
                : base(context, "interface", name)
            {
            }

            public static DeclaredInterface FromType(SourceContext context, System.Type type)
            {
                return new DeclaredInterface(context, type.Name);
            }
        }

        public interface IImplementInterface
        {
            public void AddImplementedInterface(ImplementedInterface @interface);
        }

        public class ImplementedInterface
        {
            public ImplementedInterface(SourceContext context, string name)
            {
                this.name = name;
            }
            
            public string name { get; set; }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.DeclaredInterface DeclareInterface<TTarget>(this TTarget target, string name, 
            Syntax.TypeReference returnType = null)
            where TTarget : Syntax.INode, Syntax.IDeclareInterface
        {
            var x = new Syntax.DeclaredInterface(target.context, name);
            target.AddInterface(x);
            return x;
        }

        public static Syntax.IImplementInterface ImplementInterface<TTarget>(this TTarget target, string @interface)
            where TTarget : Syntax.INode, Syntax.IImplementInterface
        {
            var x = new Syntax.ImplementedInterface(target.context, @interface);
            target.AddImplementedInterface(x);
            return target;
        }
        
        public static Syntax.IImplementInterface ImplementInterface<TTarget>(this TTarget target, Type @interface)
            where TTarget : Syntax.INode, Syntax.IImplementInterface
        {
            // TODO FIX when supporting inheritance Debug.Assert(@interface.IsInterface);
            
            // TODO Register namespace of interface type
            var x = new Syntax.ImplementedInterface(target.context, SourceUtils.GetTypeName(@interface));
            target.AddImplementedInterface(x);
            return target;
        }
    }
}