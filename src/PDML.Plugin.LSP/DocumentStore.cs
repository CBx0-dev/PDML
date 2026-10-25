using System.Collections.Concurrent;
using System.IO;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace PDML.Plugin.LSP;

public class DocumentStore
{
    private readonly ConcurrentDictionary<DocumentUri, string> _documents = new();

    public void Update(DocumentUri uri, string text)
    {
        _documents[uri] = text;
    }

    public void Remove(DocumentUri uri)
    {
        _documents.TryRemove(uri, out _);
    }

    public string? GetText(DocumentUri uri)
    {
        if (_documents.TryGetValue(uri, out string? text))
        {
            return text;
        }

        try
        {
            string path = uri.GetFileSystemPath();
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }
        catch
        {
            // Ignore file read exceptions
        }

        return null;
    }
}
