using MarketAlly.IronJava.Core.AST.Nodes;

namespace MarketAlly.IronJava.Core.AST.Visitors
{
    /// <summary>
    /// Base implementation of IJavaVisitor that visits all child nodes by default.
    /// </summary>
    public abstract class JavaVisitorBase : IJavaVisitor
    {
        protected virtual void DefaultVisit(JavaNode node)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this);
            }
        }

        public virtual void VisitCompilationUnit(CompilationUnit node) => DefaultVisit(node);
        public virtual void VisitPackageDeclaration(PackageDeclaration node) => DefaultVisit(node);
        public virtual void VisitImportDeclaration(ImportDeclaration node) => DefaultVisit(node);

        public virtual void VisitClassDeclaration(ClassDeclaration node) => DefaultVisit(node);
        public virtual void VisitInterfaceDeclaration(InterfaceDeclaration node) => DefaultVisit(node);
        public virtual void VisitEnumDeclaration(EnumDeclaration node) => DefaultVisit(node);
        public virtual void VisitAnnotationDeclaration(AnnotationDeclaration node) => DefaultVisit(node);

        public virtual void VisitFieldDeclaration(FieldDeclaration node) => DefaultVisit(node);
        public virtual void VisitMethodDeclaration(MethodDeclaration node) => DefaultVisit(node);
        public virtual void VisitInitializerBlock(InitializerBlock node) => DefaultVisit(node);
        public virtual void VisitVariableDeclarator(VariableDeclarator node) => DefaultVisit(node);
        public virtual void VisitParameter(Parameter node) => DefaultVisit(node);
        public virtual void VisitEnumConstant(EnumConstant node) => DefaultVisit(node);
        public virtual void VisitAnnotationMember(AnnotationMember node) => DefaultVisit(node);

        public virtual void VisitPrimitiveType(PrimitiveType node) => DefaultVisit(node);
        public virtual void VisitClassOrInterfaceType(ClassOrInterfaceType node) => DefaultVisit(node);
        public virtual void VisitArrayType(ArrayType node) => DefaultVisit(node);
        public virtual void VisitTypeParameter(TypeParameter node) => DefaultVisit(node);
        public virtual void VisitTypeArgumentType(TypeArgumentType node) => DefaultVisit(node);
        public virtual void VisitWildcardType(WildcardType node) => DefaultVisit(node);

        public virtual void VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);
        public virtual void VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        public virtual void VisitThisExpression(ThisExpression node) => DefaultVisit(node);
        public virtual void VisitSuperExpression(SuperExpression node) => DefaultVisit(node);
        public virtual void VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        public virtual void VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        public virtual void VisitConditionalExpression(ConditionalExpression node) => DefaultVisit(node);
        public virtual void VisitMethodCallExpression(MethodCallExpression node) => DefaultVisit(node);
        public virtual void VisitFieldAccessExpression(FieldAccessExpression node) => DefaultVisit(node);
        public virtual void VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        public virtual void VisitCastExpression(CastExpression node) => DefaultVisit(node);
        public virtual void VisitInstanceOfExpression(InstanceOfExpression node) => DefaultVisit(node);
        public virtual void VisitNewExpression(NewExpression node) => DefaultVisit(node);
        public virtual void VisitNewArrayExpression(NewArrayExpression node) => DefaultVisit(node);
        public virtual void VisitArrayInitializer(ArrayInitializer node) => DefaultVisit(node);
        public virtual void VisitAnnotationExpression(AnnotationExpression node) => DefaultVisit(node);
        public virtual void VisitLambdaExpression(LambdaExpression node) => DefaultVisit(node);
        public virtual void VisitLambdaParameter(LambdaParameter node) => DefaultVisit(node);
        public virtual void VisitMethodReferenceExpression(MethodReferenceExpression node) => DefaultVisit(node);
        public virtual void VisitClassLiteralExpression(ClassLiteralExpression node) => DefaultVisit(node);

        public virtual void VisitBlockStatement(BlockStatement node) => DefaultVisit(node);
        public virtual void VisitLocalVariableStatement(LocalVariableStatement node) => DefaultVisit(node);
        public virtual void VisitExpressionStatement(ExpressionStatement node) => DefaultVisit(node);
        public virtual void VisitIfStatement(IfStatement node) => DefaultVisit(node);
        public virtual void VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        public virtual void VisitDoWhileStatement(DoWhileStatement node) => DefaultVisit(node);
        public virtual void VisitForStatement(ForStatement node) => DefaultVisit(node);
        public virtual void VisitForEachStatement(ForEachStatement node) => DefaultVisit(node);
        public virtual void VisitSwitchStatement(SwitchStatement node) => DefaultVisit(node);
        public virtual void VisitSwitchCase(SwitchCase node) => DefaultVisit(node);
        public virtual void VisitBreakStatement(BreakStatement node) => DefaultVisit(node);
        public virtual void VisitContinueStatement(ContinueStatement node) => DefaultVisit(node);
        public virtual void VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        public virtual void VisitThrowStatement(ThrowStatement node) => DefaultVisit(node);
        public virtual void VisitTryStatement(TryStatement node) => DefaultVisit(node);
        public virtual void VisitResourceDeclaration(ResourceDeclaration node) => DefaultVisit(node);
        public virtual void VisitCatchClause(CatchClause node) => DefaultVisit(node);
        public virtual void VisitSynchronizedStatement(SynchronizedStatement node) => DefaultVisit(node);
        public virtual void VisitLabeledStatement(LabeledStatement node) => DefaultVisit(node);
        public virtual void VisitEmptyStatement(EmptyStatement node) => DefaultVisit(node);
        public virtual void VisitAssertStatement(AssertStatement node) => DefaultVisit(node);

        public virtual void VisitAnnotation(Annotation node) => DefaultVisit(node);
        public virtual void VisitAnnotationValueArgument(AnnotationValueArgument node) => DefaultVisit(node);
        public virtual void VisitAnnotationArrayArgument(AnnotationArrayArgument node) => DefaultVisit(node);
        public virtual void VisitJavaDoc(JavaDoc node) => DefaultVisit(node);
    }

    /// <summary>
    /// Base implementation of IJavaVisitor with generic return type that visits all child nodes by default.
    /// </summary>
    public abstract class JavaVisitorBase<T> : IJavaVisitor<T>
    {
        protected abstract T DefaultVisit(JavaNode node);

        public virtual T VisitCompilationUnit(CompilationUnit node) => DefaultVisit(node);
        public virtual T VisitPackageDeclaration(PackageDeclaration node) => DefaultVisit(node);
        public virtual T VisitImportDeclaration(ImportDeclaration node) => DefaultVisit(node);

        public virtual T VisitClassDeclaration(ClassDeclaration node) => DefaultVisit(node);
        public virtual T VisitInterfaceDeclaration(InterfaceDeclaration node) => DefaultVisit(node);
        public virtual T VisitEnumDeclaration(EnumDeclaration node) => DefaultVisit(node);
        public virtual T VisitAnnotationDeclaration(AnnotationDeclaration node) => DefaultVisit(node);

        public virtual T VisitFieldDeclaration(FieldDeclaration node) => DefaultVisit(node);
        public virtual T VisitMethodDeclaration(MethodDeclaration node) => DefaultVisit(node);
        public virtual T VisitInitializerBlock(InitializerBlock node) => DefaultVisit(node);
        public virtual T VisitVariableDeclarator(VariableDeclarator node) => DefaultVisit(node);
        public virtual T VisitParameter(Parameter node) => DefaultVisit(node);
        public virtual T VisitEnumConstant(EnumConstant node) => DefaultVisit(node);
        public virtual T VisitAnnotationMember(AnnotationMember node) => DefaultVisit(node);

        public virtual T VisitPrimitiveType(PrimitiveType node) => DefaultVisit(node);
        public virtual T VisitClassOrInterfaceType(ClassOrInterfaceType node) => DefaultVisit(node);
        public virtual T VisitArrayType(ArrayType node) => DefaultVisit(node);
        public virtual T VisitTypeParameter(TypeParameter node) => DefaultVisit(node);
        public virtual T VisitTypeArgumentType(TypeArgumentType node) => DefaultVisit(node);
        public virtual T VisitWildcardType(WildcardType node) => DefaultVisit(node);

        public virtual T VisitLiteralExpression(LiteralExpression node) => DefaultVisit(node);
        public virtual T VisitIdentifierExpression(IdentifierExpression node) => DefaultVisit(node);
        public virtual T VisitThisExpression(ThisExpression node) => DefaultVisit(node);
        public virtual T VisitSuperExpression(SuperExpression node) => DefaultVisit(node);
        public virtual T VisitBinaryExpression(BinaryExpression node) => DefaultVisit(node);
        public virtual T VisitUnaryExpression(UnaryExpression node) => DefaultVisit(node);
        public virtual T VisitConditionalExpression(ConditionalExpression node) => DefaultVisit(node);
        public virtual T VisitMethodCallExpression(MethodCallExpression node) => DefaultVisit(node);
        public virtual T VisitFieldAccessExpression(FieldAccessExpression node) => DefaultVisit(node);
        public virtual T VisitArrayAccessExpression(ArrayAccessExpression node) => DefaultVisit(node);
        public virtual T VisitCastExpression(CastExpression node) => DefaultVisit(node);
        public virtual T VisitInstanceOfExpression(InstanceOfExpression node) => DefaultVisit(node);
        public virtual T VisitNewExpression(NewExpression node) => DefaultVisit(node);
        public virtual T VisitNewArrayExpression(NewArrayExpression node) => DefaultVisit(node);
        public virtual T VisitArrayInitializer(ArrayInitializer node) => DefaultVisit(node);
        public virtual T VisitAnnotationExpression(AnnotationExpression node) => DefaultVisit(node);
        public virtual T VisitLambdaExpression(LambdaExpression node) => DefaultVisit(node);
        public virtual T VisitLambdaParameter(LambdaParameter node) => DefaultVisit(node);
        public virtual T VisitMethodReferenceExpression(MethodReferenceExpression node) => DefaultVisit(node);
        public virtual T VisitClassLiteralExpression(ClassLiteralExpression node) => DefaultVisit(node);

        public virtual T VisitBlockStatement(BlockStatement node) => DefaultVisit(node);
        public virtual T VisitLocalVariableStatement(LocalVariableStatement node) => DefaultVisit(node);
        public virtual T VisitExpressionStatement(ExpressionStatement node) => DefaultVisit(node);
        public virtual T VisitIfStatement(IfStatement node) => DefaultVisit(node);
        public virtual T VisitWhileStatement(WhileStatement node) => DefaultVisit(node);
        public virtual T VisitDoWhileStatement(DoWhileStatement node) => DefaultVisit(node);
        public virtual T VisitForStatement(ForStatement node) => DefaultVisit(node);
        public virtual T VisitForEachStatement(ForEachStatement node) => DefaultVisit(node);
        public virtual T VisitSwitchStatement(SwitchStatement node) => DefaultVisit(node);
        public virtual T VisitSwitchCase(SwitchCase node) => DefaultVisit(node);
        public virtual T VisitBreakStatement(BreakStatement node) => DefaultVisit(node);
        public virtual T VisitContinueStatement(ContinueStatement node) => DefaultVisit(node);
        public virtual T VisitReturnStatement(ReturnStatement node) => DefaultVisit(node);
        public virtual T VisitThrowStatement(ThrowStatement node) => DefaultVisit(node);
        public virtual T VisitTryStatement(TryStatement node) => DefaultVisit(node);
        public virtual T VisitResourceDeclaration(ResourceDeclaration node) => DefaultVisit(node);
        public virtual T VisitCatchClause(CatchClause node) => DefaultVisit(node);
        public virtual T VisitSynchronizedStatement(SynchronizedStatement node) => DefaultVisit(node);
        public virtual T VisitLabeledStatement(LabeledStatement node) => DefaultVisit(node);
        public virtual T VisitEmptyStatement(EmptyStatement node) => DefaultVisit(node);
        public virtual T VisitAssertStatement(AssertStatement node) => DefaultVisit(node);

        public virtual T VisitAnnotation(Annotation node) => DefaultVisit(node);
        public virtual T VisitAnnotationValueArgument(AnnotationValueArgument node) => DefaultVisit(node);
        public virtual T VisitAnnotationArrayArgument(AnnotationArrayArgument node) => DefaultVisit(node);
        public virtual T VisitJavaDoc(JavaDoc node) => DefaultVisit(node);
    }
}