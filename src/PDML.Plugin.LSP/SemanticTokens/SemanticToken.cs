using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Location = PDML.Compiler.Common.Location;

namespace PDML.Plugin.LSP.SemanticTokens;

public sealed record SemanticToken
{
    public Location Location { get; }
    public SemanticTokenType Type { get; }
    public SemanticTokenModifier[] Modifiers { get; }

    public SemanticToken(Location location, SemanticTokenType type)
    {
        Location = location;
        Type = type;
        Modifiers = [];
    }

    public SemanticToken(Location location, SemanticTokenType type, params SemanticTokenModifier[] modifiers)
    {
        Location = location;
        Type = type;
        Modifiers = modifiers;
    }
}