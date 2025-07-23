using System;
using System.Collections.Generic;
using System.Linq;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Transformation
{
    /// <summary>
    /// Base class for AST transformations that create modified copies of nodes.
    /// </summary>
    public abstract class AstTransformer : JavaVisitorBase<JavaNode?>
    {
        protected override JavaNode? DefaultVisit(JavaNode node)
        {
            // By default, return null to indicate no transformation
            return null;
        }

        /// <summary>
        /// Transform a list of nodes, filtering out nulls.
        /// </summary>
        protected List<T> TransformList<T>(IReadOnlyList<T> nodes) where T : JavaNode
        {
            return nodes
                .Select(n => n.Accept(this) as T)
                .Where(n => n != null)
                .Cast<T>()
                .ToList();
        }

        /// <summary>
        /// Transform a node or return the original if no transformation.
        /// </summary>
        protected T TransformOrOriginal<T>(T node) where T : JavaNode
        {
            var transformed = node.Accept(this) as T;
            return transformed ?? node;
        }

        /// <summary>
        /// Transform an optional node.
        /// </summary>
        protected T? TransformOptional<T>(T? node) where T : JavaNode
        {
            if (node == null) return null;
            return node.Accept(this) as T ?? node;
        }
    }

    /// <summary>
    /// Transformer that renames all occurrences of a specific identifier.
    /// </summary>
    public class IdentifierRenamer : AstTransformer
    {
        private readonly string _oldName;
        private readonly string _newName;

        public IdentifierRenamer(string oldName, string newName)
        {
            _oldName = oldName;
            _newName = newName;
        }

        public override JavaNode? VisitIdentifierExpression(IdentifierExpression node)
        {
            if (node.Name == _oldName)
            {
                return new IdentifierExpression(node.Location, _newName);
            }
            return null;
        }

        public override JavaNode? VisitFieldDeclaration(FieldDeclaration node)
        {
            var hasChanges = false;
            var transformedVariables = node.Variables
                .Select(v => {
                    if (v.Name == _oldName) {
                        hasChanges = true;
                        return new VariableDeclarator(v.Location, _newName, v.ArrayDimensions, TransformOptional(v.Initializer));
                    }
                    return TransformOrOriginal(v);
                })
                .ToList();

            var transformedType = TransformOrOriginal(node.Type);
            if (transformedType != node.Type) hasChanges = true;

            if (!hasChanges) return null;

            return new FieldDeclaration(
                node.Location,
                node.Modifiers,
                node.Annotations,
                transformedType,
                transformedVariables,
                node.JavaDoc
            );
        }

        public override JavaNode? VisitMethodDeclaration(MethodDeclaration node)
        {
            var needsTransform = node.Name == _oldName || 
                                node.Parameters.Any(p => p.Name == _oldName);

            if (!needsTransform) return null;

            var newName = node.Name == _oldName ? _newName : node.Name;
            var transformedParams = node.Parameters
                .Select(p => p.Name == _oldName
                    ? new Parameter(p.Location, TransformOrOriginal(p.Type), _newName, p.IsVarArgs, p.IsFinal, p.Annotations)
                    : TransformOrOriginal(p))
                .ToList();

            return new MethodDeclaration(
                node.Location,
                newName,
                node.Modifiers,
                node.Annotations,
                TransformOptional(node.ReturnType),
                node.TypeParameters,
                transformedParams,
                node.Throws,
                TransformOptional(node.Body),
                node.JavaDoc,
                node.IsConstructor
            );
        }

        public override JavaNode? VisitClassDeclaration(ClassDeclaration node)
        {
            var transformedMembers = node.Members
                .Select(m => TransformOrOriginal(m))
                .Cast<MemberDeclaration>()
                .ToList();

            if (transformedMembers.SequenceEqual(node.Members))
            {
                return null;
            }

            return new ClassDeclaration(
                node.Location,
                node.Name,
                node.Modifiers,
                node.Annotations,
                node.TypeParameters,
                node.SuperClass,
                node.Interfaces,
                transformedMembers,
                node.JavaDoc,
                node.IsRecord
            );
        }
    }

    /// <summary>
    /// Transformer that adds or removes modifiers from declarations.
    /// </summary>
    public class ModifierTransformer : AstTransformer
    {
        private readonly Func<Modifiers, Modifiers> _transform;

        public ModifierTransformer(Func<Modifiers, Modifiers> transform)
        {
            _transform = transform;
        }

        public static ModifierTransformer AddModifier(Modifiers modifier)
        {
            return new ModifierTransformer(m => m | modifier);
        }

        public static ModifierTransformer RemoveModifier(Modifiers modifier)
        {
            return new ModifierTransformer(m => m & ~modifier);
        }

        public override JavaNode? VisitClassDeclaration(ClassDeclaration node)
        {
            var newModifiers = _transform(node.Modifiers);
            var transformedMembers = node.Members
                .Select(m => TransformOrOriginal(m))
                .Cast<MemberDeclaration>()
                .ToList();

            // Only create new node if something changed
            if (newModifiers == node.Modifiers && 
                transformedMembers.SequenceEqual(node.Members))
            {
                return null;
            }

            return new ClassDeclaration(
                node.Location,
                node.Name,
                newModifiers,
                node.Annotations,
                node.TypeParameters,
                node.SuperClass,
                node.Interfaces,
                transformedMembers,
                node.JavaDoc,
                node.IsRecord
            );
        }

        public override JavaNode? VisitMethodDeclaration(MethodDeclaration node)
        {
            var newModifiers = _transform(node.Modifiers);
            if (newModifiers == node.Modifiers) return null;

            return new MethodDeclaration(
                node.Location,
                node.Name,
                newModifiers,
                node.Annotations,
                node.ReturnType,
                node.TypeParameters,
                node.Parameters,
                node.Throws,
                node.Body,
                node.JavaDoc,
                node.IsConstructor
            );
        }

        public override JavaNode? VisitFieldDeclaration(FieldDeclaration node)
        {
            var newModifiers = _transform(node.Modifiers);
            if (newModifiers == node.Modifiers) return null;

            return new FieldDeclaration(
                node.Location,
                newModifiers,
                node.Annotations,
                node.Type,
                node.Variables,
                node.JavaDoc
            );
        }

        public override JavaNode? VisitCompilationUnit(CompilationUnit node)
        {
            var transformedTypes = node.Types
                .Select(t => TransformOrOriginal(t))
                .Cast<TypeDeclaration>()
                .ToList();

            if (transformedTypes.SequenceEqual(node.Types))
            {
                return null;
            }

            return new CompilationUnit(
                node.Location,
                node.Package,
                node.Imports,
                transformedTypes
            );
        }
    }

    /// <summary>
    /// Transformer that removes nodes based on a predicate.
    /// </summary>
    public class NodeRemover : AstTransformer
    {
        private readonly Func<JavaNode, bool> _shouldRemove;
        private static readonly JavaNode RemovedMarker = new RemovedNode();

        public NodeRemover(Func<JavaNode, bool> shouldRemove)
        {
            _shouldRemove = shouldRemove;
        }

        protected override JavaNode? DefaultVisit(JavaNode node)
        {
            if (_shouldRemove(node))
            {
                return RemovedMarker;
            }
            return null;
        }

        public override JavaNode? VisitCompilationUnit(CompilationUnit node)
        {
            var transformedTypes = node.Types
                .Select(t => t.Accept(this) ?? t)
                .Where(t => t != RemovedMarker)
                .Cast<TypeDeclaration>()
                .ToList();

            var transformedImports = node.Imports
                .Select(i => i.Accept(this) ?? i)
                .Where(i => i != RemovedMarker)
                .Cast<ImportDeclaration>()
                .ToList();

            if (transformedTypes.Count == node.Types.Count && 
                transformedImports.Count == node.Imports.Count)
            {
                return null;
            }

            return new CompilationUnit(
                node.Location,
                node.Package,
                transformedImports,
                transformedTypes
            );
        }

        public override JavaNode? VisitClassDeclaration(ClassDeclaration node)
        {
            if (_shouldRemove(node)) return RemovedMarker;

            var transformedMembers = node.Members
                .Select(m => m.Accept(this) ?? m)
                .Where(m => m != RemovedMarker)
                .Cast<MemberDeclaration>()
                .ToList();

            if (transformedMembers.Count == node.Members.Count)
            {
                return null;
            }

            return new ClassDeclaration(
                node.Location,
                node.Name,
                node.Modifiers,
                node.Annotations,
                node.TypeParameters,
                node.SuperClass,
                node.Interfaces,
                transformedMembers,
                node.JavaDoc,
                node.IsRecord
            );
        }

        private class RemovedNode : JavaNode
        {
            public RemovedNode() : base(new SourceRange(
                new SourceLocation(0, 0, 0, 0),
                new SourceLocation(0, 0, 0, 0))) { }

            public override T Accept<T>(IJavaVisitor<T> visitor) => default!;
            public override void Accept(IJavaVisitor visitor) { }
        }
    }

    /// <summary>
    /// Builder for creating complex transformations.
    /// </summary>
    public class TransformationBuilder
    {
        private readonly List<AstTransformer> _transformers = new();

        public TransformationBuilder AddTransformer(AstTransformer transformer)
        {
            _transformers.Add(transformer);
            return this;
        }

        public TransformationBuilder RenameIdentifier(string oldName, string newName)
        {
            _transformers.Add(new IdentifierRenamer(oldName, newName));
            return this;
        }

        public TransformationBuilder AddModifier(Modifiers modifier)
        {
            _transformers.Add(ModifierTransformer.AddModifier(modifier));
            return this;
        }

        public TransformationBuilder RemoveModifier(Modifiers modifier)
        {
            _transformers.Add(ModifierTransformer.RemoveModifier(modifier));
            return this;
        }

        public TransformationBuilder RemoveNodes(Func<JavaNode, bool> predicate)
        {
            _transformers.Add(new NodeRemover(predicate));
            return this;
        }

        public JavaNode Transform(JavaNode node)
        {
            var current = node;
            foreach (var transformer in _transformers)
            {
                var transformed = current.Accept(transformer);
                if (transformed != null)
                {
                    current = transformed;
                }
            }
            return current;
        }
    }
}