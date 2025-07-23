using System.Collections.Generic;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Base class for all expressions.
    /// </summary>
    public abstract class Expression : JavaNode
    {
        protected Expression(SourceRange location) : base(location) { }
    }

    /// <summary>
    /// Represents a literal value.
    /// </summary>
    public class LiteralExpression : Expression
    {
        public object? Value { get; }
        public LiteralKind Kind { get; }

        public LiteralExpression(SourceRange location, object? value, LiteralKind kind) 
            : base(location)
        {
            Value = value;
            Kind = kind;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitLiteralExpression(this);
    }

    public enum LiteralKind
    {
        Null,
        Boolean,
        Integer,
        Long,
        Float,
        Double,
        Character,
        String,
        TextBlock
    }

    /// <summary>
    /// Represents an identifier reference.
    /// </summary>
    public class IdentifierExpression : Expression
    {
        public string Name { get; }

        public IdentifierExpression(SourceRange location, string name) : base(location)
        {
            Name = name;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitIdentifierExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitIdentifierExpression(this);
    }

    /// <summary>
    /// Represents 'this' expression.
    /// </summary>
    public class ThisExpression : Expression
    {
        public Expression? Qualifier { get; }

        public ThisExpression(SourceRange location, Expression? qualifier = null) : base(location)
        {
            Qualifier = qualifier;
            if (qualifier != null) AddChild(qualifier);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitThisExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitThisExpression(this);
    }

    /// <summary>
    /// Represents 'super' expression.
    /// </summary>
    public class SuperExpression : Expression
    {
        public Expression? Qualifier { get; }

        public SuperExpression(SourceRange location, Expression? qualifier = null) : base(location)
        {
            Qualifier = qualifier;
            if (qualifier != null) AddChild(qualifier);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitSuperExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitSuperExpression(this);
    }

    /// <summary>
    /// Represents a binary expression (a + b, a AND b, etc.).
    /// </summary>
    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public BinaryOperator Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(
            SourceRange location,
            Expression left,
            BinaryOperator @operator,
            Expression right) : base(location)
        {
            Left = left;
            Operator = @operator;
            Right = right;

            AddChild(left);
            AddChild(right);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitBinaryExpression(this);
    }

    public enum BinaryOperator
    {
        // Arithmetic
        Add, Subtract, Multiply, Divide, Modulo,
        // Bitwise
        BitwiseAnd, BitwiseOr, BitwiseXor, LeftShift, RightShift, UnsignedRightShift,
        // Logical
        LogicalAnd, LogicalOr,
        // Comparison
        Equals, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
        // Assignment
        Assign, AddAssign, SubtractAssign, MultiplyAssign, DivideAssign, ModuloAssign,
        BitwiseAndAssign, BitwiseOrAssign, BitwiseXorAssign,
        LeftShiftAssign, RightShiftAssign, UnsignedRightShiftAssign
    }

    /// <summary>
    /// Represents a unary expression (!a, ++i, etc.).
    /// </summary>
    public class UnaryExpression : Expression
    {
        public UnaryOperator Operator { get; }
        public Expression Operand { get; }
        public bool IsPrefix { get; }

        public UnaryExpression(
            SourceRange location,
            UnaryOperator @operator,
            Expression operand,
            bool isPrefix = true) : base(location)
        {
            Operator = @operator;
            Operand = operand;
            IsPrefix = isPrefix;

            AddChild(operand);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitUnaryExpression(this);
    }

    public enum UnaryOperator
    {
        Plus, Minus, BitwiseNot, LogicalNot,
        PreIncrement, PreDecrement, PostIncrement, PostDecrement
    }

    /// <summary>
    /// Represents a conditional expression (a ? b : c).
    /// </summary>
    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; }
        public Expression ThenExpression { get; }
        public Expression ElseExpression { get; }

        public ConditionalExpression(
            SourceRange location,
            Expression condition,
            Expression thenExpression,
            Expression elseExpression) : base(location)
        {
            Condition = condition;
            ThenExpression = thenExpression;
            ElseExpression = elseExpression;

            AddChild(condition);
            AddChild(thenExpression);
            AddChild(elseExpression);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitConditionalExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitConditionalExpression(this);
    }

    /// <summary>
    /// Represents a method call expression.
    /// </summary>
    public class MethodCallExpression : Expression
    {
        public Expression? Target { get; }
        public string MethodName { get; }
        public IReadOnlyList<TypeArgument> TypeArguments { get; }
        public IReadOnlyList<Expression> Arguments { get; }

        public MethodCallExpression(
            SourceRange location,
            Expression? target,
            string methodName,
            IReadOnlyList<TypeArgument> typeArguments,
            IReadOnlyList<Expression> arguments) : base(location)
        {
            Target = target;
            MethodName = methodName;
            TypeArguments = typeArguments;
            Arguments = arguments;

            if (target != null) AddChild(target);
            AddChildren(typeArguments);
            AddChildren(arguments);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitMethodCallExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitMethodCallExpression(this);
    }

    /// <summary>
    /// Represents a field access expression.
    /// </summary>
    public class FieldAccessExpression : Expression
    {
        public Expression Target { get; }
        public string FieldName { get; }

        public FieldAccessExpression(
            SourceRange location,
            Expression target,
            string fieldName) : base(location)
        {
            Target = target;
            FieldName = fieldName;

            AddChild(target);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitFieldAccessExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitFieldAccessExpression(this);
    }

    /// <summary>
    /// Represents an array access expression.
    /// </summary>
    public class ArrayAccessExpression : Expression
    {
        public Expression Array { get; }
        public Expression Index { get; }

        public ArrayAccessExpression(
            SourceRange location,
            Expression array,
            Expression index) : base(location)
        {
            Array = array;
            Index = index;

            AddChild(array);
            AddChild(index);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitArrayAccessExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitArrayAccessExpression(this);
    }

    /// <summary>
    /// Represents a cast expression.
    /// </summary>
    public class CastExpression : Expression
    {
        public TypeReference Type { get; }
        public Expression Expression { get; }

        public CastExpression(
            SourceRange location,
            TypeReference type,
            Expression expression) : base(location)
        {
            Type = type;
            Expression = expression;

            AddChild(type);
            AddChild(expression);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitCastExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitCastExpression(this);
    }

    /// <summary>
    /// Represents an instanceof expression.
    /// </summary>
    public class InstanceOfExpression : Expression
    {
        public Expression Expression { get; }
        public TypeReference Type { get; }
        public string? PatternVariable { get; }

        public InstanceOfExpression(
            SourceRange location,
            Expression expression,
            TypeReference type,
            string? patternVariable = null) : base(location)
        {
            Expression = expression;
            Type = type;
            PatternVariable = patternVariable;

            AddChild(expression);
            AddChild(type);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitInstanceOfExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitInstanceOfExpression(this);
    }

    /// <summary>
    /// Represents a 'new' expression for object creation.
    /// </summary>
    public class NewExpression : Expression
    {
        public ClassOrInterfaceType Type { get; }
        public IReadOnlyList<Expression> Arguments { get; }
        public ClassDeclaration? AnonymousClassBody { get; }

        public NewExpression(
            SourceRange location,
            ClassOrInterfaceType type,
            IReadOnlyList<Expression> arguments,
            ClassDeclaration? anonymousClassBody = null) : base(location)
        {
            Type = type;
            Arguments = arguments;
            AnonymousClassBody = anonymousClassBody;

            AddChild(type);
            AddChildren(arguments);
            if (anonymousClassBody != null) AddChild(anonymousClassBody);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitNewExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitNewExpression(this);
    }

    /// <summary>
    /// Represents an array creation expression.
    /// </summary>
    public class NewArrayExpression : Expression
    {
        public TypeReference ElementType { get; }
        public IReadOnlyList<Expression> Dimensions { get; }
        public ArrayInitializer? Initializer { get; }

        public NewArrayExpression(
            SourceRange location,
            TypeReference elementType,
            IReadOnlyList<Expression> dimensions,
            ArrayInitializer? initializer = null) : base(location)
        {
            ElementType = elementType;
            Dimensions = dimensions;
            Initializer = initializer;

            AddChild(elementType);
            AddChildren(dimensions);
            if (initializer != null) AddChild(initializer);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitNewArrayExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitNewArrayExpression(this);
    }

    /// <summary>
    /// Represents an array initializer.
    /// </summary>
    public class ArrayInitializer : Expression
    {
        public IReadOnlyList<Expression> Elements { get; }

        public ArrayInitializer(
            SourceRange location,
            IReadOnlyList<Expression> elements) : base(location)
        {
            Elements = elements;
            AddChildren(elements);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitArrayInitializer(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitArrayInitializer(this);
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    public class LambdaExpression : Expression
    {
        public IReadOnlyList<LambdaParameter> Parameters { get; }
        public JavaNode Body { get; } // Can be Expression or BlockStatement

        public LambdaExpression(
            SourceRange location,
            IReadOnlyList<LambdaParameter> parameters,
            JavaNode body) : base(location)
        {
            Parameters = parameters;
            Body = body;

            AddChildren(parameters);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitLambdaExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitLambdaExpression(this);
    }

    /// <summary>
    /// Represents a lambda parameter.
    /// </summary>
    public class LambdaParameter : JavaNode
    {
        public string Name { get; }
        public TypeReference? Type { get; }
        public bool IsFinal { get; }

        public LambdaParameter(
            SourceRange location,
            string name,
            TypeReference? type = null,
            bool isFinal = false) : base(location)
        {
            Name = name;
            Type = type;
            IsFinal = isFinal;

            if (type != null) AddChild(type);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitLambdaParameter(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitLambdaParameter(this);
    }

    /// <summary>
    /// Represents a method reference expression (String::length).
    /// </summary>
    public class MethodReferenceExpression : Expression
    {
        public Expression Target { get; }
        public string MethodName { get; }
        public IReadOnlyList<TypeArgument> TypeArguments { get; }

        public MethodReferenceExpression(
            SourceRange location,
            Expression target,
            string methodName,
            IReadOnlyList<TypeArgument> typeArguments) : base(location)
        {
            Target = target;
            MethodName = methodName;
            TypeArguments = typeArguments;

            AddChild(target);
            AddChildren(typeArguments);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitMethodReferenceExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitMethodReferenceExpression(this);
    }

    /// <summary>
    /// Represents a class literal expression (String.class).
    /// </summary>
    public class ClassLiteralExpression : Expression
    {
        public TypeReference Type { get; }

        public ClassLiteralExpression(SourceRange location, TypeReference type) : base(location)
        {
            Type = type;
            AddChild(type);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitClassLiteralExpression(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitClassLiteralExpression(this);
    }
}