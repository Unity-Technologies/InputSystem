using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public partial class Syntax
    {
        public interface IDefineTypeArgument
        {
            public void AddTypeArgument(TypeArgument arg);
        }
        
        public class TypeArgument : Node
        {
            private readonly List<string> m_Constraints;
            
            public TypeArgument(SourceContext context, string name) 
                : base(context)
            {
                this.name = name;
                this.m_Constraints = new List<string>();
            }
            
            public string name { get; set; }

            public void AddConstraint(string constraint)
            {
                m_Constraints.Add(constraint);
            }

            public IReadOnlyList<string> constraints => m_Constraints;
        }
    }
}