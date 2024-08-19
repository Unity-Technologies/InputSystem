using System;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class Field : Node
        {
            public Field(SourceContext context, TypeReference type, string name, string value) 
                : base(context)
            {
                this.fieldOffset = -1;
                this.visibility = Visibility.Default;
                this.name = name;
                this.value = value;
                this.type = type;
                context.AddUsing(type.declaredNamespace);
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
                formatter.Write(formatter.ToString(visibility));
                if (isFixed)
                    formatter.Write("fixed");
                if (type.isType)
                    formatter.WriteType(context.GetTypeName(type.type));
                else
                    throw new NotImplementedException("Need to generate valid type and namespace from declared");
                if (value != null)
                    formatter.Assign(name, value);
                else
                    formatter.Write(name);
                formatter.EndStatement();
            }
        }
    }
    
    public static partial class SyntaxExtensions
    {
        public static Syntax.Field DeclareField(this Syntax.MutableNode target, Syntax.DeclaredType type, string name, string value = null)
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(type), name, value);
            target.Add(field);
            return field;
        }
        
        public static Syntax.Field DeclareField(this Syntax.MutableNode target, System.Type type, string name, string value = null)
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(type), name, value);
            target.Add(field);
            return field;
        }
        
        public static Syntax.Field DeclareField<T>(this Syntax.MutableNode target, string name, string value = null)
        {
            var field = new Syntax.Field(target.context, new Syntax.TypeReference(typeof(T)), name, value);
            target.Add(field);
            return field;
        }
    }
}