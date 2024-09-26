using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public class DeclaredType : Node, IDeclareField, IDeclareInterface, IDeclareClass, IDefineMethod, 
            IDeclareAttribute, IDefineSnippet, IImplementInterface, IDefineTypeArgument, IDefineProperty
        {
            private readonly List<TypeArgument> m_GenericTypeArguments;
            private readonly List<DeclaredInterface> m_Interfaces;
            private readonly List<Attribute> m_Attributes;
            private readonly List<Field> m_Fields;
            private readonly List<Property> m_Properties;
            private readonly List<Class> m_Classes;
            private readonly List<Method> m_Methods;
            private readonly List<Snippet> m_Snippets;
            private readonly List<ImplementedInterface> m_ImplementedInterfaces;
            private readonly string m_Token;

            protected DeclaredType(SourceContext context, string token, string name)
                : base(context)
            {
                this.name = name;
                this.m_Token = token;

                m_GenericTypeArguments = new List<TypeArgument>();
                m_Interfaces = new List<DeclaredInterface>();
                m_Attributes = new List<Attribute>();
                m_Fields = new List<Field>();
                m_Properties = new List<Property>();
                m_Classes = new List<Class>();
                m_Methods = new List<Method>();
                m_Snippets = new List<Snippet>();
                m_ImplementedInterfaces = new List<ImplementedInterface>();
                
                SetChildren(m_Interfaces, m_Fields, m_Classes, m_Methods, m_Properties, m_Snippets);
            }
            public string name { get; set; }
            public DocSummary docSummary { get; set; }
            public Visibility visibility { get; set; }
            public bool isReadOnly { get; set; }
            public bool isSealed { get; set; }
            public bool isPartial { get; set; }
            public bool isStatic { get; set; }
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
                if (isReadOnly)
                    formatter.Write("readonly");
                if (isStatic)
                    formatter.Write("static");
                if (isPartial)
                    formatter.Write("partial");
                formatter.Write(m_Token);
                formatter.Write(name);
                if (m_GenericTypeArguments.Count > 0)
                {
                    formatter.WriteUnformatted('<');
                    for (var i = 0; i < m_GenericTypeArguments.Count; ++i)
                    {
                        if (i > 0)
                            formatter.WriteUnformatted(", ");
                        formatter.WriteUnformatted(m_GenericTypeArguments[i].name);
                    }
                    formatter.WriteUnformatted('>');
                }
                if (m_ImplementedInterfaces.Count > 0)
                {
                    formatter.WriteUnformatted(" : ");
                    for (var i = 0; i < m_ImplementedInterfaces.Count; ++i)
                    {
                        if (i > 0)
                            formatter.WriteUnformatted(", ");
                        formatter.WriteUnformatted(m_ImplementedInterfaces[i].name);
                    }
                }

                if (m_GenericTypeArguments.Count > 0)
                {
                    for (var i = 0; i < m_GenericTypeArguments.Count; ++i)
                    {
                        var typeArgument = m_GenericTypeArguments[i];
                        if (typeArgument.constraints.Count == 0)
                            continue;
                        
                        formatter.Newline();
                        formatter.IncreaseIndent();
                        if (typeArgument.constraints.Count > 0)
                        {
                            formatter.Write("where ");
                            formatter.WriteUnformatted(typeArgument.name);
                            formatter.WriteUnformatted(" : ");
                            for (var j = 0; j < typeArgument.constraints.Count; ++j)
                            {
                                if (j > 0)
                                    formatter.WriteUnformatted(", ");
                                formatter.WriteUnformatted(typeArgument.constraints[j]);   
                            }
                        }
                        formatter.DecreaseIndent();
                    }
                }
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
            public void AddImplementedInterface(ImplementedInterface @interface) => m_ImplementedInterfaces.Add(@interface);
            public void AddTypeArgument(TypeArgument type) => m_GenericTypeArguments.Add(type);
            public void AddProperty(Property property) => m_Properties.Add(property);
        }
    }
}