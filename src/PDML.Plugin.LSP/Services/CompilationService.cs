using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using PDML.Compiler;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics.Abstractions;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Plugin.LSP.Diagnostics;

namespace PDML.Plugin.LSP.Services;

public sealed class CompilationService
{
    private readonly IWindowLanguageServer _logger;
    private readonly ILanguageServerFacade _server;
    private readonly DocumentStoreService _documentStoreService;
    private readonly LspDiagnosticService _diagnosticService;

    private Compilation? _current;

    public Compilation Current
    {
        get
        {
            Compilation? current = Volatile.Read(ref _current);

            if (current is null)
            {
                NextCompilation();
                current = Volatile.Read(ref _current);
            }

            if (current is null)
            {
                throw new InvalidOperationException("Compilation should not be null at this point");
            }

            return current;
        }
    }

    public CompilationService(
        IWindowLanguageServer logger,
        ILanguageServerFacade server,
        DocumentStoreService documentStoreService
    )
    {
        _logger = logger;
        _server = server;
        _documentStoreService = documentStoreService;
        _diagnosticService = new LspDiagnosticService();

        _current = null;

        _logger.LogInfo("Compilation service created");
    }

    internal void NextCompilation()
    {
        _logger.LogInfo("Compiling next compilation");
        SourceFile[] sourceFiles = _documentStoreService.ReadAll();
        Compilation compilation;
        if (sourceFiles.Length == 0)
        {
            _logger.LogInfo("Producing empty compilation, because of the leak of source files");
            compilation = Compilation.Empty(_diagnosticService.CreateDiagnosticBag());
            Volatile.Write(ref _current, compilation);
            return;
        }

        string? projectRoot = FindProjectRoot();
        if (projectRoot is null)
        {
            _logger.LogError("Project root not found");
            compilation = Compilation.Empty(_diagnosticService.CreateDiagnosticBag());
            Volatile.Write(ref _current, compilation);
            return;
        }

        compilation = CompilerService.CompileEntryPoints(
            _diagnosticService,
            projectRoot,
            sourceFiles
        );
        Volatile.Write(ref _current, compilation);
        _logger.LogInfo("Compilation completed");
    }

    internal IReadOnlyList<Token> Tokenize(SourceFile sourceFile, out DiagnosticBag diagnostics)
        => CompilerService.Tokenize(_diagnosticService, sourceFile, out diagnostics);

    internal Module? ParseModule(SourceFile sourceFile, out DiagnosticBag diagnostics)
        => CompilerService.ParseModule(_diagnosticService, sourceFile, out diagnostics);

    internal string? FindProjectRoot()
    {
        Container<WorkspaceFolder>? folders = _server.ClientSettings.WorkspaceFolders;
        if (folders is null)
        {
            return _server.ClientSettings.RootUri?.GetFileSystemPath() ?? _server.ClientSettings.RootPath;
        }

        WorkspaceFolder? workspaceFolder = folders.FirstOrDefault();
        if (workspaceFolder is null)
        {
            return _server.ClientSettings.RootUri?.GetFileSystemPath() ?? _server.ClientSettings.RootPath;
        }

        return workspaceFolder.Uri.GetFileSystemPath();
    }
}