using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class DeclaredType : Node, IDeclareField, IDeclareInterface, IDeclareClass, IDefineMethod, 
            IDeclareAttribute, IDefineSnippet
        {
            private readonly List<DeclaredInterface> m_Interfaces;
            private readonly List<Attribute> m_Attributes;
            private readonly List<Field> m_Fields;
            private readonly List<Class> m_Classes;
            private readonly List<Method> m_Methods;
            private readonly List<Snippet> m_Snippets;
            private readonly string m_Token;

            protected DeclaredType(SourceContext context, string token, string name)
                : base(context)
            {
                this.name = name;
                this.m_Token = token;
                
                m_Interfaces = new List<DeclaredInterface>();
                m_Attributes = new List<Attribute>();
                m_Fields = new List<Field>();
                m_Classes = new List<Class>();
                m_Methods = new List<Method>();
                m_Snippets = new List<Snippet>();
                
                SetChildren(m_Interfaces, m_Fields, m_Classes, m_Methods, m_Snippets);
            }
            public string name { get; set; }
            public DocSummary docSummary { get; set; }
            public Visibility visibility { get; set; }
            public bool isReadOnly { get; set; }
            public bool isSealed { get; set; }
            public bool isPartial { get; set; }
            public string declaredNamespace { get; set; }
            
            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.Paragraph();
                
                // TODO Insert newline if formatter has previous data?
                docSummary?.Format(formatter);

                foreach (var annotation in m_Attributes)
                    annotation.Format(formatter);
                
                // Declaration
                formatter.Write(formatter.Format(visibility));
                if (isSealed)
                    formatter.Write("sealed");
                if (isPartial)
                    formatter.Write("partial");
                if (isReadOnly)
                    formatter.Write("readonly");
                formatter.Write(m_Token);
                formatter.Write(name);
                formatter.BeginScope();
            }

            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.EndScope();
            }
            
            #region IDeclareInterface
            public void AddInterface(DeclaredInterface @interface) => m_Interfaces.Add(@interface);
            #endregion
            
            #region IDeclareField

            public void AddField(Field field)
            {
                m_Fields.Add(@field);
            }

            public Field DeclareField<T>(string fieldName, ref T value)
            {
                // Note: We have to declare this as a regular method since otherwise compiler won't be capable of
                //       inferring types of both T and TTarget.
                return this.DeclareField(typeof(T), fieldName, value.ToString());
            }
            public Field DeclareField<T>(string fieldName, string value = null)
            {
                // Note: We have to declare this as a regular method since otherwise compiler won't be capable of
                //       inferring types of both T and TTarget.
                return this.DeclareField(typeof(T), fieldName, value);
            }
            #endregion
            
            #region IDeclareClass
            public void AddClass(Class @class)
            {
                m_Classes.Add(@class);
            }
            #endregion
            
            #region IDeclareMethod
            public void AddMethod(Method method)
            {
                m_Methods.Add(method);
            }
            #endregion

            public void AddAttribute(Attribute attribute)
            {
                m_Attributes.Add(attribute);
            }
            
            public void AddSnippet(Syntax.Snippet snippet) => m_Snippets.Add(@snippet);
        }
    }
}