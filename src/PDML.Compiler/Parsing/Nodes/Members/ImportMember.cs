using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes.Members;

public record ImportMember : Member
{
    public required override Location Location { get; init; }

    public required Token ImportKeyword { get; init; }
    public required Token LBrace { get; init; }
    public required DelimitedList<Token> Symbols { get; init; }
    public required Token RBrace { get; init; }
    public required Token FromKeyword { get; init; }
    public required Token Locator { get; init; }
    public required Token Semicolon { get; init; }
}