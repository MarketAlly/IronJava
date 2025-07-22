using System.Collections.Generic;
using IronJava.Core.AST.Visitors;

namespace IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Base class for all statements.
    /// </summary>
    public abstract class Statement : JavaNode
    {
        protected Statement(SourceRange location) : base(location) { }
    }

    /// <summary>
    /// Represents a block statement { ... }.
    /// </summary>
    public class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> Statements { get; }

        public BlockStatement(SourceRange location, IReadOnlyList<Statement> statements) 
            : base(location)
        {
            Statements = statements;
            AddChildren(statements);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitBlockStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitBlockStatement(this);
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    public class LocalVariableStatement : Statement
    {
        public TypeReference Type { get; }
        public IReadOnlyList<VariableDeclarator> Variables { get; }
        public bool IsFinal { get; }

        public LocalVariableStatement(
            SourceRange location,
            TypeReference type,
            IReadOnlyList<VariableDeclarator> variables,
            bool isFinal) : base(location)
        {
            Type = type;
            Variables = variables;
            IsFinal = isFinal;

            AddChild(type);
            AddChildren(variables);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitLocalVariableStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitLocalVariableStatement(this);
    }

    /// <summary>
    /// Represents an expression statement.
    /// </summary>
    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; }

        public ExpressionStatement(SourceRange location, Expression expression) : base(location)
        {
            Expression = expression;
            AddChild(expression);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitExpressionStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitExpressionStatement(this);
    }

    /// <summary>
    /// Represents an if statement.
    /// </summary>
    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public Statement ThenStatement { get; }
        public Statement? ElseStatement { get; }

        public IfStatement(
            SourceRange location,
            Expression condition,
            Statement thenStatement,
            Statement? elseStatement = null) : base(location)
        {
            Condition = condition;
            ThenStatement = thenStatement;
            ElseStatement = elseStatement;

            AddChild(condition);
            AddChild(thenStatement);
            if (elseStatement != null) AddChild(elseStatement);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitIfStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitIfStatement(this);
    }

    /// <summary>
    /// Represents a while loop.
    /// </summary>
    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(
            SourceRange location,
            Expression condition,
            Statement body) : base(location)
        {
            Condition = condition;
            Body = body;

            AddChild(condition);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitWhileStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitWhileStatement(this);
    }

    /// <summary>
    /// Represents a do-while loop.
    /// </summary>
    public class DoWhileStatement : Statement
    {
        public Statement Body { get; }
        public Expression Condition { get; }

        public DoWhileStatement(
            SourceRange location,
            Statement body,
            Expression condition) : base(location)
        {
            Body = body;
            Condition = condition;

            AddChild(body);
            AddChild(condition);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitDoWhileStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitDoWhileStatement(this);
    }

    /// <summary>
    /// Represents a for loop.
    /// </summary>
    public class ForStatement : Statement
    {
        public IReadOnlyList<Statement> Initializers { get; }
        public Expression? Condition { get; }
        public IReadOnlyList<Expression> Updates { get; }
        public Statement Body { get; }

        public ForStatement(
            SourceRange location,
            IReadOnlyList<Statement> initializers,
            Expression? condition,
            IReadOnlyList<Expression> updates,
            Statement body) : base(location)
        {
            Initializers = initializers;
            Condition = condition;
            Updates = updates;
            Body = body;

            AddChildren(initializers);
            if (condition != null) AddChild(condition);
            AddChildren(updates);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitForStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitForStatement(this);
    }

    /// <summary>
    /// Represents an enhanced for loop (for-each).
    /// </summary>
    public class ForEachStatement : Statement
    {
        public TypeReference VariableType { get; }
        public string VariableName { get; }
        public Expression Iterable { get; }
        public Statement Body { get; }
        public bool IsFinal { get; }

        public ForEachStatement(
            SourceRange location,
            TypeReference variableType,
            string variableName,
            Expression iterable,
            Statement body,
            bool isFinal) : base(location)
        {
            VariableType = variableType;
            VariableName = variableName;
            Iterable = iterable;
            Body = body;
            IsFinal = isFinal;

            AddChild(variableType);
            AddChild(iterable);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitForEachStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitForEachStatement(this);
    }

    /// <summary>
    /// Represents a switch statement.
    /// </summary>
    public class SwitchStatement : Statement
    {
        public Expression Selector { get; }
        public IReadOnlyList<SwitchCase> Cases { get; }

        public SwitchStatement(
            SourceRange location,
            Expression selector,
            IReadOnlyList<SwitchCase> cases) : base(location)
        {
            Selector = selector;
            Cases = cases;

            AddChild(selector);
            AddChildren(cases);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitSwitchStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitSwitchStatement(this);
    }

    /// <summary>
    /// Represents a case in a switch statement.
    /// </summary>
    public class SwitchCase : JavaNode
    {
        public IReadOnlyList<Expression> Labels { get; } // Empty for default case
        public IReadOnlyList<Statement> Statements { get; }
        public bool IsDefault { get; }

        public SwitchCase(
            SourceRange location,
            IReadOnlyList<Expression> labels,
            IReadOnlyList<Statement> statements,
            bool isDefault) : base(location)
        {
            Labels = labels;
            Statements = statements;
            IsDefault = isDefault;

            AddChildren(labels);
            AddChildren(statements);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitSwitchCase(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitSwitchCase(this);
    }

    /// <summary>
    /// Represents a break statement.
    /// </summary>
    public class BreakStatement : Statement
    {
        public string? Label { get; }

        public BreakStatement(SourceRange location, string? label = null) : base(location)
        {
            Label = label;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitBreakStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitBreakStatement(this);
    }

    /// <summary>
    /// Represents a continue statement.
    /// </summary>
    public class ContinueStatement : Statement
    {
        public string? Label { get; }

        public ContinueStatement(SourceRange location, string? label = null) : base(location)
        {
            Label = label;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitContinueStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitContinueStatement(this);
    }

    /// <summary>
    /// Represents a return statement.
    /// </summary>
    public class ReturnStatement : Statement
    {
        public Expression? Value { get; }

        public ReturnStatement(SourceRange location, Expression? value = null) : base(location)
        {
            Value = value;
            if (value != null) AddChild(value);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitReturnStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitReturnStatement(this);
    }

    /// <summary>
    /// Represents a throw statement.
    /// </summary>
    public class ThrowStatement : Statement
    {
        public Expression Exception { get; }

        public ThrowStatement(SourceRange location, Expression exception) : base(location)
        {
            Exception = exception;
            AddChild(exception);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitThrowStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitThrowStatement(this);
    }

    /// <summary>
    /// Represents a try statement.
    /// </summary>
    public class TryStatement : Statement
    {
        public IReadOnlyList<ResourceDeclaration> Resources { get; }
        public BlockStatement Body { get; }
        public IReadOnlyList<CatchClause> CatchClauses { get; }
        public BlockStatement? FinallyBlock { get; }

        public TryStatement(
            SourceRange location,
            IReadOnlyList<ResourceDeclaration> resources,
            BlockStatement body,
            IReadOnlyList<CatchClause> catchClauses,
            BlockStatement? finallyBlock = null) : base(location)
        {
            Resources = resources;
            Body = body;
            CatchClauses = catchClauses;
            FinallyBlock = finallyBlock;

            AddChildren(resources);
            AddChild(body);
            AddChildren(catchClauses);
            if (finallyBlock != null) AddChild(finallyBlock);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitTryStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitTryStatement(this);
    }

    /// <summary>
    /// Represents a resource declaration in try-with-resources.
    /// </summary>
    public class ResourceDeclaration : JavaNode
    {
        public TypeReference Type { get; }
        public string Name { get; }
        public Expression Initializer { get; }
        public bool IsFinal { get; }

        public ResourceDeclaration(
            SourceRange location,
            TypeReference type,
            string name,
            Expression initializer,
            bool isFinal) : base(location)
        {
            Type = type;
            Name = name;
            Initializer = initializer;
            IsFinal = isFinal;

            AddChild(type);
            AddChild(initializer);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitResourceDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitResourceDeclaration(this);
    }

    /// <summary>
    /// Represents a catch clause.
    /// </summary>
    public class CatchClause : JavaNode
    {
        public IReadOnlyList<TypeReference> ExceptionTypes { get; }
        public string VariableName { get; }
        public BlockStatement Body { get; }

        public CatchClause(
            SourceRange location,
            IReadOnlyList<TypeReference> exceptionTypes,
            string variableName,
            BlockStatement body) : base(location)
        {
            ExceptionTypes = exceptionTypes;
            VariableName = variableName;
            Body = body;

            AddChildren(exceptionTypes);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitCatchClause(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitCatchClause(this);
    }

    /// <summary>
    /// Represents a synchronized statement.
    /// </summary>
    public class SynchronizedStatement : Statement
    {
        public Expression Lock { get; }
        public BlockStatement Body { get; }

        public SynchronizedStatement(
            SourceRange location,
            Expression @lock,
            BlockStatement body) : base(location)
        {
            Lock = @lock;
            Body = body;

            AddChild(@lock);
            AddChild(body);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitSynchronizedStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitSynchronizedStatement(this);
    }

    /// <summary>
    /// Represents a labeled statement.
    /// </summary>
    public class LabeledStatement : Statement
    {
        public string Label { get; }
        public Statement Statement { get; }

        public LabeledStatement(
            SourceRange location,
            string label,
            Statement statement) : base(location)
        {
            Label = label;
            Statement = statement;

            AddChild(statement);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitLabeledStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitLabeledStatement(this);
    }

    /// <summary>
    /// Represents an empty statement (;).
    /// </summary>
    public class EmptyStatement : Statement
    {
        public EmptyStatement(SourceRange location) : base(location) { }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitEmptyStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitEmptyStatement(this);
    }

    /// <summary>
    /// Represents an assert statement.
    /// </summary>
    public class AssertStatement : Statement
    {
        public Expression Condition { get; }
        public Expression? Message { get; }

        public AssertStatement(
            SourceRange location,
            Expression condition,
            Expression? message = null) : base(location)
        {
            Condition = condition;
            Message = message;

            AddChild(condition);
            if (message != null) AddChild(message);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitAssertStatement(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitAssertStatement(this);
    }
}