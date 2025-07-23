using BenchmarkDotNet.Attributes;
using MarketAlly.IronJava.Core;

namespace MarketAlly.IronJava.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    public class ParsingBenchmarks
    {
            private string _simpleClass = null!;
        private string _complexClass = null!;
        private string _largeFile = null!;

        [GlobalSetup]
        public void Setup()
        {

            _simpleClass = @"
public class Simple {
    private int value;
    
    public int getValue() {
        return value;
    }
    
    public void setValue(int value) {
        this.value = value;
    }
}";

            _complexClass = @"
package com.example.app;

import java.util.*;
import java.util.stream.*;
import java.util.concurrent.*;

@Service
@Transactional
public class ComplexService<T extends Entity> implements ServiceInterface<T> {
    private static final Logger LOG = LoggerFactory.getLogger(ComplexService.class);
    private final Repository<T> repository;
    private final EventPublisher eventPublisher;
    
    @Autowired
    public ComplexService(Repository<T> repository, EventPublisher eventPublisher) {
        this.repository = repository;
        this.eventPublisher = eventPublisher;
    }
    
    @Override
    @Cacheable(value = ""entities"", key = ""#id"")
    public Optional<T> findById(Long id) {
        LOG.debug(""Finding entity by id: {}"", id);
        return repository.findById(id)
            .map(entity -> {
                eventPublisher.publish(new EntityAccessedEvent(entity));
                return entity;
            });
    }
    
    @Override
    @Transactional(isolation = Isolation.READ_COMMITTED)
    public CompletableFuture<List<T>> findAllAsync() {
        return CompletableFuture.supplyAsync(() -> {
            try {
                return repository.findAll().stream()
                    .filter(Objects::nonNull)
                    .sorted(Comparator.comparing(Entity::getCreatedAt))
                    .collect(Collectors.toList());
            } catch (Exception e) {
                LOG.error(""Error finding entities"", e);
                throw new ServiceException(""Failed to load entities"", e);
            }
        });
    }
    
    public record EntityAccessedEvent(Entity entity) implements DomainEvent {
        @Override
        public Instant occurredOn() {
            return Instant.now();
        }
    }
}";

            // Generate a large file
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("package com.example.generated;");
            sb.AppendLine();
            sb.AppendLine("public class LargeGeneratedClass {");
            
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"    private String field{i};");
                sb.AppendLine($"    ");
                sb.AppendLine($"    public String getField{i}() {{");
                sb.AppendLine($"        return field{i};");
                sb.AppendLine($"    }}");
                sb.AppendLine($"    ");
                sb.AppendLine($"    public void setField{i}(String field{i}) {{");
                sb.AppendLine($"        this.field{i} = field{i};");
                sb.AppendLine($"    }}");
                sb.AppendLine();
            }
            
            sb.AppendLine("}");
            _largeFile = sb.ToString();
        }

        [Benchmark]
        public void ParseSimpleClass()
        {
            var result = JavaParser.Parse(_simpleClass);
            if (!result.Success)
                throw new Exception("Parse failed");
        }

        [Benchmark]
        public void ParseComplexClass()
        {
            var result = JavaParser.Parse(_complexClass);
            if (!result.Success)
                throw new Exception("Parse failed");
        }

        [Benchmark]
        public void ParseLargeFile()
        {
            var result = JavaParser.Parse(_largeFile);
            if (!result.Success)
                throw new Exception("Parse failed");
        }
    }
}