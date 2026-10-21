using PDML.Compiler.Common;
using PDML.Compiler.Lexing;

namespace PDML.Compiler.Parsing.Nodes;

public record TypeSignature : Node
{
    public override required Location Location { get; init; }

    public required Token Name { get; init; }
    public required Token? LAngle { get; init; }
    public required DelimitedList<TypeSignature, Token>? Generics { get; init; }
    public required Token? RAngle { get; init; }
}