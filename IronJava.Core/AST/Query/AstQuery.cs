using System;
using System.Collections.Generic;
using System.Linq;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Query
{
    /// <summary>
    /// Provides LINQ-style querying capabilities for AST nodes.
    /// </summary>
    public static class AstQuery
    {
        /// <summary>
        /// Find all nodes of a specific type in the AST.
        /// </summary>
        public static IEnumerable<T> FindAll<T>(this JavaNode root) where T : JavaNode
        {
            var finder = new TypeFinder<T>();
            root.Accept(finder);
            return finder.Results;
        }

        /// <summary>
        /// Find the first node of a specific type, or null if not found.
        /// </summary>
        public static T? FindFirst<T>(this JavaNode root) where T : JavaNode
        {
            return root.FindAll<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find all nodes matching a predicate.
        /// </summary>
        public static IEnumerable<T> Where<T>(this JavaNode root, Func<T, bool> predicate) where T : JavaNode
        {
            return root.FindAll<T>().Where(predicate);
        }

        /// <summary>
        /// Find the parent of a specific type.
        /// </summary>
        public static T? FindParent<T>(this JavaNode node) where T : JavaNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is T typedParent)
                {
                    return typedParent;
                }
                current = current.Parent;
            }
            return null;
        }

        /// <summary>
        /// Get all ancestors of a node.
        /// </summary>
        public static IEnumerable<JavaNode> Ancestors(this JavaNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        /// <summary>
        /// Get all descendants of a node.
        /// </summary>
        public static IEnumerable<JavaNode> Descendants(this JavaNode node)
        {
            var collector = new DescendantCollector();
            node.Accept(collector);
            return collector.Results.Skip(1); // Skip the root node itself
        }

        /// <summary>
        /// Check if a node contains another node.
        /// </summary>
        public static bool Contains(this JavaNode ancestor, JavaNode descendant)
        {
            return descendant.Ancestors().Contains(ancestor);
        }

        /// <summary>
        /// Get the depth of a node in the tree.
        /// </summary>
        public static int Depth(this JavaNode node)
        {
            return node.Ancestors().Count();
        }

        /// <summary>
        /// Find all method calls to a specific method name.
        /// </summary>
        public static IEnumerable<MethodCallExpression> FindMethodCalls(this JavaNode root, string methodName)
        {
            return root.FindAll<MethodCallExpression>()
                       .Where(m => m.MethodName == methodName);
        }

        /// <summary>
        /// Find all references to a specific type.
        /// </summary>
        public static IEnumerable<ClassOrInterfaceType> FindTypeReferences(this JavaNode root, string typeName)
        {
            return root.FindAll<ClassOrInterfaceType>()
                       .Where(t => t.Name == typeName || t.FullName == typeName);
        }

        /// <summary>
        /// Find all fields with a specific type.
        /// </summary>
        public static IEnumerable<FieldDeclaration> FindFieldsOfType(this JavaNode root, string typeName)
        {
            return root.FindAll<FieldDeclaration>()
                       .Where(f => f.Type is ClassOrInterfaceType type && 
                                  (type.Name == typeName || type.FullName == typeName));
        }

        /// <summary>
        /// Get the enclosing class of a node.
        /// </summary>
        public static ClassDeclaration? GetEnclosingClass(this JavaNode node)
        {
            return node.FindParent<ClassDeclaration>();
        }

        /// <summary>
        /// Get the enclosing method of a node.
        /// </summary>
        public static MethodDeclaration? GetEnclosingMethod(this JavaNode node)
        {
            return node.FindParent<MethodDeclaration>();
        }

        /// <summary>
        /// Check if a node is within a static context.
        /// </summary>
        public static bool IsInStaticContext(this JavaNode node)
        {
            var method = node.GetEnclosingMethod();
            if (method?.Modifiers.IsStatic() == true)
                return true;

            var field = node.FindParent<FieldDeclaration>();
            if (field?.Modifiers.IsStatic() == true)
                return true;

            var initializer = node.FindParent<InitializerBlock>();
            return initializer?.IsStatic == true;
        }

        private class TypeFinder<T> : JavaVisitorBase where T : JavaNode
        {
            public List<T> Results { get; } = new();

            protected override void DefaultVisit(JavaNode node)
            {
                if (node is T typedNode)
                {
                    Results.Add(typedNode);
                }
                base.DefaultVisit(node);
            }
        }

        private class DescendantCollector : JavaVisitorBase
        {
            public List<JavaNode> Results { get; } = new();

            protected override void DefaultVisit(JavaNode node)
            {
                Results.Add(node);
                base.DefaultVisit(node);
            }
        }
    }

    /// <summary>
    /// Fluent query builder for complex AST queries.
    /// </summary>
    public class AstQueryBuilder<T> where T : JavaNode
    {
        private readonly JavaNode _root;
        private readonly List<Func<T, bool>> _predicates = new();

        public AstQueryBuilder(JavaNode root)
        {
            _root = root;
        }

        public AstQueryBuilder<T> Where(Func<T, bool> predicate)
        {
            _predicates.Add(predicate);
            return this;
        }

        public AstQueryBuilder<T> WithModifier(Modifiers modifier)
        {
            _predicates.Add(node =>
            {
                if (node is TypeDeclaration type)
                    return type.Modifiers.HasFlag(modifier);
                if (node is MemberDeclaration member)
                    return member.Modifiers.HasFlag(modifier);
                return false;
            });
            return this;
        }

        public AstQueryBuilder<T> WithName(string name)
        {
            _predicates.Add(node =>
            {
                return node switch
                {
                    TypeDeclaration type => type.Name == name,
                    MethodDeclaration method => method.Name == name,
                    VariableDeclarator variable => variable.Name == name,
                    Parameter parameter => parameter.Name == name,
                    IdentifierExpression identifier => identifier.Name == name,
                    _ => false
                };
            });
            return this;
        }

        public AstQueryBuilder<T> InClass(string className)
        {
            _predicates.Add(node =>
            {
                var enclosingClass = node.GetEnclosingClass();
                return enclosingClass?.Name == className;
            });
            return this;
        }

        public AstQueryBuilder<T> InMethod(string methodName)
        {
            _predicates.Add(node =>
            {
                var enclosingMethod = node.GetEnclosingMethod();
                return enclosingMethod?.Name == methodName;
            });
            return this;
        }

        public IEnumerable<T> Execute()
        {
            var results = _root.FindAll<T>();
            
            foreach (var predicate in _predicates)
            {
                results = results.Where(predicate);
            }
            
            return results;
        }

        public T? ExecuteFirst()
        {
            return Execute().FirstOrDefault();
        }

        public int Count()
        {
            return Execute().Count();
        }

        public bool Any()
        {
            return Execute().Any();
        }
    }

    /// <summary>
    /// Extension methods for creating query builders.
    /// </summary>
    public static class AstQueryBuilderExtensions
    {
        public static AstQueryBuilder<T> Query<T>(this JavaNode root) where T : JavaNode
        {
            return new AstQueryBuilder<T>(root);
        }

        public static AstQueryBuilder<ClassDeclaration> QueryClasses(this JavaNode root)
        {
            return new AstQueryBuilder<ClassDeclaration>(root);
        }

        public static AstQueryBuilder<MethodDeclaration> QueryMethods(this JavaNode root)
        {
            return new AstQueryBuilder<MethodDeclaration>(root);
        }

        public static AstQueryBuilder<FieldDeclaration> QueryFields(this JavaNode root)
        {
            return new AstQueryBuilder<FieldDeclaration>(root);
        }
    }

    /// <summary>
    /// Pattern matching for AST nodes.
    /// </summary>
    public static class AstPattern
    {
        /// <summary>
        /// Match getter methods (methods starting with "get" that return a value and have no parameters).
        /// </summary>
        public static bool IsGetter(this MethodDeclaration method)
        {
            return method.Name.StartsWith("get", StringComparison.Ordinal) &&
                   method.ReturnType != null &&
                   !method.Parameters.Any() &&
                   !method.Modifiers.IsStatic();
        }

        /// <summary>
        /// Match setter methods (methods starting with "set" that return void and have one parameter).
        /// </summary>
        public static bool IsSetter(this MethodDeclaration method)
        {
            return method.Name.StartsWith("set", StringComparison.Ordinal) &&
                   method.ReturnType is PrimitiveType pt && pt.Kind == PrimitiveTypeKind.Void &&
                   method.Parameters.Count == 1 &&
                   !method.Modifiers.IsStatic();
        }

        /// <summary>
        /// Match main method pattern.
        /// </summary>
        public static bool IsMainMethod(this MethodDeclaration method)
        {
            return method.Name == "main" &&
                   method.Modifiers.IsPublic() &&
                   method.Modifiers.IsStatic() &&
                   method.ReturnType is PrimitiveType pt && pt.Kind == PrimitiveTypeKind.Void &&
                   method.Parameters.Count == 1 &&
                   method.Parameters[0].Type is ArrayType arrayType &&
                   arrayType.ElementType is ClassOrInterfaceType classType &&
                   classType.Name == "String";
        }

        /// <summary>
        /// Match constructor pattern.
        /// </summary>
        public static bool IsConstructor(this MethodDeclaration method)
        {
            return method.IsConstructor;
        }

        /// <summary>
        /// Match singleton pattern (private constructor, static instance field).
        /// </summary>
        public static bool IsSingletonClass(this ClassDeclaration cls)
        {
            var hasPrivateConstructor = cls.Members
                .OfType<MethodDeclaration>()
                .Any(m => m.IsConstructor && m.Modifiers.IsPrivate());

            var hasStaticInstanceField = cls.Members
                .OfType<FieldDeclaration>()
                .Any(f => f.Modifiers.IsStatic() && 
                         f.Type is ClassOrInterfaceType type && 
                         type.Name == cls.Name);

            return hasPrivateConstructor && hasStaticInstanceField;
        }
    }
}