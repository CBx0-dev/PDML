using System.Collections;
using PDML.Compiler.Common;

namespace PDML.Compiler.Diagnostics;

public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics;

    public bool HasItems => _diagnostics.Count > 0;

    public bool HasErrors => _diagnostics.Any(d => d.Descriptor.Severity == DiagnosticSeverity.Error);
    public bool HasWarnings => _diagnostics.Any(d => d.Descriptor.Severity == DiagnosticSeverity.Warning);
    public bool HasInfos => _diagnostics.Any(d => d.Descriptor.Severity == DiagnosticSeverity.Info);


    public DiagnosticBag()
    {
        _diagnostics = new List<Diagnostic>();
    }

    public void Report(DiagnosticDescriptor descriptor, Location location, params object[] args)
    {
        if (args.Length != descriptor.ArgumentCount)
        {
            throw new ArgumentException($"Expected {descriptor.ArgumentCount} arguments, but got {args.Length}.", nameof(args));
        }

        _diagnostics.Add(new Diagnostic
        {
            Location = location,
            Message = string.Format(descriptor.MessageFormat, args),
            Descriptor = descriptor
        });
    }

    public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}