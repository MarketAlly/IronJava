using System;
using System.Collections.Generic;
using System.Linq;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Comparison
{
    /// <summary>
    /// Provides equality comparison for AST nodes.
    /// </summary>
    public class AstEqualityComparer : IEqualityComparer<JavaNode>
    {
        private readonly bool _ignoreLocation;
        private readonly bool _ignoreJavaDoc;
        private readonly bool _ignoreAnnotations;

        public AstEqualityComparer(
            bool ignoreLocation = true,
            bool ignoreJavaDoc = false,
            bool ignoreAnnotations = false)
        {
            _ignoreLocation = ignoreLocation;
            _ignoreJavaDoc = ignoreJavaDoc;
            _ignoreAnnotations = ignoreAnnotations;
        }

        public bool Equals(JavaNode? x, JavaNode? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            if (x.GetType() != y.GetType()) return false;

            var comparator = new NodeComparator(_ignoreLocation, _ignoreJavaDoc, _ignoreAnnotations);
            return comparator.Compare(x, y);
        }

        public int GetHashCode(JavaNode obj)
        {
            if (obj == null) return 0;

            var hasher = new NodeHasher(_ignoreLocation, _ignoreJavaDoc, _ignoreAnnotations);
            return hasher.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Internal visitor for comparing nodes.
    /// </summary>
    internal class NodeComparator : JavaVisitorBase<bool>
    {
        private readonly bool _ignoreLocation;
        private readonly bool _ignoreJavaDoc;
        private readonly bool _ignoreAnnotations;
        private JavaNode? _other;

        public NodeComparator(bool ignoreLocation, bool ignoreJavaDoc, bool ignoreAnnotations)
        {
            _ignoreLocation = ignoreLocation;
            _ignoreJavaDoc = ignoreJavaDoc;
            _ignoreAnnotations = ignoreAnnotations;
        }

        public bool Compare(JavaNode x, JavaNode y)
        {
            _other = y;
            return x.Accept(this);
        }

        protected override bool DefaultVisit(JavaNode node)
        {
            return false;
        }

        private bool CompareLocation(SourceRange x, SourceRange y)
        {
            if (_ignoreLocation) return true;
            return x.Start.Line == y.Start.Line &&
                   x.Start.Column == y.Start.Column &&
                   x.End.Line == y.End.Line &&
                   x.End.Column == y.End.Column;
        }

        private bool CompareAnnotations(IReadOnlyList<Annotation> x, IReadOnlyList<Annotation> y)
        {
            if (_ignoreAnnotations) return true;
            if (x.Count != y.Count) return false;
            
            for (int i = 0; i < x.Count; i++)
            {
                var otherSaved = _other;
                _other = y[i];
                if (!x[i].Accept(this))
                {
                    _other = otherSaved;
                    return false;
                }
                _other = otherSaved;
            }
            return true;
        }

        private bool CompareLists<T>(IReadOnlyList<T> x, IReadOnlyList<T> y) where T : JavaNode
        {
            if (x.Count != y.Count) return false;
            
            for (int i = 0; i < x.Count; i++)
            {
                var otherSaved = _other;
                _other = y[i];
                if (!x[i].Accept(this))
                {
                    _other = otherSaved;
                    return false;
                }
                _other = otherSaved;
            }
            return true;
        }

        public override bool VisitCompilationUnit(CompilationUnit node)
        {
            if (_other is not CompilationUnit other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            if (node.Package != null && other.Package != null)
            {
                var otherSaved = _other;
                _other = other.Package;
                if (!node.Package.Accept(this))
                {
                    _other = otherSaved;
                    return false;
                }
                _other = otherSaved;
            }
            else if (node.Package != null || other.Package != null)
            {
                return false;
            }

            return CompareLists(node.Imports, other.Imports) &&
                   CompareLists(node.Types, other.Types);
        }

        public override bool VisitClassDeclaration(ClassDeclaration node)
        {
            if (_other is not ClassDeclaration other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            return node.Name == other.Name &&
                   node.Modifiers == other.Modifiers &&
                   node.IsRecord == other.IsRecord &&
                   CompareAnnotations(node.Annotations, other.Annotations) &&
                   CompareLists(node.TypeParameters, other.TypeParameters) &&
                   CompareLists(node.Interfaces, other.Interfaces) &&
                   CompareLists(node.Members, other.Members);
        }

        public override bool VisitMethodDeclaration(MethodDeclaration node)
        {
            if (_other is not MethodDeclaration other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            return node.Name == other.Name &&
                   node.Modifiers == other.Modifiers &&
                   node.IsConstructor == other.IsConstructor &&
                   CompareAnnotations(node.Annotations, other.Annotations) &&
                   CompareLists(node.TypeParameters, other.TypeParameters) &&
                   CompareLists(node.Parameters, other.Parameters) &&
                   CompareLists(node.Throws, other.Throws);
        }

        public override bool VisitFieldDeclaration(FieldDeclaration node)
        {
            if (_other is not FieldDeclaration other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            return node.Modifiers == other.Modifiers &&
                   CompareAnnotations(node.Annotations, other.Annotations) &&
                   CompareLists(node.Variables, other.Variables);
        }

        public override bool VisitIdentifierExpression(IdentifierExpression node)
        {
            if (_other is not IdentifierExpression other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            return node.Name == other.Name;
        }

        public override bool VisitLiteralExpression(LiteralExpression node)
        {
            if (_other is not LiteralExpression other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            return node.Kind == other.Kind &&
                   Equals(node.Value, other.Value);
        }

        public override bool VisitBinaryExpression(BinaryExpression node)
        {
            if (_other is not BinaryExpression other) return false;
            if (!CompareLocation(node.Location, other.Location)) return false;

            if (node.Operator != other.Operator) return false;

            var otherSaved = _other;
            _other = other.Left;
            if (!node.Left.Accept(this))
            {
                _other = otherSaved;
                return false;
            }

            _other = other.Right;
            var result = node.Right.Accept(this);
            _other = otherSaved;
            return result;
        }

        // Additional visit methods would follow the same pattern...
    }

    /// <summary>
    /// Internal visitor for computing hash codes.
    /// </summary>
    internal class NodeHasher : JavaVisitorBase<int>
    {
        private readonly bool _ignoreLocation;
        private readonly bool _ignoreJavaDoc;
        private readonly bool _ignoreAnnotations;

        public NodeHasher(bool ignoreLocation, bool ignoreJavaDoc, bool ignoreAnnotations)
        {
            _ignoreLocation = ignoreLocation;
            _ignoreJavaDoc = ignoreJavaDoc;
            _ignoreAnnotations = ignoreAnnotations;
        }

        public int GetHashCode(JavaNode node)
        {
            return node.Accept(this);
        }

        protected override int DefaultVisit(JavaNode node)
        {
            return node.GetType().GetHashCode();
        }

        private int CombineHashCodes(params int[] hashCodes)
        {
            unchecked
            {
                int hash = 17;
                foreach (var hashCode in hashCodes)
                {
                    hash = hash * 31 + hashCode;
                }
                return hash;
            }
        }

        public override int VisitClassDeclaration(ClassDeclaration node)
        {
            return CombineHashCodes(
                node.GetType().GetHashCode(),
                node.Name.GetHashCode(),
                node.Modifiers.GetHashCode(),
                node.IsRecord.GetHashCode()
            );
        }

        public override int VisitMethodDeclaration(MethodDeclaration node)
        {
            return CombineHashCodes(
                node.GetType().GetHashCode(),
                node.Name.GetHashCode(),
                node.Modifiers.GetHashCode(),
                node.IsConstructor.GetHashCode()
            );
        }

        public override int VisitIdentifierExpression(IdentifierExpression node)
        {
            return CombineHashCodes(
                node.GetType().GetHashCode(),
                node.Name.GetHashCode()
            );
        }

        public override int VisitLiteralExpression(LiteralExpression node)
        {
            return CombineHashCodes(
                node.GetType().GetHashCode(),
                node.Kind.GetHashCode(),
                node.Value?.GetHashCode() ?? 0
            );
        }
    }

    /// <summary>
    /// Computes differences between two AST nodes.
    /// </summary>
    public class AstDiffer
    {
        private readonly AstEqualityComparer _comparer;

        public AstDiffer(bool ignoreLocation = true, bool ignoreJavaDoc = false, bool ignoreAnnotations = false)
        {
            _comparer = new AstEqualityComparer(ignoreLocation, ignoreJavaDoc, ignoreAnnotations);
        }

        public AstDiff ComputeDiff(JavaNode original, JavaNode modified)
        {
            var diff = new AstDiff();
            ComputeDiffRecursive(original, modified, diff);
            return diff;
        }

        private void ComputeDiffRecursive(JavaNode? original, JavaNode? modified, AstDiff diff)
        {
            if (original == null && modified == null) return;

            if (original == null)
            {
                diff.AddAddition(modified!);
                return;
            }

            if (modified == null)
            {
                diff.AddDeletion(original);
                return;
            }

            if (!_comparer.Equals(original, modified))
            {
                if (original.GetType() != modified.GetType())
                {
                    diff.AddDeletion(original);
                    diff.AddAddition(modified);
                }
                else
                {
                    diff.AddModification(original, modified);
                }
            }

            // Compare children
            var originalChildren = original.Children.ToList();
            var modifiedChildren = modified.Children.ToList();

            // Simple comparison - could be improved with LCS algorithm
            int minCount = Math.Min(originalChildren.Count, modifiedChildren.Count);
            
            for (int i = 0; i < minCount; i++)
            {
                ComputeDiffRecursive(originalChildren[i], modifiedChildren[i], diff);
            }

            for (int i = minCount; i < originalChildren.Count; i++)
            {
                diff.AddDeletion(originalChildren[i]);
            }

            for (int i = minCount; i < modifiedChildren.Count; i++)
            {
                diff.AddAddition(modifiedChildren[i]);
            }
        }
    }

    /// <summary>
    /// Represents differences between two AST nodes.
    /// </summary>
    public class AstDiff
    {
        private readonly List<DiffEntry> _entries = new();

        public IReadOnlyList<DiffEntry> Entries => _entries;

        public IEnumerable<DiffEntry> Additions => _entries.Where(e => e.Type == DiffType.Added);
        public IEnumerable<DiffEntry> Deletions => _entries.Where(e => e.Type == DiffType.Deleted);
        public IEnumerable<DiffEntry> Modifications => _entries.Where(e => e.Type == DiffType.Modified);

        internal void AddAddition(JavaNode node)
        {
            _entries.Add(new DiffEntry(DiffType.Added, null, node));
        }

        internal void AddDeletion(JavaNode node)
        {
            _entries.Add(new DiffEntry(DiffType.Deleted, node, null));
        }

        internal void AddModification(JavaNode original, JavaNode modified)
        {
            _entries.Add(new DiffEntry(DiffType.Modified, original, modified));
        }

        public bool IsEmpty => _entries.Count == 0;

        public int TotalChanges => _entries.Count;
    }

    public class DiffEntry
    {
        public DiffType Type { get; }
        public JavaNode? Original { get; }
        public JavaNode? Modified { get; }

        public DiffEntry(DiffType type, JavaNode? original, JavaNode? modified)
        {
            Type = type;
            Original = original;
            Modified = modified;
        }
    }

    public enum DiffType
    {
        Added,
        Deleted,
        Modified
    }
}