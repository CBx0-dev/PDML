using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using PDML.Plugin.LSP.Handlers;

namespace PDML.Plugin.LSP.Tests;

public class DefinitionHandlerTests
{
    [Fact]
    public async Task Handle_PropertyTypeSignature_JumpsToTypeDefinition()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type Role = {\n    name: string;\n};\n\n@type User = {\n    role: Role;\n};";
        store.Update(uri, content);

        // Position on 'Role' in 'role: Role;' (line 5, character 11)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(5, 11)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        LocationOrLocationLink locationOrLink = result.First();
        Assert.NotNull(locationOrLink.Location);
        Assert.Equal(uri, locationOrLink.Location.Uri);
        // Target should be '@type Role' name declaration on line 0 (columns 6 to 10 in 0-based)
        Assert.Equal(0, locationOrLink.Location.Range.Start.Line);
        Assert.Equal(6, locationOrLink.Location.Range.Start.Character);
        Assert.Equal(0, locationOrLink.Location.Range.End.Line);
        Assert.Equal(10, locationOrLink.Location.Range.End.Character);
    }

    [Fact]
    public async Task Handle_InheritingTypeSignature_JumpsToTypeDefinition()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type Base = {\n    id: int32;\n};\n\n@type Sub : Base = {\n    name: string;\n};";
        store.Update(uri, content);

        // Position on 'Base' in '@type Sub : Base =' (line 4, character 14)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(4, 14)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        LocationOrLocationLink locationOrLink = result.First();
        Assert.NotNull(locationOrLink.Location);
        Assert.Equal(0, locationOrLink.Location.Range.Start.Line);
        Assert.Equal(6, locationOrLink.Location.Range.Start.Character);
    }

    [Fact]
    public async Task Handle_CompositionTargetTypeSignature_JumpsToTypeDefinition()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type Timestamps = {\n    createdAt: string;\n};\n\n@type User = Timestamps & {\n    name: string;\n};";
        store.Update(uri, content);

        // Position on 'Timestamps' in '@type User = Timestamps &' (line 4, character 15)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(4, 15)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        LocationOrLocationLink locationOrLink = result.First();
        Assert.NotNull(locationOrLink.Location);
        Assert.Equal(0, locationOrLink.Location.Range.Start.Line);
        Assert.Equal(6, locationOrLink.Location.Range.Start.Character);
    }

    [Fact]
    public async Task Handle_GenericTypeSignature_JumpsToTypeDefinition()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type Item = {\n    id: int32;\n};\n\n@type Container = {\n    items: List<Item>;\n};";
        store.Update(uri, content);

        // Position on 'Item' inside 'List<Item>' (line 5, character 17)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(5, 17)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        LocationOrLocationLink locationOrLink = result.First();
        Assert.NotNull(locationOrLink.Location);
        Assert.Equal(0, locationOrLink.Location.Range.Start.Line);
        Assert.Equal(6, locationOrLink.Location.Range.Start.Character);
    }

    [Fact]
    public async Task Handle_IntrinsicType_ReturnsEmpty()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type User = {\n    id: int32;\n};";
        store.Update(uri, content);

        // Position on 'int32' (line 1, character 10)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(1, 10)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PropertyName_ReturnsEmpty()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type User = {\n    id: int32;\n};";
        store.Update(uri, content);

        // Position on 'id' (line 1, character 5)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(1, 5)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_UnknownType_ReturnsEmpty()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/test.pdml");
        string content = "@type User = {\n    field: NonExistentType;\n};";
        store.Update(uri, content);

        // Position on 'NonExistentType' (line 1, character 15)
        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(1, 15)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_DocumentNotInStore_ReturnsEmpty()
    {
        DocumentStore store = new DocumentStore();
        DefinitionHandler handler = new DefinitionHandler(store);

        DocumentUri uri = DocumentUri.From("file:///path/to/nonexistent.pdml");

        DefinitionParams @params = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = new Position(0, 0)
        };

        LocationOrLocationLinks? result = await handler.Handle(@params, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
