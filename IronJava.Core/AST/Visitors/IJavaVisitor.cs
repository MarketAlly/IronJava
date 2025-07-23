using MarketAlly.IronJava.Core.AST.Nodes;

namespace MarketAlly.IronJava.Core.AST.Visitors
{
    /// <summary>
    /// Visitor interface for traversing Java AST nodes without returning values.
    /// </summary>
    public interface IJavaVisitor
    {
        // Compilation Unit
        void VisitCompilationUnit(CompilationUnit node);
        void VisitPackageDeclaration(PackageDeclaration node);
        void VisitImportDeclaration(ImportDeclaration node);

        // Type Declarations
        void VisitClassDeclaration(ClassDeclaration node);
        void VisitInterfaceDeclaration(InterfaceDeclaration node);
        void VisitEnumDeclaration(EnumDeclaration node);
        void VisitAnnotationDeclaration(AnnotationDeclaration node);

        // Members
        void VisitFieldDeclaration(FieldDeclaration node);
        void VisitMethodDeclaration(MethodDeclaration node);
        void VisitInitializerBlock(InitializerBlock node);
        void VisitVariableDeclarator(VariableDeclarator node);
        void VisitParameter(Parameter node);
        void VisitEnumConstant(EnumConstant node);
        void VisitAnnotationMember(AnnotationMember node);

        // Types
        void VisitPrimitiveType(PrimitiveType node);
        void VisitClassOrInterfaceType(ClassOrInterfaceType node);
        void VisitArrayType(ArrayType node);
        void VisitTypeParameter(TypeParameter node);
        void VisitTypeArgumentType(TypeArgumentType node);
        void VisitWildcardType(WildcardType node);

        // Expressions
        void VisitLiteralExpression(LiteralExpression node);
        void VisitIdentifierExpression(IdentifierExpression node);
        void VisitThisExpression(ThisExpression node);
        void VisitSuperExpression(SuperExpression node);
        void VisitBinaryExpression(BinaryExpression node);
        void VisitUnaryExpression(UnaryExpression node);
        void VisitConditionalExpression(ConditionalExpression node);
        void VisitMethodCallExpression(MethodCallExpression node);
        void VisitFieldAccessExpression(FieldAccessExpression node);
        void VisitArrayAccessExpression(ArrayAccessExpression node);
        void VisitCastExpression(CastExpression node);
        void VisitInstanceOfExpression(InstanceOfExpression node);
        void VisitNewExpression(NewExpression node);
        void VisitNewArrayExpression(NewArrayExpression node);
        void VisitArrayInitializer(ArrayInitializer node);
        void VisitLambdaExpression(LambdaExpression node);
        void VisitLambdaParameter(LambdaParameter node);
        void VisitMethodReferenceExpression(MethodReferenceExpression node);
        void VisitClassLiteralExpression(ClassLiteralExpression node);

        // Statements
        void VisitBlockStatement(BlockStatement node);
        void VisitLocalVariableStatement(LocalVariableStatement node);
        void VisitExpressionStatement(ExpressionStatement node);
        void VisitIfStatement(IfStatement node);
        void VisitWhileStatement(WhileStatement node);
        void VisitDoWhileStatement(DoWhileStatement node);
        void VisitForStatement(ForStatement node);
        void VisitForEachStatement(ForEachStatement node);
        void VisitSwitchStatement(SwitchStatement node);
        void VisitSwitchCase(SwitchCase node);
        void VisitBreakStatement(BreakStatement node);
        void VisitContinueStatement(ContinueStatement node);
        void VisitReturnStatement(ReturnStatement node);
        void VisitThrowStatement(ThrowStatement node);
        void VisitTryStatement(TryStatement node);
        void VisitResourceDeclaration(ResourceDeclaration node);
        void VisitCatchClause(CatchClause node);
        void VisitSynchronizedStatement(SynchronizedStatement node);
        void VisitLabeledStatement(LabeledStatement node);
        void VisitEmptyStatement(EmptyStatement node);
        void VisitAssertStatement(AssertStatement node);

        // Annotations and JavaDoc
        void VisitAnnotation(Annotation node);
        void VisitAnnotationValueArgument(AnnotationValueArgument node);
        void VisitAnnotationArrayArgument(AnnotationArrayArgument node);
        void VisitJavaDoc(JavaDoc node);
    }

    /// <summary>
    /// Visitor interface for traversing Java AST nodes with return values.
    /// </summary>
    public interface IJavaVisitor<T>
    {
        // Compilation Unit
        T VisitCompilationUnit(CompilationUnit node);
        T VisitPackageDeclaration(PackageDeclaration node);
        T VisitImportDeclaration(ImportDeclaration node);

        // Type Declarations
        T VisitClassDeclaration(ClassDeclaration node);
        T VisitInterfaceDeclaration(InterfaceDeclaration node);
        T VisitEnumDeclaration(EnumDeclaration node);
        T VisitAnnotationDeclaration(AnnotationDeclaration node);

        // Members
        T VisitFieldDeclaration(FieldDeclaration node);
        T VisitMethodDeclaration(MethodDeclaration node);
        T VisitInitializerBlock(InitializerBlock node);
        T VisitVariableDeclarator(VariableDeclarator node);
        T VisitParameter(Parameter node);
        T VisitEnumConstant(EnumConstant node);
        T VisitAnnotationMember(AnnotationMember node);

        // Types
        T VisitPrimitiveType(PrimitiveType node);
        T VisitClassOrInterfaceType(ClassOrInterfaceType node);
        T VisitArrayType(ArrayType node);
        T VisitTypeParameter(TypeParameter node);
        T VisitTypeArgumentType(TypeArgumentType node);
        T VisitWildcardType(WildcardType node);

        // Expressions
        T VisitLiteralExpression(LiteralExpression node);
        T VisitIdentifierExpression(IdentifierExpression node);
        T VisitThisExpression(ThisExpression node);
        T VisitSuperExpression(SuperExpression node);
        T VisitBinaryExpression(BinaryExpression node);
        T VisitUnaryExpression(UnaryExpression node);
        T VisitConditionalExpression(ConditionalExpression node);
        T VisitMethodCallExpression(MethodCallExpression node);
        T VisitFieldAccessExpression(FieldAccessExpression node);
        T VisitArrayAccessExpression(ArrayAccessExpression node);
        T VisitCastExpression(CastExpression node);
        T VisitInstanceOfExpression(InstanceOfExpression node);
        T VisitNewExpression(NewExpression node);
        T VisitNewArrayExpression(NewArrayExpression node);
        T VisitArrayInitializer(ArrayInitializer node);
        T VisitLambdaExpression(LambdaExpression node);
        T VisitLambdaParameter(LambdaParameter node);
        T VisitMethodReferenceExpression(MethodReferenceExpression node);
        T VisitClassLiteralExpression(ClassLiteralExpression node);

        // Statements
        T VisitBlockStatement(BlockStatement node);
        T VisitLocalVariableStatement(LocalVariableStatement node);
        T VisitExpressionStatement(ExpressionStatement node);
        T VisitIfStatement(IfStatement node);
        T VisitWhileStatement(WhileStatement node);
        T VisitDoWhileStatement(DoWhileStatement node);
        T VisitForStatement(ForStatement node);
        T VisitForEachStatement(ForEachStatement node);
        T VisitSwitchStatement(SwitchStatement node);
        T VisitSwitchCase(SwitchCase node);
        T VisitBreakStatement(BreakStatement node);
        T VisitContinueStatement(ContinueStatement node);
        T VisitReturnStatement(ReturnStatement node);
        T VisitThrowStatement(ThrowStatement node);
        T VisitTryStatement(TryStatement node);
        T VisitResourceDeclaration(ResourceDeclaration node);
        T VisitCatchClause(CatchClause node);
        T VisitSynchronizedStatement(SynchronizedStatement node);
        T VisitLabeledStatement(LabeledStatement node);
        T VisitEmptyStatement(EmptyStatement node);
        T VisitAssertStatement(AssertStatement node);

        // Annotations and JavaDoc
        T VisitAnnotation(Annotation node);
        T VisitAnnotationValueArgument(AnnotationValueArgument node);
        T VisitAnnotationArrayArgument(AnnotationArrayArgument node);
        T VisitJavaDoc(JavaDoc node);
    }
}