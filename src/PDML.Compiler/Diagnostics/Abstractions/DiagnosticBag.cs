using System.Collections;
using PDML.Compiler.Common;

namespace PDML.Compiler.Diagnostics.Abstractions;

public abstract class DiagnosticBag
{
    public abstract bool HasItems { get; }
    public abstract bool HasErrors { get; }
    public abstract bool HasWarnings { get; }
    public abstract bool HasInfos { get; }

    public abstract void Consume(DiagnosticBag other);

    public abstract void Report(DiagnosticDescriptor descriptor, Location location, params object[] args);

    public abstract void Report(DiagnosticDescriptor descriptor, Location location, Location[] additionalLocations, params object[] args);

    public abstract void Report(DiagnosticDescriptor descriptor, Location location, Location? additonalLocation, params object[] args);
}

public abstract class DiagnosticBag<TDiagnostic> : DiagnosticBag, IEnumerable<TDiagnostic>
{
    protected readonly List<TDiagnostic> _diagnostics;

    public override bool HasItems => _diagnostics.Count > 0;

    public DiagnosticBag()
    {
        _diagnostics = new List<TDiagnostic>();
    }

    public sealed override void Consume(DiagnosticBag other)
    {
        if (other is not DiagnosticBag<TDiagnostic> bag)
        {
            throw new ArgumentException("DiagnosticBag's are not compatible with each other.", nameof(other));
        }

        _diagnostics.AddRange(bag._diagnostics);
    }

    public override sealed void Report(DiagnosticDescriptor descriptor, Location location, params object[] args)
    {
        if (args.Length != descriptor.ArgumentCount)
        {
            throw new ArgumentException($"Expected {descriptor.ArgumentCount} arguments, but got {args.Length}.", nameof(args));
        }

        Report(descriptor, location, [], args);
    }

    public override sealed void Report(DiagnosticDescriptor descriptor, Location location, Location? additonalLocation, params object[] args)
    {
        Location[] additonalLocations = additonalLocation is null
            ? []
            : [additonalLocation];

        Report(descriptor, location, additonalLocations, args);
    }

    public IEnumerator<TDiagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}