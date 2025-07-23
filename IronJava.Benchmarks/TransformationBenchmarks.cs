using BenchmarkDotNet.Attributes;
using MarketAlly.IronJava.Core;
using MarketAlly.IronJava.Core.AST;
using MarketAlly.IronJava.Core.AST.Nodes;
using MarketAlly.IronJava.Core.AST.Transformation;
using MarketAlly.IronJava.Core.Serialization;

namespace MarketAlly.IronJava.Benchmarks
{
    [MemoryDiagnoser]
    public class TransformationBenchmarks
    {
        private CompilationUnit _ast = null!;
        private IdentifierRenamer _renamer = null!;
        private ModifierTransformer _modifierTransformer = null!;
        private TransformationBuilder _complexTransformer = null!;
        private AstJsonSerializer _serializer = null!;

        [GlobalSetup]
        public void Setup()
        {
            var javaCode = @"
package com.example;

public class Service {
    private Repository repository;
    private Logger logger;
    
    public Service(Repository repository) {
        this.repository = repository;
        this.logger = LoggerFactory.getLogger(Service.class);
    }
    
    public Entity findById(Long id) {
        logger.debug(""Finding entity: "" + id);
        return repository.findById(id);
    }
    
    public List<Entity> findAll() {
        logger.debug(""Finding all entities"");
        return repository.findAll();
    }
    
    private void validateEntity(Entity entity) {
        if (entity == null) {
            throw new IllegalArgumentException(""Entity cannot be null"");
        }
    }
}";

            var result = JavaParser.Parse(javaCode);
            if (!result.Success)
                throw new Exception("Failed to parse test code");
                
            _ast = result.Ast!;
            
            _renamer = new IdentifierRenamer("repository", "repo");
            _modifierTransformer = ModifierTransformer.AddModifier(Modifiers.Final);
            _complexTransformer = new TransformationBuilder()
                .AddModifier(Modifiers.Final)
                .RenameIdentifier("repository", "repo")
                .RenameIdentifier("logger", "log")
                ;
            
            _serializer = new AstJsonSerializer(indented: false);
        }

        [Benchmark]
        public void SimpleRename()
        {
            var transformed = _ast.Accept(_renamer);
        }

        [Benchmark]
        public void AddModifier()
        {
            var transformed = _ast.Accept(_modifierTransformer);
        }

        [Benchmark]
        public void ComplexTransformation()
        {
            var transformed = _complexTransformer.Transform(_ast);
        }

        [Benchmark]
        public void SerializeToJson()
        {
            var json = _serializer.Serialize(_ast);
        }

        [Benchmark]
        public void TransformAndSerialize()
        {
            var transformed = _complexTransformer.Transform(_ast);
            var json = _serializer.Serialize(transformed);
        }
    }
}