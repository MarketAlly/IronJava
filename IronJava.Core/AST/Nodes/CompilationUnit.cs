using System.Collections.Generic;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.AST.Nodes
{
    /// <summary>
    /// Represents a Java source file (compilation unit).
    /// </summary>
    public class CompilationUnit : JavaNode
    {
        public PackageDeclaration? Package { get; }
        public IReadOnlyList<ImportDeclaration> Imports { get; }
        public IReadOnlyList<TypeDeclaration> Types { get; }

        public CompilationUnit(
            SourceRange location,
            PackageDeclaration? package,
            IReadOnlyList<ImportDeclaration> imports,
            IReadOnlyList<TypeDeclaration> types) : base(location)
        {
            Package = package;
            Imports = imports;
            Types = types;

            if (package != null) AddChild(package);
            AddChildren(imports);
            AddChildren(types);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitCompilationUnit(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitCompilationUnit(this);
    }

    /// <summary>
    /// Represents a package declaration.
    /// </summary>
    public class PackageDeclaration : JavaNode
    {
        public string PackageName { get; }
        public IReadOnlyList<Annotation> Annotations { get; }
        
        /// <summary>
        /// Gets the package name. Alias for PackageName for consistency.
        /// </summary>
        public string Name => PackageName;

        public PackageDeclaration(
            SourceRange location,
            string packageName,
            IReadOnlyList<Annotation> annotations) : base(location)
        {
            PackageName = packageName;
            Annotations = annotations;
            AddChildren(annotations);
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitPackageDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitPackageDeclaration(this);
    }

    /// <summary>
    /// Represents an import declaration.
    /// </summary>
    public class ImportDeclaration : JavaNode
    {
        public string ImportPath { get; }
        public bool IsStatic { get; }
        public bool IsWildcard { get; }
        
        /// <summary>
        /// Gets the name of the imported type or package.
        /// For "import java.util.List", returns "java.util.List"
        /// For "import java.util.*", returns "java.util.*"
        /// </summary>
        public string Name => ImportPath;

        public ImportDeclaration(
            SourceRange location,
            string importPath,
            bool isStatic,
            bool isWildcard) : base(location)
        {
            ImportPath = importPath;
            IsStatic = isStatic;
            IsWildcard = isWildcard;
        }

        public override T Accept<T>(IJavaVisitor<T> visitor) => visitor.VisitImportDeclaration(this);
        public override void Accept(IJavaVisitor visitor) => visitor.VisitImportDeclaration(this);
    }
}