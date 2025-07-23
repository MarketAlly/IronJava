using System.Collections.Generic;
using System.Text;
using MarketAlly.IronJava.Core.AST.Nodes;

namespace MarketAlly.IronJava.Core.AST.Visitors
{
    /// <summary>
    /// Example visitor that collects all class names in the AST.
    /// </summary>
    public class ClassNameCollector : JavaVisitorBase
    {
        public List<string> ClassNames { get; } = new();

        public override void VisitClassDeclaration(ClassDeclaration node)
        {
            ClassNames.Add(node.Name);
            base.VisitClassDeclaration(node);
        }
    }

    /// <summary>
    /// Example visitor that counts different types of nodes.
    /// </summary>
    public class NodeCounter : JavaVisitorBase
    {
        public int ClassCount { get; private set; }
        public int InterfaceCount { get; private set; }
        public int MethodCount { get; private set; }
        public int FieldCount { get; private set; }

        public override void VisitClassDeclaration(ClassDeclaration node)
        {
            ClassCount++;
            base.VisitClassDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclaration node)
        {
            InterfaceCount++;
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclaration node)
        {
            MethodCount++;
            base.VisitMethodDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclaration node)
        {
            FieldCount++;
            base.VisitFieldDeclaration(node);
        }
    }

    /// <summary>
    /// Example visitor that pretty-prints the AST structure.
    /// </summary>
    public class PrettyPrinter : JavaVisitorBase<string>
    {
        private int _indentLevel = 0;
        private readonly StringBuilder _builder = new();

        protected override string DefaultVisit(JavaNode node)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this);
            }
            return _builder.ToString();
        }

        private void AppendLine(string text)
        {
            _builder.Append(new string(' ', _indentLevel * 2));
            _builder.AppendLine(text);
        }

        public override string VisitCompilationUnit(CompilationUnit node)
        {
            AppendLine("CompilationUnit");
            _indentLevel++;
            base.VisitCompilationUnit(node);
            _indentLevel--;
            return _builder.ToString();
        }

        public override string VisitPackageDeclaration(PackageDeclaration node)
        {
            AppendLine($"Package: {node.PackageName}");
            return base.VisitPackageDeclaration(node);
        }

        public override string VisitImportDeclaration(ImportDeclaration node)
        {
            var staticStr = node.IsStatic ? "static " : "";
            var wildcardStr = node.IsWildcard ? ".*" : "";
            AppendLine($"Import: {staticStr}{node.ImportPath}{wildcardStr}");
            return base.VisitImportDeclaration(node);
        }

        public override string VisitClassDeclaration(ClassDeclaration node)
        {
            var modifiers = node.Modifiers.ToString().ToLower();
            AppendLine($"Class: {modifiers} {node.Name}");
            _indentLevel++;
            base.VisitClassDeclaration(node);
            _indentLevel--;
            return _builder.ToString();
        }

        public override string VisitMethodDeclaration(MethodDeclaration node)
        {
            var modifiers = node.Modifiers.ToString().ToLower();
            var returnType = node.ReturnType?.ToString() ?? "void";
            AppendLine($"Method: {modifiers} {returnType} {node.Name}()");
            _indentLevel++;
            base.VisitMethodDeclaration(node);
            _indentLevel--;
            return _builder.ToString();
        }

        public override string VisitFieldDeclaration(FieldDeclaration node)
        {
            var modifiers = node.Modifiers.ToString().ToLower();
            AppendLine($"Field: {modifiers} {node.Type}");
            _indentLevel++;
            foreach (var variable in node.Variables)
            {
                AppendLine($"Variable: {variable.Name}");
            }
            _indentLevel--;
            return base.VisitFieldDeclaration(node);
        }
    }

    /// <summary>
    /// Example visitor that finds all method calls to a specific method.
    /// </summary>
    public class MethodCallFinder : JavaVisitorBase
    {
        private readonly string _methodName;
        public List<MethodCallExpression> FoundCalls { get; } = new();

        public MethodCallFinder(string methodName)
        {
            _methodName = methodName;
        }

        public override void VisitMethodCallExpression(MethodCallExpression node)
        {
            if (node.MethodName == _methodName)
            {
                FoundCalls.Add(node);
            }
            base.VisitMethodCallExpression(node);
        }
    }

    /// <summary>
    /// Example visitor that extracts all string literals.
    /// </summary>
    public class StringLiteralExtractor : JavaVisitorBase
    {
        public List<string> StringLiterals { get; } = new();

        public override void VisitLiteralExpression(LiteralExpression node)
        {
            if (node.Kind == LiteralKind.String && node.Value is string str)
            {
                StringLiterals.Add(str);
            }
            base.VisitLiteralExpression(node);
        }
    }
}