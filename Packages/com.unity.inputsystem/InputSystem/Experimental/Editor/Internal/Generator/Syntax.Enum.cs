using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public static partial class Syntax
    {
        public interface IDeclareEnum
        {
            public void AddEnum(Enum @enum);
        }
        
        public sealed class Enum : DeclaredType
        {
            public sealed class Item
            {
                public Item(string name, string value = null)
                {
                    this.name = name;
                    this.value = value;
                }
                
                public string name { get; }
                public string value { get; }
            }

            // TODO Annotation, DocSummary, Visibility
            // TODO Items, Underlying

            private List<Item> m_Items;
            
            /// <summary>
            /// Constructs a new enum syntax node.
            /// </summary>
            /// <param name="context">The associated context.</param>
            /// <param name="name">The unique enum identifier.</param>
            internal Enum([NotNull] SourceContext context, [NotNull] string name)
                : base(context, "enum", name)
            {
                m_Items = new List<Item>();
                this.name = name;
            }
            
            public IReadOnlyList<Item> items => m_Items;

            public void AddItem(Item item)
            {
                if (m_Items.Contains(item))
                    throw new ArgumentException();
                
                m_Items.Add(item);
            }

            public void AddItem(string name, string value = null)
            {
                AddItem(new Item(name, value));
            }

            public override void PreFormat(SourceContext context, SourceFormatter formatter)
            {
                base.PreFormat(context, formatter);
            }

            public override void Format(SourceContext context, SourceFormatter formatter)
            {
                var n = items.Count;
                for (var i=0; i < n; ++i)
                {
                    if (i > 0)
                    {
                        formatter.WriteUnformatted(',');
                        formatter.Newline();
                    }
                    if (items[i].value != null)
                        formatter.Assign(items[i].name, items[i].value);
                    else
                        formatter.Write(items[i].name);
                    formatter.NeedsNewline();
                }
            }

            public override void PostFormat(SourceContext context, SourceFormatter formatter)
            {
                base.PostFormat(context, formatter);                
            }
        }
    }

    public static partial class SyntaxExtensions
    {
        public static Syntax.Enum DeclareEnum<TTarget>(this TTarget target, string name)
            where TTarget : Syntax.IDeclareEnum, Syntax.INode
        {
            var node = new Syntax.Enum(target.context, name);
            target.AddEnum(node);
            return node;
        }
        
        public static Syntax.Enum DeclareEnumFlags<TTarget>(this TTarget target, string name)
            where TTarget : Syntax.IDeclareEnum, Syntax.INode
        {
            var node = DeclareEnum(target, name);
            node.AddAttribute(new Syntax.Attribute("Flags"));
            return node;
        }
        
        public static Syntax.Enum DeclareEnumFlags<TTarget>(this TTarget target, string name, params Syntax.Enum.Item[] items)
            where TTarget : Syntax.IDeclareEnum, Syntax.INode
        {
            var node = DeclareEnumFlags(target, name);
            if (items != null)
            {
                foreach (var item in items)
                {
                    node.AddItem(item);
                }
            }
            return node;
        }
    }
}