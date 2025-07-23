using System.Collections.Generic;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Represents a Java annotation usage.
    /// </summary>
    public class Annotation : JavaNode
    {
        public TypeReference Type { get; }
        public IReadOnlyList<AnnotationArgument> Arguments { get; }
        
        /// <summary>
        /// Gets the name of the annotation (e.g., "Override" for @Override).
        /// </summary>
        public string Name => Type.Name;

        public Annotation(
            SourceRange location,
            TypeReference type,
            IReadOnlyList<AnnotationArgument> arguments) : base(location)
        {
            Type = type;
            Arguments = arguments;

            AddChild(type);
            AddChildren(arguments);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAnnotation(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAnnotation(this);
    }

    /// <summary>
    /// Base class for annotation arguments.
    /// </summary>
    public abstract class AnnotationArgument : JavaNode
    {
        public string? Name { get; }

        protected AnnotationArgument(SourceRange location, string? name) : base(location)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a simple annotation argument (name = value).
    /// </summary>
    public class AnnotationValueArgument : AnnotationArgument
    {
        public Expression Value { get; }

        public AnnotationValueArgument(
            SourceRange location,
            string? name,
            Expression value) : base(location, name)
        {
            Value = value;
            AddChild(value);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAnnotationValueArgument(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAnnotationValueArgument(this);
    }

    /// <summary>
    /// Represents an array annotation argument.
    /// </summary>
    public class AnnotationArrayArgument : AnnotationArgument
    {
        public IReadOnlyList<Expression> Values { get; }

        public AnnotationArrayArgument(
            SourceRange location,
            string? name,
            IReadOnlyList<Expression> values) : base(location, name)
        {
            Values = values;
            AddChildren(values);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAnnotationArrayArgument(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAnnotationArrayArgument(this);
    }

    /// <summary>
    /// Represents JavaDoc documentation.
    /// </summary>
    public class JavaDoc : JavaNode
    {
        public string Content { get; }
        public IReadOnlyList<JavaDocTag> Tags { get; }

        public JavaDoc(
            SourceRange location,
            string content,
            IReadOnlyList<JavaDocTag> tags) : base(location)
        {
            Content = content;
            Tags = tags;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitJavaDoc(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitJavaDoc(this);
    }

    /// <summary>
    /// Represents a JavaDoc tag (@param, @return, etc.).
    /// </summary>
    public class JavaDocTag
    {
        public string Name { get; }
        public string? Parameter { get; }
        public string Description { get; }

        public JavaDocTag(string name, string? parameter, string description)
        {
            Name = name;
            Parameter = parameter;
            Description = description;
        }
    }
}