using System.Collections.Generic;
using IronJava.Core.AST.Visitors;

namespace IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Represents a reference to a type.
    /// </summary>
    public abstract class TypeReference : JavaNode
    {
        protected TypeReference(SourceRange location) : base(location) { }
    }

    /// <summary>
    /// Represents a primitive type (int, boolean, etc.).
    /// </summary>
    public class PrimitiveType : TypeReference
    {
        public PrimitiveTypeKind Kind { get; }

        public PrimitiveType(SourceRange location, PrimitiveTypeKind kind) : base(location)
        {
            Kind = kind;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitPrimitiveType(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitPrimitiveType(this);
    }

    public enum PrimitiveTypeKind
    {
        Boolean,
        Byte,
        Short,
        Int,
        Long,
        Char,
        Float,
        Double,
        Void
    }

    /// <summary>
    /// Represents a reference type (class, interface, etc.).
    /// </summary>
    public class ClassOrInterfaceType : TypeReference
    {
        public string Name { get; }
        public ClassOrInterfaceType? Scope { get; }
        public IReadOnlyList<TypeArgument> TypeArguments { get; }
        public IReadOnlyList<Annotation> Annotations { get; }

        public ClassOrInterfaceType(
            SourceRange location,
            string name,
            ClassOrInterfaceType? scope,
            IReadOnlyList<TypeArgument> typeArguments,
            IReadOnlyList<Annotation> annotations) : base(location)
        {
            Name = name;
            Scope = scope;
            TypeArguments = typeArguments;
            Annotations = annotations;

            if (scope != null) AddChild(scope);
            AddChildren(typeArguments);
            AddChildren(annotations);
        }

        public string FullName => Scope != null ? $"{Scope.FullName}.{Name}" : Name;

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitClassOrInterfaceType(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitClassOrInterfaceType(this);
    }

    /// <summary>
    /// Represents an array type.
    /// </summary>
    public class ArrayType : TypeReference
    {
        public TypeReference ElementType { get; }
        public int Dimensions { get; }

        public ArrayType(
            SourceRange location,
            TypeReference elementType,
            int dimensions) : base(location)
        {
            ElementType = elementType;
            Dimensions = dimensions;
            AddChild(elementType);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitArrayType(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitArrayType(this);
    }

    /// <summary>
    /// Represents a type parameter in a generic declaration.
    /// </summary>
    public class TypeParameter : JavaNode
    {
        public string Name { get; }
        public IReadOnlyList<TypeReference> Bounds { get; }
        public IReadOnlyList<Annotation> Annotations { get; }

        public TypeParameter(
            SourceRange location,
            string name,
            IReadOnlyList<TypeReference> bounds,
            IReadOnlyList<Annotation> annotations) : base(location)
        {
            Name = name;
            Bounds = bounds;
            Annotations = annotations;

            AddChildren(bounds);
            AddChildren(annotations);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitTypeParameter(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitTypeParameter(this);
    }

    /// <summary>
    /// Represents a type argument in a generic type reference.
    /// </summary>
    public abstract class TypeArgument : JavaNode
    {
        protected TypeArgument(SourceRange location) : base(location) { }
    }

    /// <summary>
    /// Represents a concrete type argument.
    /// </summary>
    public class TypeArgumentType : TypeArgument
    {
        public TypeReference Type { get; }

        public TypeArgumentType(SourceRange location, TypeReference type) : base(location)
        {
            Type = type;
            AddChild(type);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitTypeArgumentType(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitTypeArgumentType(this);
    }

    /// <summary>
    /// Represents a wildcard type argument (? extends T, ? super T).
    /// </summary>
    public class WildcardType : TypeArgument
    {
        public TypeReference? Bound { get; }
        public WildcardBoundKind BoundKind { get; }

        public WildcardType(
            SourceRange location,
            TypeReference? bound,
            WildcardBoundKind boundKind) : base(location)
        {
            Bound = bound;
            BoundKind = boundKind;

            if (bound != null) AddChild(bound);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitWildcardType(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitWildcardType(this);
    }

    public enum WildcardBoundKind
    {
        None,      // ?
        Extends,   // ? extends T
        Super      // ? super T
    }
}