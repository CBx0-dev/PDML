using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Compiler.Binding;
using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;
using System.IO;
using LspLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace PDML.Plugin.LSP.Handlers;

public class DefinitionHandler : DefinitionHandlerBase
{
    private readonly DocumentStore _documentStore;

    public DefinitionHandler(DocumentStore? documentStore = null)
    {
        _documentStore = documentStore ?? new DocumentStore();
    }

    protected override DefinitionRegistrationOptions CreateRegistrationOptions(
        DefinitionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DefinitionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("pdml")
        };
    }

    public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        string? content = _documentStore.GetText(request.TextDocument.Uri);
        if (content is null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        LspLocation? location = FindDefinition(request.TextDocument.Uri, content, request.Position);
        if (location is null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(location));
    }

    public static LspLocation? FindDefinition(DocumentUri uri, string content, Position position)
    {
        string file = uri.GetFileSystemPath();
        string fileDirectory = Path.GetDirectoryName(file) ?? ".";
        string fileName = Path.GetFileName(file);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "document.pdml";
        }

        SourceFile sourceFile = SourceFile.Create(fileDirectory, fileName, content);
        DiagnosticBag diagnostics = new DiagnosticBag();
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        List<Token> tokens = new List<Token>();
        Token token;

        do
        {
            token = lexer.NextToken();
            if (token.Type is not TokenType.Error and not TokenType.Whitespace)
            {
                tokens.Add(token);
            }
        } while (token.Type is not TokenType.Eof);

        Parser parser = new Parser(diagnostics, tokens);
        Module module = parser.ParseModule();

        DiagnosticBag bindingDiagnostics = new DiagnosticBag();
        BoundModule boundModule = Binder.BindModule(bindingDiagnostics, module);

        int targetLine = position.Line + 1;
        int targetCol = position.Character + 1;

        TypeSignature? matchedSignature = FindTypeSignatureAt(module, targetLine, targetCol);
        if (matchedSignature is null)
        {
            return null;
        }

        string typeName = matchedSignature.Name.Value;
        BoundTypeDefinition? definition = boundModule.Types.FirstOrDefault(t => t.Name == typeName);
        if (definition is null || definition.Location is null)
        {
            return null;
        }

        DocumentUri targetUri = uri;
        if (!string.IsNullOrEmpty(definition.Location.SourceFile.FilePath))
        {
            try
            {
                targetUri = DocumentUri.FromFileSystemPath(definition.Location.SourceFile.FilePath);
            }
            catch
            {
                targetUri = uri;
            }
        }

        int startLine = Math.Max(0, definition.Location.Start.Line - 1);
        int startCol = Math.Max(0, definition.Location.Start.Column - 1);
        int endLine = Math.Max(0, definition.Location.End.Line - 1);
        int endCol = Math.Max(0, definition.Location.End.Column - 1);

        return new LspLocation
        {
            Uri = targetUri,
            Range = new LspRange(new Position(startLine, startCol), new Position(endLine, endCol))
        };
    }

    private static TypeSignature? FindTypeSignatureAt(Module module, int line, int column)
    {
        foreach (TypeDefinitionMember def in module.TypeDefinitions)
        {
            if (def.InheritingType is not null)
            {
                TypeSignature? match = FindTypeSignatureAt(def.InheritingType, line, column);
                if (match is not null)
                {
                    return match;
                }
            }

            foreach (TypeDefinitionItem item in def.Items)
            {
                if (item is TypeDefinitionItemTarget target)
                {
                    TypeSignature? match = FindTypeSignatureAt(target.Type, line, column);
                    if (match is not null)
                    {
                        return match;
                    }
                }
                else if (item is TypeDefinitionItemBody body)
                {
                    foreach (TypeDefinitionBodyProperty prop in body.Properties)
                    {
                        TypeSignature? match = FindTypeSignatureAt(prop.TypeClause.TypeSignature, line, column);
                        if (match is not null)
                        {
                            return match;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static TypeSignature? FindTypeSignatureAt(TypeSignature signature, int line, int column)
    {
        if (signature.Generics is not null)
        {
            foreach (TypeSignature generic in signature.Generics)
            {
                TypeSignature? match = FindTypeSignatureAt(generic, line, column);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        if (Contains(signature.Name.Location, line, column))
        {
            return signature;
        }

        return null;
    }

    private static bool Contains(PDML.Compiler.Common.Location location, int line, int column)
    {
        if (line < location.Start.Line || line > location.End.Line)
        {
            return false;
        }

        if (line == location.Start.Line && column < location.Start.Column)
        {
            return false;
        }

        if (line == location.End.Line && column > location.End.Column)
        {
            return false;
        }

        return true;
    }
}
