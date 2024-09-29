using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineParameter
        {
            public void AddParameter(Parameter parameter);
        }
        
        public sealed class Parameter : Node, IDeclareAttribute 
        {
            private List<Attribute> m_Attributes;
            
            public Parameter(SourceContext context, string name, TypeReference type, string value = null)
                : base(context)
            {
                this.name = name;
                this.type = type;
                this.m_Attributes = new List<Attribute>();
            }
            
            public string name { get; set; }
            public TypeReference type { get; set; }

            public override void Format(SourceContext _, SourceFormatter formatter)
            {
                Attribute.Format(formatter, m_Attributes);
                
                formatter.WriteUnformatted(type.value);
                //formatter.WriteUnformatted(context.GetTypeName(type.type));
                formatter.Write(name);
            }

            public static void Format(SourceFormatter formatter, Parameter param)
            {
                Attribute.Format(formatter, param.m_Attributes);
                formatter.WriteUnformatted(param.type.value);
                formatter.Write(param.name);
            }
            
            public static void Format(SourceFormatter formatter, IReadOnlyList<Parameter> parameters)
            {
                if (parameters.Count == 0) return;
                Format(formatter, parameters[0]);
                for (var i = 1; i < parameters.Count; ++i)
                {
                    formatter.WriteUnformatted(", ");
                    Format(formatter, parameters[i]);
                }
            }

            public void AddAttribute(Attribute attribute)
            {
                m_Attributes.Add(attribute);
            }
        }
    }

    // TODO Return parameter to allow for attributes etc.
    public static class ParameterExtensions
    {
        public static Syntax.Parameter Parameter<TTarget>(this TTarget target, string name, Syntax.TypeReference type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(target.context, name, type);
            target.AddParameter(node);
            return node;
        }
        
        public static Syntax.Parameter Parameter<TTarget>(this TTarget target, string name, System.Type type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(target.context, name, new Syntax.TypeReference(type));
            target.AddParameter(node);
            return node;
        }
        
        public static Syntax.Parameter Parameter<TTarget>(this TTarget target, string name, Syntax.TypeArgument type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(target.context, name, new Syntax.TypeReference(type));
            target.AddParameter(node);
            return node;
        }
    }
}