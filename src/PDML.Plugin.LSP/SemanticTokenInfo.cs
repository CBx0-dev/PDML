using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace PDML.Plugin.LSP;

internal readonly struct SemanticTokenInfo : IEquatable<SemanticTokenInfo>
{
    public int Line { get; }
    public int Column { get; }
    public int Length { get; }
    public SemanticTokenType TokenType { get; }
    public SemanticTokenModifier[] Modifiers { get; }

    public SemanticTokenInfo(int line, int column, int length, SemanticTokenType tokenType, SemanticTokenModifier[] modifiers)
    {
        Line = line;
        Column = column;
        Length = length;
        TokenType = tokenType;
        Modifiers = modifiers;
    }

    public bool Equals(SemanticTokenInfo other)
    {
        return Line == other.Line &&
               Column == other.Column &&
               Length == other.Length &&
               TokenType == other.TokenType &&
               Modifiers.SequenceEqual(other.Modifiers);
    }

    public override bool Equals(object? obj) => obj is SemanticTokenInfo other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(Line);
        hash.Add(Column);
        hash.Add(Length);
        hash.Add(TokenType);
        foreach (SemanticTokenModifier mod in Modifiers)
        {
            hash.Add(mod);
        }
        return hash.ToHashCode();
    }

    public override string ToString() =>
        $"SemanticTokenInfo(Line={Line}, Column={Column}, Length={Length}, Type={TokenType}, Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}])";
}
