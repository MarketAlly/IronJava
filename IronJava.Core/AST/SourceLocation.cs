namespace IronJava.Core.AST
{
    /// <summary>
    /// Represents a location in the source code.
    /// </summary>
    public readonly struct SourceLocation
    {
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }
        public int Length { get; }

        public SourceLocation(int line, int column, int position, int length)
        {
            Line = line;
            Column = column;
            Position = position;
            Length = length;
        }

        public override string ToString() => $"Line {Line}, Column {Column}";
    }

    /// <summary>
    /// Represents a range in the source code.
    /// </summary>
    public readonly struct SourceRange
    {
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        public SourceRange(SourceLocation start, SourceLocation end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start} - {End}";
    }
}