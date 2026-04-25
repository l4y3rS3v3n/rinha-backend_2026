using System.Collections.Frozen;

namespace Rinha.Fraud.Vectorization;

internal sealed class MccRiskTable
{
    private readonly FrozenDictionary<string, float> _table;

    public MccRiskTable(IReadOnlyDictionary<string, float> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _table = source.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public float Get(string mcc) =>
        _table.TryGetValue(mcc, out var risk) ? risk : (float)NormalizationConstants.DefaultMccRisk;
}
