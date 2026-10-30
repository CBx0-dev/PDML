using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Diagnostics;

namespace PDML.Compiler;

public sealed class Compilation
{
    private readonly IReadOnlyList<BoundModule>? _modules;

    public DiagnosticBag Diagnostics { get; }

    public IReadOnlyList<BoundModule> Modules { 
        get
        {
            if (_modules is null)
            {
                throw new InvalidOperationException("Compilation was not successful");
            }

            return _modules;
        }
    }

    public bool HasErrors => _modules is null || Diagnostics.HasErrors;

    internal Compilation(DiagnosticBag diagnostics, IReadOnlyList<BoundModule>? modules)
    {
        _modules = modules;
        Diagnostics = diagnostics;
    }
}