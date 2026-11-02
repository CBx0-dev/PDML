using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using PDML.Compiler.Common;

namespace PDML.Plugin.LSP.Services;

public sealed class DocumentStoreService
{
    private readonly IWindowLanguageServer _logger;
    private readonly ConcurrentDictionary<DocumentUri, SourceFile> _files;

    public DocumentStoreService(IWindowLanguageServer logger)
    {
        _logger = logger;
        _files = new ConcurrentDictionary<DocumentUri, SourceFile>();

        _logger.LogInfo("DocumentStoreService created");
    }

    public void Update(DocumentUri document, string content)
    {
        if (!document.Path.EndsWith(".pdml"))
        {
            return;
        }

        SourceFile existing = _files[document];
        _files[document] = SourceFile.Create(existing.Directory, existing.FileName, content);
    }

    public void Update(DocumentUri document)
    {
        if (!document.Path.EndsWith(".pdml"))
        {
            return;
        }

        _files[document] = SourceFile.Read(document.GetFileSystemPath());
    }

    public void Remove(DocumentUri document) => _files.TryRemove(document, out _);

    public SourceFile? Read(DocumentUri document)
    {
        if (!document.Path.EndsWith(".pdml"))
        {
            return null;
        }

        if (_files.TryGetValue(document, out SourceFile? existingSource))
        {
            return existingSource;
        }

        try
        {
            string path = document.GetFileSystemPath();
            if (File.Exists(path))
            {
                SourceFile source = SourceFile.Read(path);
                _files[document] = source;
                return source;
            }
        }
        catch (IOException)
        {
            // Ignore file read exceptions
        }

        return null;
    }

    public SourceFile[] ReadAll() => _files.Values.ToArray();
}