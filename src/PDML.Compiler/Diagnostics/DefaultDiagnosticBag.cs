using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics.Abstractions;

namespace PDML.Compiler.Diagnostics;

public sealed class DefaultDiagnosticBag : DiagnosticBag<Diagnostic>
{
    public override bool HasErrors => _diagnostics.Any(diagnostic => diagnostic.Descriptor.Severity == DiagnosticSeverity.Error);

    public override bool HasWarnings => _diagnostics.Any(diagnostic => diagnostic.Descriptor.Severity == DiagnosticSeverity.Warning);

    public override bool HasInfos => _diagnostics.Any(diagnostic => diagnostic.Descriptor.Severity == DiagnosticSeverity.Info);

    public override void Report(DiagnosticDescriptor descriptor, Location location, Location[] additionalLocations, params object[] args)
    {
        if (args.Length != descriptor.ArgumentCount)
        {
            throw new ArgumentException($"Expected {descriptor.ArgumentCount} arguments, but got {args.Length}.", nameof(args));
        }

        _diagnostics.Add(new Diagnostic
        {
            Location = location,
            AdditionalLocations = additionalLocations,
            Message = string.Format(descriptor.MessageFormat, args),
            Descriptor = descriptor
        });
    }
}