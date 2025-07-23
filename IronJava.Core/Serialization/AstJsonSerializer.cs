using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Visitors;

namespace MarketAlly.IronJava.Core.Serialization
{
    /// <summary>
    /// Provides JSON serialization for Java AST nodes.
    /// </summary>
    public class AstJsonSerializer
    {
        private readonly JsonSerializerOptions _options;

        public AstJsonSerializer(bool indented = true)
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new AstNodeConverterFactory()
                }
            };
        }

        public string Serialize(JavaNode node)
        {
            var visitor = new JsonSerializationVisitor();
            var jsonNode = node.Accept(visitor);
            return JsonSerializer.Serialize(jsonNode, _options);
        }

        public T? Deserialize<T>(string json) where T : JavaNode
        {
            using var document = JsonDocument.Parse(json);
            var deserializer = new JsonDeserializationVisitor();
            return deserializer.Deserialize(document.RootElement) as T;
        }
    }

    /// <summary>
    /// Custom converter factory for AST nodes.
    /// </summary>
    internal class AstNodeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(JavaNode).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new AstNodeConverter();
        }
    }

    /// <summary>
    /// Custom converter for AST nodes.
    /// </summary>
    internal class AstNodeConverter : JsonConverter<JavaNode>
    {
        public override JavaNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var deserializer = new JsonDeserializationVisitor();
            return deserializer.Deserialize(document.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, JavaNode value, JsonSerializerOptions options)
        {
            var visitor = new JsonSerializationVisitor();
            var jsonNode = value.Accept(visitor);
            JsonSerializer.Serialize(writer, jsonNode, options);
        }
    }

    /// <summary>
    /// Visitor that converts AST nodes to JSON-serializable dictionaries.
    /// </summary>
    internal class JsonSerializationVisitor : JavaVisitorBase<Dictionary<string, object?>>
    {
        protected override Dictionary<string, object?> DefaultVisit(JavaNode node)
        {
            var result = new Dictionary<string, object?>
            {
                ["nodeType"] = node.GetType().Name,
                ["location"] = SerializeLocation(node.Location)
            };

            var children = node.Children.Select(child => child.Accept(this)).ToList();
            if (children.Count > 0)
            {
                result["children"] = children;
            }

            return result;
        }

        private Dictionary<string, object> SerializeLocation(SourceRange location)
        {
            return new Dictionary<string, object>
            {
                ["start"] = new Dictionary<string, object>
                {
                    ["line"] = location.Start.Line,
                    ["column"] = location.Start.Column,
                    ["position"] = location.Start.Position
                },
                ["end"] = new Dictionary<string, object>
                {
                    ["line"] = location.End.Line,
                    ["column"] = location.End.Column,
                    ["position"] = location.End.Position
                }
            };
        }

        public override Dictionary<string, object?> VisitCompilationUnit(CompilationUnit node)
        {
            var result = CreateBaseNode(node);
            result["package"] = node.Package?.Accept(this);
            result["imports"] = node.Imports.Select(i => i.Accept(this)).ToList();
            result["types"] = node.Types.Select(t => t.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitPackageDeclaration(PackageDeclaration node)
        {
            var result = CreateBaseNode(node);
            result["packageName"] = node.PackageName;
            result["annotations"] = SerializeAnnotations(node.Annotations);
            return result;
        }

        public override Dictionary<string, object?> VisitImportDeclaration(ImportDeclaration node)
        {
            var result = CreateBaseNode(node);
            result["importPath"] = node.ImportPath;
            result["isStatic"] = node.IsStatic;
            result["isWildcard"] = node.IsWildcard;
            return result;
        }

        public override Dictionary<string, object?> VisitClassDeclaration(ClassDeclaration node)
        {
            var result = CreateTypeDeclarationBase(node);
            result["superClass"] = node.SuperClass?.Accept(this);
            result["interfaces"] = node.Interfaces.Select(i => i.Accept(this)).ToList();
            result["members"] = node.Members.Select(m => m.Accept(this)).ToList();
            result["nestedTypes"] = node.NestedTypes.Select(t => t.Accept(this)).ToList();
            result["isRecord"] = node.IsRecord;
            return result;
        }

        public override Dictionary<string, object?> VisitInterfaceDeclaration(InterfaceDeclaration node)
        {
            var result = CreateTypeDeclarationBase(node);
            result["extendedInterfaces"] = node.ExtendedInterfaces.Select(i => i.Accept(this)).ToList();
            result["members"] = node.Members.Select(m => m.Accept(this)).ToList();
            result["nestedTypes"] = node.NestedTypes.Select(t => t.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitEnumDeclaration(EnumDeclaration node)
        {
            var result = CreateTypeDeclarationBase(node);
            result["interfaces"] = node.Interfaces.Select(i => i.Accept(this)).ToList();
            result["constants"] = node.Constants.Select(c => c.Accept(this)).ToList();
            result["members"] = node.Members.Select(m => m.Accept(this)).ToList();
            result["nestedTypes"] = node.NestedTypes.Select(t => t.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitAnnotationDeclaration(AnnotationDeclaration node)
        {
            var result = CreateTypeDeclarationBase(node);
            result["members"] = node.Members.Select(m => m.Accept(this)).ToList();
            result["nestedTypes"] = node.NestedTypes.Select(t => t.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitFieldDeclaration(FieldDeclaration node)
        {
            var result = CreateMemberDeclarationBase(node);
            result["type"] = node.Type.Accept(this);
            result["variables"] = node.Variables.Select(v => v.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitMethodDeclaration(MethodDeclaration node)
        {
            var result = CreateMemberDeclarationBase(node);
            result["name"] = node.Name;
            result["returnType"] = node.ReturnType?.Accept(this);
            result["typeParameters"] = SerializeTypeParameters(node.TypeParameters);
            result["parameters"] = node.Parameters.Select(p => p.Accept(this)).ToList();
            result["throws"] = node.Throws.Select(t => t.Accept(this)).ToList();
            result["body"] = node.Body?.Accept(this);
            result["isConstructor"] = node.IsConstructor;
            return result;
        }

        public override Dictionary<string, object?> VisitParameter(Parameter node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["name"] = node.Name;
            result["isVarArgs"] = node.IsVarArgs;
            result["isFinal"] = node.IsFinal;
            result["annotations"] = SerializeAnnotations(node.Annotations);
            return result;
        }

        public override Dictionary<string, object?> VisitVariableDeclarator(VariableDeclarator node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["arrayDimensions"] = node.ArrayDimensions;
            result["initializer"] = node.Initializer?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitPrimitiveType(PrimitiveType node)
        {
            var result = CreateBaseNode(node);
            result["kind"] = node.Kind.ToString();
            return result;
        }

        public override Dictionary<string, object?> VisitClassOrInterfaceType(ClassOrInterfaceType node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["scope"] = node.Scope?.Accept(this);
            result["typeArguments"] = node.TypeArguments.Select(t => t.Accept(this)).ToList();
            result["annotations"] = SerializeAnnotations(node.Annotations);
            result["fullName"] = node.FullName;
            return result;
        }

        public override Dictionary<string, object?> VisitArrayType(ArrayType node)
        {
            var result = CreateBaseNode(node);
            result["elementType"] = node.ElementType.Accept(this);
            result["dimensions"] = node.Dimensions;
            return result;
        }

        public override Dictionary<string, object?> VisitTypeParameter(TypeParameter node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["bounds"] = node.Bounds.Select(b => b.Accept(this)).ToList();
            result["annotations"] = SerializeAnnotations(node.Annotations);
            return result;
        }

        public override Dictionary<string, object?> VisitLiteralExpression(LiteralExpression node)
        {
            var result = CreateBaseNode(node);
            result["value"] = node.Value;
            result["kind"] = node.Kind.ToString();
            return result;
        }

        public override Dictionary<string, object?> VisitIdentifierExpression(IdentifierExpression node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            return result;
        }

        public override Dictionary<string, object?> VisitBinaryExpression(BinaryExpression node)
        {
            var result = CreateBaseNode(node);
            result["left"] = node.Left.Accept(this);
            result["operator"] = node.Operator.ToString();
            result["right"] = node.Right.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitUnaryExpression(UnaryExpression node)
        {
            var result = CreateBaseNode(node);
            result["operator"] = node.Operator.ToString();
            result["operand"] = node.Operand.Accept(this);
            result["isPrefix"] = node.IsPrefix;
            return result;
        }

        public override Dictionary<string, object?> VisitMethodCallExpression(MethodCallExpression node)
        {
            var result = CreateBaseNode(node);
            result["target"] = node.Target?.Accept(this);
            result["methodName"] = node.MethodName;
            result["typeArguments"] = node.TypeArguments.Select(t => t.Accept(this)).ToList();
            result["arguments"] = node.Arguments.Select(a => a.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitFieldAccessExpression(FieldAccessExpression node)
        {
            var result = CreateBaseNode(node);
            result["target"] = node.Target.Accept(this);
            result["fieldName"] = node.FieldName;
            return result;
        }

        public override Dictionary<string, object?> VisitBlockStatement(BlockStatement node)
        {
            var result = CreateBaseNode(node);
            result["statements"] = node.Statements.Select(s => s.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitIfStatement(IfStatement node)
        {
            var result = CreateBaseNode(node);
            result["condition"] = node.Condition.Accept(this);
            result["thenStatement"] = node.ThenStatement.Accept(this);
            result["elseStatement"] = node.ElseStatement?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitWhileStatement(WhileStatement node)
        {
            var result = CreateBaseNode(node);
            result["condition"] = node.Condition.Accept(this);
            result["body"] = node.Body.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitForStatement(ForStatement node)
        {
            var result = CreateBaseNode(node);
            result["initializers"] = node.Initializers.Select(i => i.Accept(this)).ToList();
            result["condition"] = node.Condition?.Accept(this);
            result["updates"] = node.Updates.Select(u => u.Accept(this)).ToList();
            result["body"] = node.Body.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitReturnStatement(ReturnStatement node)
        {
            var result = CreateBaseNode(node);
            result["value"] = node.Value?.Accept(this);
            return result;
        }

        // Helper methods

        private Dictionary<string, object?> CreateBaseNode(JavaNode node)
        {
            return new Dictionary<string, object?>
            {
                ["nodeType"] = node.GetType().Name,
                ["location"] = SerializeLocation(node.Location)
            };
        }

        private Dictionary<string, object?> CreateTypeDeclarationBase(TypeDeclaration node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["modifiers"] = SerializeModifiers(node.Modifiers);
            result["annotations"] = SerializeAnnotations(node.Annotations);
            result["typeParameters"] = SerializeTypeParameters(node.TypeParameters);
            result["javaDoc"] = node.JavaDoc?.Accept(this);
            return result;
        }

        private Dictionary<string, object?> CreateMemberDeclarationBase(MemberDeclaration node)
        {
            var result = CreateBaseNode(node);
            result["modifiers"] = SerializeModifiers(node.Modifiers);
            result["annotations"] = SerializeAnnotations(node.Annotations);
            result["javaDoc"] = node.JavaDoc?.Accept(this);
            return result;
        }

        private List<string> SerializeModifiers(Modifiers modifiers)
        {
            var result = new List<string>();
            foreach (Modifiers value in Enum.GetValues<Modifiers>())
            {
                if (value != Modifiers.None && modifiers.HasFlag(value))
                {
                    result.Add(value.ToString().ToLowerInvariant());
                }
            }
            return result;
        }

        private List<Dictionary<string, object?>> SerializeAnnotations(IReadOnlyList<Annotation> annotations)
        {
            return annotations.Select(a => a.Accept(this)).ToList();
        }

        private List<Dictionary<string, object?>> SerializeTypeParameters(IReadOnlyList<TypeParameter> typeParameters)
        {
            return typeParameters.Select(tp => tp.Accept(this)).ToList();
        }

        public override Dictionary<string, object?> VisitAnnotation(Annotation node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["arguments"] = node.Arguments.Select(a => a.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitJavaDoc(JavaDoc node)
        {
            var result = CreateBaseNode(node);
            result["content"] = node.Content;
            result["tags"] = node.Tags.Select(t => new Dictionary<string, object?>
            {
                ["name"] = t.Name,
                ["parameter"] = t.Parameter,
                ["description"] = t.Description
            }).ToList();
            return result;
        }

        // Implement remaining visit methods...
        // (For brevity, showing pattern - all other node types follow similar structure)

        public override Dictionary<string, object?> VisitThisExpression(ThisExpression node)
        {
            var result = CreateBaseNode(node);
            result["qualifier"] = node.Qualifier?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitNewExpression(NewExpression node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["arguments"] = node.Arguments.Select(a => a.Accept(this)).ToList();
            result["anonymousClassBody"] = node.AnonymousClassBody?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitLambdaExpression(LambdaExpression node)
        {
            var result = CreateBaseNode(node);
            result["parameters"] = node.Parameters.Select(p => p.Accept(this)).ToList();
            result["body"] = node.Body.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitLambdaParameter(LambdaParameter node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["type"] = node.Type?.Accept(this);
            result["isFinal"] = node.IsFinal;
            return result;
        }

        public override Dictionary<string, object?> VisitTryStatement(TryStatement node)
        {
            var result = CreateBaseNode(node);
            result["resources"] = node.Resources.Select(r => r.Accept(this)).ToList();
            result["body"] = node.Body.Accept(this);
            result["catchClauses"] = node.CatchClauses.Select(c => c.Accept(this)).ToList();
            result["finallyBlock"] = node.FinallyBlock?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitResourceDeclaration(ResourceDeclaration node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["name"] = node.Name;
            result["initializer"] = node.Initializer.Accept(this);
            result["isFinal"] = node.IsFinal;
            return result;
        }

        public override Dictionary<string, object?> VisitEnumConstant(EnumConstant node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["arguments"] = node.Arguments.Select(a => a.Accept(this)).ToList();
            result["body"] = node.Body?.Accept(this);
            result["annotations"] = SerializeAnnotations(node.Annotations);
            return result;
        }

        public override Dictionary<string, object?> VisitAnnotationMember(AnnotationMember node)
        {
            var result = CreateBaseNode(node);
            result["name"] = node.Name;
            result["type"] = node.Type.Accept(this);
            result["defaultValue"] = node.DefaultValue?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitSuperExpression(SuperExpression node)
        {
            var result = CreateBaseNode(node);
            result["qualifier"] = node.Qualifier?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitConditionalExpression(ConditionalExpression node)
        {
            var result = CreateBaseNode(node);
            result["condition"] = node.Condition.Accept(this);
            result["thenExpression"] = node.ThenExpression.Accept(this);
            result["elseExpression"] = node.ElseExpression.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitArrayAccessExpression(ArrayAccessExpression node)
        {
            var result = CreateBaseNode(node);
            result["array"] = node.Array.Accept(this);
            result["index"] = node.Index.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitCastExpression(CastExpression node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["expression"] = node.Expression.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitInstanceOfExpression(InstanceOfExpression node)
        {
            var result = CreateBaseNode(node);
            result["expression"] = node.Expression.Accept(this);
            result["type"] = node.Type.Accept(this);
            result["patternVariable"] = node.PatternVariable;
            return result;
        }

        public override Dictionary<string, object?> VisitNewArrayExpression(NewArrayExpression node)
        {
            var result = CreateBaseNode(node);
            result["elementType"] = node.ElementType.Accept(this);
            result["dimensions"] = node.Dimensions.Select(d => d.Accept(this)).ToList();
            result["initializer"] = node.Initializer?.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitArrayInitializer(ArrayInitializer node)
        {
            var result = CreateBaseNode(node);
            result["elements"] = node.Elements.Select(e => e.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitMethodReferenceExpression(MethodReferenceExpression node)
        {
            var result = CreateBaseNode(node);
            result["target"] = node.Target.Accept(this);
            result["methodName"] = node.MethodName;
            result["typeArguments"] = node.TypeArguments.Select(t => t.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitClassLiteralExpression(ClassLiteralExpression node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitLocalVariableStatement(LocalVariableStatement node)
        {
            var result = CreateBaseNode(node);
            result["type"] = node.Type.Accept(this);
            result["variables"] = node.Variables.Select(v => v.Accept(this)).ToList();
            result["isFinal"] = node.IsFinal;
            return result;
        }

        public override Dictionary<string, object?> VisitExpressionStatement(ExpressionStatement node)
        {
            var result = CreateBaseNode(node);
            result["expression"] = node.Expression.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitDoWhileStatement(DoWhileStatement node)
        {
            var result = CreateBaseNode(node);
            result["body"] = node.Body.Accept(this);
            result["condition"] = node.Condition.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitForEachStatement(ForEachStatement node)
        {
            var result = CreateBaseNode(node);
            result["variableType"] = node.VariableType.Accept(this);
            result["variableName"] = node.VariableName;
            result["iterable"] = node.Iterable.Accept(this);
            result["body"] = node.Body.Accept(this);
            result["isFinal"] = node.IsFinal;
            return result;
        }

        public override Dictionary<string, object?> VisitSwitchStatement(SwitchStatement node)
        {
            var result = CreateBaseNode(node);
            result["selector"] = node.Selector.Accept(this);
            result["cases"] = node.Cases.Select(c => c.Accept(this)).ToList();
            return result;
        }

        public override Dictionary<string, object?> VisitSwitchCase(SwitchCase node)
        {
            var result = CreateBaseNode(node);
            result["labels"] = node.Labels.Select(l => l.Accept(this)).ToList();
            result["statements"] = node.Statements.Select(s => s.Accept(this)).ToList();
            result["isDefault"] = node.IsDefault;
            return result;
        }

        public override Dictionary<string, object?> VisitBreakStatement(BreakStatement node)
        {
            var result = CreateBaseNode(node);
            result["label"] = node.Label;
            return result;
        }

        public override Dictionary<string, object?> VisitContinueStatement(ContinueStatement node)
        {
            var result = CreateBaseNode(node);
            result["label"] = node.Label;
            return result;
        }

        public override Dictionary<string, object?> VisitThrowStatement(ThrowStatement node)
        {
            var result = CreateBaseNode(node);
            result["exception"] = node.Exception.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitCatchClause(CatchClause node)
        {
            var result = CreateBaseNode(node);
            result["exceptionTypes"] = node.ExceptionTypes.Select(t => t.Accept(this)).ToList();
            result["variableName"] = node.VariableName;
            result["body"] = node.Body.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitSynchronizedStatement(SynchronizedStatement node)
        {
            var result = CreateBaseNode(node);
            result["lock"] = node.Lock.Accept(this);
            result["body"] = node.Body.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitLabeledStatement(LabeledStatement node)
        {
            var result = CreateBaseNode(node);
            result["label"] = node.Label;
            result["statement"] = node.Statement.Accept(this);
            return result;
        }

        public override Dictionary<string, object?> VisitEmptyStatement(EmptyStatement node)
        {
            return CreateBaseNode(node);
        }

        public override Dictionary<string, object?> VisitAssertStatement(AssertStatement node)
        {
            var result = CreateBaseNode(node);
            result["condition"] = node.Condition.Accept(this);
            result["message"] = node.Message?.Accept(this);
            return result;
        }
    }

    /// <summary>
    /// Visitor that deserializes JSON to AST nodes.
    /// </summary>
    internal class JsonDeserializationVisitor
    {
        public JavaNode Deserialize(JsonElement element)
        {
            if (!element.TryGetProperty("nodeType", out var nodeTypeElement))
            {
                throw new JsonException("Missing nodeType property");
            }

            var nodeType = nodeTypeElement.GetString() ?? throw new JsonException("nodeType is null");
            var location = DeserializeLocation(element.GetProperty("location"));

            return nodeType switch
            {
                "CompilationUnit" => DeserializeCompilationUnit(element, location),
                "PackageDeclaration" => DeserializePackageDeclaration(element, location),
                "ImportDeclaration" => DeserializeImportDeclaration(element, location),
                "ClassDeclaration" => DeserializeClassDeclaration(element, location),
                "InterfaceDeclaration" => DeserializeInterfaceDeclaration(element, location),
                "EnumDeclaration" => DeserializeEnumDeclaration(element, location),
                "AnnotationDeclaration" => DeserializeAnnotationDeclaration(element, location),
                "FieldDeclaration" => DeserializeFieldDeclaration(element, location),
                "MethodDeclaration" => DeserializeMethodDeclaration(element, location),
                "Parameter" => DeserializeParameter(element, location),
                "VariableDeclarator" => DeserializeVariableDeclarator(element, location),
                "PrimitiveType" => DeserializePrimitiveType(element, location),
                "ClassOrInterfaceType" => DeserializeClassOrInterfaceType(element, location),
                "ArrayType" => DeserializeArrayType(element, location),
                "TypeParameter" => DeserializeTypeParameter(element, location),
                "LiteralExpression" => DeserializeLiteralExpression(element, location),
                "IdentifierExpression" => DeserializeIdentifierExpression(element, location),
                "BinaryExpression" => DeserializeBinaryExpression(element, location),
                "UnaryExpression" => DeserializeUnaryExpression(element, location),
                "MethodCallExpression" => DeserializeMethodCallExpression(element, location),
                "FieldAccessExpression" => DeserializeFieldAccessExpression(element, location),
                "BlockStatement" => DeserializeBlockStatement(element, location),
                "IfStatement" => DeserializeIfStatement(element, location),
                "WhileStatement" => DeserializeWhileStatement(element, location),
                "ForStatement" => DeserializeForStatement(element, location),
                "ReturnStatement" => DeserializeReturnStatement(element, location),
                "ThisExpression" => DeserializeThisExpression(element, location),
                "NewExpression" => DeserializeNewExpression(element, location),
                "LambdaExpression" => DeserializeLambdaExpression(element, location),
                "TryStatement" => DeserializeTryStatement(element, location),
                "Annotation" => DeserializeAnnotation(element, location),
                "JavaDoc" => DeserializeJavaDoc(element, location),
                "EnumConstant" => DeserializeEnumConstant(element, location),
                "AnnotationMember" => DeserializeAnnotationMember(element, location),
                "SuperExpression" => DeserializeSuperExpression(element, location),
                "ConditionalExpression" => DeserializeConditionalExpression(element, location),
                "ArrayAccessExpression" => DeserializeArrayAccessExpression(element, location),
                "CastExpression" => DeserializeCastExpression(element, location),
                "InstanceOfExpression" => DeserializeInstanceOfExpression(element, location),
                "NewArrayExpression" => DeserializeNewArrayExpression(element, location),
                "ArrayInitializer" => DeserializeArrayInitializer(element, location),
                "MethodReferenceExpression" => DeserializeMethodReferenceExpression(element, location),
                "ClassLiteralExpression" => DeserializeClassLiteralExpression(element, location),
                "LocalVariableStatement" => DeserializeLocalVariableStatement(element, location),
                "ExpressionStatement" => DeserializeExpressionStatement(element, location),
                "DoWhileStatement" => DeserializeDoWhileStatement(element, location),
                "ForEachStatement" => DeserializeForEachStatement(element, location),
                "SwitchStatement" => DeserializeSwitchStatement(element, location),
                "BreakStatement" => DeserializeBreakStatement(element, location),
                "ContinueStatement" => DeserializeContinueStatement(element, location),
                "ThrowStatement" => DeserializeThrowStatement(element, location),
                "SynchronizedStatement" => DeserializeSynchronizedStatement(element, location),
                "LabeledStatement" => DeserializeLabeledStatement(element, location),
                "EmptyStatement" => DeserializeEmptyStatement(element, location),
                "AssertStatement" => DeserializeAssertStatement(element, location),
                "CatchClause" => DeserializeCatchClause(element, location),
                "SwitchCase" => DeserializeSwitchCase(element, location),
                "ResourceDeclaration" => DeserializeResourceDeclaration(element, location),
                "LambdaParameter" => DeserializeLambdaParameter(element, location),
                _ => throw new JsonException($"Unknown node type: {nodeType}")
            };
        }

        private SourceRange DeserializeLocation(JsonElement element)
        {
            var start = element.GetProperty("start");
            var end = element.GetProperty("end");

            return new SourceRange(
                new SourceLocation(
                    start.GetProperty("line").GetInt32(),
                    start.GetProperty("column").GetInt32(),
                    start.GetProperty("position").GetInt32(),
                    0
                ),
                new SourceLocation(
                    end.GetProperty("line").GetInt32(),
                    end.GetProperty("column").GetInt32(),
                    end.GetProperty("position").GetInt32(),
                    0
                )
            );
        }

        private CompilationUnit DeserializeCompilationUnit(JsonElement element, SourceRange location)
        {
            var package = element.TryGetProperty("package", out var packageEl) && packageEl.ValueKind != JsonValueKind.Null
                ? Deserialize(packageEl) as PackageDeclaration
                : null;

            var imports = DeserializeList<ImportDeclaration>(element.GetProperty("imports"));
            var types = DeserializeList<TypeDeclaration>(element.GetProperty("types"));

            return new CompilationUnit(location, package, imports, types);
        }

        private PackageDeclaration DeserializePackageDeclaration(JsonElement element, SourceRange location)
        {
            var packageName = element.GetProperty("packageName").GetString() ?? throw new JsonException("packageName is null");
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            return new PackageDeclaration(location, packageName, annotations);
        }

        private ImportDeclaration DeserializeImportDeclaration(JsonElement element, SourceRange location)
        {
            var importPath = element.GetProperty("importPath").GetString() ?? throw new JsonException("importPath is null");
            var isStatic = element.GetProperty("isStatic").GetBoolean();
            var isWildcard = element.GetProperty("isWildcard").GetBoolean();
            return new ImportDeclaration(location, importPath, isStatic, isWildcard);
        }

        private ClassDeclaration DeserializeClassDeclaration(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var typeParameters = DeserializeList<TypeParameter>(element.GetProperty("typeParameters"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var superClass = element.TryGetProperty("superClass", out var superClassEl) && superClassEl.ValueKind != JsonValueKind.Null
                ? Deserialize(superClassEl) as ClassOrInterfaceType
                : null;

            var interfaces = DeserializeList<ClassOrInterfaceType>(element.GetProperty("interfaces"));
            var members = DeserializeList<MemberDeclaration>(element.GetProperty("members"));
            var nestedTypes = element.TryGetProperty("nestedTypes", out var nestedTypesEl) 
                ? DeserializeList<TypeDeclaration>(nestedTypesEl) 
                : new List<TypeDeclaration>();
            var isRecord = element.TryGetProperty("isRecord", out var isRecordEl) 
                ? isRecordEl.GetBoolean() 
                : false;

            return new ClassDeclaration(location, name, modifiers, annotations, typeParameters, superClass, interfaces, members, nestedTypes, javaDoc, isRecord);
        }

        private InterfaceDeclaration DeserializeInterfaceDeclaration(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var typeParameters = DeserializeList<TypeParameter>(element.GetProperty("typeParameters"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var extendedInterfaces = DeserializeList<ClassOrInterfaceType>(element.GetProperty("extendedInterfaces"));
            var members = DeserializeList<MemberDeclaration>(element.GetProperty("members"));
            var nestedTypes = element.TryGetProperty("nestedTypes", out var nestedTypesEl) 
                ? DeserializeList<TypeDeclaration>(nestedTypesEl) 
                : new List<TypeDeclaration>();

            return new InterfaceDeclaration(location, name, modifiers, annotations, typeParameters, extendedInterfaces, members, nestedTypes, javaDoc);
        }

        private EnumDeclaration DeserializeEnumDeclaration(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var interfaces = DeserializeList<ClassOrInterfaceType>(element.GetProperty("interfaces"));
            var constants = DeserializeList<EnumConstant>(element.GetProperty("constants"));
            var members = DeserializeList<MemberDeclaration>(element.GetProperty("members"));
            var nestedTypes = element.TryGetProperty("nestedTypes", out var nestedTypesEl) 
                ? DeserializeList<TypeDeclaration>(nestedTypesEl) 
                : new List<TypeDeclaration>();

            return new EnumDeclaration(location, name, modifiers, annotations, interfaces, constants, members, nestedTypes, javaDoc);
        }

        private AnnotationDeclaration DeserializeAnnotationDeclaration(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var members = DeserializeList<AnnotationMember>(element.GetProperty("members"));
            var nestedTypes = element.TryGetProperty("nestedTypes", out var nestedTypesEl) 
                ? DeserializeList<TypeDeclaration>(nestedTypesEl) 
                : new List<TypeDeclaration>();

            return new AnnotationDeclaration(location, name, modifiers, annotations, members, nestedTypes, javaDoc);
        }

        private FieldDeclaration DeserializeFieldDeclaration(JsonElement element, SourceRange location)
        {
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var variables = DeserializeList<VariableDeclarator>(element.GetProperty("variables"));

            return new FieldDeclaration(location, modifiers, annotations, type, variables, javaDoc);
        }

        private MethodDeclaration DeserializeMethodDeclaration(JsonElement element, SourceRange location)
        {
            var modifiers = DeserializeModifiers(element.GetProperty("modifiers"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            var javaDoc = element.TryGetProperty("javaDoc", out var javaDocEl) && javaDocEl.ValueKind != JsonValueKind.Null
                ? Deserialize(javaDocEl) as JavaDoc
                : null;

            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var returnType = element.TryGetProperty("returnType", out var returnTypeEl) && returnTypeEl.ValueKind != JsonValueKind.Null
                ? Deserialize(returnTypeEl) as TypeReference
                : null;

            var typeParameters = DeserializeList<TypeParameter>(element.GetProperty("typeParameters"));
            var parameters = DeserializeList<Parameter>(element.GetProperty("parameters"));
            var throws = DeserializeList<ClassOrInterfaceType>(element.GetProperty("throws"));
            var body = element.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind != JsonValueKind.Null
                ? Deserialize(bodyEl) as BlockStatement
                : null;

            var isConstructor = element.GetProperty("isConstructor").GetBoolean();

            return new MethodDeclaration(location, name, modifiers, annotations, returnType, typeParameters, parameters, throws, body, javaDoc, isConstructor);
        }

        private Parameter DeserializeParameter(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var isVarArgs = element.GetProperty("isVarArgs").GetBoolean();
            var isFinal = element.GetProperty("isFinal").GetBoolean();
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));

            return new Parameter(location, type, name, isVarArgs, isFinal, annotations);
        }

        private VariableDeclarator DeserializeVariableDeclarator(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var arrayDimensions = element.GetProperty("arrayDimensions").GetInt32();
            var initializer = element.TryGetProperty("initializer", out var initializerEl) && initializerEl.ValueKind != JsonValueKind.Null
                ? Deserialize(initializerEl) as Expression
                : null;

            return new VariableDeclarator(location, name, arrayDimensions, initializer);
        }

        private PrimitiveType DeserializePrimitiveType(JsonElement element, SourceRange location)
        {
            var kindStr = element.GetProperty("kind").GetString() ?? throw new JsonException("kind is null");
            var kind = Enum.Parse<PrimitiveTypeKind>(kindStr);
            return new PrimitiveType(location, kind);
        }

        private ClassOrInterfaceType DeserializeClassOrInterfaceType(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var scope = element.TryGetProperty("scope", out var scopeEl) && scopeEl.ValueKind != JsonValueKind.Null
                ? Deserialize(scopeEl) as ClassOrInterfaceType
                : null;

            var typeArguments = DeserializeList<TypeArgument>(element.GetProperty("typeArguments"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));

            return new ClassOrInterfaceType(location, name, scope, typeArguments, annotations);
        }

        private ArrayType DeserializeArrayType(JsonElement element, SourceRange location)
        {
            var elementType = Deserialize(element.GetProperty("elementType")) as TypeReference ?? throw new JsonException("elementType is not TypeReference");
            var dimensions = element.GetProperty("dimensions").GetInt32();
            return new ArrayType(location, elementType, dimensions);
        }

        private TypeParameter DeserializeTypeParameter(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var bounds = DeserializeList<TypeReference>(element.GetProperty("bounds"));
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));
            return new TypeParameter(location, name, bounds, annotations);
        }

        private LiteralExpression DeserializeLiteralExpression(JsonElement element, SourceRange location)
        {
            var value = element.GetProperty("value");
            var kindStr = element.GetProperty("kind").GetString() ?? throw new JsonException("kind is null");
            var kind = Enum.Parse<LiteralKind>(kindStr);

            object? literalValue = kind switch
            {
                LiteralKind.Integer => value.GetInt32(),
                LiteralKind.Long => value.GetInt64(),
                LiteralKind.Float => value.GetSingle(),
                LiteralKind.Double => value.GetDouble(),
                LiteralKind.Boolean => value.GetBoolean(),
                LiteralKind.Character => value.GetString()?.FirstOrDefault(),
                LiteralKind.String => value.GetString(),
                LiteralKind.Null => null,
                _ => throw new JsonException($"Unknown literal kind: {kind}")
            };

            return new LiteralExpression(location, literalValue, kind);
        }

        private IdentifierExpression DeserializeIdentifierExpression(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            return new IdentifierExpression(location, name);
        }

        private BinaryExpression DeserializeBinaryExpression(JsonElement element, SourceRange location)
        {
            var left = Deserialize(element.GetProperty("left")) as Expression ?? throw new JsonException("left is not Expression");
            var operatorStr = element.GetProperty("operator").GetString() ?? throw new JsonException("operator is null");
            var @operator = Enum.Parse<BinaryOperator>(operatorStr);
            var right = Deserialize(element.GetProperty("right")) as Expression ?? throw new JsonException("right is not Expression");

            return new BinaryExpression(location, left, @operator, right);
        }

        private UnaryExpression DeserializeUnaryExpression(JsonElement element, SourceRange location)
        {
            var operatorStr = element.GetProperty("operator").GetString() ?? throw new JsonException("operator is null");
            var @operator = Enum.Parse<UnaryOperator>(operatorStr);
            var operand = Deserialize(element.GetProperty("operand")) as Expression ?? throw new JsonException("operand is not Expression");
            var isPrefix = element.GetProperty("isPrefix").GetBoolean();

            return new UnaryExpression(location, @operator, operand, isPrefix);
        }

        private MethodCallExpression DeserializeMethodCallExpression(JsonElement element, SourceRange location)
        {
            var target = element.TryGetProperty("target", out var targetEl) && targetEl.ValueKind != JsonValueKind.Null
                ? Deserialize(targetEl) as Expression
                : null;

            var methodName = element.GetProperty("methodName").GetString() ?? throw new JsonException("methodName is null");
            var typeArguments = DeserializeList<TypeArgument>(element.GetProperty("typeArguments"));
            var arguments = DeserializeList<Expression>(element.GetProperty("arguments"));

            return new MethodCallExpression(location, target, methodName, typeArguments, arguments);
        }

        private FieldAccessExpression DeserializeFieldAccessExpression(JsonElement element, SourceRange location)
        {
            var target = Deserialize(element.GetProperty("target")) as Expression ?? throw new JsonException("target is not Expression");
            var fieldName = element.GetProperty("fieldName").GetString() ?? throw new JsonException("fieldName is null");
            return new FieldAccessExpression(location, target, fieldName);
        }

        private BlockStatement DeserializeBlockStatement(JsonElement element, SourceRange location)
        {
            var statements = DeserializeList<Statement>(element.GetProperty("statements"));
            return new BlockStatement(location, statements);
        }

        private IfStatement DeserializeIfStatement(JsonElement element, SourceRange location)
        {
            var condition = Deserialize(element.GetProperty("condition")) as Expression ?? throw new JsonException("condition is not Expression");
            var thenStatement = Deserialize(element.GetProperty("thenStatement")) as Statement ?? throw new JsonException("thenStatement is not Statement");
            var elseStatement = element.TryGetProperty("elseStatement", out var elseEl) && elseEl.ValueKind != JsonValueKind.Null
                ? Deserialize(elseEl) as Statement
                : null;

            return new IfStatement(location, condition, thenStatement, elseStatement);
        }

        private WhileStatement DeserializeWhileStatement(JsonElement element, SourceRange location)
        {
            var condition = Deserialize(element.GetProperty("condition")) as Expression ?? throw new JsonException("condition is not Expression");
            var body = Deserialize(element.GetProperty("body")) as Statement ?? throw new JsonException("body is not Statement");
            return new WhileStatement(location, condition, body);
        }

        private ForStatement DeserializeForStatement(JsonElement element, SourceRange location)
        {
            var initializers = DeserializeList<Statement>(element.GetProperty("initializers"));
            var condition = element.TryGetProperty("condition", out var conditionEl) && conditionEl.ValueKind != JsonValueKind.Null
                ? Deserialize(conditionEl) as Expression
                : null;
            var updates = DeserializeList<Expression>(element.GetProperty("updates"));
            var body = Deserialize(element.GetProperty("body")) as Statement ?? throw new JsonException("body is not Statement");

            return new ForStatement(location, initializers, condition, updates, body);
        }

        private ReturnStatement DeserializeReturnStatement(JsonElement element, SourceRange location)
        {
            var value = element.TryGetProperty("value", out var valueEl) && valueEl.ValueKind != JsonValueKind.Null
                ? Deserialize(valueEl) as Expression
                : null;
            return new ReturnStatement(location, value);
        }

        private ThisExpression DeserializeThisExpression(JsonElement element, SourceRange location)
        {
            var qualifier = element.TryGetProperty("qualifier", out var qualifierEl) && qualifierEl.ValueKind != JsonValueKind.Null
                ? Deserialize(qualifierEl) as Expression
                : null;
            return new ThisExpression(location, qualifier);
        }

        private NewExpression DeserializeNewExpression(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as ClassOrInterfaceType ?? throw new JsonException("type is not ClassOrInterfaceType");
            var arguments = DeserializeList<Expression>(element.GetProperty("arguments"));
            var anonymousClassBody = element.TryGetProperty("anonymousClassBody", out var bodyEl) && bodyEl.ValueKind != JsonValueKind.Null
                ? Deserialize(bodyEl) as ClassDeclaration
                : null;

            return new NewExpression(location, type, arguments, anonymousClassBody);
        }

        private LambdaExpression DeserializeLambdaExpression(JsonElement element, SourceRange location)
        {
            var parameters = DeserializeList<LambdaParameter>(element.GetProperty("parameters"));
            var body = Deserialize(element.GetProperty("body")) ?? throw new JsonException("body is null");
            return new LambdaExpression(location, parameters, body);
        }

        private TryStatement DeserializeTryStatement(JsonElement element, SourceRange location)
        {
            var resources = DeserializeList<ResourceDeclaration>(element.GetProperty("resources"));
            var body = Deserialize(element.GetProperty("body")) as BlockStatement ?? throw new JsonException("body is not BlockStatement");
            var catchClauses = DeserializeList<CatchClause>(element.GetProperty("catchClauses"));
            var finallyBlock = element.TryGetProperty("finallyBlock", out var finallyEl) && finallyEl.ValueKind != JsonValueKind.Null
                ? Deserialize(finallyEl) as BlockStatement
                : null;

            return new TryStatement(location, resources, body, catchClauses, finallyBlock);
        }

        private Annotation DeserializeAnnotation(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as ClassOrInterfaceType ?? throw new JsonException("type is not ClassOrInterfaceType");
            var arguments = DeserializeList<AnnotationArgument>(element.GetProperty("arguments"));
            return new Annotation(location, type, arguments);
        }

        private JavaDoc DeserializeJavaDoc(JsonElement element, SourceRange location)
        {
            var content = element.GetProperty("content").GetString() ?? throw new JsonException("content is null");
            var tags = element.GetProperty("tags").EnumerateArray().Select(tagEl =>
            {
                var name = tagEl.GetProperty("name").GetString() ?? "";
                var parameter = tagEl.TryGetProperty("parameter", out var paramEl) ? paramEl.GetString() : null;
                var description = tagEl.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
                return new JavaDocTag(name, parameter, description ?? "");
            }).ToList();

            return new JavaDoc(location, content, tags);
        }

        private List<T> DeserializeList<T>(JsonElement element) where T : JavaNode
        {
            return element.EnumerateArray()
                .Select(item => Deserialize(item))
                .OfType<T>()
                .ToList();
        }

        private List<Annotation> DeserializeAnnotations(JsonElement element)
        {
            return element.EnumerateArray()
                .Select(item => Deserialize(item) as Annotation)
                .Where(ann => ann != null)
                .Cast<Annotation>()
                .ToList();
        }

        private Modifiers DeserializeModifiers(JsonElement element)
        {
            var modifiers = Modifiers.None;
            foreach (var modifierStr in element.EnumerateArray())
            {
                var str = modifierStr.GetString();
                if (str != null && Enum.TryParse<Modifiers>(str, true, out var modifier))
                {
                    modifiers |= modifier;
                }
            }
            return modifiers;
        }

        // Additional node deserializers

        private EnumConstant DeserializeEnumConstant(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var arguments = DeserializeList<Expression>(element.GetProperty("arguments"));
            var body = element.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind != JsonValueKind.Null
                ? Deserialize(bodyEl) as ClassDeclaration
                : null;
            var annotations = DeserializeAnnotations(element.GetProperty("annotations"));

            return new EnumConstant(location, name, annotations, arguments, body);
        }

        private AnnotationMember DeserializeAnnotationMember(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var defaultValue = element.TryGetProperty("defaultValue", out var defaultEl) && defaultEl.ValueKind != JsonValueKind.Null
                ? Deserialize(defaultEl) as Expression
                : null;

            return new AnnotationMember(location, name, type, defaultValue);
        }

        private SuperExpression DeserializeSuperExpression(JsonElement element, SourceRange location)
        {
            var qualifier = element.TryGetProperty("qualifier", out var qualifierEl) && qualifierEl.ValueKind != JsonValueKind.Null
                ? Deserialize(qualifierEl) as Expression
                : null;
            return new SuperExpression(location, qualifier);
        }

        private ConditionalExpression DeserializeConditionalExpression(JsonElement element, SourceRange location)
        {
            var condition = Deserialize(element.GetProperty("condition")) as Expression ?? throw new JsonException("condition is not Expression");
            var thenExpression = Deserialize(element.GetProperty("thenExpression")) as Expression ?? throw new JsonException("thenExpression is not Expression");
            var elseExpression = Deserialize(element.GetProperty("elseExpression")) as Expression ?? throw new JsonException("elseExpression is not Expression");

            return new ConditionalExpression(location, condition, thenExpression, elseExpression);
        }

        private ArrayAccessExpression DeserializeArrayAccessExpression(JsonElement element, SourceRange location)
        {
            var array = Deserialize(element.GetProperty("array")) as Expression ?? throw new JsonException("array is not Expression");
            var index = Deserialize(element.GetProperty("index")) as Expression ?? throw new JsonException("index is not Expression");

            return new ArrayAccessExpression(location, array, index);
        }

        private CastExpression DeserializeCastExpression(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var expression = Deserialize(element.GetProperty("expression")) as Expression ?? throw new JsonException("expression is not Expression");

            return new CastExpression(location, type, expression);
        }

        private InstanceOfExpression DeserializeInstanceOfExpression(JsonElement element, SourceRange location)
        {
            var expression = Deserialize(element.GetProperty("expression")) as Expression ?? throw new JsonException("expression is not Expression");
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var patternVariable = element.TryGetProperty("patternVariable", out var patternEl) && patternEl.ValueKind != JsonValueKind.Null
                ? patternEl.GetString()
                : null;

            return new InstanceOfExpression(location, expression, type, patternVariable);
        }

        private NewArrayExpression DeserializeNewArrayExpression(JsonElement element, SourceRange location)
        {
            var elementType = Deserialize(element.GetProperty("elementType")) as TypeReference ?? throw new JsonException("elementType is not TypeReference");
            var dimensions = DeserializeList<Expression>(element.GetProperty("dimensions"));
            var initializer = element.TryGetProperty("initializer", out var initEl) && initEl.ValueKind != JsonValueKind.Null
                ? Deserialize(initEl) as ArrayInitializer
                : null;

            return new NewArrayExpression(location, elementType, dimensions, initializer);
        }

        private ArrayInitializer DeserializeArrayInitializer(JsonElement element, SourceRange location)
        {
            var elements = DeserializeList<Expression>(element.GetProperty("elements"));
            return new ArrayInitializer(location, elements);
        }

        private MethodReferenceExpression DeserializeMethodReferenceExpression(JsonElement element, SourceRange location)
        {
            var target = Deserialize(element.GetProperty("target")) as Expression ?? throw new JsonException("target is not Expression");
            var methodName = element.GetProperty("methodName").GetString() ?? throw new JsonException("methodName is null");
            var typeArguments = DeserializeList<TypeArgument>(element.GetProperty("typeArguments"));

            return new MethodReferenceExpression(location, target, methodName, typeArguments);
        }

        private ClassLiteralExpression DeserializeClassLiteralExpression(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            return new ClassLiteralExpression(location, type);
        }

        private LocalVariableStatement DeserializeLocalVariableStatement(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var variables = DeserializeList<VariableDeclarator>(element.GetProperty("variables"));
            var isFinal = element.GetProperty("isFinal").GetBoolean();

            return new LocalVariableStatement(location, type, variables, isFinal);
        }

        private ExpressionStatement DeserializeExpressionStatement(JsonElement element, SourceRange location)
        {
            var expression = Deserialize(element.GetProperty("expression")) as Expression ?? throw new JsonException("expression is not Expression");
            return new ExpressionStatement(location, expression);
        }

        private DoWhileStatement DeserializeDoWhileStatement(JsonElement element, SourceRange location)
        {
            var body = Deserialize(element.GetProperty("body")) as Statement ?? throw new JsonException("body is not Statement");
            var condition = Deserialize(element.GetProperty("condition")) as Expression ?? throw new JsonException("condition is not Expression");

            return new DoWhileStatement(location, body, condition);
        }

        private ForEachStatement DeserializeForEachStatement(JsonElement element, SourceRange location)
        {
            var variableType = Deserialize(element.GetProperty("variableType")) as TypeReference ?? throw new JsonException("variableType is not TypeReference");
            var variableName = element.GetProperty("variableName").GetString() ?? throw new JsonException("variableName is null");
            var iterable = Deserialize(element.GetProperty("iterable")) as Expression ?? throw new JsonException("iterable is not Expression");
            var body = Deserialize(element.GetProperty("body")) as Statement ?? throw new JsonException("body is not Statement");
            var isFinal = element.GetProperty("isFinal").GetBoolean();

            return new ForEachStatement(location, variableType, variableName, iterable, body, isFinal);
        }

        private SwitchStatement DeserializeSwitchStatement(JsonElement element, SourceRange location)
        {
            var expression = Deserialize(element.GetProperty("selector")) as Expression ?? throw new JsonException("selector is not Expression");
            var cases = DeserializeList<SwitchCase>(element.GetProperty("cases"));

            return new SwitchStatement(location, expression, cases);
        }

        private SwitchCase DeserializeSwitchCase(JsonElement element, SourceRange location)
        {
            var labels = DeserializeList<Expression>(element.GetProperty("labels"));
            var statements = DeserializeList<Statement>(element.GetProperty("statements"));
            var isDefault = element.GetProperty("isDefault").GetBoolean();

            return new SwitchCase(location, labels, statements, isDefault);
        }

        private BreakStatement DeserializeBreakStatement(JsonElement element, SourceRange location)
        {
            var label = element.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : null;
            return new BreakStatement(location, label);
        }

        private ContinueStatement DeserializeContinueStatement(JsonElement element, SourceRange location)
        {
            var label = element.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : null;
            return new ContinueStatement(location, label);
        }

        private ThrowStatement DeserializeThrowStatement(JsonElement element, SourceRange location)
        {
            var expression = Deserialize(element.GetProperty("exception")) as Expression ?? throw new JsonException("exception is not Expression");
            return new ThrowStatement(location, expression);
        }

        private CatchClause DeserializeCatchClause(JsonElement element, SourceRange location)
        {
            var exceptionTypes = DeserializeList<TypeReference>(element.GetProperty("exceptionTypes"));
            var variableName = element.GetProperty("variableName").GetString() ?? throw new JsonException("variableName is null");
            var body = Deserialize(element.GetProperty("body")) as BlockStatement ?? throw new JsonException("body is not BlockStatement");

            return new CatchClause(location, exceptionTypes, variableName, body);
        }

        private SynchronizedStatement DeserializeSynchronizedStatement(JsonElement element, SourceRange location)
        {
            var expression = Deserialize(element.GetProperty("lock")) as Expression ?? throw new JsonException("lock is not Expression");
            var body = Deserialize(element.GetProperty("body")) as BlockStatement ?? throw new JsonException("body is not BlockStatement");

            return new SynchronizedStatement(location, expression, body);
        }

        private LabeledStatement DeserializeLabeledStatement(JsonElement element, SourceRange location)
        {
            var label = element.GetProperty("label").GetString() ?? throw new JsonException("label is null");
            var statement = Deserialize(element.GetProperty("statement")) as Statement ?? throw new JsonException("statement is not Statement");

            return new LabeledStatement(location, label, statement);
        }

        private EmptyStatement DeserializeEmptyStatement(JsonElement element, SourceRange location)
        {
            return new EmptyStatement(location);
        }

        private AssertStatement DeserializeAssertStatement(JsonElement element, SourceRange location)
        {
            var condition = Deserialize(element.GetProperty("condition")) as Expression ?? throw new JsonException("condition is not Expression");
            var message = element.TryGetProperty("message", out var msgEl) && msgEl.ValueKind != JsonValueKind.Null
                ? Deserialize(msgEl) as Expression
                : null;

            return new AssertStatement(location, condition, message);
        }

        private ResourceDeclaration DeserializeResourceDeclaration(JsonElement element, SourceRange location)
        {
            var type = Deserialize(element.GetProperty("type")) as TypeReference ?? throw new JsonException("type is not TypeReference");
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var initializer = Deserialize(element.GetProperty("initializer")) as Expression ?? throw new JsonException("initializer is not Expression");
            var isFinal = element.GetProperty("isFinal").GetBoolean();

            return new ResourceDeclaration(location, type, name, initializer, isFinal);
        }

        private LambdaParameter DeserializeLambdaParameter(JsonElement element, SourceRange location)
        {
            var name = element.GetProperty("name").GetString() ?? throw new JsonException("name is null");
            var type = element.TryGetProperty("type", out var typeEl) && typeEl.ValueKind != JsonValueKind.Null
                ? Deserialize(typeEl) as TypeReference
                : null;
            var isFinal = element.GetProperty("isFinal").GetBoolean();

            return new LambdaParameter(location, name, type, isFinal);
        }
    }
}