using System.Collections.Generic;
using IronJava.Core.AST.Visitors;

namespace IronJava.Core.AST
{
    /// <summary>
    /// Base class for all Java AST nodes.
    /// </summary>
    public abstract class JavaNode
    {
        /// <summary>
        /// The location of this node in the source code.
        /// </summary>
        public SourceRange Location { get; }

        /// <summary>
        /// Parent node in the AST.
        /// </summary>
        public JavaNode? Parent { get; internal set; }

        /// <summary>
        /// Child nodes in the AST.
        /// </summary>
        public IReadOnlyList<JavaNode> Children => _children;
        private readonly List<JavaNode> _children = new();

        protected JavaNode(SourceRange location)
        {
            Location = location;
        }

        /// <summary>
        /// Accept a visitor to traverse this node.
        /// </summary>
        public abstract T Accept<T>(IJavaVisitor<T> visitor);

        /// <summary>
        /// Accept a visitor to traverse this node without returning a value.
        /// </summary>
        public abstract void Accept(IJavaVisitor visitor);

        /// <summary>
        /// Add a child node.
        /// </summary>
        protected internal void AddChild(JavaNode child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// Add multiple child nodes.
        /// </summary>
        protected internal void AddChildren(IEnumerable<JavaNode> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }
    }
}