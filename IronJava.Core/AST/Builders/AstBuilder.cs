using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.Grammar;

namespace MarketAlly.IronJava.Core.AST.Builders
{
    /// <summary>
    /// Builds a typed AST from ANTLR parse tree.
    /// </summary>
    public class AstBuilder : Java9ParserBaseVisitor<JavaNode?>
    {
        private readonly ITokenStream _tokens;

        public AstBuilder(ITokenStream tokens)
        {
            _tokens = tokens;
        }

        private SourceRange GetSourceRange(ParserRuleContext context)
        {
            var start = new SourceLocation(
                context.Start.Line,
                context.Start.Column,
                context.Start.StartIndex,
                context.Start.Text?.Length ?? 0
            );

            var stop = context.Stop ?? context.Start;
            var end = new SourceLocation(
                stop.Line,
                stop.Column + (stop.Text?.Length ?? 0),
                stop.StopIndex,
                stop.Text?.Length ?? 0
            );

            return new SourceRange(start, end);
        }

        private SourceRange GetSourceRange(ITerminalNode node)
        {
            var token = node.Symbol;
            var location = new SourceLocation(
                token.Line,
                token.Column,
                token.StartIndex,
                token.Text?.Length ?? 0
            );
            return new SourceRange(location, location);
        }

        private PrimitiveTypeKind ParsePrimitiveTypeKind(string typeName)
        {
            return typeName switch
            {
                "boolean" => PrimitiveTypeKind.Boolean,
                "byte" => PrimitiveTypeKind.Byte,
                "short" => PrimitiveTypeKind.Short,
                "int" => PrimitiveTypeKind.Int,
                "long" => PrimitiveTypeKind.Long,
                "char" => PrimitiveTypeKind.Char,
                "float" => PrimitiveTypeKind.Float,
                "double" => PrimitiveTypeKind.Double,
                "void" => PrimitiveTypeKind.Void,
                _ => PrimitiveTypeKind.Int // Default fallback
            };
        }

        public override JavaNode? VisitCompilationUnit(Java9Parser.CompilationUnitContext context)
        {
            var location = GetSourceRange(context);
            PackageDeclaration? package = null;
            var imports = new List<ImportDeclaration>();
            var types = new List<TypeDeclaration>();

            // Check if it's ordinary compilation or modular compilation
            if (context.ordinaryCompilation() != null)
            {
                var ordinaryComp = context.ordinaryCompilation();
                
                package = ordinaryComp.packageDeclaration() != null
                    ? Visit(ordinaryComp.packageDeclaration()) as PackageDeclaration
                    : null;

                imports = ordinaryComp.importDeclaration()
                    .Select(i => Visit(i) as ImportDeclaration)
                    .Where(i => i != null)
                    .Cast<ImportDeclaration>()
                    .ToList();

                types = ordinaryComp.typeDeclaration()
                    .Select(t => Visit(t) as TypeDeclaration)
                    .Where(t => t != null)
                    .Cast<TypeDeclaration>()
                    .ToList();
            }
            else if (context.modularCompilation() != null)
            {
                var modularComp = context.modularCompilation();
                
                // Handle module declaration if needed
                // For now, we'll just handle imports
                imports = modularComp.importDeclaration()
                    .Select(i => Visit(i) as ImportDeclaration)
                    .Where(i => i != null)
                    .Cast<ImportDeclaration>()
                    .ToList();
            }

            return new CompilationUnit(location, package, imports, types);
        }

        public override JavaNode? VisitPackageDeclaration(Java9Parser.PackageDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var annotations = BuildAnnotations(context.packageModifier());
            var packageName = context.packageName().GetText();

            return new PackageDeclaration(location, packageName, annotations);
        }

        public override JavaNode? VisitImportDeclaration(Java9Parser.ImportDeclarationContext context)
        {
            if (context.singleTypeImportDeclaration() != null)
            {
                return VisitSingleTypeImportDeclaration(context.singleTypeImportDeclaration());
            }
            else if (context.typeImportOnDemandDeclaration() != null)
            {
                return VisitTypeImportOnDemandDeclaration(context.typeImportOnDemandDeclaration());
            }
            else if (context.singleStaticImportDeclaration() != null)
            {
                return VisitSingleStaticImportDeclaration(context.singleStaticImportDeclaration());
            }
            else if (context.staticImportOnDemandDeclaration() != null)
            {
                return VisitStaticImportOnDemandDeclaration(context.staticImportOnDemandDeclaration());
            }

            return null;
        }

        public override JavaNode? VisitSingleTypeImportDeclaration(Java9Parser.SingleTypeImportDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var typeName = context.typeName().GetText();
            return new ImportDeclaration(location, typeName, false, false);
        }

        public override JavaNode? VisitTypeImportOnDemandDeclaration(Java9Parser.TypeImportOnDemandDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var packageOrTypeName = context.packageOrTypeName().GetText();
            return new ImportDeclaration(location, packageOrTypeName, false, true);
        }

        public override JavaNode? VisitSingleStaticImportDeclaration(Java9Parser.SingleStaticImportDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var typeName = context.typeName().GetText() + "." + context.identifier().GetText();
            return new ImportDeclaration(location, typeName, true, false);
        }

        public override JavaNode? VisitStaticImportOnDemandDeclaration(Java9Parser.StaticImportOnDemandDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var typeName = context.typeName().GetText();
            return new ImportDeclaration(location, typeName, true, true);
        }

        public override JavaNode? VisitTypeDeclaration(Java9Parser.TypeDeclarationContext context)
        {
            if (context.classDeclaration() != null)
            {
                return Visit(context.classDeclaration());
            }
            else if (context.interfaceDeclaration() != null)
            {
                return Visit(context.interfaceDeclaration());
            }

            return null;
        }

        public override JavaNode? VisitNormalClassDeclaration(Java9Parser.NormalClassDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.classModifier());
            var annotations = BuildAnnotations(context.classModifier());
            var name = context.identifier().GetText();
            var typeParameters = BuildTypeParameters(context.typeParameters());
            var superClass = context.superclass() != null ? BuildTypeReference(context.superclass().classType()) : null;
            var interfaces = BuildInterfaces(context.superinterfaces());
            var members = BuildClassMembers(context.classBody());
            var javaDoc = ExtractJavaDoc(context);

            return new ClassDeclaration(
                location, name, modifiers, annotations, typeParameters,
                superClass, interfaces, members, javaDoc
            );
        }

        public override JavaNode? VisitNormalInterfaceDeclaration(Java9Parser.NormalInterfaceDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.interfaceModifier());
            var annotations = BuildAnnotations(context.interfaceModifier());
            var name = context.identifier().GetText();
            var typeParameters = BuildTypeParameters(context.typeParameters());
            var extendedInterfaces = BuildExtendedInterfaces(context.extendsInterfaces());
            var members = BuildInterfaceMembers(context.interfaceBody());
            var javaDoc = ExtractJavaDoc(context);

            return new InterfaceDeclaration(
                location, name, modifiers, annotations, typeParameters,
                extendedInterfaces, members, javaDoc
            );
        }

        public override JavaNode? VisitEnumDeclaration(Java9Parser.EnumDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.classModifier());
            var annotations = BuildAnnotations(context.classModifier());
            var name = context.identifier().GetText();
            var interfaces = BuildInterfaces(context.superinterfaces());
            var enumBody = context.enumBody();
            var constants = BuildEnumConstants(enumBody.enumConstantList());
            var members = BuildEnumMembers(enumBody.enumBodyDeclarations());
            var javaDoc = ExtractJavaDoc(context);

            return new EnumDeclaration(
                location, name, modifiers, annotations,
                interfaces, constants, members, javaDoc
            );
        }

        public override JavaNode? VisitAnnotationTypeDeclaration(Java9Parser.AnnotationTypeDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.interfaceModifier());
            var annotations = BuildAnnotations(context.interfaceModifier());
            var name = context.identifier().GetText();
            var members = BuildAnnotationMembers(context.annotationTypeBody());
            var javaDoc = ExtractJavaDoc(context);

            return new AnnotationDeclaration(
                location, name, modifiers, annotations, members, javaDoc
            );
        }

        // Helper methods for building AST components

        private Modifiers BuildModifiers(IEnumerable<IParseTree>? modifierContexts)
        {
            if (modifierContexts == null) return Modifiers.None;

            var modifiers = Modifiers.None;
            foreach (var modifier in modifierContexts)
            {
                var text = modifier.GetText();
                modifiers |= text switch
                {
                    "public" => Modifiers.Public,
                    "protected" => Modifiers.Protected,
                    "private" => Modifiers.Private,
                    "static" => Modifiers.Static,
                    "final" => Modifiers.Final,
                    "abstract" => Modifiers.Abstract,
                    "native" => Modifiers.Native,
                    "synchronized" => Modifiers.Synchronized,
                    "transient" => Modifiers.Transient,
                    "volatile" => Modifiers.Volatile,
                    "strictfp" => Modifiers.Strictfp,
                    "default" => Modifiers.Default,
                    "sealed" => Modifiers.Sealed,
                    "non-sealed" => Modifiers.NonSealed,
                    _ => Modifiers.None
                };
            }
            return modifiers;
        }

        private List<Annotation> BuildAnnotations(IEnumerable<IParseTree>? contexts)
        {
            if (contexts == null) return new List<Annotation>();

            var annotations = new List<Annotation>();
            foreach (var context in contexts)
            {
                if (context is Java9Parser.AnnotationContext annotationContext)
                {
                    var annotation = BuildAnnotation(annotationContext);
                    if (annotation != null) annotations.Add(annotation);
                }
            }
            return annotations;
        }

        private Annotation? BuildAnnotation(Java9Parser.AnnotationContext context)
        {
            var location = GetSourceRange(context);
            TypeReference? type = null;
            var arguments = new List<AnnotationArgument>();

            // Annotations can be NormalAnnotation, MarkerAnnotation, or SingleElementAnnotation
            if (context.normalAnnotation() != null)
            {
                var normalAnn = context.normalAnnotation();
                type = BuildTypeReference(normalAnn.typeName());
                if (type == null) return null;

                if (normalAnn.elementValuePairList() != null)
                {
                    foreach (var pair in normalAnn.elementValuePairList().elementValuePair())
                    {
                        var name = pair.identifier().GetText();
                        var value = BuildExpression(pair.elementValue());
                        if (value != null)
                        {
                            arguments.Add(new AnnotationValueArgument(
                                GetSourceRange(pair), name, value
                            ));
                        }
                    }
                }
            }
            else if (context.markerAnnotation() != null)
            {
                var markerAnn = context.markerAnnotation();
                type = BuildTypeReference(markerAnn.typeName());
                if (type == null) return null;
                // Marker annotations have no arguments
            }
            else if (context.singleElementAnnotation() != null)
            {
                var singleAnn = context.singleElementAnnotation();
                type = BuildTypeReference(singleAnn.typeName());
                if (type == null) return null;

                if (singleAnn.elementValue() != null)
                {
                    var value = BuildExpression(singleAnn.elementValue());
                    if (value != null)
                    {
                        arguments.Add(new AnnotationValueArgument(
                            location, "value", value
                        ));
                    }
                }
            }

            return type != null ? new Annotation(location, type, arguments) : null;
        }

        private List<TypeParameter> BuildTypeParameters(Java9Parser.TypeParametersContext? context)
        {
            if (context == null) return new List<TypeParameter>();

            return context.typeParameterList().typeParameter()
                .Select(tp => BuildTypeParameter(tp))
                .Where(tp => tp != null)
                .Cast<TypeParameter>()
                .ToList();
        }

        private TypeParameter? BuildTypeParameter(Java9Parser.TypeParameterContext context)
        {
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();
            var bounds = new List<TypeReference>();
            var annotations = BuildAnnotations(context.typeParameterModifier());

            if (context.typeBound() != null)
            {
                if (context.typeBound().typeVariable() != null)
                {
                    var boundType = BuildTypeReference(context.typeBound().typeVariable());
                    if (boundType != null) bounds.Add(boundType);
                }
                else if (context.typeBound().classOrInterfaceType() != null)
                {
                    var boundType = BuildTypeReference(context.typeBound().classOrInterfaceType());
                    if (boundType != null) bounds.Add(boundType);

                    foreach (var additionalBound in context.typeBound().additionalBound())
                    {
                        var additionalType = BuildTypeReference(additionalBound.interfaceType());
                        if (additionalType != null) bounds.Add(additionalType);
                    }
                }
            }

            return new TypeParameter(location, name, bounds, annotations);
        }

        private TypeReference? BuildTypeReference(IParseTree? context)
        {
            if (context == null) return null;

            return context switch
            {
                Java9Parser.PrimitiveTypeContext primitive => BuildPrimitiveType(primitive),
                Java9Parser.ClassOrInterfaceTypeContext classOrInterface => BuildClassOrInterfaceType(classOrInterface),
                Java9Parser.ArrayTypeContext array => BuildArrayType(array),
                Java9Parser.TypeVariableContext typeVar => BuildTypeVariable(typeVar),
                Java9Parser.ClassTypeContext classType => BuildClassType(classType),
                Java9Parser.InterfaceTypeContext interfaceType => BuildInterfaceType(interfaceType),
                Java9Parser.TypeNameContext typeName => BuildTypeName(typeName),
                _ => null
            };
        }

        private PrimitiveType? BuildPrimitiveType(Java9Parser.PrimitiveTypeContext context)
        {
            var location = GetSourceRange(context);
            var text = context.GetText();
            
            var kind = text switch
            {
                "boolean" => PrimitiveTypeKind.Boolean,
                "byte" => PrimitiveTypeKind.Byte,
                "short" => PrimitiveTypeKind.Short,
                "int" => PrimitiveTypeKind.Int,
                "long" => PrimitiveTypeKind.Long,
                "char" => PrimitiveTypeKind.Char,
                "float" => PrimitiveTypeKind.Float,
                "double" => PrimitiveTypeKind.Double,
                _ => (PrimitiveTypeKind?)null
            };

            return kind.HasValue ? new PrimitiveType(location, kind.Value) : null;
        }

        private ClassOrInterfaceType? BuildClassOrInterfaceType(Java9Parser.ClassOrInterfaceTypeContext context)
        {
            ClassOrInterfaceType? current = null;

            if (context.classType_lfno_classOrInterfaceType() != null)
            {
                current = BuildClassType_lfno(context.classType_lfno_classOrInterfaceType());
            }
            else if (context.interfaceType_lfno_classOrInterfaceType() != null)
            {
                current = BuildInterfaceType_lfno(context.interfaceType_lfno_classOrInterfaceType());
            }

            foreach (var additional in context.classType_lf_classOrInterfaceType())
            {
                var name = additional.identifier().GetText();
                var typeArgs = BuildTypeArguments(additional.typeArguments());
                var annotations = BuildAnnotations(additional.annotation());
                var location = GetSourceRange(additional);

                current = new ClassOrInterfaceType(location, name, current, typeArgs, annotations);
            }

            foreach (var additional in context.interfaceType_lf_classOrInterfaceType())
            {
                // InterfaceType_lf_classOrInterfaceType delegates to classType_lf_classOrInterfaceType
                var classType = additional.classType_lf_classOrInterfaceType();
                if (classType != null)
                {
                    var name = classType.identifier().GetText();
                    var typeArgs = BuildTypeArguments(classType.typeArguments());
                    var annotations = BuildAnnotations(classType.annotation());
                    var location = GetSourceRange(additional);

                    current = new ClassOrInterfaceType(location, name, current, typeArgs, annotations);
                }
            }

            return current;
        }

        private ClassOrInterfaceType? BuildClassType(Java9Parser.ClassTypeContext context)
        {
            var location = GetSourceRange(context);
            var annotations = BuildAnnotations(context.annotation());

            if (context.identifier() != null)
            {
                var name = context.identifier().GetText();
                var typeArgs = BuildTypeArguments(context.typeArguments());
                var scope = context.classOrInterfaceType() != null
                    ? BuildClassOrInterfaceType(context.classOrInterfaceType())
                    : null;

                return new ClassOrInterfaceType(location, name, scope, typeArgs, annotations);
            }

            return null;
        }

        private ClassOrInterfaceType? BuildClassType_lfno(Java9Parser.ClassType_lfno_classOrInterfaceTypeContext context)
        {
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();
            var typeArgs = BuildTypeArguments(context.typeArguments());
            var annotations = BuildAnnotations(context.annotation());

            return new ClassOrInterfaceType(location, name, null, typeArgs, annotations);
        }

        private ClassOrInterfaceType? BuildInterfaceType(Java9Parser.InterfaceTypeContext context)
        {
            return BuildClassType(context.classType());
        }

        private ClassOrInterfaceType? BuildInterfaceType_lfno(Java9Parser.InterfaceType_lfno_classOrInterfaceTypeContext context)
        {
            return BuildClassType_lfno(context.classType_lfno_classOrInterfaceType());
        }

        private ClassOrInterfaceType? BuildTypeName(Java9Parser.TypeNameContext context)
        {
            var location = GetSourceRange(context);
            // TypeName has a recursive structure, just use GetText()
            return new ClassOrInterfaceType(
                location,
                context.GetText(),
                null,
                new List<TypeArgument>(),
                new List<Annotation>()
            );
        }

        private ClassOrInterfaceType? BuildTypeVariable(Java9Parser.TypeVariableContext context)
        {
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();
            var annotations = BuildAnnotations(context.annotation());

            return new ClassOrInterfaceType(location, name, null, new List<TypeArgument>(), annotations);
        }

        private ArrayType? BuildArrayType(Java9Parser.ArrayTypeContext context)
        {
            TypeReference? elementType = null;
            int dimensions = 0;

            if (context.primitiveType() != null)
            {
                elementType = BuildPrimitiveType(context.primitiveType());
                dimensions = context.dims().GetText().Length / 2; // Each dimension is "[]"
            }
            else if (context.classOrInterfaceType() != null)
            {
                elementType = BuildClassOrInterfaceType(context.classOrInterfaceType());
                dimensions = context.dims().GetText().Length / 2;
            }
            else if (context.typeVariable() != null)
            {
                elementType = BuildTypeVariable(context.typeVariable());
                dimensions = context.dims().GetText().Length / 2;
            }

            return elementType != null 
                ? new ArrayType(GetSourceRange(context), elementType, dimensions) 
                : null;
        }

        private List<TypeArgument> BuildTypeArguments(Java9Parser.TypeArgumentsContext? context)
        {
            if (context?.typeArgumentList() == null) return new List<TypeArgument>();

            return context.typeArgumentList().typeArgument()
                .Select(ta => BuildTypeArgument(ta))
                .Where(ta => ta != null)
                .Cast<TypeArgument>()
                .ToList();
        }

        private TypeArgument? BuildTypeArgument(Java9Parser.TypeArgumentContext context)
        {
            if (context.referenceType() != null)
            {
                var type = BuildTypeReference(context.referenceType());
                return type != null 
                    ? new TypeArgumentType(GetSourceRange(context), type) 
                    : null;
            }
            else if (context.wildcard() != null)
            {
                return BuildWildcard(context.wildcard());
            }

            return null;
        }

        private WildcardType? BuildWildcard(Java9Parser.WildcardContext context)
        {
            var location = GetSourceRange(context);
            TypeReference? bound = null;
            var boundKind = WildcardBoundKind.None;

            if (context.wildcardBounds() != null)
            {
                var boundsContext = context.wildcardBounds();
                if (boundsContext.referenceType() != null)
                {
                    bound = BuildTypeReference(boundsContext.referenceType());
                    boundKind = boundsContext.GetChild(0).GetText() == "extends" 
                        ? WildcardBoundKind.Extends 
                        : WildcardBoundKind.Super;
                }
            }

            return new WildcardType(location, bound, boundKind);
        }

        private List<TypeReference> BuildInterfaces(Java9Parser.SuperinterfacesContext? context)
        {
            if (context?.interfaceTypeList() == null) return new List<TypeReference>();

            return context.interfaceTypeList().interfaceType()
                .Select(i => BuildTypeReference(i))
                .Where(i => i != null)
                .Cast<TypeReference>()
                .ToList();
        }

        private List<TypeReference> BuildExtendedInterfaces(Java9Parser.ExtendsInterfacesContext? context)
        {
            if (context?.interfaceTypeList() == null) return new List<TypeReference>();

            return context.interfaceTypeList().interfaceType()
                .Select(i => BuildTypeReference(i))
                .Where(i => i != null)
                .Cast<TypeReference>()
                .ToList();
        }

        private List<MemberDeclaration> BuildClassMembers(Java9Parser.ClassBodyContext context)
        {
            var members = new List<MemberDeclaration>();

            foreach (var declaration in context.classBodyDeclaration())
            {
                if (declaration.classMemberDeclaration() != null)
                {
                    var member = BuildClassMember(declaration.classMemberDeclaration());
                    if (member != null) members.Add(member);
                }
                else if (declaration.instanceInitializer() != null)
                {
                    var initializer = BuildInstanceInitializer(declaration.instanceInitializer());
                    if (initializer != null) members.Add(initializer);
                }
                else if (declaration.staticInitializer() != null)
                {
                    var initializer = BuildStaticInitializer(declaration.staticInitializer());
                    if (initializer != null) members.Add(initializer);
                }
                else if (declaration.constructorDeclaration() != null)
                {
                    var constructor = BuildConstructor(declaration.constructorDeclaration());
                    if (constructor != null) members.Add(constructor);
                }
            }

            return members;
        }

        private MemberDeclaration? BuildClassMember(Java9Parser.ClassMemberDeclarationContext context)
        {
            if (context.fieldDeclaration() != null)
            {
                return BuildFieldDeclaration(context.fieldDeclaration());
            }
            else if (context.methodDeclaration() != null)
            {
                return BuildMethodDeclaration(context.methodDeclaration());
            }
            else if (context.classDeclaration() != null)
            {
                return Visit(context.classDeclaration()) as MemberDeclaration;
            }
            else if (context.interfaceDeclaration() != null)
            {
                return Visit(context.interfaceDeclaration()) as MemberDeclaration;
            }

            return null;
        }

        private FieldDeclaration? BuildFieldDeclaration(Java9Parser.FieldDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.fieldModifier());
            var annotations = BuildAnnotations(context.fieldModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var variables = context.variableDeclaratorList().variableDeclarator()
                .Select(v => BuildVariableDeclarator(v))
                .Where(v => v != null)
                .Cast<VariableDeclarator>()
                .ToList();

            var javaDoc = ExtractJavaDoc(context);

            return new FieldDeclaration(location, modifiers, annotations, type, variables, javaDoc);
        }

        private VariableDeclarator? BuildVariableDeclarator(Java9Parser.VariableDeclaratorContext context)
        {
            var location = GetSourceRange(context);
            var id = context.variableDeclaratorId();
            var name = id.identifier().GetText();
            var dimensions = id.dims()?.GetText().Length / 2 ?? 0;
            
            Expression? initializer = null;
            if (context.variableInitializer() != null)
            {
                initializer = BuildExpression(context.variableInitializer());
            }

            return new VariableDeclarator(location, name, dimensions, initializer);
        }

        private MethodDeclaration? BuildMethodDeclaration(Java9Parser.MethodDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.methodModifier());
            var annotations = BuildAnnotations(context.methodModifier());
            var header = context.methodHeader();
            
            var typeParameters = BuildTypeParameters(header.typeParameters());
            var returnType = BuildTypeReference(header.result());
            var declarator = header.methodDeclarator();
            var name = declarator.identifier().GetText();
            var parameters = BuildParameters(declarator.formalParameterList());
            var throws_ = BuildThrows(header.throws_());
            var body = context.methodBody().block() != null 
                ? BuildBlock(context.methodBody().block()) 
                : null;

            var javaDoc = ExtractJavaDoc(context);

            return new MethodDeclaration(
                location, name, modifiers, annotations, returnType,
                typeParameters, parameters, throws_, body, javaDoc
            );
        }

        private TypeReference? BuildTypeReference(Java9Parser.ResultContext? context)
        {
            if (context == null) return null;

            if (context.unannType() != null)
            {
                return BuildTypeReference(context.unannType());
            }
            else if (context.GetText() == "void")
            {
                return new PrimitiveType(GetSourceRange(context), PrimitiveTypeKind.Void);
            }

            return null;
        }

        private TypeReference? BuildTypeReference(Java9Parser.UnannTypeContext? context)
        {
            if (context == null) return null;

            if (context.unannPrimitiveType() != null)
            {
                return BuildPrimitiveType(context.unannPrimitiveType());
            }
            else if (context.unannReferenceType() != null)
            {
                return BuildTypeReference(context.unannReferenceType());
            }

            return null;
        }

        private PrimitiveType? BuildPrimitiveType(Java9Parser.UnannPrimitiveTypeContext context)
        {
            var location = GetSourceRange(context);
            var text = context.GetText();
            
            var kind = text switch
            {
                "boolean" => PrimitiveTypeKind.Boolean,
                "byte" => PrimitiveTypeKind.Byte,
                "short" => PrimitiveTypeKind.Short,
                "int" => PrimitiveTypeKind.Int,
                "long" => PrimitiveTypeKind.Long,
                "char" => PrimitiveTypeKind.Char,
                "float" => PrimitiveTypeKind.Float,
                "double" => PrimitiveTypeKind.Double,
                _ => (PrimitiveTypeKind?)null
            };

            return kind.HasValue ? new PrimitiveType(location, kind.Value) : null;
        }

        private TypeReference? BuildTypeReference(Java9Parser.UnannReferenceTypeContext? context)
        {
            if (context == null) return null;

            if (context.unannClassOrInterfaceType() != null)
            {
                return BuildClassOrInterfaceType(context.unannClassOrInterfaceType());
            }
            else if (context.unannTypeVariable() != null)
            {
                return BuildTypeVariable(context.unannTypeVariable());
            }
            else if (context.unannArrayType() != null)
            {
                return BuildArrayType(context.unannArrayType());
            }

            return null;
        }

        private ClassOrInterfaceType? BuildClassOrInterfaceType(Java9Parser.UnannClassOrInterfaceTypeContext context)
        {
            ClassOrInterfaceType? current = null;

            if (context.unannClassType_lfno_unannClassOrInterfaceType() != null)
            {
                var first = context.unannClassType_lfno_unannClassOrInterfaceType();
                var name = first.identifier().GetText();
                var typeArgs = BuildTypeArguments(first.typeArguments());
                var location = GetSourceRange(first);

                current = new ClassOrInterfaceType(location, name, null, typeArgs, new List<Annotation>());
            }

            foreach (var additional in context.unannClassType_lf_unannClassOrInterfaceType())
            {
                var name = additional.identifier().GetText();
                var typeArgs = BuildTypeArguments(additional.typeArguments());
                var location = GetSourceRange(additional);

                current = new ClassOrInterfaceType(location, name, current, typeArgs, new List<Annotation>());
            }

            return current;
        }

        private ClassOrInterfaceType? BuildTypeVariable(Java9Parser.UnannTypeVariableContext context)
        {
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();

            return new ClassOrInterfaceType(location, name, null, new List<TypeArgument>(), new List<Annotation>());
        }

        private ArrayType? BuildArrayType(Java9Parser.UnannArrayTypeContext context)
        {
            TypeReference? elementType = null;
            int dimensions = 0;

            if (context.unannPrimitiveType() != null)
            {
                elementType = BuildPrimitiveType(context.unannPrimitiveType());
                dimensions = context.dims().GetText().Length / 2;
            }
            else if (context.unannClassOrInterfaceType() != null)
            {
                elementType = BuildClassOrInterfaceType(context.unannClassOrInterfaceType());
                dimensions = context.dims().GetText().Length / 2;
            }
            else if (context.unannTypeVariable() != null)
            {
                elementType = BuildTypeVariable(context.unannTypeVariable());
                dimensions = context.dims().GetText().Length / 2;
            }

            return elementType != null 
                ? new ArrayType(GetSourceRange(context), elementType, dimensions) 
                : null;
        }

        private List<Parameter> BuildParameters(Java9Parser.FormalParameterListContext? context)
        {
            if (context == null) return new List<Parameter>();

            var parameters = new List<Parameter>();

            if (context.formalParameters() != null)
            {
                foreach (var param in context.formalParameters().formalParameter())
                {
                    var parameter = BuildParameter(param);
                    if (parameter != null) parameters.Add(parameter);
                }

                if (context.formalParameters().receiverParameter() != null)
                {
                    // Skip receiver parameter for now
                }
            }

            if (context.lastFormalParameter() != null)
            {
                var parameter = BuildLastParameter(context.lastFormalParameter());
                if (parameter != null) parameters.Add(parameter);
            }

            return parameters;
        }

        private Parameter? BuildParameter(Java9Parser.FormalParameterContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var annotations = BuildAnnotations(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var declaratorId = context.variableDeclaratorId();
            var name = declaratorId.identifier().GetText();
            var isFinal = modifiers.HasFlag(Modifiers.Final);

            // Handle array dimensions on parameter name
            if (declaratorId.dims() != null)
            {
                var dimensions = declaratorId.dims().GetText().Length / 2;
                type = new ArrayType(location, type, dimensions);
            }

            return new Parameter(location, type, name, false, isFinal, annotations);
        }

        private Parameter? BuildLastParameter(Java9Parser.LastFormalParameterContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var annotations = BuildAnnotations(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var declaratorId = context.variableDeclaratorId();
            var name = declaratorId.identifier().GetText();
            var isFinal = modifiers.HasFlag(Modifiers.Final);
            var isVarArgs = context.GetChild(context.ChildCount - 2)?.GetText() == "...";

            return new Parameter(location, type, name, isVarArgs, isFinal, annotations);
        }

        private List<TypeReference> BuildThrows(Java9Parser.Throws_Context? context)
        {
            if (context?.exceptionTypeList() == null) return new List<TypeReference>();

            return context.exceptionTypeList().exceptionType()
                .Select(e => BuildTypeReference(e))
                .Where(e => e != null)
                .Cast<TypeReference>()
                .ToList();
        }

        private TypeReference? BuildTypeReference(Java9Parser.ExceptionTypeContext context)
        {
            if (context.classType() != null)
            {
                return BuildClassType(context.classType());
            }
            else if (context.typeVariable() != null)
            {
                return BuildTypeVariable(context.typeVariable());
            }

            return null;
        }

        private MethodDeclaration? BuildConstructor(Java9Parser.ConstructorDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.constructorModifier());
            var annotations = BuildAnnotations(context.constructorModifier());
            var declarator = context.constructorDeclarator();
            var typeParameters = BuildTypeParameters(declarator.typeParameters());
            var name = declarator.simpleTypeName().identifier().GetText();
            var parameters = BuildParameters(declarator.formalParameterList());
            var throws_ = BuildThrows(context.throws_());
            var body = context.constructorBody().blockStatements() != null
                ? BuildBlockFromStatements(context.constructorBody().blockStatements())
                : new BlockStatement(GetSourceRange(context.constructorBody()), new List<Statement>());
            var javaDoc = ExtractJavaDoc(context);

            return new MethodDeclaration(
                location, name, modifiers, annotations, null,
                typeParameters, parameters, throws_, body, javaDoc, true
            );
        }

        private InitializerBlock? BuildInstanceInitializer(Java9Parser.InstanceInitializerContext context)
        {
            var location = GetSourceRange(context);
            var body = BuildBlock(context.block());
            return body != null ? new InitializerBlock(location, body, false) : null;
        }

        private InitializerBlock? BuildStaticInitializer(Java9Parser.StaticInitializerContext context)
        {
            var location = GetSourceRange(context);
            var body = BuildBlock(context.block());
            return body != null ? new InitializerBlock(location, body, true) : null;
        }

        private List<MemberDeclaration> BuildInterfaceMembers(Java9Parser.InterfaceBodyContext context)
        {
            var members = new List<MemberDeclaration>();

            foreach (var declaration in context.interfaceMemberDeclaration())
            {
                if (declaration.constantDeclaration() != null)
                {
                    var constant = BuildConstantDeclaration(declaration.constantDeclaration());
                    if (constant != null) members.Add(constant);
                }
                else if (declaration.interfaceMethodDeclaration() != null)
                {
                    var method = BuildInterfaceMethodDeclaration(declaration.interfaceMethodDeclaration());
                    if (method != null) members.Add(method);
                }
                else if (declaration.classDeclaration() != null)
                {
                    var classDecl = Visit(declaration.classDeclaration()) as MemberDeclaration;
                    if (classDecl != null) members.Add(classDecl);
                }
                else if (declaration.interfaceDeclaration() != null)
                {
                    var interfaceDecl = Visit(declaration.interfaceDeclaration()) as MemberDeclaration;
                    if (interfaceDecl != null) members.Add(interfaceDecl);
                }
            }

            return members;
        }

        private FieldDeclaration? BuildConstantDeclaration(Java9Parser.ConstantDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.constantModifier()) | Modifiers.Public | Modifiers.Static | Modifiers.Final;
            var annotations = BuildAnnotations(context.constantModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var variables = context.variableDeclaratorList().variableDeclarator()
                .Select(v => BuildVariableDeclarator(v))
                .Where(v => v != null)
                .Cast<VariableDeclarator>()
                .ToList();

            var javaDoc = ExtractJavaDoc(context);

            return new FieldDeclaration(location, modifiers, annotations, type, variables, javaDoc);
        }

        private MethodDeclaration? BuildInterfaceMethodDeclaration(Java9Parser.InterfaceMethodDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.interfaceMethodModifier());
            var annotations = BuildAnnotations(context.interfaceMethodModifier());
            var header = context.methodHeader();
            
            var typeParameters = BuildTypeParameters(header.typeParameters());
            var returnType = BuildTypeReference(header.result());
            var declarator = header.methodDeclarator();
            var name = declarator.identifier().GetText();
            var parameters = BuildParameters(declarator.formalParameterList());
            var throws_ = BuildThrows(header.throws_());
            var body = context.methodBody().block() != null 
                ? BuildBlock(context.methodBody().block()) 
                : null;

            var javaDoc = ExtractJavaDoc(context);

            return new MethodDeclaration(
                location, name, modifiers, annotations, returnType,
                typeParameters, parameters, throws_, body, javaDoc
            );
        }

        private List<EnumConstant> BuildEnumConstants(Java9Parser.EnumConstantListContext? context)
        {
            if (context == null) return new List<EnumConstant>();

            return context.enumConstant()
                .Select(ec => BuildEnumConstant(ec))
                .Where(ec => ec != null)
                .Cast<EnumConstant>()
                .ToList();
        }

        private EnumConstant? BuildEnumConstant(Java9Parser.EnumConstantContext context)
        {
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();
            var annotations = BuildAnnotations(context.enumConstantModifier());
            var arguments = new List<Expression>();

            if (context.argumentList() != null)
            {
                arguments = context.argumentList().expression()
                    .Select(e => BuildExpression(e))
                    .Where(e => e != null)
                    .Cast<Expression>()
                    .ToList();
            }

            ClassDeclaration? body = null;
            if (context.classBody() != null)
            {
                // Build anonymous class body
                var members = BuildClassMembers(context.classBody());
                body = new ClassDeclaration(
                    GetSourceRange(context.classBody()),
                    "", // Anonymous
                    Modifiers.None,
                    new List<Annotation>(),
                    new List<TypeParameter>(),
                    null,
                    new List<TypeReference>(),
                    members,
                    null
                );
            }

            return new EnumConstant(location, name, annotations, arguments, body);
        }

        private List<MemberDeclaration> BuildEnumMembers(Java9Parser.EnumBodyDeclarationsContext? context)
        {
            if (context == null) return new List<MemberDeclaration>();

            return BuildClassMembers(context);
        }

        private List<MemberDeclaration> BuildClassMembers(Java9Parser.EnumBodyDeclarationsContext context)
        {
            var members = new List<MemberDeclaration>();

            foreach (var declaration in context.classBodyDeclaration())
            {
                if (declaration.classMemberDeclaration() != null)
                {
                    var member = BuildClassMember(declaration.classMemberDeclaration());
                    if (member != null) members.Add(member);
                }
                else if (declaration.instanceInitializer() != null)
                {
                    var initializer = BuildInstanceInitializer(declaration.instanceInitializer());
                    if (initializer != null) members.Add(initializer);
                }
                else if (declaration.staticInitializer() != null)
                {
                    var initializer = BuildStaticInitializer(declaration.staticInitializer());
                    if (initializer != null) members.Add(initializer);
                }
                else if (declaration.constructorDeclaration() != null)
                {
                    var constructor = BuildConstructor(declaration.constructorDeclaration());
                    if (constructor != null) members.Add(constructor);
                }
            }

            return members;
        }

        private List<AnnotationMember> BuildAnnotationMembers(Java9Parser.AnnotationTypeBodyContext context)
        {
            var members = new List<AnnotationMember>();

            foreach (var declaration in context.annotationTypeMemberDeclaration())
            {
                if (declaration.annotationTypeElementDeclaration() != null)
                {
                    var member = BuildAnnotationElement(declaration.annotationTypeElementDeclaration());
                    if (member != null) members.Add(member);
                }
            }

            return members;
        }

        private AnnotationMember? BuildAnnotationElement(Java9Parser.AnnotationTypeElementDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var name = context.identifier().GetText();
            Expression? defaultValue = null;

            if (context.defaultValue() != null)
            {
                defaultValue = BuildExpression(context.defaultValue().elementValue());
            }

            return new AnnotationMember(location, name, type, defaultValue);
        }

        // Expression building methods

        private Expression? BuildExpression(IParseTree? context)
        {
            if (context == null) return null;

            return context switch
            {
                Java9Parser.ExpressionContext expr => BuildExpression(expr),
                Java9Parser.LiteralContext literal => BuildLiteral(literal),
                Java9Parser.PrimaryContext primary => BuildPrimary(primary),
                Java9Parser.ElementValueContext elementValue => BuildElementValue(elementValue),
                Java9Parser.VariableInitializerContext varInit => BuildVariableInitializer(varInit),
                Java9Parser.ArrayInitializerContext arrayInit => BuildArrayInitializer(arrayInit),
                _ => null
            };
        }

        private Expression? BuildExpression(Java9Parser.ExpressionContext context)
        {
            if (context.lambdaExpression() != null)
            {
                return BuildLambdaExpression(context.lambdaExpression());
            }
            else if (context.assignmentExpression() != null)
            {
                return BuildAssignmentExpression(context.assignmentExpression());
            }

            return null;
        }

        private Expression? BuildAssignmentExpression(Java9Parser.AssignmentExpressionContext context)
        {
            if (context.conditionalExpression() != null)
            {
                return BuildConditionalExpression(context.conditionalExpression());
            }
            else if (context.assignment() != null)
            {
                return BuildAssignment(context.assignment());
            }

            return null;
        }

        private Expression? BuildAssignment(Java9Parser.AssignmentContext context)
        {
            var location = GetSourceRange(context);
            var left = BuildExpression(context.leftHandSide());
            var right = BuildExpression(context.expression());
            
            if (left == null || right == null) return null;

            var operatorText = context.assignmentOperator().GetText();
            var @operator = operatorText switch
            {
                "=" => BinaryOperator.Assign,
                "+=" => BinaryOperator.AddAssign,
                "-=" => BinaryOperator.SubtractAssign,
                "*=" => BinaryOperator.MultiplyAssign,
                "/=" => BinaryOperator.DivideAssign,
                "%=" => BinaryOperator.ModuloAssign,
                "&=" => BinaryOperator.BitwiseAndAssign,
                "|=" => BinaryOperator.BitwiseOrAssign,
                "^=" => BinaryOperator.BitwiseXorAssign,
                "<<=" => BinaryOperator.LeftShiftAssign,
                ">>=" => BinaryOperator.RightShiftAssign,
                ">>>=" => BinaryOperator.UnsignedRightShiftAssign,
                _ => BinaryOperator.Assign
            };

            return new BinaryExpression(location, left, @operator, right);
        }

        private Expression? BuildExpression(Java9Parser.LeftHandSideContext context)
        {
            if (context.expressionName() != null)
            {
                return BuildExpressionName(context.expressionName());
            }
            else if (context.fieldAccess() != null)
            {
                return BuildFieldAccess(context.fieldAccess());
            }
            else if (context.arrayAccess() != null)
            {
                return BuildArrayAccess(context.arrayAccess());
            }

            return null;
        }

        private Expression? BuildConditionalExpression(Java9Parser.ConditionalExpressionContext context)
        {
            if (context.conditionalOrExpression() != null && 
                context.expression() == null)
            {
                return BuildConditionalOrExpression(context.conditionalOrExpression());
            }
            else if (context.conditionalOrExpression() != null && 
                     context.expression() != null && 
                     context.conditionalExpression() != null)
            {
                var location = GetSourceRange(context);
                var condition = BuildConditionalOrExpression(context.conditionalOrExpression());
                var thenExpr = BuildExpression(context.expression());
                var elseExpr = BuildConditionalExpression(context.conditionalExpression());

                if (condition != null && thenExpr != null && elseExpr != null)
                {
                    return new ConditionalExpression(location, condition, thenExpr, elseExpr);
                }
            }

            return null;
        }

        private Expression? BuildConditionalOrExpression(Java9Parser.ConditionalOrExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.conditionalAndExpression() != null && context.conditionalOrExpression() == null)
            {
                // Simple case - just one conditional AND expression
                return BuildConditionalAndExpression(context.conditionalAndExpression());
            }
            else if (context.conditionalOrExpression() != null && context.conditionalAndExpression() != null)
            {
                // Binary OR expression
                var left = BuildConditionalOrExpression(context.conditionalOrExpression());
                var right = BuildConditionalAndExpression(context.conditionalAndExpression());
                
                if (left != null && right != null)
                {
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, BinaryOperator.LogicalOr, right);
                }
            }

            return null;
        }

        private Expression? BuildConditionalAndExpression(Java9Parser.ConditionalAndExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.inclusiveOrExpression() != null && context.conditionalAndExpression() == null)
            {
                // Simple case - just one inclusive OR expression
                return BuildInclusiveOrExpression(context.inclusiveOrExpression());
            }
            else if (context.conditionalAndExpression() != null && context.inclusiveOrExpression() != null)
            {
                // Binary AND expression
                var left = BuildConditionalAndExpression(context.conditionalAndExpression());
                var right = BuildInclusiveOrExpression(context.inclusiveOrExpression());
                
                if (left != null && right != null)
                {
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, BinaryOperator.LogicalAnd, right);
                }
            }

            return null;
        }

        private Expression? BuildInclusiveOrExpression(Java9Parser.InclusiveOrExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.exclusiveOrExpression() != null && context.inclusiveOrExpression() == null)
            {
                // Simple case - just one exclusive OR expression
                return BuildExclusiveOrExpression(context.exclusiveOrExpression());
            }
            else if (context.inclusiveOrExpression() != null && context.exclusiveOrExpression() != null)
            {
                // Binary OR expression
                var left = BuildInclusiveOrExpression(context.inclusiveOrExpression());
                var right = BuildExclusiveOrExpression(context.exclusiveOrExpression());
                
                if (left != null && right != null)
                {
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, BinaryOperator.BitwiseOr, right);
                }
            }

            return null;
        }

        private Expression? BuildExclusiveOrExpression(Java9Parser.ExclusiveOrExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.andExpression() != null && context.exclusiveOrExpression() == null)
            {
                // Simple case - just one AND expression
                return BuildAndExpression(context.andExpression());
            }
            else if (context.exclusiveOrExpression() != null && context.andExpression() != null)
            {
                // Binary XOR expression
                var left = BuildExclusiveOrExpression(context.exclusiveOrExpression());
                var right = BuildAndExpression(context.andExpression());
                
                if (left != null && right != null)
                {
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, BinaryOperator.BitwiseXor, right);
                }
            }

            return null;
        }

        private Expression? BuildAndExpression(Java9Parser.AndExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.equalityExpression() != null && context.andExpression() == null)
            {
                // Simple case - just one equality expression
                return BuildEqualityExpression(context.equalityExpression());
            }
            else if (context.andExpression() != null && context.equalityExpression() != null)
            {
                // Binary AND expression
                var left = BuildAndExpression(context.andExpression());
                var right = BuildEqualityExpression(context.equalityExpression());
                
                if (left != null && right != null)
                {
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, BinaryOperator.BitwiseAnd, right);
                }
            }

            return null;
        }

        private Expression? BuildEqualityExpression(Java9Parser.EqualityExpressionContext context)
        {
            // Handle left-recursive grammar
            if (context.relationalExpression() != null && context.equalityExpression() == null)
            {
                // Simple case - just one relational expression
                return BuildRelationalExpression(context.relationalExpression());
            }
            else if (context.equalityExpression() != null && context.relationalExpression() != null)
            {
                // Binary equality expression
                var left = BuildEqualityExpression(context.equalityExpression());
                var right = BuildRelationalExpression(context.relationalExpression());
                
                if (left != null && right != null)
                {
                    var @operator = context.EQUAL() != null ? BinaryOperator.Equals : BinaryOperator.NotEquals;
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, @operator, right);
                }
            }

            return null;
        }

        private Expression? BuildRelationalExpression(Java9Parser.RelationalExpressionContext context)
        {
            // Handle instanceof expression
            if (context.referenceType() != null && context.shiftExpression() != null)
            {
                var location = GetSourceRange(context);
                var expr = BuildShiftExpression(context.shiftExpression());
                var type = BuildTypeReference(context.referenceType());
                if (expr != null && type != null)
                {
                    return new InstanceOfExpression(location, expr, type);
                }
            }
            // Handle left-recursive grammar for relational expressions
            else if (context.shiftExpression() != null && context.relationalExpression() == null)
            {
                // Simple case - just one shift expression
                return BuildShiftExpression(context.shiftExpression());
            }
            else if (context.relationalExpression() != null && context.shiftExpression() != null)
            {
                // Binary relational expression
                var left = BuildRelationalExpression(context.relationalExpression());
                var right = BuildShiftExpression(context.shiftExpression());
                
                if (left != null && right != null)
                {
                    BinaryOperator @operator;
                    if (context.LT() != null) @operator = BinaryOperator.LessThan;
                    else if (context.GT() != null) @operator = BinaryOperator.GreaterThan;
                    else if (context.LE() != null) @operator = BinaryOperator.LessThanOrEqual;
                    else if (context.GE() != null) @operator = BinaryOperator.GreaterThanOrEqual;
                    else @operator = BinaryOperator.LessThan;
                    
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, @operator, right);
                }
            }

            return null;
        }

        private Expression? BuildShiftExpression(Java9Parser.ShiftExpressionContext context)
        {
            // Handle left-recursive grammar for shift expressions
            if (context.additiveExpression() != null && context.shiftExpression() == null)
            {
                // Simple case - just one additive expression
                return BuildAdditiveExpression(context.additiveExpression());
            }
            else if (context.shiftExpression() != null && context.additiveExpression() != null)
            {
                // Binary shift expression
                var left = BuildShiftExpression(context.shiftExpression());
                var right = BuildAdditiveExpression(context.additiveExpression());
                
                if (left != null && right != null)
                {
                    // Check the operator between the operands
                    BinaryOperator @operator;
                    var operatorText = "";
                    var gtCount = 0;
                    var ltCount = 0;
                    
                    // Count consecutive < or > operators
                    for (int i = 0; i < context.ChildCount; i++)
                    {
                        var child = context.GetChild(i);
                        var text = child.GetText();
                        if (text == "<")
                        {
                            ltCount++;
                            if (ltCount == 2)
                            {
                                operatorText = "<<";
                                break;
                            }
                        }
                        else if (text == ">")
                        {
                            gtCount++;
                            if (gtCount == 3)
                            {
                                operatorText = ">>>";
                                break;
                            }
                            else if (gtCount == 2 && i + 1 < context.ChildCount && context.GetChild(i + 1).GetText() != ">")
                            {
                                operatorText = ">>";
                                break;
                            }
                        }
                        else if (ltCount > 0 || gtCount > 0)
                        {
                            // Reset if we hit a non-operator
                            break;
                        }
                    }
                    
                    @operator = operatorText switch
                    {
                        "<<" => BinaryOperator.LeftShift,
                        ">>" => BinaryOperator.RightShift,
                        ">>>" => BinaryOperator.UnsignedRightShift,
                        _ => BinaryOperator.LeftShift
                    };
                    
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, @operator, right);
                }
            }

            return null;
        }

        private Expression? BuildAdditiveExpression(Java9Parser.AdditiveExpressionContext context)
        {
            // Handle left-recursive grammar for additive expressions
            if (context.multiplicativeExpression() != null && context.additiveExpression() == null)
            {
                // Simple case - just one multiplicative expression
                return BuildMultiplicativeExpression(context.multiplicativeExpression());
            }
            else if (context.additiveExpression() != null && context.multiplicativeExpression() != null)
            {
                // Binary additive expression
                var left = BuildAdditiveExpression(context.additiveExpression());
                var right = BuildMultiplicativeExpression(context.multiplicativeExpression());
                
                if (left != null && right != null)
                {
                    // Check the operator between the operands
                    var operatorText = "";
                    for (int i = 0; i < context.ChildCount; i++)
                    {
                        var child = context.GetChild(i);
                        if (child.GetText() == "+" || child.GetText() == "-")
                        {
                            operatorText = child.GetText();
                            break;
                        }
                    }
                    
                    var @operator = operatorText == "+" ? BinaryOperator.Add : BinaryOperator.Subtract;
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, @operator, right);
                }
            }

            return null;
        }

        private Expression? BuildMultiplicativeExpression(Java9Parser.MultiplicativeExpressionContext context)
        {
            // Handle left-recursive grammar for multiplicative expressions
            if (context.unaryExpression() != null && context.multiplicativeExpression() == null)
            {
                // Simple case - just one unary expression
                return BuildUnaryExpression(context.unaryExpression());
            }
            else if (context.multiplicativeExpression() != null && context.unaryExpression() != null)
            {
                // Binary multiplicative expression
                var left = BuildMultiplicativeExpression(context.multiplicativeExpression());
                var right = BuildUnaryExpression(context.unaryExpression());
                
                if (left != null && right != null)
                {
                    BinaryOperator @operator;
                    // Check the operator between the operands
                    var operatorText = "";
                    for (int i = 0; i < context.ChildCount; i++)
                    {
                        var child = context.GetChild(i);
                        if (child.GetText() == "*" || child.GetText() == "/" || child.GetText() == "%")
                        {
                            operatorText = child.GetText();
                            break;
                        }
                    }
                    
                    @operator = operatorText switch
                    {
                        "*" => BinaryOperator.Multiply,
                        "/" => BinaryOperator.Divide,
                        "%" => BinaryOperator.Modulo,
                        _ => BinaryOperator.Multiply
                    };
                    
                    var location = new SourceRange(left.Location.Start, right.Location.End);
                    return new BinaryExpression(location, left, @operator, right);
                }
            }

            return null;
        }

        private Expression? BuildUnaryExpression(Java9Parser.UnaryExpressionContext context)
        {
            if (context.preIncrementExpression() != null)
            {
                return BuildPreIncrementExpression(context.preIncrementExpression());
            }
            else if (context.preDecrementExpression() != null)
            {
                return BuildPreDecrementExpression(context.preDecrementExpression());
            }
            else if (context.unaryExpressionNotPlusMinus() != null)
            {
                return BuildUnaryExpressionNotPlusMinus(context.unaryExpressionNotPlusMinus());
            }
            else if (context.GetChild(0).GetText() == "+" && context.unaryExpression() != null)
            {
                var location = GetSourceRange(context);
                var operand = BuildUnaryExpression(context.unaryExpression());
                return operand != null 
                    ? new UnaryExpression(location, UnaryOperator.Plus, operand) 
                    : null;
            }
            else if (context.GetChild(0).GetText() == "-" && context.unaryExpression() != null)
            {
                var location = GetSourceRange(context);
                var operand = BuildUnaryExpression(context.unaryExpression());
                return operand != null 
                    ? new UnaryExpression(location, UnaryOperator.Minus, operand) 
                    : null;
            }

            return null;
        }

        private Expression? BuildPreIncrementExpression(Java9Parser.PreIncrementExpressionContext context)
        {
            var location = GetSourceRange(context);
            var operand = BuildUnaryExpression(context.unaryExpression());
            return operand != null 
                ? new UnaryExpression(location, UnaryOperator.PreIncrement, operand) 
                : null;
        }

        private Expression? BuildPreDecrementExpression(Java9Parser.PreDecrementExpressionContext context)
        {
            var location = GetSourceRange(context);
            var operand = BuildUnaryExpression(context.unaryExpression());
            return operand != null 
                ? new UnaryExpression(location, UnaryOperator.PreDecrement, operand) 
                : null;
        }

        private Expression? BuildUnaryExpressionNotPlusMinus(Java9Parser.UnaryExpressionNotPlusMinusContext context)
        {
            if (context.postfixExpression() != null)
            {
                return BuildPostfixExpression(context.postfixExpression());
            }
            else if (context.GetChild(0).GetText() == "~" && context.unaryExpression() != null)
            {
                var location = GetSourceRange(context);
                var operand = BuildUnaryExpression(context.unaryExpression());
                return operand != null 
                    ? new UnaryExpression(location, UnaryOperator.BitwiseNot, operand) 
                    : null;
            }
            else if (context.GetChild(0).GetText() == "!" && context.unaryExpression() != null)
            {
                var location = GetSourceRange(context);
                var operand = BuildUnaryExpression(context.unaryExpression());
                return operand != null 
                    ? new UnaryExpression(location, UnaryOperator.LogicalNot, operand) 
                    : null;
            }
            else if (context.castExpression() != null)
            {
                return BuildCastExpression(context.castExpression());
            }

            return null;
        }

        private Expression? BuildPostfixExpression(Java9Parser.PostfixExpressionContext context)
        {
            Expression? expr = null;

            if (context.primary() != null)
            {
                expr = BuildPrimary(context.primary());
            }
            else if (context.expressionName() != null)
            {
                expr = BuildExpressionName(context.expressionName());
            }

            if (expr == null) return null;

            // Handle postfix operators
            foreach (var child in context.children.Skip(1))
            {
                if (child.GetText() == "++")
                {
                    var location = new SourceRange(expr.Location.Start, GetSourceRange(context).End);
                    expr = new UnaryExpression(location, UnaryOperator.PostIncrement, expr, false);
                }
                else if (child.GetText() == "--")
                {
                    var location = new SourceRange(expr.Location.Start, GetSourceRange(context).End);
                    expr = new UnaryExpression(location, UnaryOperator.PostDecrement, expr, false);
                }
            }

            return expr;
        }

        private Expression? BuildCastExpression(Java9Parser.CastExpressionContext context)
        {
            var location = GetSourceRange(context);

            if (context.primitiveType() != null)
            {
                var type = BuildPrimitiveType(context.primitiveType());
                var expr = BuildUnaryExpression(context.unaryExpression());
                
                if (type != null && expr != null)
                {
                    return new CastExpression(location, type, expr);
                }
            }
            else if (context.referenceType() != null)
            {
                var type = BuildTypeReference(context.referenceType());
                var expr = context.unaryExpressionNotPlusMinus() != null
                    ? BuildUnaryExpressionNotPlusMinus(context.unaryExpressionNotPlusMinus())
                    : BuildLambdaExpression(context.lambdaExpression());
                
                if (type != null && expr != null)
                {
                    return new CastExpression(location, type, expr);
                }
            }

            return null;
        }

        private Expression? BuildPrimary(Java9Parser.PrimaryContext context)
        {
            if (context.primaryNoNewArray_lfno_primary() != null)
            {
                return BuildPrimaryNoNewArray(context.primaryNoNewArray_lfno_primary());
            }
            else if (context.arrayCreationExpression() != null)
            {
                return BuildArrayCreationExpression(context.arrayCreationExpression());
            }

            return null;
        }

        private Expression? BuildPrimaryNoNewArray(Java9Parser.PrimaryNoNewArray_lfno_primaryContext context)
        {
            if (context.literal() != null)
            {
                return BuildLiteral(context.literal());
            }
            else if (context.CLASS() != null)
            {
                // Handle class literal
                var location = GetSourceRange(context);
                TypeReference? type = null;
                
                if (context.typeName() != null)
                {
                    type = BuildTypeName(context.typeName());
                    // Handle array dimensions
                    var dimsCount = context.LBRACK()?.Length ?? 0;
                    if (dimsCount > 0 && type != null)
                    {
                        type = new ArrayType(type.Location, type, dimsCount);
                    }
                }
                else if (context.unannPrimitiveType() != null)
                {
                    var primitiveType = ParsePrimitiveTypeKind(context.unannPrimitiveType().GetText());
                    type = new PrimitiveType(GetSourceRange(context.unannPrimitiveType()), primitiveType);
                    // Handle array dimensions
                    var dimsCount = context.LBRACK()?.Length ?? 0;
                    if (dimsCount > 0)
                    {
                        type = new ArrayType(type.Location, type, dimsCount);
                    }
                }
                else if (context.VOID() != null)
                {
                    type = new PrimitiveType(GetSourceRange(context), PrimitiveTypeKind.Void);
                }
                
                return type != null ? new ClassLiteralExpression(location, type) : null;
            }
            else if (context.GetText() == "this")
            {
                return new ThisExpression(GetSourceRange(context));
            }
            else if (context.typeName() != null && context.GetChild(context.ChildCount - 1).GetText() == "this")
            {
                var qualifier = BuildExpressionName(context.typeName());
                return new ThisExpression(GetSourceRange(context), qualifier);
            }
            else if (context.expression() != null)
            {
                // Parenthesized expression
                return BuildExpression(context.expression());
            }
            else if (context.classInstanceCreationExpression_lfno_primary() != null)
            {
                return BuildClassInstanceCreationExpression(context.classInstanceCreationExpression_lfno_primary());
            }
            else if (context.fieldAccess_lfno_primary() != null)
            {
                return BuildFieldAccess(context.fieldAccess_lfno_primary());
            }
            else if (context.arrayAccess_lfno_primary() != null)
            {
                return BuildArrayAccess(context.arrayAccess_lfno_primary());
            }
            else if (context.methodInvocation_lfno_primary() != null)
            {
                return BuildMethodInvocation(context.methodInvocation_lfno_primary());
            }
            else if (context.methodReference_lfno_primary() != null)
            {
                return BuildMethodReference(context.methodReference_lfno_primary());
            }

            return null;
        }

        private Expression? BuildLiteral(Java9Parser.LiteralContext context)
        {
            var location = GetSourceRange(context);
            var text = context.GetText();

            if (context.IntegerLiteral() != null)
            {
                if (int.TryParse(text.TrimEnd('l', 'L'), out int intValue))
                {
                    return new LiteralExpression(location, intValue, LiteralKind.Integer);
                }
                else if (long.TryParse(text.TrimEnd('l', 'L'), out long longValue))
                {
                    return new LiteralExpression(location, longValue, LiteralKind.Long);
                }
            }
            else if (context.FloatingPointLiteral() != null)
            {
                if (text.EndsWith('f') || text.EndsWith('F'))
                {
                    if (float.TryParse(text.TrimEnd('f', 'F'), out float floatValue))
                    {
                        return new LiteralExpression(location, floatValue, LiteralKind.Float);
                    }
                }
                else if (double.TryParse(text.TrimEnd('d', 'D'), out double doubleValue))
                {
                    return new LiteralExpression(location, doubleValue, LiteralKind.Double);
                }
            }
            else if (context.BooleanLiteral() != null)
            {
                var boolValue = text == "true";
                return new LiteralExpression(location, boolValue, LiteralKind.Boolean);
            }
            else if (context.CharacterLiteral() != null)
            {
                var charValue = UnescapeCharacterLiteral(text);
                return new LiteralExpression(location, charValue, LiteralKind.Character);
            }
            else if (context.StringLiteral() != null)
            {
                var stringValue = UnescapeStringLiteral(text);
                return new LiteralExpression(location, stringValue, LiteralKind.String);
            }
            else if (context.NullLiteral() != null)
            {
                return new LiteralExpression(location, null, LiteralKind.Null);
            }

            return null;
        }

        private char UnescapeCharacterLiteral(string literal)
        {
            // Remove quotes
            literal = literal.Substring(1, literal.Length - 2);
            
            if (literal.Length == 1) return literal[0];
            
            // Handle escape sequences
            return literal[1] switch
            {
                'b' => '\b',
                't' => '\t',
                'n' => '\n',
                'f' => '\f',
                'r' => '\r',
                '\"' => '\"',
                '\'' => '\'',
                '\\' => '\\',
                _ => literal[1]
            };
        }

        private string UnescapeStringLiteral(string literal)
        {
            // Remove quotes
            literal = literal.Substring(1, literal.Length - 2);
            
            // Handle escape sequences
            return literal
                .Replace("\\b", "\b")
                .Replace("\\t", "\t")
                .Replace("\\n", "\n")
                .Replace("\\f", "\f")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\");
        }

        private Expression? BuildClassLiteral(Java9Parser.ClassLiteralContext context)
        {
            var location = GetSourceRange(context);
            TypeReference? type = null;

            if (context.typeName() != null)
            {
                type = BuildTypeName(context.typeName());
                
                // Handle array dimensions
                var dimsCount = context.children.Count(c => c.GetText() == "[");
                if (dimsCount > 0 && type != null)
                {
                    type = new ArrayType(location, type, dimsCount);
                }
            }
            else if (context.numericType() != null)
            {
                type = BuildNumericType(context.numericType());
                
                // Handle array dimensions
                var dimsCount = context.children.Count(c => c.GetText() == "[");
                if (dimsCount > 0 && type != null)
                {
                    type = new ArrayType(location, type, dimsCount);
                }
            }
            else if (context.GetText() == "boolean.class")
            {
                type = new PrimitiveType(location, PrimitiveTypeKind.Boolean);
            }
            else if (context.GetText() == "void.class")
            {
                type = new PrimitiveType(location, PrimitiveTypeKind.Void);
            }

            return type != null ? new ClassLiteralExpression(location, type) : null;
        }

        private PrimitiveType? BuildNumericType(Java9Parser.NumericTypeContext context)
        {
            var location = GetSourceRange(context);
            var text = context.GetText();
            
            var kind = text switch
            {
                "byte" => PrimitiveTypeKind.Byte,
                "short" => PrimitiveTypeKind.Short,
                "int" => PrimitiveTypeKind.Int,
                "long" => PrimitiveTypeKind.Long,
                "char" => PrimitiveTypeKind.Char,
                "float" => PrimitiveTypeKind.Float,
                "double" => PrimitiveTypeKind.Double,
                _ => (PrimitiveTypeKind?)null
            };

            return kind.HasValue ? new PrimitiveType(location, kind.Value) : null;
        }

        private Expression? BuildExpressionName(IParseTree context)
        {
            if (context is Java9Parser.TypeNameContext typeName)
            {
                // TypeName has a recursive structure
                var location = GetSourceRange(typeName);
                return new IdentifierExpression(location, typeName.GetText());
            }
            else if (context is Java9Parser.ExpressionNameContext exprName)
            {
                // ExpressionName has a recursive structure
                var location = GetSourceRange(exprName);
                return new IdentifierExpression(location, exprName.GetText());
            }

            return null;
        }

        private Expression? BuildClassInstanceCreationExpression(Java9Parser.ClassInstanceCreationExpression_lfno_primaryContext context)
        {
            var location = GetSourceRange(context);
            var typeArgs = BuildTypeArguments(context.typeArguments());
            var annotations = BuildAnnotations(context.annotation());
            var identifiers = context.identifier();
            
            ClassOrInterfaceType? type = null;
            foreach (var id in identifiers)
            {
                var idLocation = GetSourceRange(id);
                type = new ClassOrInterfaceType(idLocation, id.GetText(), type, 
                    id == identifiers.Last() ? typeArgs : new List<TypeArgument>(), 
                    annotations);
            }

            if (type == null) return null;

            var arguments = context.argumentList() != null
                ? context.argumentList().expression()
                    .Select(e => BuildExpression(e))
                    .Where(e => e != null)
                    .Cast<Expression>()
                    .ToList()
                : new List<Expression>();

            ClassDeclaration? anonymousBody = null;
            if (context.classBody() != null)
            {
                var members = BuildClassMembers(context.classBody());
                anonymousBody = new ClassDeclaration(
                    GetSourceRange(context.classBody()),
                    "", // Anonymous
                    Modifiers.None,
                    new List<Annotation>(),
                    new List<TypeParameter>(),
                    type,
                    new List<TypeReference>(),
                    members,
                    null
                );
            }

            return new NewExpression(location, type, arguments, anonymousBody);
        }

        private Expression? BuildFieldAccess(Java9Parser.FieldAccess_lfno_primaryContext context)
        {
            var location = GetSourceRange(context);
            Expression? target = null;

            if (context.GetChild(0).GetText() == "super")
            {
                var childContext = context.GetChild(0) as ParserRuleContext;
                if (childContext != null)
                {
                    target = new SuperExpression(GetSourceRange(childContext));
                }
            }
            else if (context.typeName() != null)
            {
                var qualifier = BuildExpressionName(context.typeName());
                target = new SuperExpression(location, qualifier);
            }

            if (target != null && context.identifier() != null)
            {
                return new FieldAccessExpression(location, target, context.identifier().GetText());
            }

            return null;
        }

        private Expression? BuildFieldAccess(Java9Parser.FieldAccessContext context)
        {
            var location = GetSourceRange(context);
            var target = BuildPrimary(context.primary());
            
            if (target != null && context.identifier() != null)
            {
                return new FieldAccessExpression(location, target, context.identifier().GetText());
            }

            return null;
        }

        private Expression? BuildArrayAccess(Java9Parser.ArrayAccess_lfno_primaryContext context)
        {
            var location = GetSourceRange(context);
            var array = BuildExpressionName(context.expressionName());
            var indices = context.expression()
                .Select(e => BuildExpression(e))
                .Where(e => e != null)
                .Cast<Expression>()
                .ToList();

            if (array == null || indices.Count == 0) return null;

            Expression result = array;
            foreach (var index in indices)
            {
                result = new ArrayAccessExpression(location, result, index);
            }

            return result;
        }

        private Expression? BuildArrayAccess(Java9Parser.ArrayAccessContext context)
        {
            var location = GetSourceRange(context);
            Expression? array = null;

            if (context.expressionName() != null)
            {
                array = BuildExpressionName(context.expressionName());
            }
            else if (context.primaryNoNewArray_lfno_arrayAccess() != null)
            {
                // Complex array access - simplified for now
                array = BuildExpression(context.primaryNoNewArray_lfno_arrayAccess());
            }

            if (array == null) return null;

            var indices = context.expression()
                .Select(e => BuildExpression(e))
                .Where(e => e != null)
                .Cast<Expression>()
                .ToList();

            Expression result = array;
            foreach (var index in indices)
            {
                result = new ArrayAccessExpression(location, result, index);
            }

            return result;
        }

        private Expression? BuildMethodInvocation(Java9Parser.MethodInvocation_lfno_primaryContext context)
        {
            var location = GetSourceRange(context);
            Expression? target = null;
            string? methodName = null;
            List<TypeArgument> typeArgs = new();

            if (context.methodName() != null)
            {
                // Simple method name
                methodName = context.methodName().identifier().GetText();
            }
            else if (context.typeName() != null)
            {
                // TypeName.method or TypeName.super.method
                target = BuildExpressionName(context.typeName());
                
                if (context.identifier() != null)
                {
                    methodName = context.identifier().GetText();
                }
                else if (context.GetChild(context.ChildCount - 3)?.GetText() == "super")
                {
                    target = new SuperExpression(location, target);
                    methodName = context.identifier().GetText();
                }
            }
            else if (context.expressionName() != null)
            {
                // ExpressionName.method or ExpressionName.super.method
                target = BuildExpressionName(context.expressionName());
                
                if (context.identifier() != null)
                {
                    methodName = context.identifier().GetText();
                }
                else if (context.GetChild(context.ChildCount - 3)?.GetText() == "super")
                {
                    target = new SuperExpression(location, target);
                    methodName = context.identifier().GetText();
                }
            }
            else if (context.GetChild(0).GetText() == "super")
            {
                // super.method
                var childContext = context.GetChild(0) as ParserRuleContext;
                if (childContext != null)
                {
                    target = new SuperExpression(GetSourceRange(childContext));
                }
                methodName = context.identifier().GetText();
            }

            if (methodName == null) return null;

            // Type arguments
            if (context.typeArguments() != null)
            {
                typeArgs = BuildTypeArguments(context.typeArguments());
            }

            // Arguments
            var arguments = context.argumentList() != null
                ? context.argumentList().expression()
                    .Select(e => BuildExpression(e))
                    .Where(e => e != null)
                    .Cast<Expression>()
                    .ToList()
                : new List<Expression>();

            return new MethodCallExpression(location, target, methodName, typeArgs, arguments);
        }

        private Expression? BuildMethodReference(Java9Parser.MethodReference_lfno_primaryContext context)
        {
            var location = GetSourceRange(context);
            Expression? target = null;
            string? methodName = null;
            List<TypeArgument> typeArgs = new();

            if (context.expressionName() != null)
            {
                target = BuildExpressionName(context.expressionName());
            }
            else if (context.referenceType() != null)
            {
                var type = BuildTypeReference(context.referenceType());
                if (type != null)
                {
                    target = new ClassLiteralExpression(location, type);
                }
            }
            else if (context.GetChild(0).GetText() == "super")
            {
                var childContext = context.GetChild(0) as ParserRuleContext;
                if (childContext != null)
                {
                    target = new SuperExpression(GetSourceRange(childContext));
                }
            }
            else if (context.typeName() != null)
            {
                var qualifier = BuildExpressionName(context.typeName());
                target = new SuperExpression(location, qualifier);
            }

            if (target == null) return null;

            // Type arguments
            if (context.typeArguments() != null)
            {
                typeArgs = BuildTypeArguments(context.typeArguments());
            }

            // Method name
            if (context.identifier() != null)
            {
                methodName = context.identifier().GetText();
            }
            else if (context.GetChild(context.ChildCount - 1).GetText() == "new")
            {
                methodName = "new";
            }

            if (methodName == null) return null;

            return new MethodReferenceExpression(location, target, methodName, typeArgs);
        }

        private Expression? BuildArrayCreationExpression(Java9Parser.ArrayCreationExpressionContext context)
        {
            var location = GetSourceRange(context);
            TypeReference? elementType = null;
            List<Expression> dimensions = new();
            ArrayInitializer? initializer = null;

            if (context.primitiveType() != null)
            {
                elementType = BuildPrimitiveType(context.primitiveType());
            }
            else if (context.classOrInterfaceType() != null)
            {
                elementType = BuildClassOrInterfaceType(context.classOrInterfaceType());
            }

            if (elementType == null) return null;

            // Dimensions with expressions
            if (context.dimExprs() != null)
            {
                foreach (var dimExpr in context.dimExprs().dimExpr())
                {
                    var expr = BuildExpression(dimExpr.expression());
                    if (expr != null) dimensions.Add(expr);
                }
            }

            // Array initializer
            if (context.arrayInitializer() != null)
            {
                initializer = BuildArrayInitializer(context.arrayInitializer());
            }

            return new NewArrayExpression(location, elementType, dimensions, initializer);
        }

        private ArrayInitializer? BuildArrayInitializer(Java9Parser.ArrayInitializerContext context)
        {
            var location = GetSourceRange(context);
            var elements = new List<Expression>();

            if (context.variableInitializerList() != null)
            {
                foreach (var init in context.variableInitializerList().variableInitializer())
                {
                    var expr = BuildVariableInitializer(init);
                    if (expr != null) elements.Add(expr);
                }
            }

            return new ArrayInitializer(location, elements);
        }

        private Expression? BuildVariableInitializer(Java9Parser.VariableInitializerContext context)
        {
            if (context.expression() != null)
            {
                return BuildExpression(context.expression());
            }
            else if (context.arrayInitializer() != null)
            {
                return BuildArrayInitializer(context.arrayInitializer());
            }

            return null;
        }

        private Expression? BuildLambdaExpression(Java9Parser.LambdaExpressionContext context)
        {
            var location = GetSourceRange(context);
            var parameters = BuildLambdaParameters(context.lambdaParameters());
            JavaNode? body = null;

            if (context.lambdaBody().expression() != null)
            {
                body = BuildExpression(context.lambdaBody().expression());
            }
            else if (context.lambdaBody().block() != null)
            {
                body = BuildBlock(context.lambdaBody().block());
            }

            return body != null ? new LambdaExpression(location, parameters, body) : null;
        }

        private List<LambdaParameter> BuildLambdaParameters(Java9Parser.LambdaParametersContext context)
        {
            var parameters = new List<LambdaParameter>();

            if (context.identifier() != null)
            {
                // Single parameter without parentheses
                var location = GetSourceRange(context.identifier());
                parameters.Add(new LambdaParameter(location, context.identifier().GetText()));
            }
            else if (context.formalParameterList() != null)
            {
                // Formal parameters with types
                var formalParams = BuildParameters(context.formalParameterList());
                foreach (var param in formalParams)
                {
                    parameters.Add(new LambdaParameter(
                        param.Location,
                        param.Name,
                        param.Type,
                        param.IsFinal
                    ));
                }
            }
            else if (context.inferredFormalParameterList() != null)
            {
                // Inferred parameters without types
                foreach (var id in context.inferredFormalParameterList().identifier())
                {
                    var location = GetSourceRange(id);
                    parameters.Add(new LambdaParameter(location, id.GetText()));
                }
            }

            return parameters;
        }

        private Expression? BuildElementValue(Java9Parser.ElementValueContext context)
        {
            if (context.conditionalExpression() != null)
            {
                return BuildConditionalExpression(context.conditionalExpression());
            }
            else if (context.elementValueArrayInitializer() != null)
            {
                return BuildElementValueArrayInitializer(context.elementValueArrayInitializer());
            }
            else if (context.annotation() != null)
            {
                // Annotation as expression - wrap it
                var annotation = BuildAnnotation(context.annotation());
                if (annotation != null)
                {
                    // Create a special expression to hold the annotation
                    return new IdentifierExpression(annotation.Location, "@" + annotation.Type.ToString());
                }
            }

            return null;
        }

        private ArrayInitializer? BuildElementValueArrayInitializer(Java9Parser.ElementValueArrayInitializerContext context)
        {
            var location = GetSourceRange(context);
            var elements = new List<Expression>();

            if (context.elementValueList() != null)
            {
                foreach (var value in context.elementValueList().elementValue())
                {
                    var expr = BuildElementValue(value);
                    if (expr != null) elements.Add(expr);
                }
            }

            return new ArrayInitializer(location, elements);
        }

        // Statement building methods

        private BlockStatement BuildBlockFromStatements(Java9Parser.BlockStatementsContext blockStatements)
        {
            var statements = new List<Statement>();
            foreach (var stmt in blockStatements.blockStatement())
            {
                var statement = BuildBlockStatement(stmt);
                if (statement != null) statements.Add(statement);
            }
            
            return new BlockStatement(GetSourceRange(blockStatements), statements);
        }

        private BlockStatement? BuildBlock(Java9Parser.BlockContext context)
        {
            var location = GetSourceRange(context);
            var statements = new List<Statement>();

            if (context.blockStatements() != null)
            {
                foreach (var stmt in context.blockStatements().blockStatement())
                {
                    var statement = BuildBlockStatement(stmt);
                    if (statement != null) statements.Add(statement);
                }
            }

            return new BlockStatement(location, statements);
        }

        private Statement? BuildBlockStatement(Java9Parser.BlockStatementContext context)
        {
            if (context.localVariableDeclarationStatement() != null)
            {
                return BuildLocalVariableDeclarationStatement(context.localVariableDeclarationStatement());
            }
            else if (context.classDeclaration() != null)
            {
                // Local class declaration - we'll skip this for now
                return null;
            }
            else if (context.statement() != null)
            {
                return BuildStatement(context.statement());
            }

            return null;
        }

        private Statement? BuildLocalVariableDeclarationStatement(Java9Parser.LocalVariableDeclarationStatementContext context)
        {
            return BuildLocalVariableDeclaration(context.localVariableDeclaration());
        }

        private LocalVariableStatement? BuildLocalVariableDeclaration(Java9Parser.LocalVariableDeclarationContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var variables = context.variableDeclaratorList().variableDeclarator()
                .Select(v => BuildVariableDeclarator(v))
                .Where(v => v != null)
                .Cast<VariableDeclarator>()
                .ToList();

            var isFinal = modifiers.HasFlag(Modifiers.Final);

            return new LocalVariableStatement(location, type, variables, isFinal);
        }

        private Statement? BuildStatement(Java9Parser.StatementContext context)
        {
            if (context.statementWithoutTrailingSubstatement() != null)
            {
                return BuildStatementWithoutTrailingSubstatement(context.statementWithoutTrailingSubstatement());
            }
            else if (context.labeledStatement() != null)
            {
                return BuildLabeledStatement(context.labeledStatement());
            }
            else if (context.ifThenStatement() != null)
            {
                return BuildIfStatement(context.ifThenStatement());
            }
            else if (context.ifThenElseStatement() != null)
            {
                return BuildIfElseStatement(context.ifThenElseStatement());
            }
            else if (context.whileStatement() != null)
            {
                return BuildWhileStatement(context.whileStatement());
            }
            else if (context.forStatement() != null)
            {
                return BuildForStatement(context.forStatement());
            }

            return null;
        }

        private Statement? BuildStatementWithoutTrailingSubstatement(Java9Parser.StatementWithoutTrailingSubstatementContext context)
        {
            if (context.block() != null)
            {
                return BuildBlock(context.block());
            }
            else if (context.emptyStatement_() != null)
            {
                return new EmptyStatement(GetSourceRange(context.emptyStatement_()));
            }
            else if (context.expressionStatement() != null)
            {
                return BuildExpressionStatement(context.expressionStatement());
            }
            else if (context.assertStatement() != null)
            {
                return BuildAssertStatement(context.assertStatement());
            }
            else if (context.switchStatement() != null)
            {
                return BuildSwitchStatement(context.switchStatement());
            }
            else if (context.doStatement() != null)
            {
                return BuildDoStatement(context.doStatement());
            }
            else if (context.breakStatement() != null)
            {
                return BuildBreakStatement(context.breakStatement());
            }
            else if (context.continueStatement() != null)
            {
                return BuildContinueStatement(context.continueStatement());
            }
            else if (context.returnStatement() != null)
            {
                return BuildReturnStatement(context.returnStatement());
            }
            else if (context.synchronizedStatement() != null)
            {
                return BuildSynchronizedStatement(context.synchronizedStatement());
            }
            else if (context.throwStatement() != null)
            {
                return BuildThrowStatement(context.throwStatement());
            }
            else if (context.tryStatement() != null)
            {
                return BuildTryStatement(context.tryStatement());
            }

            return null;
        }

        private ExpressionStatement? BuildExpressionStatement(Java9Parser.ExpressionStatementContext context)
        {
            var location = GetSourceRange(context);
            var expr = BuildExpression(context.statementExpression());
            return expr != null ? new ExpressionStatement(location, expr) : null;
        }

        private Expression? BuildExpression(Java9Parser.StatementExpressionContext context)
        {
            if (context.assignment() != null)
            {
                return BuildAssignment(context.assignment());
            }
            else if (context.preIncrementExpression() != null)
            {
                return BuildPreIncrementExpression(context.preIncrementExpression());
            }
            else if (context.preDecrementExpression() != null)
            {
                return BuildPreDecrementExpression(context.preDecrementExpression());
            }
            else if (context.postIncrementExpression() != null)
            {
                return BuildPostIncrementExpression(context.postIncrementExpression());
            }
            else if (context.postDecrementExpression() != null)
            {
                return BuildPostDecrementExpression(context.postDecrementExpression());
            }
            else if (context.methodInvocation() != null)
            {
                return BuildMethodInvocation(context.methodInvocation());
            }
            else if (context.classInstanceCreationExpression() != null)
            {
                return BuildClassInstanceCreationExpression(context.classInstanceCreationExpression());
            }

            return null;
        }

        private Expression? BuildPostIncrementExpression(Java9Parser.PostIncrementExpressionContext context)
        {
            var location = GetSourceRange(context);
            var operand = BuildPostfixExpression(context.postfixExpression());
            return operand != null 
                ? new UnaryExpression(location, UnaryOperator.PostIncrement, operand, false) 
                : null;
        }

        private Expression? BuildPostDecrementExpression(Java9Parser.PostDecrementExpressionContext context)
        {
            var location = GetSourceRange(context);
            var operand = BuildPostfixExpression(context.postfixExpression());
            return operand != null 
                ? new UnaryExpression(location, UnaryOperator.PostDecrement, operand, false) 
                : null;
        }

        private Expression? BuildMethodInvocation(Java9Parser.MethodInvocationContext context)
        {
            var location = GetSourceRange(context);
            Expression? target = null;
            string methodName;
            List<TypeArgument> typeArgs = new List<TypeArgument>();

            // Handle different method invocation patterns
            if (context.methodName() != null)
            {
                // Simple method call
                methodName = context.methodName().GetText();
            }
            else if (context.typeName() != null && context.identifier() != null)
            {
                // Qualified method call
                target = BuildExpressionName(context.typeName());
                methodName = context.identifier().GetText();
                if (context.typeArguments() != null)
                {
                    typeArgs = BuildTypeArguments(context.typeArguments());
                }
            }
            else if (context.expressionName() != null && context.identifier() != null)
            {
                // Expression.method() call
                target = BuildExpressionName(context.expressionName());
                methodName = context.identifier().GetText();
                if (context.typeArguments() != null)
                {
                    typeArgs = BuildTypeArguments(context.typeArguments());
                }
            }
            else if (context.primary() != null && context.identifier() != null)
            {
                // primary.method() call
                target = BuildPrimary(context.primary());
                methodName = context.identifier().GetText();
                if (context.typeArguments() != null)
                {
                    typeArgs = BuildTypeArguments(context.typeArguments());
                }
            }
            else if (context.SUPER() != null && context.identifier() != null)
            {
                // super.method() call
                target = new SuperExpression(GetSourceRange(context));
                methodName = context.identifier().GetText();
                if (context.typeArguments() != null)
                {
                    typeArgs = BuildTypeArguments(context.typeArguments());
                }
            }
            else
            {
                // Fallback
                methodName = "unknown";
            }

            // Arguments
            var arguments = context.argumentList() != null
                ? context.argumentList().expression()
                    .Select(e => BuildExpression(e))
                    .Where(e => e != null)
                    .Cast<Expression>()
                    .ToList()
                : new List<Expression>();

            return new MethodCallExpression(location, target, methodName, typeArgs, arguments);
        }

        private Expression? BuildClassInstanceCreationExpression(Java9Parser.ClassInstanceCreationExpressionContext context)
        {
            var location = GetSourceRange(context);
            
            // Build the type being instantiated
            TypeReference? type = null;
            var identifiers = context.identifier();
            if (identifiers != null && identifiers.Length > 0)
            {
                var typeName = string.Join(".", identifiers.Select(id => id.GetText()));
                var typeArgs = context.typeArguments() != null 
                    ? BuildTypeArguments(context.typeArguments())
                    : new List<TypeArgument>();
                type = new ClassOrInterfaceType(location, typeName, null, typeArgs, new List<Annotation>());
            }

            if (type == null) return null;

            // Build arguments
            var arguments = context.argumentList() != null
                ? context.argumentList().expression()
                    .Select(e => BuildExpression(e))
                    .Where(e => e != null)
                    .Cast<Expression>()
                    .ToList()
                : new List<Expression>();

            // Handle anonymous class body if present
            ClassDeclaration? anonymousBody = null;
            if (context.classBody() != null)
            {
                var members = BuildClassMembers(context.classBody());
                anonymousBody = new ClassDeclaration(
                    location, 
                    "$Anonymous", 
                    Modifiers.None,
                    new List<Annotation>(),
                    new List<TypeParameter>(),
                    type,
                    new List<TypeReference>(),
                    members,
                    null
                );
            }

            return new NewExpression(location, type as ClassOrInterfaceType ?? 
                new ClassOrInterfaceType(location, "Unknown", null, new List<TypeArgument>(), new List<Annotation>()), 
                arguments, anonymousBody);
        }

        private IfStatement? BuildIfStatement(Java9Parser.IfThenStatementContext context)
        {
            var location = GetSourceRange(context);
            var condition = BuildExpression(context.expression());
            var thenStmt = BuildStatement(context.statement());

            if (condition != null && thenStmt != null)
            {
                return new IfStatement(location, condition, thenStmt);
            }

            return null;
        }

        private IfStatement? BuildIfElseStatement(Java9Parser.IfThenElseStatementContext context)
        {
            var location = GetSourceRange(context);
            var condition = BuildExpression(context.expression());
            var thenStmt = BuildStatement(context.statementNoShortIf());
            var elseStmt = BuildStatement(context.statement());

            if (condition != null && thenStmt != null)
            {
                return new IfStatement(location, condition, thenStmt, elseStmt);
            }

            return null;
        }

        private Statement? BuildStatement(Java9Parser.StatementNoShortIfContext context)
        {
            if (context.statementWithoutTrailingSubstatement() != null)
            {
                return BuildStatementWithoutTrailingSubstatement(context.statementWithoutTrailingSubstatement());
            }
            else if (context.labeledStatementNoShortIf() != null)
            {
                return BuildLabeledStatementNoShortIf(context.labeledStatementNoShortIf());
            }
            else if (context.ifThenElseStatementNoShortIf() != null)
            {
                return BuildIfElseStatementNoShortIf(context.ifThenElseStatementNoShortIf());
            }
            else if (context.whileStatementNoShortIf() != null)
            {
                return BuildWhileStatementNoShortIf(context.whileStatementNoShortIf());
            }
            else if (context.forStatementNoShortIf() != null)
            {
                return BuildForStatementNoShortIf(context.forStatementNoShortIf());
            }

            return null;
        }

        private IfStatement? BuildIfElseStatementNoShortIf(Java9Parser.IfThenElseStatementNoShortIfContext context)
        {
            var location = GetSourceRange(context);
            var condition = BuildExpression(context.expression());
            var thenStmt = BuildStatement(context.statementNoShortIf()[0]);
            var elseStmt = BuildStatement(context.statementNoShortIf()[1]);

            if (condition != null && thenStmt != null)
            {
                return new IfStatement(location, condition, thenStmt, elseStmt);
            }

            return null;
        }

        private WhileStatement? BuildWhileStatement(Java9Parser.WhileStatementContext context)
        {
            var location = GetSourceRange(context);
            var condition = BuildExpression(context.expression());
            var body = BuildStatement(context.statement());

            if (condition != null && body != null)
            {
                return new WhileStatement(location, condition, body);
            }

            return null;
        }

        private WhileStatement? BuildWhileStatementNoShortIf(Java9Parser.WhileStatementNoShortIfContext context)
        {
            var location = GetSourceRange(context);
            var condition = BuildExpression(context.expression());
            var body = BuildStatement(context.statementNoShortIf());

            if (condition != null && body != null)
            {
                return new WhileStatement(location, condition, body);
            }

            return null;
        }

        private DoWhileStatement? BuildDoStatement(Java9Parser.DoStatementContext context)
        {
            var location = GetSourceRange(context);
            var body = BuildStatement(context.statement());
            var condition = BuildExpression(context.expression());

            if (body != null && condition != null)
            {
                return new DoWhileStatement(location, body, condition);
            }

            return null;
        }

        private Statement? BuildForStatement(Java9Parser.ForStatementContext context)
        {
            if (context.basicForStatement() != null)
            {
                return BuildBasicForStatement(context.basicForStatement());
            }
            else if (context.enhancedForStatement() != null)
            {
                return BuildEnhancedForStatement(context.enhancedForStatement());
            }

            return null;
        }

        private Statement? BuildForStatementNoShortIf(Java9Parser.ForStatementNoShortIfContext context)
        {
            if (context.basicForStatementNoShortIf() != null)
            {
                return BuildBasicForStatementNoShortIf(context.basicForStatementNoShortIf());
            }
            else if (context.enhancedForStatementNoShortIf() != null)
            {
                return BuildEnhancedForStatementNoShortIf(context.enhancedForStatementNoShortIf());
            }

            return null;
        }

        private ForStatement? BuildBasicForStatement(Java9Parser.BasicForStatementContext context)
        {
            var location = GetSourceRange(context);
            var initializers = new List<Statement>();
            Expression? condition = null;
            var updates = new List<Expression>();

            if (context.forInit() != null)
            {
                initializers = BuildForInit(context.forInit());
            }

            if (context.expression() != null)
            {
                condition = BuildExpression(context.expression());
            }

            if (context.forUpdate() != null)
            {
                updates = BuildForUpdate(context.forUpdate());
            }

            var body = BuildStatement(context.statement());
            if (body == null) return null;

            return new ForStatement(location, initializers, condition, updates, body);
        }

        private ForStatement? BuildBasicForStatementNoShortIf(Java9Parser.BasicForStatementNoShortIfContext context)
        {
            var location = GetSourceRange(context);
            var initializers = new List<Statement>();
            Expression? condition = null;
            var updates = new List<Expression>();

            if (context.forInit() != null)
            {
                initializers = BuildForInit(context.forInit());
            }

            if (context.expression() != null)
            {
                condition = BuildExpression(context.expression());
            }

            if (context.forUpdate() != null)
            {
                updates = BuildForUpdate(context.forUpdate());
            }

            var body = BuildStatement(context.statementNoShortIf());
            if (body == null) return null;

            return new ForStatement(location, initializers, condition, updates, body);
        }

        private List<Statement> BuildForInit(Java9Parser.ForInitContext context)
        {
            var statements = new List<Statement>();

            if (context.statementExpressionList() != null)
            {
                foreach (var expr in context.statementExpressionList().statementExpression())
                {
                    var expression = BuildExpression(expr);
                    if (expression != null)
                    {
                        statements.Add(new ExpressionStatement(expression.Location, expression));
                    }
                }
            }
            else if (context.localVariableDeclaration() != null)
            {
                var varDecl = BuildLocalVariableDeclaration(context.localVariableDeclaration());
                if (varDecl != null) statements.Add(varDecl);
            }

            return statements;
        }

        private List<Expression> BuildForUpdate(Java9Parser.ForUpdateContext context)
        {
            return context.statementExpressionList().statementExpression()
                .Select(e => BuildExpression(e))
                .Where(e => e != null)
                .Cast<Expression>()
                .ToList();
        }

        private ForEachStatement? BuildEnhancedForStatement(Java9Parser.EnhancedForStatementContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var varId = context.variableDeclaratorId();
            var varName = varId.identifier().GetText();
            
            // Handle array dimensions on variable name
            if (varId.dims() != null)
            {
                var dimensions = varId.dims().GetText().Length / 2;
                type = new ArrayType(location, type, dimensions);
            }

            var iterable = BuildExpression(context.expression());
            var body = BuildStatement(context.statement());

            if (iterable != null && body != null)
            {
                return new ForEachStatement(location, type, varName, iterable, body, modifiers.HasFlag(Modifiers.Final));
            }

            return null;
        }

        private ForEachStatement? BuildEnhancedForStatementNoShortIf(Java9Parser.EnhancedForStatementNoShortIfContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var varId = context.variableDeclaratorId();
            var varName = varId.identifier().GetText();
            
            // Handle array dimensions on variable name
            if (varId.dims() != null)
            {
                var dimensions = varId.dims().GetText().Length / 2;
                type = new ArrayType(location, type, dimensions);
            }

            var iterable = BuildExpression(context.expression());
            var body = BuildStatement(context.statementNoShortIf());

            if (iterable != null && body != null)
            {
                return new ForEachStatement(location, type, varName, iterable, body, modifiers.HasFlag(Modifiers.Final));
            }

            return null;
        }

        private LabeledStatement? BuildLabeledStatement(Java9Parser.LabeledStatementContext context)
        {
            var location = GetSourceRange(context);
            var label = context.identifier().GetText();
            var statement = BuildStatement(context.statement());

            return statement != null 
                ? new LabeledStatement(location, label, statement) 
                : null;
        }

        private LabeledStatement? BuildLabeledStatementNoShortIf(Java9Parser.LabeledStatementNoShortIfContext context)
        {
            var location = GetSourceRange(context);
            var label = context.identifier().GetText();
            var statement = BuildStatement(context.statementNoShortIf());

            return statement != null 
                ? new LabeledStatement(location, label, statement) 
                : null;
        }

        private BreakStatement BuildBreakStatement(Java9Parser.BreakStatementContext context)
        {
            var location = GetSourceRange(context);
            var label = context.identifier()?.GetText();
            return new BreakStatement(location, label);
        }

        private ContinueStatement BuildContinueStatement(Java9Parser.ContinueStatementContext context)
        {
            var location = GetSourceRange(context);
            var label = context.identifier()?.GetText();
            return new ContinueStatement(location, label);
        }

        private ReturnStatement BuildReturnStatement(Java9Parser.ReturnStatementContext context)
        {
            var location = GetSourceRange(context);
            var value = context.expression() != null 
                ? BuildExpression(context.expression()) 
                : null;
            return new ReturnStatement(location, value);
        }

        private ThrowStatement? BuildThrowStatement(Java9Parser.ThrowStatementContext context)
        {
            var location = GetSourceRange(context);
            var exception = BuildExpression(context.expression());
            return exception != null 
                ? new ThrowStatement(location, exception) 
                : null;
        }

        private SynchronizedStatement? BuildSynchronizedStatement(Java9Parser.SynchronizedStatementContext context)
        {
            var location = GetSourceRange(context);
            var lockExpr = BuildExpression(context.expression());
            var body = BuildBlock(context.block());

            if (lockExpr != null && body != null)
            {
                return new SynchronizedStatement(location, lockExpr, body);
            }

            return null;
        }

        private TryStatement? BuildTryStatement(Java9Parser.TryStatementContext context)
        {
            var location = GetSourceRange(context);

            if (context.tryWithResourcesStatement() != null)
            {
                return BuildTryWithResourcesStatement(context.tryWithResourcesStatement());
            }

            var body = BuildBlock(context.block());
            if (body == null) return null;

            var catchClauses = new List<CatchClause>();
            if (context.catches() != null)
            {
                catchClauses = BuildCatchClauses(context.catches());
            }

            BlockStatement? finallyBlock = null;
            if (context.finally_() != null)
            {
                finallyBlock = BuildBlock(context.finally_().block());
            }

            return new TryStatement(location, new List<ResourceDeclaration>(), body, catchClauses, finallyBlock);
        }

        private TryStatement? BuildTryWithResourcesStatement(Java9Parser.TryWithResourcesStatementContext context)
        {
            var location = GetSourceRange(context);
            var resources = BuildResourceList(context.resourceSpecification());
            var body = BuildBlock(context.block());
            if (body == null) return null;

            var catchClauses = new List<CatchClause>();
            if (context.catches() != null)
            {
                catchClauses = BuildCatchClauses(context.catches());
            }

            BlockStatement? finallyBlock = null;
            if (context.finally_() != null)
            {
                finallyBlock = BuildBlock(context.finally_().block());
            }

            return new TryStatement(location, resources, body, catchClauses, finallyBlock);
        }

        private List<ResourceDeclaration> BuildResourceList(Java9Parser.ResourceSpecificationContext context)
        {
            var resources = new List<ResourceDeclaration>();

            if (context.resourceList() != null)
            {
                foreach (var resource in context.resourceList().resource())
                {
                    var decl = BuildResource(resource);
                    if (decl != null) resources.Add(decl);
                }
            }

            return resources;
        }

        private ResourceDeclaration? BuildResource(Java9Parser.ResourceContext context)
        {
            var location = GetSourceRange(context);
            var modifiers = BuildModifiers(context.variableModifier());
            var type = BuildTypeReference(context.unannType());
            if (type == null) return null;

            var name = context.variableDeclaratorId().identifier().GetText();
            var initializer = BuildExpression(context.expression());
            if (initializer == null) return null;

            return new ResourceDeclaration(location, type, name, initializer, modifiers.HasFlag(Modifiers.Final));
        }

        private List<CatchClause> BuildCatchClauses(Java9Parser.CatchesContext context)
        {
            return context.catchClause()
                .Select(c => BuildCatchClause(c))
                .Where(c => c != null)
                .Cast<CatchClause>()
                .ToList();
        }

        private CatchClause? BuildCatchClause(Java9Parser.CatchClauseContext context)
        {
            var location = GetSourceRange(context);
            var catchParam = context.catchFormalParameter();
            var exceptionTypes = new List<TypeReference>();

            if (catchParam.catchType().unannClassType() != null)
            {
                var type = BuildTypeReference(catchParam.catchType().unannClassType());
                if (type != null) exceptionTypes.Add(type);
            }

            foreach (var additional in catchParam.catchType().classType())
            {
                var type = BuildClassType(additional);
                if (type != null) exceptionTypes.Add(type);
            }

            var varName = catchParam.variableDeclaratorId().identifier().GetText();
            var body = BuildBlock(context.block());

            return body != null 
                ? new CatchClause(location, exceptionTypes, varName, body) 
                : null;
        }

        private TypeReference? BuildTypeReference(Java9Parser.UnannClassTypeContext context)
        {
            // Convert unann class type to regular class type handling
            var location = GetSourceRange(context);
            var name = context.identifier().GetText();
            var typeArgs = BuildTypeArguments(context.typeArguments());

            return new ClassOrInterfaceType(location, name, null, typeArgs, new List<Annotation>());
        }

        private SwitchStatement? BuildSwitchStatement(Java9Parser.SwitchStatementContext context)
        {
            var location = GetSourceRange(context);
            var selector = BuildExpression(context.expression());
            if (selector == null) return null;

            var cases = BuildSwitchCases(context.switchBlock());

            return new SwitchStatement(location, selector, cases);
        }

        private List<SwitchCase> BuildSwitchCases(Java9Parser.SwitchBlockContext context)
        {
            var cases = new List<SwitchCase>();

            if (context.switchBlockStatementGroup() != null)
            {
                foreach (var group in context.switchBlockStatementGroup())
                {
                    var labels = new List<Expression>();
                    bool isDefault = false;

                    foreach (var label in group.switchLabels().switchLabel())
                    {
                        if (label.constantExpression() != null)
                        {
                            var expr = BuildExpression(label.constantExpression());
                            if (expr != null) labels.Add(expr);
                        }
                        else if (label.enumConstantName() != null)
                        {
                            var location = GetSourceRange(label.enumConstantName());
                            var name = label.enumConstantName().identifier().GetText();
                            labels.Add(new IdentifierExpression(location, name));
                        }
                        else if (label.GetChild(0).GetText() == "default")
                        {
                            isDefault = true;
                        }
                    }

                    var statements = new List<Statement>();
                    foreach (var stmt in group.blockStatements().blockStatement())
                    {
                        var statement = BuildBlockStatement(stmt);
                        if (statement != null) statements.Add(statement);
                    }

                    var caseLocation = GetSourceRange(group);
                    cases.Add(new SwitchCase(caseLocation, labels, statements, isDefault));
                }
            }

            return cases;
        }

        private Expression? BuildExpression(Java9Parser.ConstantExpressionContext context)
        {
            return BuildExpression(context.expression());
        }

        private AssertStatement? BuildAssertStatement(Java9Parser.AssertStatementContext context)
        {
            var location = GetSourceRange(context);
            var expressions = context.expression();
            
            if (expressions.Length == 0) return null;

            var condition = BuildExpression(expressions[0]);
            if (condition == null) return null;

            Expression? message = null;
            if (expressions.Length > 1)
            {
                message = BuildExpression(expressions[1]);
            }

            return new AssertStatement(location, condition, message);
        }

        // Helper methods

        private string GetQualifiedName(IList<ITerminalNode> identifiers)
        {
            return string.Join(".", identifiers.Select(id => id.GetText()));
        }

        private JavaDoc? ExtractJavaDoc(ParserRuleContext context)
        {
            // Look for JavaDoc comment in the hidden channel before this node
            var tokenIndex = context.Start.TokenIndex;
            if (tokenIndex > 0)
            {
                var bufferedTokens = _tokens as BufferedTokenStream;
                var hiddenTokens = bufferedTokens?.GetHiddenTokensToLeft(tokenIndex, Java9Lexer.COMMENT);
                if (hiddenTokens != null)
                {
                    foreach (var token in hiddenTokens)
                    {
                        if (token.Text.StartsWith("/**", StringComparison.Ordinal))
                        {
                            return ParseJavaDoc(token);
                        }
                    }
                }
            }

            return null;
        }

        private JavaDoc ParseJavaDoc(IToken token)
        {
            var location = new SourceLocation(
                token.Line,
                token.Column,
                token.StartIndex,
                token.Text.Length
            );
            var range = new SourceRange(location, location);

            var content = token.Text;
            var tags = new List<JavaDocTag>();

            // Simple JavaDoc parsing - can be enhanced
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim().TrimStart('*').Trim();
                if (trimmed.StartsWith('@'))
                {
                    var parts = trimmed.Split(' ', 3);
                    if (parts.Length >= 2)
                    {
                        var tagName = parts[0];
                        var parameter = parts.Length > 2 ? parts[1] : null;
                        var description = parts.Length > 2 ? parts[2] : (parts.Length > 1 ? parts[1] : "");
                        tags.Add(new JavaDocTag(tagName, parameter, description));
                    }
                }
            }

            return new JavaDoc(range, content, tags);
        }
    }
}