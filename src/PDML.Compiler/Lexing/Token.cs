using PDML.Compiler.Common;
using System.Text;

namespace PDML.Compiler.Lexing;

public sealed record Token
{
    public required TokenType Type { get; init; }
    public required Location Location { get; init; }
    public required string Value { get; init; }
}
