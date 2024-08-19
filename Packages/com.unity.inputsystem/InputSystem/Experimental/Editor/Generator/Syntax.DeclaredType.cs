using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class DeclaredType : MutableNode
        {
            private readonly List<Field> m_Fields;

            protected DeclaredType(SourceContext context, string kind, string name)
                : base(context)
            {
                this.name = name;
                this.kind = kind;
                annotations = new List<Attribute>();
                this.m_Fields = new List<Field>();
            }
            public string name { get; set; }
            public Visibility visibility { get; set; }
            public IReadOnlyList<Field> fields => m_Fields;
            public List<Attribute> annotations { get; }

            public bool isReadOnly { get; set; }
            public bool isSealed { get; set; }
            public bool isPartial { get; set; }
            public string declaredNamespace { get; set; }
            public string kind { get; private set; }
            
            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                // TODO Insert newline if formatter has previous data?
                
                docSummary?.Format(formatter);

                foreach (var annotation in annotations)
                    annotation.Format(formatter);
                
                formatter.Write(formatter.ToString(visibility));
                if (isSealed)
                    formatter.Write("sealed");
                if (isPartial)
                    formatter.Write("partial");
                if (isReadOnly)
                    formatter.Write("readonly");
                formatter.Write(kind);
                formatter.Write(name);
                formatter.Newline();
                formatter.BeginScope();
            }

            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.EndScope();
            }
        }
    }
}