using System.Collections.Generic;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Base class for class/interface members.
    /// </summary>
    public abstract class MemberDeclaration : JavaNode
    {
        public Modifiers Modifiers { get; }
        public IReadOnlyList<Annotation> Annotations { get; }
        public JavaDoc? JavaDoc { get; }

        protected MemberDeclaration(
            SourceRange location,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            JavaDoc? javaDoc) : base(location)
        {
            Modifiers = modifiers;
            Annotations = annotations;
            JavaDoc = javaDoc;

            AddChildren(annotations);
            if (javaDoc != null) AddChild(javaDoc);
        }
    }

    /// <summary>
    /// Represents a field declaration.
    /// </summary>
    public class FieldDeclaration : MemberDeclaration
    {
        public TypeReference Type { get; }
        public IReadOnlyList<VariableDeclarator> Variables { get; }

        public FieldDeclaration(
            SourceRange location,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            TypeReference type,
            IReadOnlyList<VariableDeclarator> variables,
            JavaDoc? javaDoc) 
            : base(location, modifiers, annotations, javaDoc)
        {
            Type = type;
            Variables = variables;

            AddChild(type);
            AddChildren(variables);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitFieldDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitFieldDeclaration(this);
    }

    /// <summary>
    /// Represents a method declaration.
    /// </summary>
    public class MethodDeclaration : MemberDeclaration
    {
        public string Name { get; }
        public TypeReference? ReturnType { get; } // null for constructors
        public IReadOnlyList<TypeParameter> TypeParameters { get; }
        public IReadOnlyList<Parameter> Parameters { get; }
        public IReadOnlyList<TypeReference> Throws { get; }
        public BlockStatement? Body { get; }
        public bool IsConstructor { get; }

        public MethodDeclaration(
            SourceRange location,
            string name,
            Modifiers modifiers,
            IReadOnlyList<Annotation> annotations,
            TypeReference? returnType,
            IReadOnlyList<TypeParameter> typeParameters,
            IReadOnlyList<Parameter> parameters,
            IReadOnlyList<TypeReference> throws,
            BlockStatement? body,
            JavaDoc? javaDoc,
            bool isConstructor = false) 
            : base(location, modifiers, annotations, javaDoc)
        {
            Name = name;
            ReturnType = returnType;
            TypeParameters = typeParameters;
            Parameters = parameters;
            Throws = throws;
            Body = body;
            IsConstructor = isConstructor;

            if (returnType != null) AddChild(returnType);
            AddChildren(typeParameters);
            AddChildren(parameters);
            AddChildren(throws);
            if (body != null) AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitMethodDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitMethodDeclaration(this);
    }

    /// <summary>
    /// Represents an initializer block (static or instance).
    /// </summary>
    public class InitializerBlock : MemberDeclaration
    {
        public BlockStatement Body { get; }
        public bool IsStatic { get; }

        public InitializerBlock(
            SourceRange location,
            BlockStatement body,
            bool isStatic) 
            : base(location, isStatic ? Modifiers.Static : Modifiers.None, new List<Annotation>(), null)
        {
            Body = body;
            IsStatic = isStatic;
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitInitializerBlock(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitInitializerBlock(this);
    }

    /// <summary>
    /// Represents a variable declarator (used in fields and local variables).
    /// </summary>
    public class VariableDeclarator : JavaNode
    {
        public string Name { get; }
        public int ArrayDimensions { get; }
        public Expression? Initializer { get; }

        public VariableDeclarator(
            SourceRange location,
            string name,
            int arrayDimensions,
            Expression? initializer) : base(location)
        {
            Name = name;
            ArrayDimensions = arrayDimensions;
            Initializer = initializer;

            if (initializer != null) AddChild(initializer);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitVariableDeclarator(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitVariableDeclarator(this);
    }

    /// <summary>
    /// Represents a method parameter.
    /// </summary>
    public class Parameter : JavaNode
    {
        public TypeReference Type { get; }
        public string Name { get; }
        public bool IsVarArgs { get; }
        public bool IsFinal { get; }
        public IReadOnlyList<Annotation> Annotations { get; }

        public Parameter(
            SourceRange location,
            TypeReference type,
            string name,
            bool isVarArgs,
            bool isFinal,
            IReadOnlyList<Annotation> annotations) : base(location)
        {
            Type = type;
            Name = name;
            IsVarArgs = isVarArgs;
            IsFinal = isFinal;
            Annotations = annotations;

            AddChild(type);
            AddChildren(annotations);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitParameter(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitParameter(this);
    }

    /// <summary>
    /// Represents an enum constant.
    /// </summary>
    public class EnumConstant : JavaNode
    {
        public string Name { get; }
        public IReadOnlyList<Annotation> Annotations { get; }
        public IReadOnlyList<Expression> Arguments { get; }
        public ClassDeclaration? Body { get; }

        public EnumConstant(
            SourceRange location,
            string name,
            IReadOnlyList<Annotation> annotations,
            IReadOnlyList<Expression> arguments,
            ClassDeclaration? body) : base(location)
        {
            Name = name;
            Annotations = annotations;
            Arguments = arguments;
            Body = body;

            AddChildren(annotations);
            AddChildren(arguments);
            if (body != null) AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitEnumConstant(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitEnumConstant(this);
    }

    /// <summary>
    /// Represents an annotation member.
    /// </summary>
    public class AnnotationMember : JavaNode
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public Expression? DefaultValue { get; }

        public AnnotationMember(
            SourceRange location,
            string name,
            TypeReference type,
            Expression? defaultValue) : base(location)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;

            AddChild(type);
            if (defaultValue != null) AddChild(defaultValue);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAnnotationMember(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAnnotationMember(this);
    }
}