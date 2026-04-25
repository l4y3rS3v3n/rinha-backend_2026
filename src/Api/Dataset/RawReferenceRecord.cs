using System.Text.Json.Serialization;

namespace Rinha.Fraud.Dataset;

internal sealed class RawReferenceRecord
{
    [JsonPropertyName("vector")]
    public required double[] Vector { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }
}
