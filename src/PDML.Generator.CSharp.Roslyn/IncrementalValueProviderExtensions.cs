using Microsoft.CodeAnalysis;

namespace PDML.Generator.CSharp.Roslyn;

public static class IncrementalValueProviderExtensions
{
    public static IncrementalValueProvider<TResult> CombinedSelect<T1, T2, T3, TResult>(
        this IncrementalValueProvider<T1> first,
        IncrementalValueProvider<T2> second,
        IncrementalValueProvider<T3> third,
        Func<T1, T2, T3, CancellationToken, TResult> selector
    )
    {
        return first
            .Combine(second)
            .Combine(third)
            .Select((x, token) => selector(
                x.Left.Left,
                x.Left.Right,
                x.Right,
                token
            ));
    }
}