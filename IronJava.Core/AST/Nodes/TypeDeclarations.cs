using System.Collections.Generic;
using System.Linq;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Base class for all type declarations (class, interface, enum, annotation).
    /// </summary>
    public abstract class TypeDeclaration : JavaNode
    {
        public string Name { get; }
        public Modifiers Modifiers { get; }
        public IReadOnlyList<Annotation> Annotations { get; }
        public IReadOnlyList<TypeParameter> TypeParameters { get; }
        public JavaDoc? JavaDoc { get; }
        
        /// <summary>
        /// Gets the body/members of this type declaration.
        /// </summary>
        public abstract IEnumerable<JavaNode> Body { get; }

        protected TypeDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<TypeParameter> typeParameters,
            JavaDoc? javaDoc) : base(location)
        {
            Name = name;
            Modifiers = modifiers;
            Annotations = annotations;
            TypeParameters = typeParameters;
            JavaDoc = javaDoc;

            AddChildren(annotations);
            AddChildren(typeParameters);
            if (javaDoc != null) AddChild(javaDoc);
        }
    }

    /// <summary>
    /// Represents a class declaration.
    /// </summary>
    public class ClassDeclaration : TypeDeclaration
    {
        public TypeReference? SuperClass { get; }
        public IReadOnlyList<TypeReference> Interfaces { get; }
        public IReadOnlyList<MemberDeclaration> Members { get; }
        public IReadOnlyList<TypeDeclaration> NestedTypes { get; }
        public bool IsRecord { get; }

        public ClassDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<TypeParameter> typeParameters,
            TypeReference? superClass,
            IReadOnlyList<TypeReference> interfaces,
            IReadOnlyList<MemberDeclaration> members,
            IReadOnlyList<TypeDeclaration> nestedTypes,
            JavaDoc? javaDoc,
            bool isRecord = false) 
            : base(location, name, modifiers, annotations, typeParameters, javaDoc)
        {
            SuperClass = superClass;
            Interfaces = interfaces;
            Members = members;
            NestedTypes = nestedTypes;
            IsRecord = isRecord;

            if (superClass != null) AddChild(superClass);
            AddChildren(interfaces);
            AddChildren(members);
            AddChildren(nestedTypes);
        }

        public override IEnumerable<JavaNode> Body => Members.Cast<JavaNode>().Concat(NestedTypes);

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitClassDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitClassDeclaration(this);
    }

    /// <summary>
    /// Represents an interface declaration.
    /// </summary>
    public class InterfaceDeclaration : TypeDeclaration
    {
        public IReadOnlyList<TypeReference> ExtendedInterfaces { get; }
        public IReadOnlyList<MemberDeclaration> Members { get; }
        public IReadOnlyList<TypeDeclaration> NestedTypes { get; }

        public InterfaceDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<TypeParameter> typeParameters,
            IReadOnlyList<TypeReference> extendedInterfaces,
            IReadOnlyList<MemberDeclaration> members,
            IReadOnlyList<TypeDeclaration> nestedTypes,
            JavaDoc? javaDoc) 
            : base(location, name, modifiers, annotations, typeParameters, javaDoc)
        {
            ExtendedInterfaces = extendedInterfaces;
            Members = members;
            NestedTypes = nestedTypes;

            AddChildren(extendedInterfaces);
            AddChildren(members);
            AddChildren(nestedTypes);
        }

        public override IEnumerable<JavaNode> Body => Members.Cast<JavaNode>().Concat(NestedTypes);

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitInterfaceDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitInterfaceDeclaration(this);
    }

    /// <summary>
    /// Represents an enum declaration.
    /// </summary>
    public class EnumDeclaration : TypeDeclaration
    {
        public IReadOnlyList<TypeReference> Interfaces { get; }
        public IReadOnlyList<EnumConstant> Constants { get; }
        public IReadOnlyList<MemberDeclaration> Members { get; }
        public IReadOnlyList<TypeDeclaration> NestedTypes { get; }

        public EnumDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<TypeReference> interfaces,
            IReadOnlyList<EnumConstant> constants,
            IReadOnlyList<MemberDeclaration> members,
            IReadOnlyList<TypeDeclaration> nestedTypes,
            JavaDoc? javaDoc) 
            : base(location, name, modifiers, annotations, new List<TypeParameter>(), javaDoc)
        {
            Interfaces = interfaces;
            Constants = constants;
            Members = members;
            NestedTypes = nestedTypes;

            AddChildren(interfaces);
            AddChildren(constants);
            AddChildren(members);
            AddChildren(nestedTypes);
        }

        public override IEnumerable<JavaNode> Body => Constants.Cast<JavaNode>().Concat(Members).Concat(NestedTypes);

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitEnumDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitEnumDeclaration(this);
    }

    /// <summary>
    /// Represents an annotation type declaration.
    /// </summary>
    public class AnnotationDeclaration : TypeDeclaration
    {
        public IReadOnlyList<AnnotationMember> Members { get; }
        public IReadOnlyList<TypeDeclaration> NestedTypes { get; }

        public AnnotationDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<AnnotationMember> members,
            IReadOnlyList<TypeDeclaration> nestedTypes,
            JavaDoc? javaDoc) 
            : base(location, name, modifiers, annotations, new List<TypeParameter>(), javaDoc)
        {
            Members = members;
            NestedTypes = nestedTypes;
            AddChildren(members);
            AddChildren(nestedTypes);
        }

        public override IEnumerable<JavaNode> Body => Members.Cast<JavaNode>().Concat(NestedTypes);

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAnnotationDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAnnotationDeclaration(this);
    }
}