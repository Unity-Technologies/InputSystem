using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineProperty
        {
            public void AddProperty(Property property);
        }
        
        public class Property : Node
        {
            private readonly TypeReference m_Type;
            
            public Property(SourceContext context, TypeReference type, string name) : base(context)
            {
                this.name = name;
                this.visibility = Visibility.Default;
                this.m_Type = type;
            }
            
            public string name { get; set; }
            public Visibility visibility { get; set; }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                formatter.Write(formatter.Format(visibility));
                formatter.Write(name);
            }
        }
    }

    public static class PropertyExtensions
    {
        public static Syntax.Property DefineProperty<TTarget>(this TTarget target, string name, Syntax.TypeReference type)
            where TTarget : Syntax.IDefineProperty, Syntax.INode
        {
            var x = new Syntax.Property(target.context, type, name);
            target.AddProperty(x);
            return x;
        }
    }
}