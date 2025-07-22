using Antlr4.Runtime;
using IronJava.Core.AST.Builders;
using IronJava.Core.AST.Nodes;
using IronJava.Core.Grammar;

namespace IronJava.Core
{
    public class JavaParser
    {
        public static ParseResult Parse(string sourceCode)
        {
            var inputStream = new AntlrInputStream(sourceCode);
            var lexer = new Java9Lexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new Java9Parser(tokens);
            
            // Collect errors
            var errorListener = new ErrorCollector();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);
            
            // Parse
            var parseTree = parser.compilationUnit();
            
            // Build AST
            var astBuilder = new AstBuilder(tokens);
            var ast = astBuilder.Visit(parseTree) as CompilationUnit;
            
            return new ParseResult(ast, errorListener.Errors);
        }
        
        public static Java9Parser.CompilationUnitContext ParseToAntlrTree(string sourceCode)
        {
            var inputStream = new AntlrInputStream(sourceCode);
            var lexer = new Java9Lexer(inputStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new Java9Parser(tokens);
            
            return parser.compilationUnit();
        }
    }
    
    public class ParseResult
    {
        public CompilationUnit? Ast { get; }
        public IReadOnlyList<ParseError> Errors { get; }
        public bool Success => Ast != null && Errors.Count == 0;
        
        public ParseResult(CompilationUnit? ast, IReadOnlyList<ParseError> errors)
        {
            Ast = ast;
            Errors = errors;
        }
    }
    
    public class ParseError
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public ParseErrorSeverity Severity { get; }
        
        public ParseError(string message, int line, int column, ParseErrorSeverity severity)
        {
            Message = message;
            Line = line;
            Column = column;
            Severity = severity;
        }
    }
    
    public enum ParseErrorSeverity
    {
        Warning,
        Error
    }
    
    internal class ErrorCollector : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
    {
        private readonly List<ParseError> _errors = new();
        
        public IReadOnlyList<ParseError> Errors => _errors;
        
        public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            _errors.Add(new ParseError(msg, line, charPositionInLine, ParseErrorSeverity.Error));
        }
        
        public void SyntaxError(System.IO.TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            _errors.Add(new ParseError(msg, line, charPositionInLine, ParseErrorSeverity.Error));
        }
    }
}