using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public class Body : Node
        {
            public Body(SourceContext context) 
                : base(context)
            {
            }

            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.BeginScope();
            }

            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                formatter.EndScope();
            }
        } 

        public class FunctionBody : Body, IDefineStatement
        {
            private List<Statement> m_Statements;
            
            public FunctionBody(SourceContext context) : base(context)
            {
                m_Statements = new List<Statement>();
            }

            public void AddStatement(Statement statement)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}