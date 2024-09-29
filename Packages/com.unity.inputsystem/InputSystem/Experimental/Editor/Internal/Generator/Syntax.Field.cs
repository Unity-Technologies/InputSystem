using System;
using System.Collections.Generic;
using UnityEditor.InputSystem.Experimental.Generator;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareField
        {
            public void AddField(Field field);
        }
        
        public class Field : Node, IDeclareAttribute
        {
            private readonly List<Syntax.Attribute> m_Attributes;

            public Field(SourceContext context, TypeReference type, string name, string value) 
                : base(context)
            {
                m_Attributes = new List<Syntax.Attribute>();
                
                this.fieldOffset = -1;
                this.visibility = Visibility.Default;
                this.name = name;
                this.value = value;
                this.type = type;
                if (!type.isGeneric)
                    context.root.Using(type.declaredNamespace);
            }
            
            public string name { get; set; }
            public int fieldOffset { get; set; }
            public Visibility visibility { get; set; }
            public string value { get; set; }
            public bool isFixed { get; set; }
            public TypeReference type { get; }
            
            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                if (fieldOffset >= 0)
                    formatter.Write($"[FieldOffset({fieldOffset})]"); // TODO Should be annotation
                Attribute.Format(formatter, m_Attributes);
                formatter.Write(formatter.Format(visibility));
                if (isFixed)
                    formatter.Write("fixed");
                if (type.isType)
                    formatter.Write(SourceUtils.GetTypeName(type.type));
                else if (type.isGeneric)
                    formatter.Write(type.value);
                else if (!type.isGeneric)
                    throw new NotImplementedException("Need to generate valid type and namespace from declared");
                if (value != null)
                    formatter.Assign(name, value);
                else
                    formatter.Write(name);
                formatter.EndStatement();
            }

            public void AddAttribute(Attribute attribute) => m_Attributes.Add(attribute);
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Field DeclareField<TTarget>(this TTarget target, Syntax.DeclaredType type, string name, string value = null)
            where TTarget : Syntax.IDeclareField, Syntax.INode
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(type), name, value);
            target.AddField(field);
            return field;
        }
        
        public static Syntax.Field DeclareField<TTarget>(this TTarget target, System.Type type, string name, string value = null)
            where TTarget : Syntax.IDeclareField, Syntax.INode
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(type), name, value);
            target.AddField(field);
            return field;
        }
        
        public static Syntax.Field DeclareField<TTarget>(this TTarget target, Syntax.TypeArgument typeArgument, string name)
            where TTarget : Syntax.IDeclareField, Syntax.INode
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(typeArgument), name, null);
            target.AddField(field);
            return field;
        }
    }
}