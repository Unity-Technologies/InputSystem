using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineParameter
        {
            public void AddParameter(Parameter parameter);
        }
        
        public sealed class Parameter : IDeclareAttribute
        {
            private List<Attribute> m_Attributes;
            
            public Parameter(string name, TypeReference type, string value = null)
            {
                this.name = name;
                this.type = type;
                this.m_Attributes = new List<Attribute>();
            }
            
            public string name { get; set; }
            public TypeReference type { get; set; }

            public void Format(SourceContext context, SourceFormatter formatter)
            {
                formatter.WriteUnformatted(type.value);
                //formatter.WriteUnformatted(context.GetTypeName(type.type));
                formatter.Write(name);
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
        public static TTarget Parameter<TTarget>(this TTarget target, string name, Syntax.TypeReference type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(name, type);
            target.AddParameter(node);
            return target;
        }
        
        public static TTarget Parameter<TTarget>(this TTarget target, string name, System.Type type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(name, new Syntax.TypeReference(type));
            target.AddParameter(node);
            return target;
        }
        
        public static TTarget Parameter<TTarget>(this TTarget target, string name, Syntax.TypeArgument type)
            where TTarget : Syntax.IDefineParameter, Syntax.INode
        {
            var node = new Syntax.Parameter(name, new Syntax.TypeReference(type));
            target.AddParameter(node);
            return target;
        }
    }
}